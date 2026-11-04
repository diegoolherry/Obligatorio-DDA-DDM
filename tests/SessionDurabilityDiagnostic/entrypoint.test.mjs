import test from 'node:test';
import assert from 'node:assert/strict';
import vm from 'node:vm';
import { readFileSync } from 'node:fs';
import { installSessionDurabilityPanel } from '../../odd/browser-checks/session-durability-20261028/session-durability-panel.mjs';

const source = readFileSync(new URL('../../odd/browser-checks/session-durability-20261028/install-panel.mjs', import.meta.url), 'utf8');
// Replace only the local import binding; execute all entrypoint statements with synthetic browser APIs.
const importLine = "import { installSessionDurabilityPanel } from './session-durability-panel.mjs';";
assert.ok(source.startsWith(importLine));
for (const installed of [true, false]) test(`entrypoint: one space, p1, navigation, sanitized metadata; installed=${installed}`, async () => {
  const calls = [], logs = [];
  const task = {
    spaceId: 'synthetic-space',
    page: name => {
      calls.push(['page', name]);
      return {
        goto: async url => calls.push(['goto', url]),
        evaluate: async fn => {
          assert.equal(fn, installSessionDurabilityPanel);
          calls.push(['evaluate']);
          return { installed, temporaryDiagnostic: true, passwordMasked: true, passwordEmpty: true,
            token: 'SYNTHETIC-DO-NOT-PRINT', exception: 'SYNTHETIC-DO-NOT-PRINT' };
        }
      };
    },
    handOff: async () => calls.push(['handOff'])
  };
  await vm.runInNewContext(`(async () => {${source.slice(importLine.length)}\n})()`, {
    installSessionDurabilityPanel,
    taskSpace: async name => { calls.push(['taskSpace', name]); return task; },
    console: { log: value => logs.push(value) }
  });
  assert.deepEqual(calls, [ ['taskSpace', 'ctc-session-durability-20261028'], ['page', 'p1'],
    ['goto', 'https://localhost:7053/openapi/v1.json'], ['evaluate'], ...(installed ? [['handOff']] : []) ]);
  assert.equal(logs.length, 1);
  assert.equal(logs[0].installed, installed);
  assert.equal(logs[0].authRequestsSent, 0);
  assert.equal(logs[0].spaceId, 'synthetic-space');
  assert.ok(!JSON.stringify(logs).includes('SYNTHETIC-DO-NOT-PRINT'));
});
