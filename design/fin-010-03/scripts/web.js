// Helpers Web (se concatenan después de prelude.js). Mismos tokens y componentes que el sistema Mobile.
const NAVL = { inicio: "Inicio", buscar: "Buscar viaje", horarios: "Horarios", ubic: "Ubicación del servicio", admin: "Administración operativa" };
function navItems(auth) {
  const a = ["inicio", "buscar", "horarios"];
  if (auth === "pasajero" || auth === "admin") a.push("ubic");
  if (auth === "admin") a.push("admin");
  return a;
}
function Wordmark(p, s) { return T(p, "Berrutti", { s: s || 24, w: 800, c: "surface-0", lh: 1.1, ls: -0.5, name: "Wordmark Berrutti" }); }
function SalirBtn(p, o = {}) {
  const b = H(p, Object.assign({ type: "rectangle", role: "button", name: "Boton Salir", width: 100, height: 44, padding: [0, 16], gap: 8, cornerRadius: 12, justifyContent: "center", stroke: ST("surface-0", 2) }, o));
  IC(b, "log-out", 20, "surface-0"); T(b, "Salir", { s: 16, w: 600, c: "surface-0", lh: 1.2 });
  return b;
}
// Barra superior de escritorio. active = clave de página; auth = visitante | pasajero | admin; focus = clave con foco de teclado
function NavDesktop(p, active, auth, focus) {
  const bar = H(p, { role: "navbar", name: "Barra de navegación " + auth, height: 64, padding: [0, 80], gap: 28, fill: F("ink-900") });
  Wordmark(bar);
  const links = H(bar, { role: "nav-links", name: "Enlaces", gap: 4 });
  navItems(auth).forEach((k) => {
    const on = k === active, f = k === focus;
    const o = { role: "nav-link", name: "Enlace " + NAVL[k] + (on ? " activo" : "") + (f ? " con foco" : ""), width: "fit_content", height: 44, padding: [0, 14], cornerRadius: 12, justifyContent: "center", fill: on ? F("accent-500") : [] };
    if (f) o.stroke = ST("surface-0", 2);
    const l = H(links, o);
    T(l, NAVL[k], { s: 16, w: on ? 700 : 600, c: on ? "ink-900" : "surface-0", lh: 1.2 });
  });
  if (auth === "visitante") Btn(bar, "Ingresar", { width: 124, h: 44, icon: "log-in" });
  else SalirBtn(bar);
  I(p, { type: "rectangle", name: "Franja naranja", width: "fill_container", height: 4, fill: F("accent-500") });
  return bar;
}
// Barra superior angosta: wordmark + botón Menú (alterna la lista de enlaces)
function NavNarrow(p, active, auth, open) {
  const bar = H(p, { role: "navbar", name: "Barra de navegación angosta", height: 56, padding: [0, 8, 0, 16], fill: F("ink-900") });
  Wordmark(bar, 22);
  I(bar, { type: "frame", name: "Espaciador", width: "fill_container", height: 1 });
  const m = H(bar, { type: "rectangle", role: "button", name: "Boton Menú", width: "fit_content", height: 44, padding: [0, 14], gap: 8, cornerRadius: 12, justifyContent: "center", stroke: ST("surface-0", 2) });
  IC(m, open ? "x" : "menu", 20, "surface-0"); T(m, open ? "Cerrar" : "Menú", { s: 16, w: 600, c: "surface-0", lh: 1.2 });
  I(p, { type: "rectangle", name: "Franja naranja", width: "fill_container", height: 4, fill: F("accent-500") });
  if (open) {
    const list = V(p, { name: "Menú desplegado", fill: F("ink-900"), padding: [8, 16, 16, 16], gap: 4 });
    navItems(auth).forEach((k) => {
      const on = k === active;
      const l = H(list, { role: "nav-link", name: "Enlace " + NAVL[k] + (on ? " activo" : ""), height: 52, padding: [0, 14], cornerRadius: 12, fill: on ? F("accent-500") : [] });
      T(l, NAVL[k], { s: 16, w: on ? 700 : 600, c: on ? "ink-900" : "surface-0", fill: true, lh: 1.2 });
    });
    if (auth === "visitante") { const b = Btn(list, "Ingresar", { h: 48, icon: "log-in" }); } else SalirBtn(list, { width: "fill_container", height: 48 });
    I(p, { type: "rectangle", name: "Borde menú", width: "fill_container", height: 1, fill: F("ink-700") });
  }
  return bar;
}
function Footer(p, narrow) {
  const f = (narrow ? V : H)(p, { role: "footer", name: "Pie de página", padding: narrow ? [24, 16] : [24, 80], gap: narrow ? 8 : 24, justifyContent: "space_between", fill: F("ink-900"), alignItems: narrow ? "start" : "center" });
  Wordmark(f, 18);
  T(f, "Los horarios provienen de la tabla de invierno 2026 publicada por la empresa.", { s: 14, c: "surface-100", lh: 1.4, fill: narrow, name: "Texto del pie" });
  return f;
}
function DeskPage(name, x, active, auth, focus) {
  const root = I(null, { type: "frame", name, x, y: 0, width: 1280, height: "fit_content", layout: "vertical", fill: F("surface-50") });
  NavDesktop(root, active, auth, focus);
  const main = V(root, { name: "Contenido", padding: [40, 80, 64, 80], gap: 32 });
  return { root, main };
}
function NarPage(name, x, active, auth, open) {
  const root = I(null, { type: "frame", name, x, y: 0, width: 390, height: "fit_content", layout: "vertical", fill: F("surface-50") });
  NavNarrow(root, active, auth, open);
  const main = V(root, { name: "Contenido", padding: [24, 16, 32, 16], gap: 24 });
  return { root, main };
}
function Card(p, o = {}) {
  return V(p, Object.assign({ role: "card", name: "Tarjeta", padding: 24, gap: 16, cornerRadius: 16, fill: F("surface-0"), stroke: ST("border-200", 1), effects: SH.sm }, o));
}
function Title(p, text, sub, o = {}) {
  const g = V(p, { name: "Encabezado de página", gap: 8 });
  T(g, text, { s: o.s || 32, w: 800, lh: o.s ? 1.1 : 1.19, ls: o.s ? -1 : -0.5, fill: true, name: "H1" });
  if (sub) T(g, sub, { s: o.ss || 16, c: "ink-700", fill: true, lh: 1.4, name: "Subtítulo" });
  return g;
}
function H2(p, text) { return T(p, text, { s: 24, w: 700, lh: 1.2, fill: true, name: "H2" }); }
function FW(p, w, label, value, o = {}) { const wr = V(p, { name: "Columna " + label, width: w }); Field(wr, label, value, o); return wr; }
function Tag(p, text, kind) {
  const c = { n: ["surface-100", "ink-700"], a: ["accent-100", "accent-700"], g: ["success-100", "success-700"], w: ["warning-100", "warning-800"] }[kind || "n"];
  const t = H(p, { name: "Etiqueta " + text, width: "fit_content", height: 24, padding: [0, 8], cornerRadius: 8, fill: F(c[0]), justifyContent: "center" });
  T(t, text, { s: 13, w: 700, c: c[1], lh: 1.2 });
  return t;
}
function LinkRow(p, text, o = {}) {
  const a = H(p, { name: "Enlace " + text, width: "fit_content", height: 48, gap: 6 });
  T(a, text, { s: 15, w: 700, c: "primary-700", lh: 1.2 });
  IC(a, o.icon || "chevron-right", 20, "primary-700");
  return a;
}
function Sep(p) { return I(p, { type: "rectangle", name: "Separador", width: "fill_container", height: 1, fill: F("border-200") }); }
// Caja de estado: carga, vacío, error, aviso, éxito (icono + texto + color)
function StateBox(p, kind, title, body, action) {
  const cfg = { load: ["info-100", "info-700", "loader"], empty: ["surface-100", "ink-700", "search-x"], error: ["error-100", "error-700", "circle-alert"], warn: ["warning-100", "warning-800", "clock"], ok: ["success-100", "success-700", "circle-check"], info: ["info-100", "info-700", "info"] }[kind];
  const b = H(p, { name: "Estado " + kind, alignItems: "start", padding: 16, gap: 12, cornerRadius: 12, fill: F(cfg[0]) });
  IC(b, cfg[2], 22, cfg[1]);
  const col = V(b, { gap: 4 });
  T(col, title, { s: 16, w: 700, c: cfg[1], fill: true, lh: 1.3 });
  if (body) T(col, body, { s: 14, c: cfg[1], fill: true, lh: 1.4 });
  if (action) {
    const a = H(col, { type: "rectangle", role: "button", name: "Boton " + action, width: "fit_content", height: 48, padding: [0, 16], gap: 8, cornerRadius: 12, justifyContent: "center", fill: F("surface-0"), stroke: ST(cfg[1], 2) });
    if (action === "Reintentar") IC(a, "refresh-cw", 18, cfg[1]);
    T(a, action, { s: 15, w: 700, c: cfg[1], lh: 1.2 });
  }
  return b;
}
// Fila de salida (Horarios): hora publicada, ruta, recorrido, códigos y nota
function SalidaRow(p, hora, ruta, rec, cod, nota) {
  const r = H(p, { name: "Salida " + hora + " " + ruta, alignItems: "start", gap: 16, padding: [12, 0] });
  T(r, hora, { s: 20, w: 700, lh: 1.2, width: 64 });
  const m = V(r, { gap: 2 });
  T(m, ruta, { s: 16, w: 600, lh: 1.3, fill: true });
  T(m, rec, { s: 14, c: "ink-700", lh: 1.4, fill: true });
  if (nota) T(m, nota, { s: 14, c: "ink-500", lh: 1.4, fill: true });
  if (cod) { const t = V(r, { width: "fit_content", gap: 0 }); Tag(t, cod, "n"); }
  return r;
}
// Tarjeta de itinerario de escritorio: resumen a la izquierda, tramos a la derecha
function ItinDesk(p, d) {
  const direct = d.legs.length === 1;
  const c = H(p, { role: "card", name: "Tarjeta itinerario " + d.kind, alignItems: "start", padding: 24, gap: 32, cornerRadius: 16, fill: F("surface-0"), stroke: ST("border-200", 1), effects: SH.sm });
  const l = V(c, { name: "Resumen", width: 200, gap: 8 });
  const tag = H(l, { width: "fit_content", height: 28, padding: [0, 10], gap: 6, cornerRadius: 14, fill: F(direct ? "success-100" : "accent-100") });
  IC(tag, direct ? "route" : "repeat", 16, direct ? "success-700" : "accent-700");
  T(tag, d.kind, { s: 13, w: 700, c: direct ? "success-700" : "accent-700", lh: 1.2 });
  T(l, d.total, { s: 28, w: 800, lh: 1.1 });
  T(l, "Total del itinerario", { s: 14, c: "ink-500", fill: true });
  const f = H(l, { gap: 8 }); IC(f, "timer", 16, "ink-500"); T(f, d.dur, { s: 14, c: "ink-500", fill: true, lh: 1.2 });
  I(c, { type: "rectangle", name: "Separador vertical", width: 1, height: d.legs.length > 1 ? 196 : 100, fill: F("border-200") });
  const r = V(c, { name: "Tramos", gap: 16 });
  d.legs.forEach((leg, i) => {
    const row = H(r, { name: "Tramo " + leg.n, gap: 16, alignItems: "start" });
    const tm = V(row, { width: 56, gap: 20 }); T(tm, leg.h1, { s: 18, w: 700, lh: 1.2 }); T(tm, leg.h2, { s: 18, w: 700, lh: 1.2 });
    const dot = V(row, { width: 12, height: 56, alignItems: "center", gap: 0 });
    I(dot, { type: "ellipse", width: 12, height: 12, fill: F("ink-900") });
    I(dot, { type: "rectangle", width: 2, height: 32, fill: F("ink-300") });
    I(dot, { type: "ellipse", width: 12, height: 12, fill: F("surface-0"), stroke: ST("ink-900", 2) });
    const nm = V(row, { width: 280, gap: 20 }); T(nm, leg.a, { s: 16, w: 600, fill: true, lh: 1.2 }); T(nm, leg.b, { s: 16, w: 600, fill: true, lh: 1.2 });
    const mt = V(row, { gap: 4 });
    T(mt, "Tramo " + leg.n + " · " + leg.imp, { s: 15, w: 700, lh: 1.3, fill: true });
    T(mt, "Asiento: " + leg.asiento, { s: 14, c: "ink-700", lh: 1.3, fill: true });
    if (i < d.legs.length - 1) {
      const tr = H(r, { gap: 8, padding: [8, 12], cornerRadius: 8, fill: F("surface-50"), name: "Transbordo" });
      IC(tr, "clock", 16, "ink-700"); T(tr, d.trans, { s: 14, w: 500, c: "ink-700", fill: true, lh: 1.3 });
    }
  });
  return c;
}
// Datos de la maqueta (horarios del catálogo; importes e itinerario de 2 tramos son de ejemplo)
const ITIN_2 = { kind: "2 tramos", total: "$ 170,00", dur: "1 h 50 min en total", trans: "Transbordo en Radial de Conchillas: 25 min de espera",
  legs: [{ n: 1, h1: "07:30", h2: "08:05", a: "Carmelo", b: "Radial de Conchillas", imp: "$ 90,00", asiento: "sin asiento asignado" },
         { n: 2, h1: "08:30", h2: "09:20", a: "Radial de Conchillas", b: "Colonia", imp: "$ 80,00", asiento: "asignación automática" }] };
const ITIN_1 = { kind: "Directo", total: "$ 190,00", dur: "1 h 25 min · sin transbordos",
  legs: [{ n: 1, h1: "10:15", h2: "11:40", a: "Carmelo", b: "Colonia", imp: "$ 190,00", asiento: "asignación automática" }] };
