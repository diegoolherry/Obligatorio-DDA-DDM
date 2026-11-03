import test from 'node:test';
import assert from 'node:assert/strict';
import vm from 'node:vm';
import { installSessionDurabilityPanel } from '../../odd/browser-checks/session-durability-20261028/session-durability-panel.mjs';

// Synthetic only. The actual installer is serialized as page.evaluate would do.
const token = 'A'.repeat(43);
const identity = 'B'.repeat(42) + 'A';
const password = 'synthetic-password';
const login = () => ({ identificadorUsuario: identity, roles: ['pasajero'], token,
  venceEnUtc: '2099-10-28T12:00:00.000000Z' });
const passenger = () => ({ identificadorUsuario: identity, roles: ['pasajero'] });
const response = (status, body, headers = {}) => ({ status,
  headers: new Headers({ 'cache-control': 'no-store', 'content-type': 'application/json', ...headers }),
  body: new ReadableStream({ start(c) {
    if (body) c.enqueue(new TextEncoder().encode(body));
    c.close();
  } }) });
const ok = () => response(200, JSON.stringify(passenger()));
const rejected = () => response(401, '', { 'www-authenticate': 'Bearer' });

class Element {
  constructor() { this.disabled = false; this.value = ''; this.textContent = ''; this.listeners = {}; this.children = []; }
  addEventListener(name, fn) { this.listeners[name] = fn; }
  appendChild(child) { this.children.push(child); }
  replaceChildren() { this.children = []; }
  async click() { if (!this.disabled) await this.listeners.click?.(); }
}
function fixture(replies = [], options = {}) {
  const elements = new Map();
  let mutations = 0;
  const body = { innerText: options.openApi ?? '{"openapi":"3.0.1","paths":{}}',
    set innerHTML(html) {
      mutations++;
      for (const match of html.matchAll(/<([a-z]+)\b[^>]*\bid="([^"]+)"[^>]*>/g)) {
        const element = new Element();
        element.type = /type="([^"]+)"/.exec(match[0])?.[1];
        element.value = /value="([^"]*)"/.exec(match[0])?.[1] ?? '';
        element.disabled = /\bdisabled\b/.test(match[0]);
        element.readOnly = /\breadonly\b/.test(match[0]);
        elements.set(match[2], element);
      }
    } };
  const requests = [], events = {}, logs = [];
  const context = vm.createContext({
    document: { body, documentElement: {}, getElementById: id => elements.get(id), createElement: () => new Element() },
    location: { origin: options.origin ?? 'https://localhost:7053', protocol: 'https:',
      pathname: '/openapi/v1.json', search: '', hash: '' },
    addEventListener: (name, fn) => { events[name] = fn; },
    fetch: async (url, init) => {
      requests.push({ url, ...init });
      const reply = replies.shift();
      if (!reply) throw Error('unexpected synthetic request');
      return typeof reply === 'function' ? reply(init) : reply;
    },
    AbortController, TextDecoder, setTimeout: options.setTimeout ?? setTimeout, clearTimeout: options.clearTimeout ?? clearTimeout,
    console: { log: value => logs.push(value) }
  });
  const metadata = vm.runInContext(`(${installSessionDurabilityPanel.toString()})()`, context);
  const get = id => { assert.ok(elements.has(id), `public control ${id} installed`); return elements.get(id); };
  const click = id => get(id).click();
  const text = () => [...elements.values()].flatMap(e => [e.textContent, ...e.children.map(c => c.textContent)]).join('\n');
  return { metadata, requests, get, click, text, events, logs, mutations: () => mutations };
}
async function start(f) {
  assert.equal(f.metadata.installed, true);
  f.get('durability-password').value = password;
  await f.click('durability-login');
}
const initial = () => [response(200, JSON.stringify(login())), ok()];
const full = () => [...initial(), ok(), response(204, ''), rejected()];
function sanitized(f) {
  const output = JSON.stringify([f.metadata, f.text(), f.logs]);
  for (const secret of [token, identity, password, login().venceEnUtc]) assert.ok(!output.includes(secret));
}
function terminal(f) {
  for (const id of ['login', 'resume1', 'resume2', 'abandon']) assert.equal(f.get(`durability-${id}`).disabled, true);
}

test('same token authorizes after first human-confirmed restart pause, without a new login', async () => {
  const f = fixture(full());
  assert.equal(f.requests.length, 0);
  await f.click('durability-resume1');
  await start(f);
  assert.equal(f.requests.length, 2);
  assert.match(f.text(), /PAUSA 1/);
  assert.equal(f.get('durability-password').value, '');
  assert.equal(f.get('durability-password').disabled, true);
  await f.click('durability-login');
  assert.equal(f.requests.length, 2);
  await f.click('durability-resume1');
  assert.equal(f.requests.length, 4);
  assert.match(f.text(), /PAUSA 2/);
  assert.equal(f.requests[2].headers.Authorization, `Bearer ${token}`);
  assert.equal(f.requests[3].headers.Authorization, `Bearer ${token}`);
  assert.equal(f.requests.filter(r => r.url.endsWith('/ingreso')).length, 1);
  await f.click('durability-resume1');
  assert.equal(f.requests.length, 4);
  sanitized(f);
});

