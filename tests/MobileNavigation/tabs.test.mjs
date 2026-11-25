import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { stripTypeScriptTypes } from 'node:module';

const mobileFile = relative => readFile(new URL(`../../src/Mobile/${relative}`, import.meta.url), 'utf8');
const tabsSource = await mobileFile('src/navigation/visibleTabs.ts');
const { visibleTabs } = await import(`data:text/javascript;base64,${Buffer.from(stripTypeScriptTypes(tabsSource)).toString('base64')}`);

test('without a session only Buscar viaje and Ingreso are shown', () => {
  assert.deepEqual(visibleTabs([]), ['index', 'ingreso']);
});

test('a passenger sees Buscar viaje, Ingreso, Tickets and Ubicacion del servicio, never Validar or Compartir', () => {
  assert.deepEqual(visibleTabs(['pasajero']), ['index', 'ingreso', 'tickets', 'ubicacion']);
});

test('a collector sees Ingreso, Validar and Compartir, never Buscar viaje, Tickets or Ubicacion del servicio (passenger only in the API)', () => {
  assert.deepEqual(visibleTabs(['cobrador']), ['ingreso', 'validar', 'compartir']);
});

test('an administrator sees the same tabs as without a session', () => {
  assert.deepEqual(visibleTabs(['administrador']), ['index', 'ingreso']);
});

test('several roles show the union of their tabs in the tab bar order', () => {
  assert.deepEqual(visibleTabs(['cobrador', 'pasajero']), ['index', 'ingreso', 'tickets', 'validar', 'compartir', 'ubicacion']);
  assert.deepEqual(visibleTabs(['administrador', 'cobrador']), ['index', 'ingreso', 'validar', 'compartir']);
});

test('the layout declares the six routes in tab bar order; Validar and Compartir stay mounted (source check)', async () => {
  const layout = await mobileFile('src/app/_layout.tsx');
  const names = [...layout.matchAll(/<Tabs\.Screen name="(\w+)"/g)].map(match => match[1]);
  assert.deepEqual(names, ['index', 'ingreso', 'tickets', 'validar', 'compartir', 'ubicacion']);
  assert.match(layout, /name="validar"[^>]*lazy: false/, 'a validation in flight survives a tab change');
  assert.match(layout, /name="compartir"[^>]*lazy: false/, 'sharing survives a tab change');
});

test('Validar releases the camera when its tab loses focus; Tickets opens Buscar viaje (source check)', async () => {
  const validar = await mobileFile('src/app/validar.tsx');
  const tickets = await mobileFile('src/app/tickets.tsx');
  assert.match(validar, /active=\{useIsFocused\(\)\}|active=\{focused\}/, 'active follows the tab focus');
  assert.match(tickets, /onSearchTrip=\{\(\) => router\.navigate\('\/'\)\}/, 'the empty state opens Buscar viaje');
});

test('the tab bar draws only the tabs of the session roles, with an icon each (source check)', async () => {
  const tabBar = await mobileFile('src/components/ui/TabBar.tsx');
  assert.match(tabBar, /visibleTabs\(authentication\.getRoles\(\)\)/, 'the bar filters routes by the session roles');
  for (const name of ['index', 'ingreso', 'tickets', 'validar', 'compartir', 'ubicacion']) {
    assert.match(tabBar, new RegExp(`\\b${name}: [A-Z]\\w+`), `the ${name} tab has an icon`);
  }
});
