import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { stripTypeScriptTypes } from 'node:module';

async function load(relative) {
  const source = await readFile(new URL(`../../src/Mobile/src/${relative}`, import.meta.url), 'utf8');
  return { source, module: await import(`data:text/javascript;base64,${Buffer.from(stripTypeScriptTypes(source)).toString('base64')}`) };
}
const { module: { TicketValidationService } } = await load('services/TicketValidation/TicketValidationService.ts');
const { module: { TicketValidationFlow } } = await load('services/TicketValidation/TicketValidationFlow.ts');
const { module: format } = await load('services/TicketValidation/ticketValidationFormat.ts');
const { module: { formatUruguayDateTime } } = await load('services/Location/locationFormat.ts');
// TSX cannot be stripped by node, so the screen is only checked statically.
const screenSource = await readFile(new URL('../../src/Mobile/src/components/TicketValidation/TicketValidationScreen.tsx', import.meta.url), 'utf8');

const ticket = {
  ticketId: 'TI-01', servicioId: 'SV-01', tramoId: 'TR-01', origenNombre: 'Ombúes de Lavalle', destinoNombre: 'Radial',
  salida: '2026-11-13T07:00:00', llegada: '2026-11-13T07:40:00',
};
const answer = (status, body) => ({ outcome: 'response', status, ...(body === undefined ? {} : { body }) });

/** Fake session: records the authorized requests and answers with scripted results. */
function serviceFixture(...results) {
  const requests = [];
  const service = new TicketValidationService({
    async authorizedRequest(request) {
      requests.push(request);
      return results.shift();
    },
  });
  return { service, requests };
}

// ---- Service -------------------------------------------------------------------------------

test('validate posts qrOpaco and servicioId to the exact route through the session', async () => {
  const { service, requests } = serviceFixture(answer(200, { resultado: 'valido', ticket, usadoUtc: '2026-11-13T10:02:00Z' }));
  const result = await service.validate('qr-abc', 'SV-01');
  assert.deepEqual(result, { ok: true, data: { resultado: 'valido', ticket, usadoUtc: '2026-11-13T10:02:00Z' } });
  assert.deepEqual(requests, [{ method: 'POST', path: '/api/validaciones-ticket', body: { qrOpaco: 'qr-abc', servicioId: 'SV-01' } }]);
});

test('the QR content is sent unchanged (case and symbols are significant)', async () => {
  const { service, requests } = serviceFixture(answer(200, { resultado: 'codigoInexistente' }));
  await service.validate('AbC-dEf_123', 'SV-01');
  assert.equal(requests[0].body.qrOpaco, 'AbC-dEf_123');
});

test('maps the five business results, all answered with 200', async () => {
  const { service } = serviceFixture(
    answer(200, { resultado: 'valido', ticket, usadoUtc: '2026-11-13T10:02:00Z' }),
    answer(200, { resultado: 'yaUtilizado', ticket, usadoUtc: '2026-11-13T09:58:00Z' }),
    answer(200, { resultado: 'vencido', ticket, usadoUtc: null }),
    answer(200, { resultado: 'tramoIncorrecto', ticket: { ...ticket, servicioId: 'SV-02' } }),
    answer(200, { resultado: 'codigoInexistente' }),
  );
  const results = [];
  for (let i = 0; i < 5; i++) results.push(await service.validate('qr', 'SV-01'));
  assert.deepEqual(results.map(r => r.ok && r.data.resultado), ['valido', 'yaUtilizado', 'vencido', 'tramoIncorrecto', 'codigoInexistente']);
  assert.equal(results[0].data.usadoUtc, '2026-11-13T10:02:00Z');
  assert.equal(results[1].data.usadoUtc, '2026-11-13T09:58:00Z');
  assert.equal(results[2].data.usadoUtc, null);
  assert.equal(results[3].data.usadoUtc, null); // absent field is normalised to null
  assert.equal(results[3].data.ticket.servicioId, 'SV-02');
  assert.equal(results[4].data.ticket, null);
});