test('revoked SAME OLD token is rejected after second human-confirmed restart pause', async () => {
  const f = fixture(full());
  await start(f);
  await f.click('durability-resume1');
  await f.click('durability-resume2');
  assert.equal(f.requests.length, 5);
  assert.equal(f.requests[4].headers.Authorization, `Bearer ${token}`);
  assert.match(f.text(), /ÉXITO CONDICIONAL/);
  assert.match(f.text(), /no acredita reinicios/);
  terminal(f);
  await f.click('durability-resume2');
  assert.equal(f.requests.length, 5);
  const expected = [['POST','ingreso'], ['GET','prueba-pasajero'], ['GET','prueba-pasajero'], ['POST','salida'], ['GET','prueba-pasajero']];
  f.requests.forEach((r, i) => {
    assert.equal(r.method, expected[i][0]);
    assert.equal(r.url, `/api/autenticacion/${expected[i][1]}`);
    assert.equal(r.credentials, 'omit'); assert.equal(r.redirect, 'error');
    assert.equal(r.cache, 'no-store'); assert.equal(r.mode, 'same-origin');
    assert.equal(r.referrerPolicy, 'no-referrer'); assert.ok(r.signal instanceof AbortSignal);
    assert.equal(r.headers.Accept, 'application/json');
    assert.deepEqual(Object.keys(r.headers).sort(), (i === 0 ? ['Accept', 'Content-Type'] : ['Accept', 'Authorization']).sort());
    if (i === 0) assert.deepEqual(JSON.parse(r.body), { correo: 'pasajero.demo@example.test', contrasena: password });
    else assert.equal(r.body, undefined);
  });
  sanitized(f);
});

test('installation rejects wrong origin or unconfirmed OpenAPI before mutation', () => {
  for (const options of [{ origin:'http://localhost:7053' }, { origin:'https://127.0.0.1:7053' },
    { openApi:'not JSON' }, { openApi:'{"openapi":"3.0.1","paths":null}' }]) {
    const f = fixture([], options);
    assert.equal(f.metadata.installed, false); assert.equal(f.mutations(), 0); assert.equal(f.requests.length, 0);
  }
});

test('double click and early resume blocked synchronously, password cleared before fetch', async () => {
  let release;
  const f = fixture([init => {
    assert.equal(f.get('durability-password').value, '');
    assert.equal(f.get('durability-password').disabled, true);
    return new Promise(resolve => { release = resolve; });
  }, ok()]);
  f.get('durability-password').value = password;
  const pending = f.click('durability-login');
  await f.click('durability-login'); await f.click('durability-resume1'); await f.click('durability-resume2');
  assert.equal(f.requests.length, 1);
  release(response(200, JSON.stringify(login())));
  await pending;
  assert.equal(f.requests.length, 2);
  await f.click('durability-abandon');
  assert.match(f.text(), /INCOMPLETO/); assert.match(f.text(), /no confirma revocación/);
  terminal(f); sanitized(f);
});

const failures = [
  ['login no-store', () => [response(200, JSON.stringify(login()), { 'cache-control': 'private' })]],
  ['login JSON type', () => [response(200, JSON.stringify(login()), { 'content-type': 'text/html' })]],
  ['login malformed', () => [response(200, '{')]],
  ['login oversized', () => [response(200, 'x'.repeat(16385))]],
  ['login transport', () => [() => { throw Error(`${token} ${password}`); }]],
  ['initial identity', () => [initial()[0], response(200, JSON.stringify({ ...passenger(), roles: ['administrador'] }))]],
  ['resume unauthorized', () => [...initial(), rejected()]],
  ['resume changed roles', () => [...initial(), response(200, JSON.stringify({ ...passenger(), roles: ['pasajero', 'administrador'] }))]],
  ['resume identity', () => [...initial(), response(200, JSON.stringify({ ...passenger(), identificadorUsuario: token }))]],
  ['resume JSON', () => [...initial(), response(200, JSON.stringify(passenger()), { 'content-type':'text/plain' })]],
  ['resume no-store', () => [...initial(), response(200, JSON.stringify(passenger()), { 'cache-control':'max-age=1' })]],
  ['logout nonempty', () => [...initial(), ok(), response(204, 'unexpected')]],
  ['logout no-store', () => [...initial(), ok(), response(204, '', { 'cache-control':'private' })]],
  ['old token still accepted', () => [...initial(), ok(), response(204, ''), ok()]],
  ['401 nonempty', () => [...initial(), ok(), response(204, ''), response(401, 'private')]],
  ['401 challenge', () => [...initial(), ok(), response(204, ''), response(401, '')]],
  ['401 no-store', () => [...initial(), ok(), response(204, ''), response(401, '', { 'cache-control':'private', 'www-authenticate':'Bearer' })]]
];
for (const [name, replies] of failures) test(`terminal failure: ${name}; no cleanup, retry or success`, async () => {
  const supplied = replies();
  const expectedCount = supplied.length;
  const f = fixture(supplied);
  await start(f); await f.click('durability-resume1'); await f.click('durability-resume2');
  assert.equal(f.requests.length, expectedCount);
  assert.match(f.text(), /INCOMPLETO/); assert.doesNotMatch(f.text(), /ÉXITO CONDICIONAL/);
  terminal(f);
  await f.click('durability-login'); await f.click('durability-resume1'); await f.click('durability-resume2');
  assert.equal(f.requests.length, expectedCount); sanitized(f);
});

