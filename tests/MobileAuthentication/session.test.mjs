import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { stripTypeScriptTypes } from 'node:module';

const source = await readFile(new URL('../../src/Mobile/src/services/Authentication/AuthenticationService.ts', import.meta.url), 'utf8');
const { AuthenticationService } = await import(`data:text/javascript;base64,${Buffer.from(stripTypeScriptTypes(source)).toString('base64')}`);
const request = { correo: 'ficticio@example.invalid', contrasena: 'x' };
const now = Date.parse('2026-01-01T00:00:00Z');
const token = 'A'.repeat(43);
const identity = { identificadorUsuario: 'B'.repeat(43), roles: ['pasajero'] };
const success = { ...identity, token, venceEnUtc: '2026-01-08T00:00:00.000000Z' };
const reply = (status, body) => new Response(body === undefined ? null : JSON.stringify(body), { status });
function fixture(responses, url = 'https://api.example.invalid', timeout = 100) {
  const calls = [];
  const service = new AuthenticationService(url, async (...args) => {
    calls.push(args);
    const next = responses.shift();
    if (typeof next === 'function') return next(...args);
    if (next instanceof Error) throw next;
    return next;
  }, () => now, timeout);
  return { service, calls };
}

test('public interface: exact requests, confirmed session, logout and instance isolation', async () => {
  const { service, calls } = fixture([reply(200, success), reply(200, identity), reply(204)]);
  assert.deepEqual(service.getState(), { authenticated: false, logoutPending: false });
  const login = await service.login(request);
  assert.equal(login.outcome, 'success');
  assert.equal(login.status, 200);
  assert.equal(login.message, 'Sesión confirmada.');
  assert.deepEqual(service.getState(), { authenticated: true, logoutPending: false });
  assert.equal(JSON.stringify(service.getState()).includes(token), false);
  assert.equal((await service.checkPassenger()).outcome, 'success');
  assert.equal((await service.logout()).message, 'Cierre de sesión confirmado.');
  assert.equal((await service.checkPassenger()).outcome, 'unauthenticated');
  assert.equal(calls.length, 3);
  assert.equal(calls[0][0], 'https://api.example.invalid/api/autenticacion/ingreso');
  assert.equal(calls[0][1].method, 'POST');
  assert.deepEqual(JSON.parse(calls[0][1].body), request);
  assert.deepEqual(calls[0][1].headers, { 'Content-Type': 'application/json' });
  assert.equal(calls[1][0], 'https://api.example.invalid/api/autenticacion/prueba-pasajero');
  assert.equal(calls[1][1].method, 'GET');
  assert.deepEqual(calls[1][1].headers, { Authorization: `Bearer ${token}` });
  assert.equal(calls[2][0], 'https://api.example.invalid/api/autenticacion/salida');
  assert.equal(calls[2][1].method, 'POST');
  assert.deepEqual(calls[2][1].headers, { Authorization: `Bearer ${token}` });
  for (const [, options] of calls) {
    assert.equal(options.credentials, 'omit');
    assert.equal(options.redirect, 'error');
    assert.ok(options.signal instanceof AbortSignal);
  }
  const isolated = fixture([]);
  assert.equal((await isolated.service.checkPassenger()).outcome, 'unauthenticated');
  assert.equal(isolated.calls.length, 0);
});

test('configuration rejected before transport, and local required validation', async () => {
  for (const url of [undefined, '', 'http://api.invalid', 'https://a:b@api.invalid', 'https://api.invalid?q=x', 'https://api.invalid#x', 'https://@api.invalid', 'https:///api.invalid', 'https://api.\ninvalid', 'https://api.invalid/path', 'not a url']) {
    const { service, calls } = fixture([], url === undefined ? '' : url);
    assert.equal((await service.login(request)).outcome, 'configuration');
    assert.equal(calls.length, 0);
  }
  const { service, calls } = fixture([]);
  const result = await service.login({ correo: ' ', contrasena: '' });
  assert.equal(result.outcome, 'validation');
  assert.equal(result.message, 'El correo es obligatorio. La contraseña es obligatoria.');
  assert.equal(calls.length, 0);
});

