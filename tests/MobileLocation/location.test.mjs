import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { stripTypeScriptTypes } from 'node:module';

async function load(relative) {
  const source = await readFile(new URL(`../../src/Mobile/src/${relative}`, import.meta.url), 'utf8');
  return { source, module: await import(`data:text/javascript;base64,${Buffer.from(stripTypeScriptTypes(source)).toString('base64')}`) };
}
const { source: serviceSource, module: { LocationService, roundCoordinate } } = await load('services/Location/LocationService.ts');
const { module: format } = await load('services/Location/locationFormat.ts');
const { module: { AuthenticationService } } = await load('services/Authentication/AuthenticationService.ts');

const service = { id: '1', origen: 'Montevideo', destino: 'Colonia', salida: '2026-11-11T01:00:00', llegada: '2026-11-11T03:00:00' };
const publication = {
  id: '10', servicioId: '1', latitud: -34.1928, longitud: -57.84, recibidaUtc: '2026-11-11T03:00:00Z',
  intervaloSegundos: null, periodicidadSegundos: 30, cumplePeriodicidad: null,
};
const query = {
  servicio: service, ultimaPosicion: { latitud: -34.19, longitud: -57.84, recibidaUtc: '2026-11-11T03:00:00Z', antiguedadSegundos: 12 },
  desactualizada: false, llegadaAproximada: '2026-11-11T01:18:00', estadoDemora: 'enHorario', motivo: null,
  parametros: { periodicidadSegundos: 30, umbralDesactualizacionSegundos: 120, velocidadPromedioKmH: 40, toleranciaDemoraMinutos: 5 },
  datosFicticios: true,
};

/** Fake session: records the requests and answers with scripted results. */
function fixture(...results) {
  const requests = [];
  const location = new LocationService({
    async authorizedRequest(request) {
      requests.push(request);
      return results.shift();
    },
  });
  return { location, requests };
}
const answer = (status, body) => ({ outcome: 'response', status, ...(body === undefined ? {} : { body }) });

test('lists collector and passenger services with the exact routes', async () => {
  const { location, requests } = fixture(answer(200, [service]), answer(200, []), answer(200, [service]));
  assert.deepEqual(await location.listCollectorServices(), { ok: true, data: [service] });
  assert.deepEqual(await location.listCollectorServices(), { ok: true, data: [] });
  assert.deepEqual(await location.listPassengerServices(), { ok: true, data: [service] });
  assert.deepEqual(requests, [
    { method: 'GET', path: '/api/ubicacion/cobrador/servicios' },
    { method: 'GET', path: '/api/ubicacion/cobrador/servicios' },
    { method: 'GET', path: '/api/ubicacion/servicios' },
  ]);
});

test('publishes the position with the exact route and body, rounded to 6 decimals', async () => {
  const { location, requests } = fixture(answer(201, publication));
  const result = await location.publishPosition('1', -34.19283456789, -57.8400004999);
  assert.deepEqual(result, { ok: true, data: publication });
  assert.deepEqual(requests, [{
    method: 'POST', path: '/api/ubicacion/servicios/1/posiciones', body: { latitud: -34.192835, longitud: -57.84 },
  }]);
  assert.equal(JSON.stringify(requests[0].body), '{"latitud":-34.192835,"longitud":-57.84}');
});

test('rounding keeps at most 6 decimals and is stable for typical GPS values', () => {
  for (const [input, expected] of [[-34.19283456789, -34.192835], [0.1 + 0.2, 0.3], [-57.123456499, -57.123456], [90, 90], [0, 0]]) {
    assert.equal(roundCoordinate(input), expected);
    assert.match(String(roundCoordinate(input)), /^-?\d+(\.\d{1,6})?$/);
  }
});

