import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { stripTypeScriptTypes } from 'node:module';

const source = await readFile(new URL('../../src/Mobile/src/services/Purchase/PurchaseService.ts', import.meta.url), 'utf8');
const { PurchaseService, canFinishPurchase } = await import(`data:text/javascript;base64,${Buffer.from(stripTypeScriptTypes(source)).toString('base64')}`);
const itinerarySearchSource = await readFile(new URL('../../src/Mobile/src/components/Itinerary/ItinerarySearch.tsx', import.meta.url), 'utf8');
const itinerary = { salida: '2026-11-18T09:00:00', llegada: '2026-11-18T10:00:00', precioTotal: 125,
  transbordo: null, tramos: [
    { orden: 1, servicioId: 'SV-01', tramoId: 'TR-01', origen: { id: 1, nombre: 'A' }, destino: { id: 2, nombre: 'B' }, salida: '2026-11-18T09:00:00', llegada: '2026-11-18T09:30:00', importe: 50, politicaAsiento: 'sinAsiento' },
    { orden: 2, servicioId: 'SV-02', tramoId: 'TR-02', origen: { id: 2, nombre: 'B' }, destino: { id: 3, nombre: 'C' }, salida: '2026-11-18T09:40:00', llegada: '2026-11-18T10:00:00', importe: 75, politicaAsiento: 'asignacionAutomatica' },
  ] };
const attempt = { intentoId: 'IC-01', itinerario: itinerary, holdVenceEn: '2026-11-18T09:15:00Z' };
const quote = { intentoId: 'IC-01', cotizacionOriginal: itinerary, cotizacionActual: itinerary, cambioPrecio: false,
  huellaCotizacion: 'a'.repeat(64), holdVenceEn: attempt.holdVenceEn };
const approved = { pago: { pagoId: 'PG-01', intentoId: 'IC-01', estado: 'aprobado', importe: 125, moneda: 'UYU', checkoutUrl: null, holdVenceEn: attempt.holdVenceEn },
  compra: { compraId: 'CO-01', intentoId: 'IC-01', pagoId: 'PG-01', confirmadoUtc: '2026-11-18T09:02:00Z', importeTotal: 125,
    tickets: [{ ticketId: 'TI-01', orden: 1, servicioId: 'SV-01', tramoId: 'TR-01', origenNombre: 'A', destinoNombre: 'B', importe: 50, politicaAsiento: 'sinAsiento', numeroAsiento: null, qrOpaco: 'secret-1' },
      { ticketId: 'TI-02', orden: 2, servicioId: 'SV-02', tramoId: 'TR-02', origenNombre: 'B', destinoNombre: 'C', importe: 75, politicaAsiento: 'asignacionAutomatica', numeroAsiento: 7, qrOpaco: 'secret-2' }] } };
const response = (status, body) => ({ outcome: 'response', status, ...(body === undefined ? {} : { body }) });
const fixture = responses => {
  const calls = [];
  const auth = { authorizedRequest: async request => { calls.push(request); return responses.shift(); } };
  return { client: new PurchaseService(auth), calls };
};

test('create once, re-quote and start payment send only server-selected IDs and accepted fingerprint', async () => {
  const { client, calls } = fixture([response(201, attempt), response(200, quote), response(200, { ...approved.pago, estado: 'pendiente', checkoutUrl: 'https://www.mercadopago.com.uy/checkout/123' })]);
  const created = await client.createAttempt(itinerary.tramos.map(({ orden, servicioId }) => ({ orden, servicioId })));
  assert.equal(created.ok, true);
  const current = await client.getQuote('IC-01');
  assert.equal(current.ok, true);
  const started = await client.startPayment('IC-01', 'a'.repeat(64));
  assert.equal(started.kind, 'started');
  assert.equal(calls[0].path, '/api/intentos-compra');
  assert.deepEqual(calls[0].body, { tramos: [{ orden: 1, servicioId: 'SV-01' }, { orden: 2, servicioId: 'SV-02' }] });
  assert.equal(calls[1].path, '/api/intentos-compra/IC-01/cotizacion');
  assert.deepEqual(calls[2].body, { huellaCotizacionAceptada: 'a'.repeat(64) });
  assert.equal(JSON.stringify(calls[2].body).includes('125'), false);
});