test('recognized 400 and exact generic 401; safe messages and retry', async () => {
  const { service } = fixture([
    reply(400, { errors: { Correo: ['El correo no es válido.'], Contrasena: ['secret from server'] } }),
    reply(401, { mensaje: 'secret from server' }), reply(200, success),
  ]);
  assert.equal((await service.login(request)).message, 'El correo no es válido.');
  assert.equal((await service.login(request)).message, 'No se pudo ingresar con las credenciales proporcionadas.');
  assert.deepEqual(service.getState(), { authenticated: false, logoutPending: false });
  assert.equal((await service.login(request)).outcome, 'success');
});

test('errors never create session or expose raw errors', async () => {
  for (const response of [reply(400, {}), reply(403), reply(503), reply(500), reply(302), reply(200, {}), new Error('private detail')]) {
    const { service } = fixture([response]);
    const result = await service.login(request);
    assert.notEqual(result.outcome, 'success');
    assert.equal(result.message.includes('private'), false);
    assert.deepEqual(service.getState(), { authenticated: false, logoutPending: false });
  }
});

test('validate shape, roles, canonical opaque token and future UTC expiry', async () => {
  for (const bad of [
    { ...success, token: 'US-1' }, { ...success, token: 'A'.repeat(42) + 'B' },
    { ...success, roles: [0] }, { ...success, roles: ['unknown'] }, { ...success, roles: null },
    { ...success, identificadorUsuario: '' }, { ...success, venceEnUtc: '2026-01-08T00:00:00+00:00' },
    { ...success, venceEnUtc: '2026-02-30T00:00:00.000000Z' },
    { ...success, venceEnUtc: '2026-01-01T00:00:00.000000Z' },
  ]) {
    const { service } = fixture([reply(200, bad)]);
    assert.equal((await service.login(request)).outcome, 'invalidResponse');
    assert.equal(service.getState().authenticated, false);
  }
  const { service } = fixture([reply(200, { ...success, roles: [] })]);
  assert.equal((await service.login(request)).outcome, 'success'); // no invented minimum role policy
});

test('authenticated 403 preserves state, 401 clears it; malformed identity is not success', async () => {
  const { service } = fixture([reply(200, success), reply(403), reply(200, {}), reply(401)]);
  await service.login(request);
  assert.equal((await service.checkPassenger()).outcome, 'denied');
  assert.equal(service.getState().authenticated, true);
  assert.equal((await service.checkPassenger()).outcome, 'invalidResponse');
  assert.equal(service.getState().authenticated, true);
  assert.equal((await service.checkPassenger()).outcome, 'unauthenticated');
  assert.equal(service.getState().authenticated, false);
});

test('failed logout blocks protected work, retains safe retry, never confirms revocation', async () => {
  for (const failure of [reply(503), reply(500), reply(403), reply(200, {}), new Error('offline')]) {
    const { service, calls } = fixture([reply(200, success), failure, reply(204)]);
    await service.login(request);
    const result = await service.logout();
    assert.notEqual(result.outcome, 'success');
    assert.match(result.message, /No se pudo confirmar la revocación/);
    assert.equal(service.getState().logoutPending, true);
    assert.equal((await service.checkPassenger()).outcome, 'unauthenticated');
    assert.equal(calls.length, 2);
    assert.equal((await service.logout()).outcome, 'success');
    assert.equal(calls[2][1].headers.Authorization, `Bearer ${token}`);
    assert.equal(service.getState().authenticated, false);
  }
});

test('logout 401 clears invalid session but does not confirm revocation', async () => {
  const { service } = fixture([reply(200, success), reply(401)]);
  await service.login(request);
  const result = await service.logout();
  assert.equal(result.outcome, 'unauthenticated');
  assert.match(result.message, /No se pudo confirmar la revocación/);
  assert.equal(service.getState().authenticated, false);
});