test('invalid coordinates or ids fail locally without a request', async () => {
  const { location, requests } = fixture();
  for (const [id, lat, lon] of [['1', NaN, 0], ['1', 0, Infinity], ['1', 91, 0], ['1', 0, -181], ['', 0, 0]]) {
    const result = await location.publishPosition(id, lat, lon);
    assert.equal(result.ok, false);
    assert.equal(result.failure, 'invalid');
  }
  assert.equal((await location.getServiceLocation('')).failure, 'invalid');
  assert.equal(requests.length, 0);
});

test('service id is encoded as one path segment', async () => {
  const { location, requests } = fixture(answer(200, query));
  await location.getServiceLocation('a/b?c');
  assert.equal(requests[0].path, '/api/ubicacion/servicios/a%2Fb%3Fc');
});

test('passenger query keeps arrival, delay state, parameters and fictitious label', async () => {
  const { location } = fixture(answer(200, query), answer(200, {
    ...query, ultimaPosicion: null, llegadaAproximada: null, estadoDemora: 'sinDatos', motivo: 'Sin posiciones recibidas.',
  }));
  assert.deepEqual(await location.getServiceLocation('1'), { ok: true, data: query });
  const empty = await location.getServiceLocation('1');
  assert.equal(empty.ok, true);
  assert.equal(empty.data.ultimaPosicion, null);
  assert.equal(empty.data.estadoDemora, 'sinDatos');
  assert.equal(empty.data.motivo, 'Sin posiciones recibidas.');
});

test('maps every HTTP and transport outcome to a typed failure with a Spanish message', async () => {
  const cases = [
    [answer(401), 'unauthenticated', /sesión/],
    [{ outcome: 'unauthenticated' }, 'unauthenticated', /sesión/],
    [answer(403), 'forbidden', /permiso|asignada/],
    [answer(404), 'notFound', /no existe/],
    [answer(400), 'invalid', /rechazó/],
    [{ outcome: 'network' }, 'network', /conectar/],
    [answer(500), 'server', /no está disponible/],
    [answer(503), 'server', /no está disponible/],
    [{ outcome: 'configuration' }, 'configuration', /EXPO_PUBLIC_API_URL/],
    [{ outcome: 'cancelled' }, 'cancelled', /descartada/],
    [{ outcome: 'invalidResponse', status: 200 }, 'invalidResponse', /inválida/],
    [answer(418), 'invalidResponse', /inválida/],
    [answer(200, { servicio: 1 }), 'invalidResponse', /inválida/],
  ];
  for (const [scripted, failure, message] of cases) {
    const { location } = fixture(scripted);
    const result = await location.getServiceLocation('1');
    assert.equal(result.ok, false, failure);
    assert.equal(result.failure, failure);
    assert.match(result.message, message);
  }
  assert.equal((await fixture(answer(403)).location.publishPosition('1', 1, 1)).status, 403);
});

test('responses with a wrong shape or status are never success', async () => {
  for (const body of [null, {}, [], [{ id: 1 }], [{ ...service, salida: undefined }], 'x']) {
    const result = await fixture(answer(200, body)).location.listCollectorServices();
    // An empty array is a valid empty list; everything else is an invalid response.
    if (Array.isArray(body) && body.length === 0) assert.equal(result.ok, true);
    else assert.equal(result.failure, 'invalidResponse');
  }
  assert.equal((await fixture(answer(200, publication)).location.publishPosition('1', 1, 1)).failure, 'invalidResponse'); // must be 201
  assert.equal((await fixture(answer(201, { ...publication, periodicidadSegundos: 0 })).location.publishPosition('1', 1, 1)).failure, 'invalidResponse');
  assert.equal((await fixture(answer(200, { ...query, estadoDemora: 'otro' })).location.getServiceLocation('1')).failure, 'invalidResponse');
});

