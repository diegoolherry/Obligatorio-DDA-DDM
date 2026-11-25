import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { stripTypeScriptTypes } from 'node:module';

async function load(relative) {
  const source = await readFile(new URL(`../../src/Mobile/src/${relative}`, import.meta.url), 'utf8');
  return { source, module: await import(`data:text/javascript;base64,${Buffer.from(stripTypeScriptTypes(source)).toString('base64')}`) };
}
const { source: serviceSource, module: { TicketsService } } = await load('services/Tickets/TicketsService.ts');
const { module: format } = await load('services/Tickets/ticketFormat.ts');
const { module: { AuthenticationService } } = await load('services/Authentication/AuthenticationService.ts');

const base = {
  servicioId: 'SV-1', tramoId: 'TR-1', origenNombre: 'Ombúes de Lavalle', destinoNombre: 'Radial',
  salida: '2026-11-13T07:00:00', llegada: '2026-11-13T07:55:00', politicaAsiento: 'sinAsiento', numeroAsiento: null,
};
const unused = { ...base, ticketId: 'T-1', estado: 'sinUsar', qrOpaco: 'qr-secret-1', usadoUtc: null };
const used = { ...base, ticketId: 'T-2', estado: 'utilizado', qrOpaco: 'qr-secret-2', salida: '2026-11-11T09:15:00', usadoUtc: '2026-11-11T12:17:00Z' };
const expired = { ...base, ticketId: 'T-3', estado: 'vencido', qrOpaco: 'qr-secret-3', salida: '2026-11-09T18:30:00', usadoUtc: null };
const list = { vigentes: [unused], anteriores: [used, expired] };

const answer = (status, body) => ({ outcome: 'response', status, ...(body === undefined ? {} : { body }) });
function fixture(...results) {
  const requests = [];
  const tickets = new TicketsService({ async authorizedRequest(request) { requests.push(request); return results.shift(); } });
  return { tickets, requests };
}

test('lists tickets with GET /api/tickets and keeps the vigentes/anteriores grouping', async () => {
  const { tickets, requests } = fixture(answer(200, list));
  const result = await tickets.listTickets();
  assert.deepEqual(requests, [{ method: 'GET', path: '/api/tickets' }]);
  assert.equal(result.ok, true);
  assert.deepEqual(result.data.vigentes.map(t => t.ticketId), ['T-1']);
  assert.deepEqual(result.data.anteriores.map(t => t.ticketId), ['T-2', 'T-3']);
  assert.equal(result.data.vigentes[0].qrOpaco, 'qr-secret-1');
  assert.equal(result.data.anteriores[0].usadoUtc, '2026-11-11T12:17:00Z');
});

test('empty groups are a success, not an error', async () => {
  const result = await fixture(answer(200, { vigentes: [], anteriores: [] })).tickets.listTickets();
  assert.equal(result.ok, true);
  assert.deepEqual(result.data, { vigentes: [], anteriores: [] });
});

test('maps failures: 401 session, 403 role, 5xx server, network, configuration, cancelled', async () => {
  const cases = [
    [answer(401), 'unauthenticated', /sesión/],
    [{ outcome: 'unauthenticated' }, 'unauthenticated', /sesión/],
    [answer(403), 'forbidden', /pasajero/],
    [answer(500), 'server', /no está disponible/],
    [{ outcome: 'network' }, 'network', /conexión/],
    [{ outcome: 'configuration' }, 'configuration', /EXPO_PUBLIC_API_URL/],
    [{ outcome: 'cancelled' }, 'cancelled', /descartada/],
  ];
  for (const [reply, failure, message] of cases) {
    const result = await fixture(reply).tickets.listTickets();
    assert.equal(result.ok, false);
    assert.equal(result.failure, failure);
    assert.match(result.message, message);
  }
});

test('rejects malformed bodies as invalidResponse', async () => {
  const bad = [
    answer(200), answer(200, []), answer(200, { vigentes: [] }), answer(200, { vigentes: {}, anteriores: [] }),
    answer(200, { vigentes: [{ ...unused, estado: 'otro' }], anteriores: [] }),
    answer(200, { vigentes: [{ ...unused, qrOpaco: '' }], anteriores: [] }),
    answer(200, { vigentes: [{ ...unused, ticketId: undefined }], anteriores: [] }),
    answer(204),
  ];
  for (const reply of bad) assert.equal((await fixture(reply).tickets.listTickets()).failure, 'invalidResponse');
});