test('network, server, auth and invalid-response failures never produce ok', async () => {
  const cases = [
    [{ outcome: 'network' }, 'network'],
    [{ outcome: 'cancelled' }, 'cancelled'],
    [{ outcome: 'configuration' }, 'configuration'],
    [{ outcome: 'unauthenticated', status: 401 }, 'unauthenticated'],
    [{ outcome: 'invalidRequest' }, 'invalid'],
    [{ outcome: 'invalidResponse', status: 200 }, 'invalidResponse'],
    [answer(500), 'server'],
    [answer(503), 'server'],
    [answer(401), 'unauthenticated'],
    [answer(403), 'forbidden'],
    [answer(400), 'invalid'],
    [answer(204), 'invalidResponse'],
    [{ outcome: 'response' }, 'invalidResponse'],
  ];
  for (const [scripted, failure] of cases) {
    const { service } = serviceFixture(scripted);
    const result = await service.validate('qr', 'SV-01');
    assert.equal(result.ok, false, failure);
    assert.equal(result.failure, failure);
    assert.ok(result.message.length > 0);
  }
});

test('a 200 with an unknown result or a malformed body is not trusted as success', async () => {
  const bodies = [
    undefined, null, 'valido', [], {}, { resultado: 'ok', ticket },
    { resultado: 'valido' }, // a valid scan must carry its ticket
    { resultado: 'valido', ticket: { ...ticket, ticketId: undefined } },
    { resultado: 'yaUtilizado', ticket, usadoUtc: 5 },
  ];
  for (const body of bodies) {
    const { service } = serviceFixture(answer(200, body));
    const result = await service.validate('qr', 'SV-01');
    assert.equal(result.ok, false, JSON.stringify(body));
    assert.equal(result.failure, 'invalidResponse');
  }
});

test('an empty code or service fails locally without a request', async () => {
  const { service, requests } = serviceFixture();
  for (const [code, id] of [['', 'SV-01'], ['qr', '']]) {
    const result = await service.validate(code, id);
    assert.equal(result.ok, false);
    assert.equal(result.failure, 'invalid');
  }
  assert.equal(requests.length, 0);
});

// ---- State machine -------------------------------------------------------------------------

const granted = { status: 'granted', canAskAgain: true };
const undetermined = { status: 'undetermined', canAskAgain: true };
const denied = { status: 'denied', canAskAgain: false };
const validResult = { resultado: 'valido', ticket, usadoUtc: '2026-11-13T10:02:00Z' };

/** Validation service double whose responses the test resolves by hand. */
function pendingService() {
  const calls = [];
  const service = {
    validate(qr, id) {
      return new Promise(resolve => calls.push({ qr, id, resolve }));
    },
  };
  return { service, calls };
}
const settle = () => new Promise(resolve => setTimeout(resolve, 0));

test('starts selecting and requires a service before scanning', () => {
  const flow = new TicketValidationFlow(pendingService().service);
  assert.deepEqual(flow.state, { kind: 'selecting', servicioId: undefined });
  flow.requestScan(granted);
  assert.equal(flow.state.kind, 'selecting');
});

test('permission paths: granted scans, undetermined explains, denied blocks, then retry', () => {
  const flow = new TicketValidationFlow(pendingService().service);
  flow.selectService('SV-01');
  flow.requestScan(undetermined);
  assert.deepEqual(flow.state, { kind: 'permission-explain', servicioId: 'SV-01' });
  flow.permissionAnswered(denied);
  assert.deepEqual(flow.state, { kind: 'permission-denied', servicioId: 'SV-01' });
  flow.permissionAnswered(granted); // «Volver a intentar» after enabling it in Ajustes
  assert.deepEqual(flow.state, { kind: 'scanning', servicioId: 'SV-01' });
  flow.cancel();
  assert.deepEqual(flow.state, { kind: 'selecting', servicioId: 'SV-01' });
  flow.requestScan(denied);
  assert.equal(flow.state.kind, 'permission-denied');
  flow.requestScan(granted);
  assert.equal(flow.state.kind, 'scanning');
});

