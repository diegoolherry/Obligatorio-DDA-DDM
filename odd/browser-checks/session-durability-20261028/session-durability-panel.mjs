// Serialized by page.evaluate: keep every dependency inside this function.
export function installSessionDurabilityPanel() {
  if (location.origin !== 'https://localhost:7053' || location.protocol !== 'https:' ||
      location.pathname !== '/openapi/v1.json' || location.search || location.hash) {
    return { installed: false, reason: 'unexpected-context' };
  }
  try {
    const api = JSON.parse(document.body.innerText);
    if (!api || typeof api.openapi !== 'string' || !/^3\./.test(api.openapi) ||
        !api.paths || typeof api.paths !== 'object' || Array.isArray(api.paths)) {
      return { installed: false, reason: 'unconfirmed-openapi' };
    }
  } catch { return { installed: false, reason: 'unconfirmed-openapi' }; }

  document.title = 'DIAGNÓSTICO TEMPORAL — durabilidad de sesión';
  document.documentElement.lang = 'es';
  document.body.innerHTML = `<main>
    <h1>Diagnóstico temporal local — NO es la aplicación</h1>
    <p>Una sola secuencia: ingreso y pasajero; pausa; mismo token y salida; pausa; token revocado.
    No recargar ni cerrar esta pestaña hasta terminar. No abrir herramientas de red ni compartir credenciales.</p>
    <p>La contraseña se vacía al enviar. Sesión solo en memoria; ninguna persistencia local.
    Ante un fallo no se reintenta ni se intenta limpieza en el servidor.</p>
    <label for="durability-email">Cuenta ficticia</label>
    <input id="durability-email" type="email" value="pasajero.demo@example.test" readonly autocomplete="off">
    <label for="durability-password">Contraseña privada del pasajero</label>
    <input id="durability-password" type="password" autocomplete="off" spellcheck="false">
    <button id="durability-login" type="button">Ingresar una sola vez</button>
    <button id="durability-resume1" type="button" disabled>Confirmo parada y arranque manual 1; continuar</button>
    <button id="durability-resume2" type="button" disabled>Confirmo parada y arranque manual 2; continuar</button>
    <button id="durability-abandon" type="button">Abandonar: borrar acceso local, sin revocar en servidor</button>
    <section aria-label="Resultados sanitizados"><div id="durability-status" role="status" aria-live="polite"></div></section>
    <p>Los botones no detectan reinicios: se requiere evidencia externa del terminal.
    No prueba MySQL por sí solo, vencimiento exacto, otras sesiones ni iPhone.</p>
  </main>`;
  const control = name => document.getElementById(`durability-${name}`);
  const password = control('password');
  const status = control('status');
  let phase = 'ready';
  let token = '', publicId = '', roles = [], expiry = '';
  let activeController = null;
  const note = text => { status.textContent = text; };
  const render = () => {
    control('login').disabled = phase !== 'ready';
    control('resume1').disabled = phase !== 'pause1';
    control('resume2').disabled = phase !== 'pause2';
    control('abandon').disabled = phase === 'terminal';
  };
  const terminate = success => {
    if (phase === 'terminal') return;
    phase = 'terminal';
    token = ''; publicId = ''; roles = []; expiry = '';
    password.value = ''; password.disabled = true;
    activeController?.abort(); activeController = null;
    render();
    note(success
      ? 'ÉXITO CONDICIONAL: mismo token autorizado antes de salida y rechazado después. El panel no acredita reinicios; requiere evidencia externa de ambas paradas y arranques.'
      : 'INCOMPLETO: acceso local borrado. Sesión incierta; esto no confirma revocación en servidor. Sin reintento ni limpieza automática.');
  };
  const keysAre = (value, keys) => value && typeof value === 'object' && !Array.isArray(value) &&
    Object.keys(value).sort().join('|') === [...keys].sort().join('|');
  // A canonical 32-byte Base64url string ends in one of these sixteen characters.
  const canonical = value => typeof value === 'string' && /^[A-Za-z0-9_-]{42}[AEIMQUYcgkosw048]$/.test(value);
  const validRoles = value => Array.isArray(value) && value.length > 0 &&
    value.every(role => ['pasajero', 'cobrador', 'administrador'].includes(role)) &&
    new Set(value).size === value.length && value.includes('pasajero');
  const identityOK = value => keysAre(value, ['identificadorUsuario', 'roles']) &&
    value.identificadorUsuario === publicId && validRoles(value.roles) &&
    [...value.roles].sort().join('|') === [...roles].sort().join('|');
  const check = value => { if (!value) throw Error('diagnostic-stop'); };

  async function request(method, route, body) {
    check(phase !== 'terminal' && location.origin === 'https://localhost:7053');
    const controller = new AbortController();
    activeController = controller;
    const timer = setTimeout(() => controller.abort(), 8000);
    let reader;
    try {
      const headers = { Accept: 'application/json' };
      if (token) headers.Authorization = `Bearer ${token}`;
      if (body !== undefined) headers['Content-Type'] = 'application/json';
      const response = await fetch(`/api/autenticacion/${route}`, {
        method, headers, body, credentials: 'omit', redirect: 'error', cache: 'no-store',
        mode: 'same-origin', referrerPolicy: 'no-referrer', signal: controller.signal
      });
      check(phase !== 'terminal');
      check(response.headers.get('cache-control') === 'no-store');
      // Bound allocation while streaming, not after response.text() has allocated it.
      let size = 0, text = '';
      const decoder = new TextDecoder('utf-8', { fatal: true });
      if (response.body) {
        reader = response.body.getReader();
        while (true) {
          const chunk = await reader.read();
          check(phase !== 'terminal' && !controller.signal.aborted);
          if (chunk.done) break;
          size += chunk.value.byteLength;
          check(size <= 16384);
          text += decoder.decode(chunk.value, { stream: true });
        }
      }
      text += decoder.decode();
      check(phase !== 'terminal' && !controller.signal.aborted);
      return { status: response.status, text,
        json: /^application\/json(?:\s*;|$)/i.test(response.headers.get('content-type') || ''),
        challenge: response.headers.get('www-authenticate') === 'Bearer' };
    } finally {
      controller.abort();
      clearTimeout(timer);
      if (reader) { try { await reader.cancel(); } catch {} reader.releaseLock(); }
      if (activeController === controller) activeController = null;
      // Cancellation may occur while stream cleanup yields; never return a late credential.
      check(phase !== 'terminal');
    }
  }
  async function passengerCheck() {
    const result = await request('GET', 'prueba-pasajero');
    check(result.status === 200 && result.json && identityOK(JSON.parse(result.text)));
  }
  function begin(expected) {
    if (phase !== expected) return false;
    phase = 'busy'; render();
    note('En curso. No recargar ni repetir.');
    return true;
  }
  control('login').addEventListener('click', async () => {
    if (!begin('ready')) return;
    let secret = password.value;
    password.value = ''; password.disabled = true;
    let body = '';
    try {
      check(secret && control('email').value === 'pasajero.demo@example.test');
      body = JSON.stringify({ correo: 'pasajero.demo@example.test', contrasena: secret });
      secret = '';
      const pending = request('POST', 'ingreso', body);
      body = '';
      const result = await pending;
      check(result.status === 200 && result.json);
      const value = JSON.parse(result.text);
      check(keysAre(value, ['identificadorUsuario', 'roles', 'token', 'venceEnUtc']) &&
        canonical(value.token) && canonical(value.identificadorUsuario) && value.token !== value.identificadorUsuario &&
        validRoles(value.roles) && typeof value.venceEnUtc === 'string' &&
        /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{6}Z$/.test(value.venceEnUtc) &&
        Number.isFinite(Date.parse(value.venceEnUtc)) && Date.parse(value.venceEnUtc) > Date.now());
      token = value.token; publicId = value.identificadorUsuario; roles = [...value.roles]; expiry = value.venceEnUtc;
      await passengerCheck();
      phase = 'pause1'; render();
      note('PAUSA 1: ingreso 200 y pasajero 200 verificados. Sin solicitudes automáticas. Detener y arrancar SOLO la API manualmente; confirmar en el terminal antes de continuar. Mantener pestaña abierta.');
    } catch { terminate(false); }
    finally { secret = ''; body = ''; }
  });
  control('resume1').addEventListener('click', async () => {
    if (!begin('pause1')) return;
    try {
      await passengerCheck();
      const result = await request('POST', 'salida');
      check(result.status === 204 && result.text === '');
      phase = 'pause2'; render();
      note('PAUSA 2: mismo token pasajero 200 y salida 204 verificados. Sin solicitudes automáticas. Detener y arrancar SOLO la API otra vez; confirmar en el terminal antes de continuar. Mantener pestaña abierta.');
    } catch { terminate(false); }
  });
  control('resume2').addEventListener('click', async () => {
    if (!begin('pause2')) return;
    try {
      const result = await request('GET', 'prueba-pasajero');
      check(result.status === 401 && result.text === '' && result.challenge);
      terminate(true);
    } catch { terminate(false); }
  });
  control('abandon').addEventListener('click', () => terminate(false));
  addEventListener('pagehide', () => terminate(false));
  addEventListener('beforeunload', () => terminate(false));
  render();
  note('Preparado. Sin solicitudes de autenticación. Esperando acción humana.');
  return { installed: true, temporaryDiagnostic: true, passwordMasked: password.type === 'password',
    passwordEmpty: password.value === '', authRequestsSent: 0 };
}
