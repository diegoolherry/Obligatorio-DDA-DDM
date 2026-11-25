// Pantallas 14-27 (MVP-005): estados de Mis tickets, ticket utilizado, selección de tramo,
// permiso de cámara, escáner, validando y rechazos vencido / tramo incorrecto / código inexistente.
const DX = 455, DY = 900;
function title(b, t, sub) { const g = V(b, { gap: 4 }); T(g, t, { s: 24, w: 700, lh: 1.2, fill: true }); if (sub) T(g, sub, { s: 14, c: "ink-500", fill: true }); return g; }
function kv(p, k, v) { const g = V(p, { width: "fill_container", gap: 2 }); T(g, k, { s: 12, w: 600, c: "ink-500" }); T(g, v, { s: 15, w: 600, fill: true, lh: 1.3 }); return g; }
const pos = (i) => [((i + 3) % 5) * DX, (2 + Math.floor((i + 3) / 5)) * DY]; // M14 = fila 2, columna 3

// ---- Pasajero ----
function Segmented(p, items, active) {
  const bar = H(p, { name: "Filtro de tickets", height: 56, padding: 4, gap: 4, cornerRadius: 14, fill: F("surface-100") });
  items.forEach((lb, i) => {
    const on = i === active;
    const s = H(bar, { role: "button", name: "Segmento " + lb + (on ? " activo" : ""), width: "fill_container", height: 48, cornerRadius: 10, justifyContent: "center",
      fill: on ? F("surface-0") : [], effects: on ? SH.sm : [] });
    T(s, lb, { s: 15, w: on ? 700 : 600, c: on ? "ink-900" : "ink-700", a: "center", lh: 1.2 });
  });
  return bar;
}
function ticketsHeader(b, active, counts = true) {
  title(b, "Mis tickets", "Un ticket por cada tramo.");
  Segmented(b, counts ? ["Vigentes (2)", "Anteriores (2)"] : ["Vigentes", "Anteriores"], active);
}
let [x, y] = pos(0);
let b = Screen("M14 Mis tickets - vigentes", x, y, { tabs: TABS_P, active: 1 });
ticketsHeader(b, 0);
TicketCard(b, { from: "Ombúes de Lavalle", to: "Radial", st: "valido", serv: "07:00", fecha: "Vie 13/11/2026" });
TicketCard(b, { from: "Radial", to: "Colonia", st: "valido", serv: "07:55", fecha: "Vie 13/11/2026" });

[x, y] = pos(1);
b = Screen("M15 Mis tickets - anteriores", x, y, { tabs: TABS_P, active: 1 });
ticketsHeader(b, 1);
TicketCard(b, { from: "Nueva Palmira", to: "Carmelo", st: "utilizado", serv: "09:15", fecha: "Mié 11/11/2026", cta: "Ver detalle" });
TicketCard(b, { from: "Colonia", to: "Rosario", st: "vencido", serv: "18:30", fecha: "Lun 09/11/2026", cta: "Ver detalle" });

function skeletonCard(p) {
  const c = V(p, { role: "card", name: "Tarjeta cargando", padding: 16, gap: 12, cornerRadius: 16, fill: F("surface-0"), stroke: ST("border-200", 1) });
  const top = H(c, { justifyContent: "space_between", gap: 8 });
  const t = V(top, { width: "fill_container", gap: 8 });
  I(t, { type: "rectangle", name: "Barra", width: 180, height: 16, cornerRadius: 8, fill: F("surface-100") });
  I(t, { type: "rectangle", name: "Barra", width: 120, height: 16, cornerRadius: 8, fill: F("surface-100") });
  I(top, { type: "rectangle", name: "Barra insignia", width: 84, height: 28, cornerRadius: 14, fill: F("surface-100") });
  I(c, { type: "rectangle", name: "Separador", width: "fill_container", height: 1, fill: F("border-200") });
  I(c, { type: "rectangle", name: "Barra", width: 220, height: 14, cornerRadius: 7, fill: F("surface-100") });
}
[x, y] = pos(2);
b = Screen("M16 Mis tickets - cargando", x, y, { tabs: TABS_P, active: 1 });
ticketsHeader(b, 0, false);
const ld = H(b, { gap: 8, name: "Estado de carga" });
IC(ld, "loader", 18, "ink-700"); T(ld, "Cargando tus tickets…", { s: 14, w: 500, c: "ink-700", fill: true });
skeletonCard(b); skeletonCard(b);

[x, y] = pos(3);
b = Screen("M17 Mis tickets - vacío", x, y, { tabs: TABS_P, active: 1 });
title(b, "Mis tickets", "Un ticket por cada tramo.");
Segmented(b, ["Vigentes (0)", "Anteriores (2)"], 0);
const em = V(b, { name: "Estado vacío", alignItems: "center", padding: [32, 16], gap: 12 });
const ei = H(em, { width: 72, height: 72, cornerRadius: 36, justifyContent: "center", fill: F("surface-100") });
IC(ei, "ticket", 32, "ink-700");
T(em, "No tenés tickets vigentes", { s: 18, w: 700, a: "center", fill: true, lh: 1.3 });
T(em, "Cuando compres un viaje, vas a ver acá un ticket por cada tramo.", { s: 15, c: "ink-500", a: "center", fill: true });
Btn(b, "Buscar viaje", { icon: "search" });