test('a scan sends one request with the chosen service and shows the server result', async () => {
  const { service, calls } = pendingService();
  const flow = new TicketValidationFlow(service);
  const seen = [];
  flow.subscribe(state => seen.push(state.kind));
  flow.selectService('SV-01');
  flow.requestScan(granted);
  flow.codeScanned('qr-abc');
  assert.deepEqual(flow.state, { kind: 'validating', servicioId: 'SV-01', code: 'qr-abc' });
  assert.equal(calls.length, 1);
  assert.deepEqual([calls[0].qr, calls[0].id], ['qr-abc', 'SV-01']);
  calls[0].resolve({ ok: true, data: validResult });
  await settle();
  assert.deepEqual(flow.state, { kind: 'result', servicioId: 'SV-01', result: validResult });
  // subscribe() reports the current state at once; then one entry per transition.
  assert.deepEqual(seen, ['selecting', 'selecting', 'scanning', 'validating', 'result']);
});

test('duplicate barcode events while validating or after a result are ignored: one request per scan', async () => {
  const { service, calls } = pendingService();
  const flow = new TicketValidationFlow(service);
  flow.selectService('SV-01');
  flow.requestScan(granted);
  flow.codeScanned('qr-abc');
  flow.codeScanned('qr-abc'); // same frame delivered twice
  flow.codeScanned('qr-other'); // another code while in flight
  assert.equal(calls.length, 1);
  assert.equal(flow.state.code, 'qr-abc');
  calls[0].resolve({ ok: true, data: validResult });
  await settle();
  flow.codeScanned('qr-abc'); // the camera is stopped after a result
  assert.equal(calls.length, 1);
  assert.equal(flow.state.kind, 'result');
});

test('scans outside the scanning state or with an empty code do nothing', () => {
  const { service, calls } = pendingService();
  const flow = new TicketValidationFlow(service);
  flow.codeScanned('qr'); // selecting
  flow.selectService('SV-01');
  flow.requestScan(undetermined);
  flow.codeScanned('qr'); // permission-explain
  flow.permissionAnswered(granted);
  flow.codeScanned('');
  assert.equal(calls.length, 0);
  assert.equal(flow.state.kind, 'scanning');
});

test('a network error is not a result: retry resends the same code and service', async () => {
  const { service, calls } = pendingService();
  const flow = new TicketValidationFlow(service);
  flow.selectService('SV-01');
  flow.requestScan(granted);
  flow.codeScanned('qr-abc');
  calls[0].resolve({ ok: false, failure: 'network', message: 'sin conexión' });
  await settle();
  assert.deepEqual(flow.state, {
    kind: 'network-error', servicioId: 'SV-01', code: 'qr-abc', failure: 'network', message: 'sin conexión',
  });
  flow.codeScanned('qr-other'); // the camera is stopped
  assert.equal(calls.length, 1);
  flow.retry();
  flow.retry(); // double tap
  assert.equal(calls.length, 2);
  assert.deepEqual([calls[1].qr, calls[1].id], ['qr-abc', 'SV-01']);
  assert.equal(flow.state.kind, 'validating');
  calls[1].resolve({ ok: true, data: { ...validResult, resultado: 'yaUtilizado' } });
  await settle();
  assert.equal(flow.state.kind, 'result');
  assert.equal(flow.state.result.resultado, 'yaUtilizado');
});

test('every failure kind ends in network-error, never in a result', async () => {
  for (const failure of ['network', 'server', 'unauthenticated', 'forbidden', 'invalid', 'invalidResponse', 'configuration', 'cancelled']) {
    const { service, calls } = pendingService();
    const flow = new TicketValidationFlow(service);
    flow.selectService('SV-01');
    flow.requestScan(granted);
    flow.codeScanned('qr');
    calls[0].resolve({ ok: false, failure, message: 'x' });
    await settle();
    assert.equal(flow.state.kind, 'network-error', failure);
    assert.equal(flow.state.failure, failure);
  }
});