test('end to end with the real session: Bearer is attached by the session and 403 keeps it', async () => {
  const token = 'A'.repeat(43);
  const calls = [];
  const replies = [
    new Response(JSON.stringify({ identificadorUsuario: 'B'.repeat(43), roles: ['cobrador'], token, venceEnUtc: '2099-01-01T00:00:00.000000Z' }), { status: 200 }),
    new Response(JSON.stringify([service]), { status: 200 }),
    new Response(null, { status: 403 }),
    new Response(null, { status: 401 }),
  ];
  const session = new AuthenticationService('https://api.example.invalid', async (url, options) => { calls.push([url, options]); return replies.shift(); });
  await session.login({ correo: 'ficticio@example.invalid', contrasena: 'x' });
  const location = new LocationService(session);
  assert.equal((await location.listCollectorServices()).ok, true);
  assert.equal((await location.publishPosition('1', 1, 1)).failure, 'forbidden');
  assert.equal(session.getState().authenticated, true);
  assert.equal((await location.listCollectorServices()).failure, 'unauthenticated');
  assert.equal(session.getState().authenticated, false);
  assert.equal(calls[1][1].headers.Authorization, `Bearer ${token}`);
  assert.equal(calls[1][0], 'https://api.example.invalid/api/ubicacion/cobrador/servicios');
});

test('service module is pure: no React Native, storage, logging or token handling', () => {
  assert.doesNotMatch(serviceSource, /react-native|expo-|AsyncStorage|SecureStore|console\.|Authorization|fetch\(/);
});

test('UTC instants are shown in Uruguay time (UTC-3)', () => {
  assert.equal(format.formatUruguayDateTime('2026-11-11T03:00:05Z'), '11/11/2026 00:00:05');
  assert.equal(format.formatUruguayDateTime('2026-01-01T02:30:00.123456Z'), '31/12/2025 23:30:00');
  assert.equal(format.formatUruguayDateTime('no es fecha'), undefined);
});

test('local arrival time is read as written and ages are readable', () => {
  assert.equal(format.localHour('2026-11-11T01:18:00'), '01:18');
  assert.equal(format.localHour('x'), '--:--');
  assert.equal(format.formatAge(0), 'hace 0 s');
  assert.equal(format.formatAge(45.4), 'hace 45 s');
  assert.equal(format.formatAge(125), 'hace 2 min 5 s');
  assert.equal(format.formatAge(180), 'hace 3 min');
  assert.equal(format.formatAge(3 * 3600 + 120), 'hace 3 h 2 min');
  assert.equal(format.formatAge(-1), 'antigüedad desconocida');
  assert.equal(format.formatAge(Number.NaN), 'antigüedad desconocida');
});

test('session survives switching tabs: only the app root discards it on unmount (source check)', async () => {
  const login = await readFile(new URL('../../src/Mobile/src/components/Authentication/PassengerLogin.tsx', import.meta.url), 'utf8');
  const app = await readFile(new URL('../../src/Mobile/App.tsx', import.meta.url), 'utf8');
  const loginCleanup = login.match(/return \(\) => \{([\s\S]*?)\};/)?.[1] ?? '';
  assert.doesNotMatch(loginCleanup, /discard\(\)/, 'leaving the Ingreso tab must not discard the session');
  assert.match(app, /useEffect\(\(\) => \(\) => authentication\.discard\(\), \[authentication\]\)/, 'the app root discards the session when it unmounts');
});

test('sharing keeps running across tabs and stops without a collector session (source check)', async () => {
  const app = await readFile(new URL('../../src/Mobile/App.tsx', import.meta.url), 'utf8');
  const sharing = await readFile(new URL('../../src/Mobile/src/components/Location/LocationSharing.tsx', import.meta.url), 'utf8');
  assert.doesNotMatch(app, /screen === 'compartir' && <LocationSharing/, 'the sharing screen must not unmount when changing tabs');
  assert.match(app, /<View style=\{screen === 'compartir' \? styles\.screen : styles\.hidden\}>\s*<LocationSharing/, 'the sharing screen stays mounted and is only hidden');
  assert.match(sharing, /if \(!allowed\) setSharing\(false\);/, 'sharing stops when there is no collector session');
});