[x, y] = pos(4);
b = Screen("M18 Mis tickets - error", x, y, { tabs: TABS_P, active: 1 });
ticketsHeader(b, 0, false);
Banner(b, "error", "No pudimos cargar tus tickets", "Revisá tu conexión e intentá de nuevo. Tus tickets no se perdieron.", { action: "Reintentar" });

[x, y] = pos(5);
b = Screen("M19 Ticket - utilizado", x, y, { back: true, title: "Ticket", pad: [12, 16], gap: 12 });
let top = H(b, { justifyContent: "space_between", alignItems: "start", gap: 8 });
let t = V(top, { width: "fill_container", gap: 2 });
T(t, "Nueva Palmira", { s: 18, w: 700, fill: true, lh: 1.25 });
T(t, "a Carmelo", { s: 18, w: 700, fill: true, lh: 1.25 });
Badge(top, "utilizado");
let card = V(b, { role: "card", name: "Panel QR utilizado", alignItems: "center", padding: 16, gap: 12, cornerRadius: 16, fill: F("surface-0"), stroke: ST("border-200", 1), effects: SH.sm });
const dim = V(card, { name: "QR atenuado", width: "fit_content", alignItems: "center", opacity: 0.2 });
QR(dim, 196);
const used = H(card, { gap: 8, padding: [12, 12], cornerRadius: 12, fill: F("surface-100"), alignItems: "start" });
IC(used, "check-check", 20, "ink-700");
T(V(used, { width: "fill_container" }), "Se validó el 11/11 a las 09:17. Ya no sirve para viajar.", { s: 14, w: 500, c: "ink-700", fill: true });
let g = H(card, { gap: 12, alignItems: "start" });
kv(g, "Servicio", "09:15 · Nueva Palmira – Carmelo");
kv(g, "Fecha", "Mié 11/11/2026");

// ---- Cobrador ----
const TRAMO = "Ombúes de Lavalle a Radial", SERV = "07:00 · Ombúes de Lavalle – Colonia";
function context(p) {
  const c = H(p, { name: "Tramo elegido", gap: 8, padding: [10, 12], cornerRadius: 12, fill: F("surface-0"), stroke: ST("border-200", 1) });
  IC(c, "route", 18, "ink-700");
  const col = V(c, { width: "fill_container", gap: 0 });
  T(col, TRAMO, { s: 15, w: 700, fill: true, lh: 1.3 });
  T(col, SERV, { s: 13, c: "ink-500", fill: true, lh: 1.3 });
  return c;
}
[x, y] = pos(6);
b = Screen("M20 Cobrador - elegir tramo", x, y, { tabs: TABS_C, active: 0 });
title(b, "Validar ticket", "Elegí el tramo del servicio actual.");
Field(b, "Servicio", SERV, { icon: "bus", trail: "chevron-down" });
const lst = V(b, { name: "Lista de tramos", gap: 0, cornerRadius: 12, fill: F("surface-0"), stroke: ST("primary-600", 2), clipContent: true });
[["Ombúes de Lavalle a Radial", "07:00 – 07:40", true], ["Radial a Colonia", "07:40 – 08:45", false]].forEach(([n, h, on], i) => {
  if (i) I(lst, { type: "rectangle", name: "Separador", width: "fill_container", height: 1, fill: F("border-200") });
  const r = H(lst, { role: "button", name: "Opción " + n + (on ? " elegida" : ""), height: 64, padding: [0, 16], gap: 12, fill: on ? F("primary-50") : [] });
  const rd = H(r, { width: 22, height: 22, cornerRadius: 11, justifyContent: "center", stroke: ST(on ? "primary-700" : "ink-500", 2) });
  if (on) I(rd, { type: "ellipse", width: 10, height: 10, fill: F("primary-700") });
  const col = V(r, { width: "fill_container", gap: 0 });
  T(col, n, { s: 16, w: on ? 700 : 500, c: on ? "primary-700" : "ink-900", fill: true, lh: 1.3 });
  T(col, h, { s: 13, c: on ? "primary-700" : "ink-500", fill: true, lh: 1.3 });
});
T(b, "Solo se aceptan tickets de este tramo.", { s: 14, c: "ink-500", fill: true });

function cameraPanel(name, p, o) {
  const pn = V(p, { name, alignItems: "center", padding: [24, 20], gap: 12, cornerRadius: 16, fill: F("surface-0"), stroke: ST("border-200", 1), effects: SH.sm });
  const ci = H(pn, { width: 72, height: 72, cornerRadius: 36, justifyContent: "center", fill: F(o.bg) });
  IC(ci, o.icon, 32, o.fg);
  T(pn, o.t, { s: 20, w: 700, a: "center", fill: true, lh: 1.25 });
  T(pn, o.d, { s: 15, c: "ink-700", a: "center", fill: true });
  return pn;
}
[x, y] = pos(7);
b = Screen("M21 Cobrador - permiso de cámara", x, y, { tabs: TABS_C, active: 0 });
context(b);
cameraPanel("Explicación del permiso", b, { icon: "camera", bg: "info-100", fg: "info-700", t: "Para escanear, permití la cámara",
  d: "Se usa solo para leer el código QR del ticket. No se guardan fotos." });
