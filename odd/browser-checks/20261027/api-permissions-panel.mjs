const task = await taskSpace(3);
const page = task.page('p1');
console.log(await page.evaluate(() => {
  if (location.origin !== 'https://localhost:7053' || location.protocol !== 'https:') return {installed:false, blocker:'Origen HTTPS inesperado'};
  let openApiOK = false;
  try { const d = JSON.parse(document.body.innerText); openApiOK = typeof d.openapi === 'string' && !!d.paths; } catch {}
  if (!openApiOK) return {installed:false, blocker:'Documento OpenAPI no confirmado'};
  document.title = 'DIAGNOSTICO TEMPORAL LOCAL - CTC API';
  document.documentElement.lang = 'es';
  document.body.innerHTML = `<main style="max-width:950px;margin:32px auto;font:18px system-ui;line-height:1.5;padding:24px;color:#17202a;background:#fff">
    <h1>PANEL DIAGNOSTICO TEMPORAL LOCAL</h1>
    <p><strong>No es una pantalla ni una funcionalidad de la aplicación.</strong> Está instalado únicamente en esta pestaña de https://localhost:7053. No modifica archivos del proyecto.</p>
    <p>Una sola ejecución: sin sesión 401; ingreso privado; identidad y pasajero 200; administrador 403; salida 204; mismo token anterior 401. El ingreso crea una sesión y la salida intenta revocarla. No se realizan otras operaciones de datos.</p>
    <p>La contraseña y el token permanecen en variables locales del manejador de este botón, sin almacenamiento persistente ni impresión. No abras herramientas de red ni compartas credenciales. El campo se vacía al pulsar.</p>
    <p><label for="diag-email">Cuenta ficticia local</label><br><input id="diag-email" type="email" value="pasajero.demo@example.test" readonly autocomplete="off" style="width:360px;max-width:95%;font:inherit;padding:10px"></p>
    <p><label for="diag-password">Contraseña del pasajero (escribir privadamente)</label><br><input id="diag-password" type="password" autocomplete="off" spellcheck="false" style="width:360px;max-width:95%;font:inherit;padding:10px"></p>
    <button id="diag-run" type="button" style="font:inherit;padding:16px;background:#164b83;color:white">Ejecutar matriz una sola vez</button>
    <section aria-labelledby="diag-results-title"><h2 id="diag-results-title">Resultados sanitizados</h2><div id="diag-results" role="status" aria-live="polite"><p>Preparado. Ninguna solicitud de autenticación enviada. Esperando acción humana.</p></div></section>
    <p><strong>Límites:</strong> no comprueba durabilidad tras reinicio, vencimiento exacto, otras sesiones, DB/contadores sin cambios, Mobile/iPhone ni revisión académica. No recargar, volver a pulsar ni reintentar si hay un error; avisar al agente.</p>
  </main>`;
  const button = document.getElementById('diag-run');
  button.addEventListener('click', async function () {
    if (button.disabled) return;
    button.disabled = true;
    button.textContent = 'Ejecución iniciada: no repetir';
    const passwordInput = document.getElementById('diag-password');
    let contrasena = passwordInput.value;
    passwordInput.value = '';
    passwordInput.disabled = true;
    let correo = document.getElementById('diag-email').value;
    const results = document.getElementById('diag-results');
    results.replaceChildren();
    let token = '', publicId = '', roles = [], loginBody = '';
    let loginAttempted = false, logoutAttempted = false, logoutConfirmed = false;
    const note = (text) => { const p = document.createElement('p'); p.textContent = text; results.appendChild(p); };
    const yes = (b) => b ? 'sí' : 'no';
    const cacheOK = (r) => r.headers.get('cache-control') === 'no-store';
    const challengeOK = (r) => r.headers.get('www-authenticate') === 'Bearer';
    const jsonHeaderOK = (r) => /^application\/json(?:\s*;|$)/i.test(r.headers.get('content-type') || '');
    const keysAre = (o, expected) => !!o && typeof o === 'object' && !Array.isArray(o) && Object.keys(o).sort().join('|') === [...expected].sort().join('|');
    const canonical = (v) => {
      if (typeof v !== 'string' || !/^[A-Za-z0-9_-]{43}$/.test(v)) return false;
      try { const s = atob(v.replace(/-/g,'+').replace(/_/g,'/') + '='); return s.length === 32 && btoa(s).replace(/=+$/,'').replace(/\+/g,'-').replace(/\//g,'_') === v; } catch { return false; }
    };
    const validRoles = (v) => Array.isArray(v) && v.length > 0 && v.every(x => ['pasajero','cobrador','administrador'].includes(x)) && new Set(v).size === v.length && v.includes('pasajero');
    const parseJSON = (s) => { try { return JSON.parse(s); } catch { return null; } };
    const request = async (method, route, bearer = '', body) => {
      const controller = new AbortController();
      const timer = setTimeout(() => controller.abort(), 8000);
      try {
        const headers = {Accept:'application/json'};
        if (bearer) headers.Authorization = 'Bearer ' + bearer;
        if (body !== undefined) headers['Content-Type'] = 'application/json';
        const response = await fetch(route, {method, headers, body, credentials:'omit', redirect:'error', cache:'no-store', mode:'same-origin', referrerPolicy:'no-referrer', signal:controller.signal});
        const text = await response.text();
        if (text.length > 16384) throw new Error('STOP');
        return {status:response.status, headers:response.headers, text};
      } finally { clearTimeout(timer); }
    };
    const row = (method, route, r, expectedStatus, bodyOK, extraOK = true, extraLabel = '') => {
      const pass = r.status === expectedStatus && bodyOK && cacheOK(r) && extraOK;
      note(method + ' ' + route + ' → ' + r.status + ' | estado esperado ' + expectedStatus + ': ' + yes(r.status === expectedStatus) + ' | cuerpo esperado: ' + yes(bodyOK) + ' | no-store: ' + yes(cacheOK(r)) + (extraLabel ? ' | ' + extraLabel + ': ' + yes(extraOK) : '') + ' | ' + (pass ? 'PASS' : 'FALLO'));
      return pass;
    };
    const logout = async (cleanup) => {
      logoutAttempted = true;
      const r = await request('POST', '/api/autenticacion/salida', token);
      logoutConfirmed = row('POST', '/api/autenticacion/salida', r, 204, r.text === '');
      if (cleanup) note(logoutConfirmed ? 'Limpieza única confirmada por respuesta 204; no se continuó la matriz.' : 'Limpieza única no confirmada; revocación incierta. No repetir.');
      return logoutConfirmed;
    };
    const identityOK = (d) => keysAre(d,['identificadorUsuario','roles']) && d.identificadorUsuario === publicId && validRoles(d.roles) && [...d.roles].sort().join('|') === [...roles].sort().join('|');
    try {
      if (!contrasena || correo !== 'pasajero.demo@example.test' || location.origin !== 'https://localhost:7053') { note('DETENIDO: falta contraseña o contexto local inválido. No se enviaron solicitudes.'); return; }
      note('Ejecutando una sola secuencia. No recargar ni repetir.');
      let r = await request('GET','/api/autenticacion/prueba-pasajero');
      if (!row('GET','/api/autenticacion/prueba-pasajero',r,401,r.text === '',challengeOK(r),'challenge Bearer')) throw new Error('STOP');
      loginBody = JSON.stringify({correo,contrasena});
      contrasena = ''; correo = '';
      loginAttempted = true;
      const pendingLogin = request('POST','/api/autenticacion/ingreso','',loginBody);
      loginBody = '';
      r = await pendingLogin;
      let d = r.status === 200 ? parseJSON(r.text) : null;
      if (d && canonical(d.token)) token = d.token;
      const loginOK = keysAre(d,['identificadorUsuario','roles','token','venceEnUtc']) && canonical(d.token) && canonical(d.identificadorUsuario) && d.identificadorUsuario !== d.token && validRoles(d.roles) && typeof d.venceEnUtc === 'string' && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{6}Z$/.test(d.venceEnUtc) && Number.isFinite(Date.parse(d.venceEnUtc)) && Date.parse(d.venceEnUtc) > Date.now();
      if (!row('POST','/api/autenticacion/ingreso',r,200,loginOK,jsonHeaderOK(r),'JSON')) throw new Error('STOP');
      publicId = d.identificadorUsuario; roles = [...d.roles];
      d = null; r = null;
      r = await request('GET','/api/autenticacion/identidad',token);
      if (!row('GET','/api/autenticacion/identidad',r,200,identityOK(parseJSON(r.text)),jsonHeaderOK(r),'JSON')) throw new Error('STOP');
      r = await request('GET','/api/autenticacion/prueba-pasajero',token);
      if (!row('GET','/api/autenticacion/prueba-pasajero',r,200,identityOK(parseJSON(r.text)),jsonHeaderOK(r),'JSON')) throw new Error('STOP');
      r = await request('GET','/api/autenticacion/prueba-administrador',token);
      if (!row('GET','/api/autenticacion/prueba-administrador',r,403,r.text === '')) throw new Error('STOP');
      if (!await logout(false)) throw new Error('STOP');
      r = await request('GET','/api/autenticacion/prueba-pasajero',token);
      if (!row('GET','/api/autenticacion/prueba-pasajero',r,401,r.text === '',challengeOK(r),'challenge Bearer')) throw new Error('STOP');
      note('MATRIZ COMPLETADA: sin sesión 401; pasajero 200; administrador 403; mismo token tras salida 401. Identidad 200 y salida 204 confirmadas. Token anterior usado sin nuevo ingreso.');
    } catch {
      note('DETENIDO: respuesta inesperada o fallo de transporte/tiempo. No se reintenta ni se muestran respuestas privadas. La matriz no se acredita completa.');
      if (token && !logoutAttempted) {
        try { await logout(true); } catch { note('Limpieza única intentada; respuesta no confirmada. Revocación incierta. No repetir.'); }
      } else if (loginAttempted && !token) {
        note('Ingreso no confirmado con token utilizable; una sesión podría existir. No es posible confirmar limpieza. No repetir.');
      } else if (logoutAttempted && !logoutConfirmed) {
        note('La salida ya se intentó una vez; no se repite. Revocación incierta.');
      }
    } finally {
      contrasena = ''; correo = ''; loginBody = ''; token = ''; publicId = ''; roles = [];
      passwordInput.value = '';
      button.textContent = 'Ejecución cerrada: no repetir';
      note('Campo de contraseña vaciado; botón bloqueado permanentemente en esta página. Avisá al agente sin compartir credenciales.');
    }
  });
  return {installed:true, temporaryDiagnostic:true, page:'p1', origin:location.origin, passwordMasked:document.getElementById('diag-password').type === 'password', passwordEmpty:document.getElementById('diag-password').value === '', buttonReady:!button.disabled, authRequestsSent:0};
}));
await task.handOff();
console.log({spaceId:task.spaceId,page:'p1',handedOff:true});