test('scan another returns to scanning keeping the service; retry is only valid after an error', async () => {
  const { service, calls } = pendingService();
  const flow = new TicketValidationFlow(service);
  flow.selectService('SV-01');
  flow.requestScan(granted);
  flow.retry(); // scanning: nothing to retry
  assert.equal(calls.length, 0);
  flow.codeScanned('qr');
  calls[0].resolve({ ok: true, data: validResult });
  await settle();
  flow.retry(); // a result is final: no resend
  assert.equal(calls.length, 1);
  flow.scanAnother();
  assert.deepEqual(flow.state, { kind: 'scanning', servicioId: 'SV-01' });
  flow.codeScanned('qr-2');
  assert.equal(calls.length, 2);
});

test('the service cannot change while validating and cancel does not abort a request in flight', async () => {
  const { service, calls } = pendingService();
  const flow = new TicketValidationFlow(service);
  flow.selectService('SV-01');
  flow.requestScan(granted);
  flow.codeScanned('qr');
  flow.selectService('SV-02');
  flow.cancel();
  assert.equal(flow.state.kind, 'validating');
  assert.equal(flow.state.servicioId, 'SV-01');
  calls[0].resolve({ ok: true, data: validResult });
  await settle();
  assert.equal(flow.state.kind, 'result');
  assert.equal(flow.state.servicioId, 'SV-01');
});

test('formats the service day with weekday from the local date, without time-zone shifts', () => {
  assert.equal(format.formatServiceDay('2026-11-13T07:00:00'), '07:00 · Vie 13/11/2026');
  assert.equal(format.formatServiceDay('2026-11-12T23:59:00'), '23:59 · Jue 12/11/2026');
  assert.equal(format.formatServiceDay('2026-11-15T00:05:00'), '00:05 · Dom 15/11/2026');
  assert.equal(format.formatServiceDay('garbage'), 'Fecha no disponible');
});

test('shortDateTime keeps day/month and hour:minute of the Uruguay formatted instant', () => {
  assert.equal(format.shortDateTime('13/11/2026 07:02:09'), '13/11 07:02');
  assert.equal(format.shortDateTime(undefined), 'no disponible');
});

test('403 (service not assigned) is a clear non-success message, never a result', async () => {
  const { service } = serviceFixture(answer(403));
  const result = await service.validate('qr', 'SV-09');
  assert.equal(result.ok, false);
  assert.equal(result.failure, 'forbidden');
  assert.equal(result.status, 403);
  assert.match(result.message, /no está asignado/);
  assert.match(result.message, /no se validó/);
});

test('change tramo after a result or an error keeps the service; reset forgets it and drops late answers', async () => {
  const { service, calls } = pendingService();
  const flow = new TicketValidationFlow(service);
  flow.selectService('SV-01');
  flow.requestScan(granted);
  flow.backToSelection(); // scanning: not allowed
  assert.equal(flow.state.kind, 'scanning');
  flow.codeScanned('qr');
  calls[0].resolve({ ok: false, failure: 'forbidden', message: 'x' });
  await settle();
  flow.backToSelection();
  assert.deepEqual(flow.state, { kind: 'selecting', servicioId: 'SV-01' });
  flow.requestScan(granted);
  flow.codeScanned('qr-2');
  flow.reset(); // session ended while the request is in flight
  assert.deepEqual(flow.state, { kind: 'selecting', servicioId: undefined });
  calls[1].resolve({ ok: true, data: validResult });
  await settle();
  assert.deepEqual(flow.state, { kind: 'selecting', servicioId: undefined }); // no late success
});

test('the unconfirmed view is a warning without any success wording, and only transient failures offer retry', () => {
  const chosen = { origen: 'Ombúes de Lavalle', destino: 'Radial', salida: '2026-11-13T07:00:00' };
  const view = format.describeUnconfirmed('Sin respuesta.', chosen);
  assert.equal(view.tone, 'warning');
  assert.equal(view.title, 'No se pudo confirmar');
  assert.deepEqual(view.rows.at(-1), { label: 'Estado', value: 'Sin confirmar' });
  assert.doesNotMatch(JSON.stringify(view), /válido|Puede viajar/);
  for (const failure of ['network', 'server', 'invalidResponse', 'cancelled']) assert.equal(format.canRetryFailure(failure), true, failure);
  for (const failure of ['unauthenticated', 'forbidden', 'invalid', 'configuration']) assert.equal(format.canRetryFailure(failure), false, failure);
});

