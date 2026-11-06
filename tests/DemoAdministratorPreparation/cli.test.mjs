import { test } from 'node:test';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
const dll = 'tests/DemoAdministratorPreparation/bin/Debug/net10.0/DemoAdministratorPreparation.dll';
const uso = 'Uso: DemoAdministratorPreparation --unit | --help\n';
function ejecutar(args) {
  const r = spawnSync('dotnet', [dll, ...args], { encoding: 'utf8', timeout: 5000, windowsHide: true });
  assert.ifError(r.error);
  assert.equal(r.signal, null);
  r.stdout = r.stdout.replaceAll('\r\n', '\n');
  r.stderr = r.stderr.replaceAll('\r\n', '\n');
  assert.doesNotMatch(r.stdout + r.stderr, /MARCADOR|Password|Contrasena|token|EXCEPCION/);
  return r;
}
test('help exacto', () => {
  const r = ejecutar(['--help']);
  assert.deepEqual([r.status,r.stdout,r.stderr],[0,uso,'']);
});
test('rechaza seis entradas antes de pruebas', () => {
  for (const args of [[],['--execute'],['--seed'],['--unit','SELECT 1'],['--help','--unit'],['--desconocido']]) {
    const r = ejecutar(args);
    assert.deepEqual([r.status,r.stdout,r.stderr],[1,'',uso]);
  }
});
test('unit exacto sin secretos', () => {
  const r = ejecutar(['--unit']);
  assert.deepEqual([r.status,r.stdout,r.stderr],[0,'Sintetico: 55/55 PASS\n','']);
});