Btn(b, "Continuar", { icon: "camera", h: 56 });
T(b, "Después, el teléfono te va a pedir confirmación.", { s: 14, c: "ink-500", a: "center", fill: true });

[x, y] = pos(8);
b = Screen("M22 Cobrador - cámara denegada", x, y, { tabs: TABS_C, active: 0 });
context(b);
cameraPanel("Permiso denegado", b, { icon: "camera-off", bg: "error-100", fg: "error-700", t: "La cámara está bloqueada",
  d: "Sin la cámara no se pueden leer los tickets. Activala en Ajustes > Berrutti > Cámara y volvé a esta pantalla." });
Btn(b, "Abrir Ajustes", { icon: "settings", h: 56 });
Btn(b, "Volver a intentar", { v: "secondary", icon: "refresh-cw", h: 56 });

[x, y] = pos(9);
b = Screen("M23 Cobrador - escaneando", x, y, { tabs: TABS_C, active: 0, gap: 12 });
context(b);
const vf = V(b, { name: "Visor de cámara (marcador de posición)", height: "fill_container", alignItems: "center", justifyContent: "center", gap: 16, cornerRadius: 16, fill: F("ink-900") });
const fr = H(vf, { name: "Marco de lectura", width: 230, height: 230, cornerRadius: 20, justifyContent: "center", stroke: ST("accent-500", 4) });
IC(fr, "qr-code", 48, "ink-500");
T(vf, "Apuntá al código QR del ticket", { s: 16, w: 600, c: "surface-0", a: "center", fill: true });
Btn(b, "Cancelar", { v: "secondary", h: 52 });

[x, y] = pos(10);
b = Screen("M24 Cobrador - validando", x, y, { tabs: TABS_C, active: 0, gap: 12 });
context(b);
const vw = V(b, { name: "Validando", height: "fill_container", alignItems: "center", justifyContent: "center", gap: 12, padding: [24, 20], cornerRadius: 16, fill: F("surface-0"), stroke: ST("border-200", 1) });
IC(vw, "loader", 48, "ink-700");
T(vw, "Validando…", { s: 24, w: 700, a: "center", fill: true, lh: 1.2 });
T(vw, "Esperando la confirmación del servidor. Todavía no dejes pasar al pasajero.", { s: 15, c: "ink-700", a: "center", fill: true });
Btn(b, "Validando…", { st: "loading", h: 56 });

function resultScreen(name, x, y, o) {
  const b = Screen(name, x, y, { tabs: TABS_C, active: 0, gap: 16 });
  const p = V(b, { name: "Resultado " + o.t, alignItems: "center", padding: [24, 20], gap: 12, cornerRadius: 16, fill: F(o.bg), stroke: ST(o.fg, 2) });
  IC(p, o.icon, 64, o.fg);
  T(p, o.t, { s: 28, w: 800, c: o.fg, a: "center", fill: true, lh: 1.15 });
  T(p, o.d, { s: 16, w: 500, c: o.fg, a: "center", fill: true, lh: 1.4 });
  const info = V(b, { padding: 16, gap: 12, cornerRadius: 12, fill: F("surface-0"), stroke: ST("border-200", 1) });
  kv(info, "Tramo", TRAMO);
  kv(info, "Servicio", "07:00 · Vie 13/11/2026");
  o.kv.forEach(([k, v]) => kv(info, k, v));
  Btn(b, "Escanear otro", { v: "accent", icon: "scan-line", h: 56 });
  return b;
}
[x, y] = pos(11);
resultScreen("M25 Cobrador - vencido", x, y, { bg: "error-100", fg: "error-700", icon: "calendar-x", t: "Vencido",
  d: "El ticket es de un servicio que ya pasó. No es válido para viajar.", kv: [["Servicio del ticket", "07:00 · Jue 12/11/2026"]] });
[x, y] = pos(12);
resultScreen("M26 Cobrador - tramo incorrecto", x, y, { bg: "error-100", fg: "error-700", icon: "route-off", t: "Tramo incorrecto",
  d: "Este ticket es de otro tramo. No es válido en este tramo.", kv: [["Tramo del ticket", "Radial a Colonia"], ["Ticket", "No se consumió · sigue vigente"]] });
[x, y] = pos(13);
resultScreen("M27 Cobrador - código inexistente", x, y, { bg: "error-100", fg: "error-700", icon: "search-x", t: "Código inexistente",
  d: "El código no corresponde a ningún ticket. No es válido para viajar.", kv: [["Qué hacer", "Pedí al pasajero que abra el ticket en su app"]] });
