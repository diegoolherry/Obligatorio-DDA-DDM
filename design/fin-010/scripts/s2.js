// Página "Sistema": tablero 2 (tipografía/espaciado/radios/sombras), 3 (botones y campos), 4 (insignias, tarjetas, avisos), 5 (navegación)
const BX = 1400;
function Board(name, x, w, title, sub) {
  const b = I(null, { type: "frame", name, x, y: 0, width: w, height: "fit_content", layout: "vertical", padding: 40, gap: 32, fill: F("surface-0") });
  const h = V(b, { gap: 4 });
  const row = H(h, { gap: 12 });
  T(row, "Berrutti", { s: 20, w: 800, ls: -0.5, c: "ink-900", lh: 1.1 });
  const tg = H(row, { width: "fit_content", height: 24, padding: [0, 8], cornerRadius: 12, fill: F("accent-500") });
  T(tg, "Sistema de diseño", { s: 12, w: 700, c: "ink-900", lh: 1.2 });
  T(h, title, { s: 32, w: 800, lh: 1.15, fill: true });
  if (sub) T(h, sub, { s: 16, c: "ink-700", fill: true });
  I(b, { type: "rectangle", name: "Franja naranja", width: "fill_container", height: 4, fill: F("accent-500") });
  return b;
}
function Sec(p, t, sub) { const g = V(p, { gap: 16 }); T(g, t, { s: 20, w: 700, lh: 1.2 }); if (sub) T(g, sub, { s: 14, c: "ink-500", fill: true }); return g; }
function Cap(p, t) { return T(p, t, { s: 12, w: 600, c: "ink-500", lh: 1.3 }); }

// ---- Tablero 2 ----
const b2 = Board("S2 Tipografía, espaciado, radios y sombras", 1 * BX, 1360, "Tipografía, espaciado, radios y sombras", "Inter como fuente principal (en Mobile se puede usar la fuente del sistema como alternativa). Espaciado base 8, subdivisión 4.");
const ty = Sec(b2, "Escala tipográfica", "Texto grande WCAG: ≥24 px, o ≥18,66 px en negrita. Los pares de tamaño normal exigen 4,5:1.");
const TS = [
  ["text-display", "32 / 38 · 800", "Berrutti", { s: 32, w: 800, ls: -0.5 }, "Grande"],
  ["text-title", "24 / 29 · 700", "Buscar viaje", { s: 24, w: 700 }, "Grande"],
  ["text-heading", "20 / 25 · 700", "Ombúes de Lavalle a Colonia", { s: 20, w: 700 }, "Grande (negrita)"],
  ["text-subtitle", "18 / 23 · 700", "Resultados · 2 opciones", { s: 18, w: 700 }, "Normal (4,5:1)"],
  ["text-body", "16 / 22 · 400 / 600", "Elegí origen, destino y fecha.", { s: 16, w: 400 }, "Normal (4,5:1)"],
  ["text-body-sm", "14 / 20 · 400 / 600", "Última actualización 12/11 14:30", { s: 14, w: 400 }, "Normal (4,5:1)"],
  ["text-caption", "12 / 16 · 600", "SERVICIO · FECHA", { s: 12, w: 600 }, "Normal (4,5:1)"],
];
TS.forEach(([tk, spec, sample, o, big]) => {
  const r = H(ty, { gap: 24, height: 52 });
  const l = V(r, { width: 260, gap: 2 }); T(l, tk, { s: 14, w: 700 }); Cap(l, spec);
  T(r, sample, { s: o.s, w: o.w, ls: o.ls, fill: true, lh: 1.2 });
  T(r, big, { s: 13, w: 600, c: "ink-700", width: 160, a: "right" });
  I(ty, { type: "rectangle", name: "Separador", width: "fill_container", height: 1, fill: F("border-200") });
});
const sp = Sec(b2, "Espaciado", "Equivale a la escala de Tailwind (p-1 = 4). Márgenes de pantalla 16; separación entre bloques 16–24.");
const spr = H(sp, { gap: 24, alignItems: "end" });
[["1", 4], ["2", 8], ["3", 12], ["4", 16], ["5", 20], ["6", 24], ["8", 32], ["10", 40], ["12", 48]].forEach(([k, v]) => {
  const c = V(spr, { width: "fit_content", gap: 8, alignItems: "center" });
  I(c, { type: "rectangle", name: "spacing-" + k, width: v, height: v, fill: F("primary-600") });
  T(c, "spacing-" + k, { s: 12, w: 700, lh: 1.2 }); Cap(c, v + " px");
});
const rd = Sec(b2, "Radios");
const rdr = H(rd, { gap: 24, alignItems: "end" });
[["radius-sm", 8], ["radius-md", 12], ["radius-lg", 16], ["radius-xl", 24], ["radius-full", 40]].forEach(([k, v]) => {
  const c = V(rdr, { width: "fit_content", gap: 8, alignItems: "center" });
  I(c, { type: "rectangle", name: k, width: 80, height: 80, cornerRadius: v, fill: F("surface-100"), stroke: ST("ink-300", 2) });
  T(c, k, { s: 12, w: 700, lh: 1.2 }); Cap(c, k === "radius-full" ? "999 px (píldora)" : v + " px");
});
const sh = Sec(b2, "Sombras", "Sutiles; la jerarquía se logra primero con espacio y contraste.");
const shr = H(sh, { gap: 32, alignItems: "start", padding: [8, 8, 16, 8] });
[["shadow-sm", "0 1 3 · 8 %", [{ type: "shadow", offsetX: 0, offsetY: 1, blur: 3, spread: 0, color: "rgba(20,20,20,0.08)" }]],
 ["shadow-md", "0 4 12 · 12 %", [{ type: "shadow", offsetX: 0, offsetY: 4, blur: 12, spread: 0, color: "rgba(20,20,20,0.12)" }]],
 ["shadow-lg", "0 8 24 −4 · 18 %", [{ type: "shadow", offsetX: 0, offsetY: 8, blur: 24, spread: -4, color: "rgba(20,20,20,0.18)" }]]].forEach(([k, spec, ef]) => {
  const c = V(shr, { width: 200, padding: 16, gap: 4, cornerRadius: 16, fill: F("surface-0"), stroke: ST("border-200", 1), effects: ef });
  T(c, k, { s: 14, w: 700 }); Cap(c, spec);
});