test('end to end with the real session: Bearer header attached and 401 clears the session', async () => {
  const token = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1c3VhcmlvLV8tIn0.c2lnbmF0dXJlLV8';
  const calls = [];
  const replies = [
    new Response(JSON.stringify({ identificadorUsuario: 'B'.repeat(43), roles: ['pasajero'], token, venceEnUtc: '2099-01-01T00:00:00.000000Z' }), { status: 200 }),
    new Response(JSON.stringify(list), { status: 200 }),
    new Response(null, { status: 401 }),
  ];
  const session = new AuthenticationService('https://api.example.invalid', async (url, options) => { calls.push([url, options]); return replies.shift(); });
  await session.login({ correo: 'ficticio@example.invalid', contrasena: 'x' });
  const tickets = new TicketsService(session);
  assert.equal((await tickets.listTickets()).ok, true);
  assert.equal(calls[1][0], 'https://api.example.invalid/api/tickets');
  assert.equal(calls[1][1].headers.Authorization, `Bearer ${token}`);
  assert.equal((await tickets.listTickets()).failure, 'unauthenticated');
  assert.equal(session.getState().authenticated, false);
});

test('service module is pure: no React Native, storage, logging or token handling', () => {
  assert.doesNotMatch(serviceSource, /react-native|expo-|AsyncStorage|SecureStore|console\.|Authorization|fetch\(/);
});

test('filter labels show counts only when the list is loaded', () => {
  assert.equal(format.filterLabel('vigentes', 2), 'Vigentes (2)');
  assert.equal(format.filterLabel('anteriores', 0), 'Anteriores (0)');
  assert.equal(format.filterLabel('vigentes', undefined), 'Vigentes');
  assert.equal(format.filterLabel('anteriores', undefined), 'Anteriores');
});

test('view state: loading and error have no counts; empty is per filter; ready lists the selected group', () => {
  assert.equal(format.countFor({ status: 'loading' }, 'vigentes'), undefined);
  assert.equal(format.countFor({ status: 'error', message: 'x' }, 'anteriores'), undefined);
  const ready = { status: 'ready', data: list };
  assert.equal(format.countFor(ready, 'vigentes'), 1);
  assert.equal(format.countFor(ready, 'anteriores'), 2);
  assert.deepEqual(format.visibleTickets(ready, 'anteriores').map(t => t.ticketId), ['T-2', 'T-3']);
  assert.deepEqual(format.visibleTickets({ status: 'loading' }, 'vigentes'), []);
  const noCurrent = { status: 'ready', data: { vigentes: [], anteriores: [used] } };
  assert.equal(format.isEmptyFilter(noCurrent, 'vigentes'), true);
  assert.equal(format.isEmptyFilter(noCurrent, 'anteriores'), false);
  assert.equal(format.isEmptyFilter({ status: 'loading' }, 'vigentes'), false);
});

test('status badge uses words and an icon for each state, not only color', () => {
  assert.deepEqual(format.statusBadge('sinUsar'), { label: 'Sin usar', icon: '✓', kind: 'success' });
  assert.deepEqual(format.statusBadge('utilizado'), { label: 'Utilizado', icon: '✓✓', kind: 'neutral' });
  assert.deepEqual(format.statusBadge('vencido'), { label: 'Vencido', icon: '✕', kind: 'error' });
});

test('formats service time and date from the Uruguay local value without shifting zones', () => {
  assert.equal(format.formatTime('2026-11-13T07:00:00'), '07:00');
  assert.equal(format.formatTime('basura'), '--:--');
  assert.equal(format.formatDate('2026-11-13T07:00:00'), 'Vie 13/11/2026');
  assert.equal(format.formatDate('2026-11-11T09:15:00'), 'Mié 11/11/2026');
  assert.equal(format.formatDate('2026-11-09T18:30:00'), 'Lun 09/11/2026');
  assert.equal(format.formatDate('2026-11-15T23:59:00'), 'Dom 15/11/2026');
  assert.equal(format.formatDate('basura'), 'Fecha no disponible');
});

test('used-at message converts UTC to Uruguay time and tolerates a missing zone suffix', () => {
  const tail = 'Ya no sirve para viajar.';
  assert.equal(format.usedAtMessage('2026-11-11T12:17:00Z'), `Se validó el 11/11 a las 09:17. ${tail}`);
  assert.equal(format.usedAtMessage('2026-11-11T12:17:00'), `Se validó el 11/11 a las 09:17. ${tail}`);
  assert.equal(format.usedAtMessage('2026-11-12T01:05:00Z'), `Se validó el 11/11 a las 22:05. ${tail}`);
  assert.equal(format.usedAtMessage(null), `Este ticket ya se validó. ${tail}`);
});

test('detail presentation: real QR only for sin usar, dimmed otherwise, with the right message', () => {
  assert.deepEqual(format.detailPresentation(unused), { qr: 'active', notice: 'Presentá este código al cobrador' });
  assert.deepEqual(format.detailPresentation(used), { qr: 'dimmed', notice: 'Se validó el 11/11 a las 09:17. Ya no sirve para viajar.' });
  assert.deepEqual(format.detailPresentation(expired), { qr: 'dimmed', notice: 'Este ticket venció y ya no es válido para viajar.' });
});

test('card titles and accessibility labels read as a route', () => {
  assert.equal(format.routeTitle(unused), 'Ombúes de Lavalle a Radial');
  assert.equal(format.cardAccessibilityLabel(unused), 'Ombúes de Lavalle a Radial, Sin usar, 07:00, Vie 13/11/2026');
});