test('payment sync is explicit and reuses the same attempt to surface issued ticket summary', async () => {
  const { client, calls } = fixture([response(200, approved)]);
  const result = await client.syncPayment('IC-01');
  assert.equal(result.ok, true);
  assert.equal(result.data.compra?.tickets.length, 2);
  assert.deepEqual(calls, [{ method: 'POST', path: '/api/intentos-compra/IC-01/pagos/sincronizar' }]);
});

test('unknown create result is surfaced without repeating attempt creation', async () => {
  const { client, calls } = fixture([{ outcome: 'network' }]);
  const result = await client.createAttempt([{ orden: 1, servicioId: 'SV-01' }]);
  assert.equal(result.ok, false);
  assert.equal(calls.length, 1);
});

test('malformed successful create response is ambiguous and cannot be retried automatically', async () => {
  const { client, calls } = fixture([response(201, { holdVenceEn: attempt.holdVenceEn })]);
  const result = await client.createAttempt([{ orden: 1, servicioId: 'SV-01' }]);
  assert.equal(result.ok, false);
  if (!result.ok) assert.equal(result.kind, 'ambiguous');
  assert.equal(calls.length, 1);
});

test('invalid response with unknown status is also ambiguous', async () => {
  const { client, calls } = fixture([{ outcome: 'invalidResponse' }]);
  const result = await client.createAttempt([{ orden: 1, servicioId: 'SV-01' }]);
  assert.equal(result.ok, false);
  if (!result.ok) assert.equal(result.kind, 'ambiguous');
  assert.equal(calls.length, 1);
});

test('malformed 201 body never enables a second create', async () => {
  const { client, calls } = fixture([{ outcome: 'invalidResponse', status: 201 }]);
  const result = await client.createAttempt([{ orden: 1, servicioId: 'SV-01' }]);
  assert.equal(result.ok, false);
  if (!result.ok) assert.equal(result.kind, 'ambiguous');
  assert.equal(calls.length, 1);
});

test('a changed accepted quote returns refreshed quote and does not start another payment', async () => {
  const changed = { ...quote, cambioPrecio: true, huellaCotizacion: 'b'.repeat(64), cotizacionActual: { ...itinerary, precioTotal: 130 } };
  const { client, calls } = fixture([response(409), response(200, changed)]);
  const result = await client.startPayment('IC-01', 'a'.repeat(64));
  assert.equal(result.kind, 'quoteChanged');
  if (result.kind === 'quoteChanged') assert.equal(result.quote.cotizacionActual.precioTotal, 130);
  assert.equal(calls[0].path, '/api/intentos-compra/IC-01/pagos');
  assert.equal(calls[1].path, '/api/intentos-compra/IC-01/cotizacion');
  assert.equal(calls.length, 2);
});

test('202 remains unknown until explicit sync of the same attempt', async () => {
  const { client, calls } = fixture([response(202), response(200, approved)]);
  const started = await client.startPayment('IC-01', 'a'.repeat(64));
  assert.equal(started.kind, 'unknown');
  assert.equal(calls.length, 1);
  const verified = await client.syncPayment('IC-01');
  assert.equal(verified.ok, true);
  assert.equal(calls.length, 2);
  assert.equal(calls[1].method, 'POST');
  assert.equal(calls[1].path, '/api/intentos-compra/IC-01/pagos/sincronizar');
});

test('malformed successful payment start is unknown and only sync follows', async () => {
  const { client, calls } = fixture([response(201, { pagoId: 'PG-01' }), response(200, approved)]);
  const started = await client.startPayment('IC-01', 'a'.repeat(64));
  assert.equal(started.kind, 'unknown');
  const verified = await client.syncPayment('IC-01');
  assert.equal(verified.ok, true);
  assert.equal(calls.length, 2);
  assert.equal(calls[1].path, '/api/intentos-compra/IC-01/pagos/sincronizar');
});


