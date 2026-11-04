import { test } from 'node:test';
import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import { mkdirSync, readdirSync, copyFileSync, writeFileSync, readFileSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { spawnSync } from 'node:child_process';

// Solo binarios públicos: nunca copiar .local, .env ni configuración del worktree.
const source = resolve('tests/MySqlPersistence/bin/Debug/net10.0');
const fixtures = resolve('tests/MySqlInspection/fixtures');
function escenario(configuracion) {
  const raiz = join(fixtures, `cli-${randomUUID()}`);
  const bin = join(raiz, 'tests/MySqlPersistence/bin/Debug/net10.0');
  mkdirSync(bin, { recursive: true });
  for (const nombre of readdirSync(source)) {
    if (nombre.endsWith('.dll') || nombre.endsWith('.deps.json') || nombre.endsWith('.runtimeconfig.json'))
      copyFileSync(join(source, nombre), join(bin, nombre));
  }
  const ruta = join(raiz, '.local/mysql-tests.json');
  if (configuracion !== undefined) {
    mkdirSync(join(raiz, '.local'));
    writeFileSync(ruta, configuracion);
  }
  return (args) => {
    const r = spawnSync('dotnet', [join(bin, 'MySqlPersistence.dll'), ...args], {
      cwd: raiz, encoding: process.platform === 'win32' ? 'latin1' : 'utf8', timeout: 5000, windowsHide: true,
    });
    assert.ifError(r.error);
    assert.equal(r.signal, null);
    if (configuracion !== undefined) assert.equal(readFileSync(ruta, 'utf8'), configuracion, 'fixture intacta');
    assert.doesNotMatch(r.stdout + r.stderr, /SECRETO|Password|ConnectionString|MySqlException/);
    return r;
  };
}
const modos = ['--mysql', '--mysql-boundary', '--inspect'];
const rechazo = 'Configuración rechazada: requiere loopback, puerto 3306, base berrutiauthtests, usuario berruti_test y contraseña no vacía. No se abrió conexión.\n';
// La consola .NET local usa OEM850; Node recibió sus bytes como latin1.
// Solo normalizar los tres glifos presentes en estos mensajes, no su contenido.
const normalizar = s => s.replaceAll('\r\n', '\n').replace(/[¢¤¡]/g, c => ({ '¢': 'ó', '¤': 'ñ', '¡': 'í' })[c]);
test('help público incluye inspección sin acreditar invariantes', () => {
  const r = escenario()(['--help']);
  assert.equal(r.status, 0);
  assert.equal(r.stderr, '');
  assert.match(r.stdout, /--inspect/);
  assert.match(r.stdout, /solo lectura/);
  assert.match(r.stdout, /no acredita/);
});
test('argumentos inválidos no leen configuración ni ejecutan SQL arbitrario', () => {
  const ejecutar = escenario('JSON SECRETO deliberadamente inválido');
  for (const args of [[], ['--inspect', '--mysql'], ['--inspect', 'SELECT 1'], ['--desconocido']]) {
    const r = ejecutar(args);
    assert.equal(r.status, 1);
    assert.equal(r.stdout, '');
    assert.match(r.stderr, /^Uso:.*--inspect/);
  }
});
test('tres modos rechazan configuración ausente en raíz sintética', () => {
  const ejecutar = escenario();
  for (const modo of modos) {
    const r = ejecutar([modo]);
    assert.equal(r.status, 1);
    assert.equal(r.stdout, '');
    assert.match(r.stderr, /^Falta \.local\/mysql-tests\.json/);
    assert.doesNotMatch(r.stderr, /Prueba MySQL fallida/);
  }
});
test('mismos guards privados antes de conexión para los tres modos', () => {
  const base = { Server: 'localhost', Port: 3306, Database: 'berrutiauthtests', User: 'berruti_test', Password: 'SECRETO-sintético' };
  for (const cambio of [{ User: 'root' }, { Database: 'berrutilocal' }, { Server: 'remoto' },
    { Port: 3307 }, { Password: '' }, { Password: '  ' }, { User: 'otro' }, { Server: 'localhost;otra=1' }]) {
    const ejecutar = escenario(JSON.stringify({ ...base, ...cambio }));
    for (const modo of modos) {
      const r = ejecutar([modo]);
      assert.equal(r.status, 1);
      assert.equal(r.stdout, '');
      assert.equal(normalizar(r.stderr), rechazo);
    }
  }
});
test('JSON inválido y claves desconocidas conservan rechazo seguro', () => {
  for (const contenido of ['{"Password":"SECRETO",', '{"Desconocida":"SECRETO"}', '{"port":3306}']) {
    const ejecutar = escenario(contenido);
    for (const modo of modos) {
      const r = ejecutar([modo]);
      assert.equal(r.status, 1);
      assert.equal(r.stdout, '');
      assert.equal(normalizar(r.stderr), 'JSON privado mal formado o claves desconocidas. No se abrió conexión.\n');
    }
  }
});
test('--unit conserva 17 PASS e ignora la configuración sintética inválida', () => {
  const r = escenario('JSON SECRETO deliberadamente inválido')(['--unit']);
  assert.equal(r.status, 0);
  assert.equal(r.stderr, '');
  assert.equal((r.stdout.match(/^PASS:/gm) ?? []).length, 17);
  assert.match(r.stdout, /Unit no abre MySQL ni demuestra persistencia\/atomicidad\./);
});