// ---- Tablero 3: botones y campos ----
const b3 = Board("S3 Botones y campos", 2 * BX, 1360, "Botones y campos de texto", "Alto mínimo 48 px (botones 52). Cada estado cambia más que el color: texto, icono o borde.");
const bs = Sec(b3, "Botones", "Primario = acción principal; secundario = alternativa; acento = escanear; destructivo = acciones irreversibles (con confirmación).");
const states = [["default", "Por defecto"], ["pressed", "Presionado"], ["disabled", "Deshabilitado"], ["loading", "Cargando"]];
const hdr = H(bs, { gap: 24, alignItems: "end", padding: [0, 0, 0, 144] });
states.forEach(([k, lb]) => { const c = V(hdr, { width: "fill_container" }); Cap(c, lb); });
const BV = [["primary", "Primario", "Ingresar", "log-in", "Ingresando…"], ["secondary", "Secundario", "Ver otro tramo", "arrow-right-left", "Cargando…"], ["accent", "Acento", "Escanear", "scan-line", "Escaneando…"], ["destructive", "Destructivo", "Cerrar sesión", "log-out", "Cerrando…"]];
BV.forEach(([v, lb, txt, ic, ltxt]) => {
  const r = H(bs, { gap: 24 });
  const l = V(r, { width: 120 }); T(l, lb, { s: 14, w: 700 }); Cap(l, "button-" + v);
  states.forEach(([st]) => { const c = V(r, { width: "fill_container" }); Btn(c, st === "loading" ? ltxt : txt, { v, st, icon: ic }); });
});
const fs = Sec(b3, "Campo de texto", "Etiqueta persistente arriba, ayuda o error debajo. El error lleva icono y texto, no solo borde rojo. Los valores se conservan tras un error.");
const fr = H(fs, { gap: 24, alignItems: "start" });
const cell = () => V(fr, { width: "fill_container", gap: 8 });
let c = cell(); Cap(c, "Por defecto"); Field(c, "Correo", "", { placeholder: "nombre@ejemplo.com", icon: "mail", help: "Usá el correo de tu cuenta ficticia." });
c = cell(); Cap(c, "Enfocado"); Field(c, "Correo", "lucia.pereyra@ejemplo.com", { st: "focus", icon: "mail" });
c = cell(); Cap(c, "Error con mensaje"); Field(c, "Contraseña", "••••••••", { st: "error", icon: "lock", error: "La contraseña no coincide con la cuenta." });
c = cell(); Cap(c, "Deshabilitado"); Field(c, "Servicio", "07:00 · Ombúes de Lavalle", { st: "disabled", icon: "bus" });