test('malformed quote legs are rejected instead of reaching UI rendering', async () => {
  const nullLeg = { ...quote, cotizacionActual: { ...itinerary, tramos: [null] } };
  const { client } = fixture([response(200, nullLeg)]);
  const result = await client.getQuote('IC-01');
  assert.equal(result.ok, false);
});

test('malformed successful attempt with null route leg remains ambiguous', async () => {
  const malformed = { ...attempt, itinerario: { ...itinerary, tramos: [null] } };
  const { client, calls } = fixture([response(201, malformed)]);
  const result = await client.createAttempt([{ orden: 1, servicioId: 'SV-01' }]);
  assert.equal(result.ok, false);
  if (!result.ok) assert.equal(result.kind, 'ambiguous');
  assert.equal(calls.length, 1);
});

test('a quote with non-string service or route identifiers is rejected', async () => {
  const badLeg = { ...itinerary.tramos[0], servicioId: 7, tramoId: null };
  const malformed = { ...quote, cotizacionActual: { ...itinerary, tramos: [badLeg] } };
  const { client } = fixture([response(200, malformed)]);
  assert.equal((await client.getQuote('IC-01')).ok, false);
});

test('a quote with incomplete route labels is rejected', async () => {
  const badLeg = { ...itinerary.tramos[0], destino: null };
  const malformed = { ...quote, cotizacionOriginal: { ...itinerary, tramos: [badLeg] } };
  const { client } = fixture([response(200, malformed)]);
  assert.equal((await client.getQuote('IC-01')).ok, false);
});

test('only confirmed, refunded, or known expired nonblocking purchases can finish', () => {
  const now = Date.parse('2026-11-18T10:00:00Z');
  const expired = '2026-11-18T09:15:00Z';
  const active = '2026-11-18T10:15:00Z';
  assert.equal(canFinishPurchase('confirmed', undefined, 'aprobado', now), true);
  assert.equal(canFinishPurchase('refunded', undefined, 'devuelto', now), true);
  assert.equal(canFinishPurchase('expired', expired, undefined, now), true);
  assert.equal(canFinishPurchase('rejected', expired, 'rechazado', now), true);
  assert.equal(canFinishPurchase('rejected', active, 'rechazado', now), false);
  assert.equal(canFinishPurchase('pending', expired, 'pendiente', now), false);
  assert.equal(canFinishPurchase('unknown', expired, 'rechazado', now), false);
  assert.equal(canFinishPurchase('refundPending', expired, 'devolucionPendiente', now), false);
  assert.equal(canFinishPurchase('expired', expired, 'pendiente', now), false);
});