test('each of the five results has its own title, tone and rows; only valido is a success', () => {
  const chosen = { origen: 'Ombúes de Lavalle', destino: 'Radial', salida: '2026-11-13T07:00:00' };
  const other = { ...ticket, origenNombre: 'Radial', destinoNombre: 'Colonia', salida: '2026-11-12T07:00:00' };
  const view = (resultado, withTicket, usadoUtc = null) =>
    format.describeResult({ resultado, ticket: withTicket, usadoUtc }, chosen, formatUruguayDateTime);
  const valido = view('valido', ticket, '2026-11-13T10:02:00Z');
  assert.deepEqual([valido.tone, valido.title], ['success', 'Ticket válido']);
  assert.deepEqual(valido.rows, [
    { label: 'Tramo', value: 'Ombúes de Lavalle a Radial' }, { label: 'Servicio', value: '07:00 · Vie 13/11/2026' },
    { label: 'Validado', value: '13/11 07:02' },
  ]);
  const used = view('yaUtilizado', ticket, '2026-11-13T09:58:00Z');
  assert.deepEqual([used.tone, used.title, used.rows.at(-1)], ['error', 'Ya utilizado', { label: 'Primer uso', value: '13/11 06:58' }]);
  const expired = view('vencido', other);
  assert.deepEqual([expired.tone, expired.title, expired.rows.at(-1)], ['error', 'Vencido', { label: 'Servicio del ticket', value: '07:00 · Jue 12/11/2026' }]);
  const wrong = view('tramoIncorrecto', other);
  assert.deepEqual([wrong.tone, wrong.title], ['error', 'Tramo incorrecto']);
  assert.deepEqual(wrong.rows.slice(-2), [
    { label: 'Tramo del ticket', value: 'Radial a Colonia' }, { label: 'Ticket', value: 'No se consumió · sigue vigente' },
  ]);
  const unknown = view('codigoInexistente', null);
  assert.deepEqual([unknown.tone, unknown.title], ['error', 'Código inexistente']);
  assert.match(unknown.rows.at(-1).value, /abra el ticket en su app/);
  const titles = new Set([valido, used, expired, wrong, unknown].map(item => item.title));
  assert.equal(titles.size, 5);
  assert.equal([valido, used, expired, wrong, unknown].filter(item => item.tone === 'success').length, 1);
  for (const item of [valido, used, expired, wrong, unknown]) assert.ok(item.glyph.length > 0); // icon next to the words
});

// ---- Screen wiring (static: a camera scan cannot run here) -----------------------------------

test('the screen reads QR only, with the agreed camera API and no success on its own', () => {
  assert.match(screenSource, /barcodeScannerSettings=\{\{\s*barcodeTypes:\s*\['qr'\]\s*\}\}/);
  assert.match(screenSource, /onBarcodeScanned=/);
  assert.match(screenSource, /useCameraPermissions/);
  assert.match(screenSource, /Linking\.openSettings\(\)/);
  assert.match(screenSource, /export function TicketValidationScreen/);
  assert.match(screenSource, /'Abrir Ajustes'|label="Abrir Ajustes"/);
  assert.match(screenSource, /label="Volver a intentar"/);
  assert.match(screenSource, /Validando…/);
  assert.match(screenSource, /listCollectorServices/); // same source as the location sharing flow
  // The camera exists only while scanning, so it stops after a result until «Escanear otro».
  assert.ok(screenSource.indexOf("state.kind === 'scanning' &&") >= 0
    && screenSource.indexOf("state.kind === 'scanning' &&") < screenSource.indexOf('<CameraView'));
  assert.equal((screenSource.match(/<CameraView/g) ?? []).length, 1);
});