test('duplicates, discard and late transport responses cannot resurrect session', async () => {
  let finish;
  const { service, calls } = fixture([() => new Promise(resolve => { finish = resolve; })]);
  const pending = service.login(request);
  assert.equal((await service.login(request)).outcome, 'busy');
  service.discard();
  finish(reply(200, success));
  assert.equal((await pending).outcome, 'cancelled');
  assert.equal(service.getState().authenticated, false);
  assert.equal(calls.length, 1);
});

test('timeout bounds ignored abort and response body; safe retry', async () => {
  for (const stalled of [() => new Promise(() => {}), () => ({ status: 200, redirected: false, json: () => new Promise(() => {}) })]) {
    const { service } = fixture([stalled, reply(200, success)], 'https://api.example.invalid', 5);
    assert.equal((await service.login(request)).outcome, 'network');
    assert.equal(service.getState().authenticated, false);
    assert.equal((await service.login(request)).outcome, 'success');
  }
});

test('authenticated failures preserve state and a new login cannot overwrite it', async () => {
  for (const failure of [reply(503), reply(500), new Error('offline'), reply(200, null)]) {
    const { service, calls } = fixture([reply(200, success), failure, reply(200, identity)]);
    await service.login(request);
    const state = service.getState();
    assert.notEqual((await service.checkPassenger()).outcome, 'success');
    assert.deepEqual(service.getState(), state);
    assert.equal((await service.login(request)).outcome, 'denied');
    assert.equal((await service.checkPassenger()).outcome, 'success');
    assert.equal(calls.length, 3);
  }
});

test('late body after discard cannot overwrite a different confirmed session', async () => {
  let resolveBody;
  let bodyStarted;
  const started = new Promise(resolve => { bodyStarted = resolve; });
  const secondToken = 'C'.repeat(42) + 'A';
  const { service, calls } = fixture([
    () => ({ status: 200, redirected: false, json: () => {
      bodyStarted();
      return new Promise(resolve => { resolveBody = resolve; });
    } }),
    reply(200, { ...success, token: secondToken }), reply(200, identity),
  ]);
  const old = service.login(request);
  await started;
  service.discard();
  assert.equal((await old).outcome, 'cancelled');
  assert.equal((await service.login(request)).outcome, 'success');
  resolveBody(success);
  await new Promise(resolve => setImmediate(resolve));
  await service.checkPassenger();
  assert.equal(calls[2][1].headers.Authorization, `Bearer ${secondToken}`);
});

test('simultaneous instances do not share credentials or logout state', async () => {
  const first = fixture([reply(200, success), reply(204)]);
  const secondToken = 'D'.repeat(42) + 'A';
  const second = fixture([reply(200, { ...success, token: secondToken }), reply(200, identity)]);
  await Promise.all([first.service.login(request), second.service.login(request)]);
  await first.service.logout();
  assert.equal(second.service.getState().authenticated, true);
  await second.service.checkPassenger();
  assert.equal(second.calls[1][1].headers.Authorization, `Bearer ${secondToken}`);
});

test('implementation contains no persistent storage or global authorization consumer', () => {
  assert.doesNotMatch(source, /AsyncStorage|SecureStore|localStorage|sessionStorage|document\.cookie|defaults\.headers/);
  assert.doesNotMatch(source, /console\.|JSON\.stringify\(this/);
});

test('expiry clears local state without sending Bearer; redirects cannot succeed', async () => {
  let clock = now;
  const service = new AuthenticationService('https://api.example.invalid', async () => reply(200, success), () => clock);
  await service.login(request);
  clock = Date.parse(success.venceEnUtc);
  assert.equal((await service.checkPassenger()).outcome, 'unauthenticated');
  const redirected = fixture([() => ({ status: 200, redirected: true, json: async () => success })]);
  assert.equal((await redirected.service.login(request)).outcome, 'invalidResponse');
});