test('purchase-flow identity follows itinerary selection', async () => {
  const identitySource = itinerarySearchSource.match(/export function purchaseFlowInstanceKey\([\s\S]*?\n\}/);
  assert.ok(identitySource, 'the itinerary screen must expose its pure purchase-flow identity policy');
  const { purchaseFlowInstanceKey } = await import(`data:text/javascript;base64,${Buffer.from(stripTypeScriptTypes(identitySource[0])).toString('base64')}`);

  const activeSelectionKey = purchaseFlowInstanceKey(7, 1);
  assert.equal(purchaseFlowInstanceKey(7, 1), activeSelectionKey, 'hiding and reopening the same flow preserves its identity');
  assert.notEqual(purchaseFlowInstanceKey(7, 2), activeSelectionKey, 'a new selection in the same session gets a fresh identity');
  assert.notEqual(purchaseFlowInstanceKey(8, 1), activeSelectionKey, 'a new authenticated session stays isolated');
  assert.match(itinerarySearchSource, /key=\{purchaseFlowInstanceKey\(sessionEpoch, purchaseRevision\)\}/);
  assert.match(itinerarySearchSource, /activePurchaseItinerary = purchaseSessionEpoch === sessionEpoch/);
  assert.match(itinerarySearchSource, /<View className=\{purchaseVisible \? '' : 'hidden'\}>/);
  assert.match(itinerarySearchSource, /onClose=\{\(\) => setPurchaseVisible\(false\)\}/);
  assert.match(itinerarySearchSource, /setPurchaseVisible\(true\)/);
  const selectionHandler = itinerarySearchSource.match(/onPurchase=\{\(\) => \{([\s\S]*?)\}\} \/>/);
  assert.ok(selectionHandler, 'the new revision must be tied to a purchase selection');
  assert.match(selectionHandler[1], /setPurchaseRevision\(revision => revision \+ 1\)/);
  assert.equal((itinerarySearchSource.match(/setPurchaseRevision\(/g) ?? []).length, 1);
});

const pendingPayment = { ...approved.pago, estado: 'pendiente', checkoutUrl: 'https://www.mercadopago.com.uy/checkout/123' };

test('pending attempt lookup validates the 200 body and uses the session-bound GET without a body', async () => {
  const { client, calls } = fixture([response(200, { intento: attempt, pago: pendingPayment }), response(200, { intento: attempt, pago: null })]);
  const withPayment = await client.getPendingAttempt();
  assert.equal(withPayment.ok, true);
  assert.equal(withPayment.data.pago.estado, 'pendiente');
  assert.equal(withPayment.data.intento.intentoId, 'IC-01');
  const withoutPayment = await client.getPendingAttempt();
  assert.equal(withoutPayment.ok, true);
  assert.equal(withoutPayment.data.pago, null);
  assert.deepEqual(calls[0], { method: 'GET', path: '/api/intentos-compra/pendiente' });
});

test('pending attempt whose payment belongs to another attempt is rejected', async () => {
  const { client } = fixture([response(200, { intento: attempt, pago: { ...pendingPayment, intentoId: 'IC-99' } })]);
  const result = await client.getPendingAttempt();
  assert.equal(result.ok, false);
});

test('malformed pending attempt bodies are rejected', async () => {
  const { client } = fixture([response(200, { pago: null }), response(200, { intento: { intentoId: 'IC-01' }, pago: null }), response(200, 'x')]);
  assert.equal((await client.getPendingAttempt()).ok, false);
  assert.equal((await client.getPendingAttempt()).ok, false);
  assert.equal((await client.getPendingAttempt()).ok, false);
});

test('pending attempt 204 means nothing pending', async () => {
  const { client } = fixture([response(204)]);
  const result = await client.getPendingAttempt();
  assert.equal(result.ok, true);
  assert.equal(result.data, null);
  assert.equal(result.status, 204);
});

test('pending attempt lookup surfaces network failure, 401 and 500 as typed errors without leaking details', async () => {
  const { client } = fixture([{ outcome: 'network' }, response(401), response(500, { detail: 'secret stack' }), { outcome: 'unauthenticated' }]);
  const network = await client.getPendingAttempt();
  assert.equal(network.ok, false);
  assert.equal(network.kind, 'unknown');
  const unauthorized = await client.getPendingAttempt();
  assert.equal(unauthorized.ok, false);
  assert.equal(unauthorized.kind, 'error');
  const server = await client.getPendingAttempt();
  assert.equal(server.ok, false);
  assert.equal(JSON.stringify(server).includes('secret'), false);
  const noSession = await client.getPendingAttempt();
  assert.equal(noSession.ok, false);
  assert.match(noSession.message, /pasajero/);
});

test('itinerary screen looks up the pending attempt per session and offers a way out of dead ends', () => {
  assert.match(itinerarySearchSource, /purchase\.getPendingAttempt\(\)/);
  assert.match(itinerarySearchSource, /authentication\.getState\(\)\.authenticated/);
  assert.match(itinerarySearchSource, /lookup !== pendingLookup\.current/);
  assert.match(itinerarySearchSource, /pendingPurchase\?\.epoch === sessionEpoch/);
  assert.match(itinerarySearchSource, /onDiscard=\{\(\) => \{/);
});