for (const event of ['abandon', 'pagehide', 'beforeunload']) test(`${event} during login prevents late session use`, async () => {
  let release;
  const f = fixture([() => new Promise(resolve => { release = resolve; })]);
  f.get('durability-password').value = password;
  const pending = f.click('durability-login');
  if (event === 'abandon') await f.click('durability-abandon');
  else f.events[event]();
  release(response(200, JSON.stringify(login())));
  await pending;
  terminal(f); assert.equal(f.requests.length, 1); sanitized(f);
});

test('pagehide at pause discards access without server cleanup', async () => {
  const f = fixture(initial()); await start(f);
  f.events.pagehide();
  await f.click('durability-resume1');
  assert.equal(f.requests.length, 2); terminal(f); sanitized(f);
});

for (const stage of [1, 2]) test(`abandon at pause ${stage} is terminal and sends no cleanup`, async () => {
  const f = fixture(full()); await start(f);
  if (stage === 2) await f.click('durability-resume1');
  const count = f.requests.length;
  await f.click('durability-abandon');
  await f.click(`durability-resume${stage}`);
  assert.equal(f.requests.length, count); terminal(f); sanitized(f);
});

for (const stage of [1, 2]) test(`in-flight resume ${stage}: doubles blocked; abandonment prevents late effects`, async () => {
  let release;
  const wait = () => new Promise(resolve => { release = resolve; });
  const f = fixture(stage === 1 ? [...initial(), wait] : [...initial(), ok(), response(204, ''), wait]);
  await start(f);
  if (stage === 2) await f.click('durability-resume1');
  const pending = f.click(`durability-resume${stage}`);
  const count = f.requests.length;
  await f.click('durability-login'); await f.click('durability-resume1'); await f.click('durability-resume2');
  assert.equal(f.requests.length, count);
  await f.click('durability-abandon');
  release(stage === 1 ? ok() : rejected());
  await pending;
  assert.equal(f.requests.length, count); terminal(f); sanitized(f);
  assert.doesNotMatch(f.text(), /ÉXITO CONDICIONAL/);
});

test('abandon while response stream cleanup yields cannot restore a late credential', async () => {
  let release, cleanupStarted;
  const cleaning = new Promise(resolve => { cleanupStarted = resolve; });
  const reply = response(200, JSON.stringify(login()));
  const reader = reply.body.getReader();
  reply.body = { getReader: () => ({
    read: () => reader.read(),
    cancel: () => new Promise(resolve => { release = resolve; cleanupStarted(); }),
    releaseLock: () => reader.releaseLock()
  }) };
  const f = fixture([reply]);
  f.get('durability-password').value = password;
  const pending = f.click('durability-login');
  await cleaning; await f.click('durability-abandon'); release(); await pending;
  await f.click('durability-resume1');
  assert.equal(f.requests.length, 1); terminal(f); sanitized(f);
});

test('bounded eight-second abort covers response stream, without real waiting', async () => {
  let fire, duration, started, cleared = false;
  const reading = new Promise(resolve => { started = resolve; });
  const f = fixture([init => ({ status: 200,
    headers: new Headers({ 'cache-control':'no-store', 'content-type':'application/json' }),
    body: { getReader: () => ({
      read: () => new Promise((resolve, reject) => {
        init.signal.addEventListener('abort', () => reject(Error('synthetic private failure')), { once: true });
        started();
      }),
      cancel: async () => {}, releaseLock: () => {}
    }) }
  })], {
    setTimeout: (fn, ms) => { fire = fn; duration = ms; return 1; },
    clearTimeout: () => { cleared = true; }
  });
  f.get('durability-password').value = password;
  const pending = f.click('durability-login');
  await reading; // Advance the synthetic clock only once the body read is pending.
  assert.equal(duration, 8000);
  fire(); await pending;
  assert.equal(cleared, true); assert.equal(f.requests.length, 1);
  assert.match(f.text(), /INCOMPLETO/); terminal(f); sanitized(f);
});