// ---- Tablero 4: insignias, tarjetas, avisos ----
const b4 = Board("S4 Insignias, tarjetas y avisos", 3 * BX, 1360, "Insignias, tarjetas y avisos", "El estado se comunica con texto + icono + color, nunca solo con color.");
const bd = Sec(b4, "Insignias de estado");
const bdr = H(bd, { gap: 16 });
["valido", "utilizado", "vencido", "pendiente", "offline"].forEach((k) => Badge(bdr, k));
const cs = Sec(b4, "Tarjetas", "Ticket (un tramo, un QR) e itinerario (directo y con transbordo).");
const csr = H(cs, { gap: 24, alignItems: "start" });
let w = V(csr, { width: "fill_container" });
TicketCard(w, { from: "Ombúes de Lavalle", to: "Radial", st: "valido", serv: "07:00", fecha: "Vie 13/11/2026" });
w = V(csr, { width: "fill_container" });
ItinCard(w, { kind: "Directo", precio: "$ 320", legs: [{ h1: "06:15", h2: "08:20", a: "Ombúes de Lavalle", b: "Colonia" }], dur: "2 h 05 min · sin transbordos" });
w = V(csr, { width: "fill_container" });
ItinCard(w, { kind: "2 tramos", precio: "$ 280", trans: "Transbordo en Radial · 15 min de espera", legs: [{ h1: "07:00", h2: "07:40", a: "Ombúes de Lavalle", b: "Radial" }, { h1: "07:55", h2: "08:45", a: "Radial", b: "Colonia" }], dur: "1 h 45 min en total · 2 tickets" });
const bn = Sec(b4, "Avisos", "Error con reintentar, sin conexión, información desactualizada e informativo. Reintentar mide 48 px de alto.");
const bnr = H(bn, { gap: 24, alignItems: "start" });
w = V(bnr, { width: "fill_container" }); Cap(w, "Error con reintentar"); Banner(w, "error", "No se pudo cargar tus tickets", "Revisá tu conexión e intentá de nuevo.", { action: "Reintentar" });
w = V(bnr, { width: "fill_container" }); Cap(w, "Sin conexión"); Banner(w, "offline", "Sin conexión", "Mostramos datos guardados. Validar y comprar requieren conexión.");
w = V(bnr, { width: "fill_container" }); Cap(w, "Información desactualizada"); Banner(w, "stale", "Información desactualizada", "Última actualización 07:04 · hace 18 min.");
w = V(bnr, { width: "fill_container" }); Cap(w, "Informativo"); Banner(w, "info", "Horarios sujetos a cambios", "Confirmá el servicio antes de viajar.");

// ---- Tablero 5: navegación ----
const b5 = Board("S5 Navegación y encabezado", 4 * BX, 1360, "Encabezado y barras de pestañas", "Pestañas con etiqueta persistente; la activa lleva píldora naranja con ícono ink-900 y etiqueta en negrita. Objetivos ≥48 px; áreas seguras 44 px arriba y 34 px abajo.");
const nr = H(b5, { gap: 40, alignItems: "start" });
const n1 = V(nr, { width: 375, gap: 12, name: "Encabezados" });
Cap(n1, "Encabezado raíz (wordmark Berrutti)"); let hh = V(n1, { gap: 0 }); StatusBar(hh); Header(hh);
Cap(n1, "Encabezado de detalle (volver + título)"); hh = V(n1, { gap: 0 }); StatusBar(hh); Header(hh, { back: true, title: "Ticket" });
const n2 = V(nr, { width: 375, gap: 12, name: "Barras de pestañas" });
Cap(n2, "Pasajero · Buscar activa"); TabBar(n2, TABS_P, 0);
Cap(n2, "Pasajero · Tickets activa"); TabBar(n2, TABS_P, 1);
Cap(n2, "Cobrador · Validar activa"); TabBar(n2, TABS_C, 0);
const n3 = V(nr, { width: "fill_container", gap: 12, name: "Reglas" });
T(n3, "Reglas de uso", { s: 20, w: 700 });
["Wordmark \"Berrutti\" en texto, Inter 800: sin logo ni fotografías.", "Encabezado ink-900 con franja naranja de 4 px.", "El encabezado solo lleva wordmark o título y flecha de volver.", "Pestañas: 56 px de alto, cada una ≥75 × 56 px.", "Estado activo: píldora naranja + etiqueta en negrita + icono, no solo color.", "Respetar áreas seguras: 44 px superior, 34 px inferior.", "Pasajero: Buscar · Tickets · Abonos · Servicio · Cuenta. Cobrador: Validar · Cuenta."].forEach((t) => {
  const r = H(n3, { gap: 10, alignItems: "start" }); IC(r, "check", 18, "success-700"); T(r, t, { s: 14, fill: true, lh: 1.4 });
});
