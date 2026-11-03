import { installSessionDurabilityPanel } from './session-durability-panel.mjs';

// Run only in the authorized ego-browser nodejs ES-module environment, not Node CLI.
try {
  const task = await taskSpace('ctc-session-durability-20261028');
  const page = task.page('p1');
  await page.goto('https://localhost:7053/openapi/v1.json');
  const result = await page.evaluate(installSessionDurabilityPanel);
  const installed = result?.installed === true;
  console.log({ installed, temporaryDiagnostic: result?.temporaryDiagnostic === true,
    passwordMasked: result?.passwordMasked === true, passwordEmpty: result?.passwordEmpty === true,
    authRequestsSent: 0, spaceId: task.spaceId, page: 'p1' });
  if (installed) await task.handOff();
} catch {
  console.log({ installed: false, incomplete: true, page: 'p1' });
}
