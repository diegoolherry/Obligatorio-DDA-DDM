// Helpers compartidos (se concatenan antes de cada script). Colores = variables de berrutti.op
const F = (n) => [{ type: "solid", color: "$" + n }];
const ST = (n, t = 1) => ({ thickness: t, fill: F(n), align: "inside" });
const SH = {
  sm: [{ type: "shadow", offsetX: 0, offsetY: 1, blur: 3, spread: 0, color: "rgba(20,20,20,0.08)" }],
  md: [{ type: "shadow", offsetX: 0, offsetY: 4, blur: 12, spread: 0, color: "rgba(20,20,20,0.12)" }],
};
function T(p, content, o = {}) {
  const n = { type: "text", content, fontFamily: "Inter", fontSize: o.s || 16, fontWeight: o.w || 400,
    lineHeight: o.lh || 1.4, fill: F(o.c || "ink-900"), textAlign: o.a || "left" };
  if (o.fill) { n.width = "fill_container"; n.textGrowth = "fixed-width"; }
  if (o.width) { n.width = o.width; n.textGrowth = "fixed-width"; }
  if (o.name) n.name = o.name;
  if (o.ls !== undefined) n.letterSpacing = o.ls;
  return I(p, n);
}
function IC(p, name, size, color, extra = {}) {
  return I(p, Object.assign({ type: "icon_font", name: "Icono " + name, iconFontName: name, width: size, height: size, fill: F(color) }, extra));
}
function V(p, o = {}) { return I(p, Object.assign({ type: "frame", layout: "vertical", width: "fill_container", height: "fit_content" }, o)); }
function H(p, o = {}) { return I(p, Object.assign({ type: "frame", layout: "horizontal", width: "fill_container", height: "fit_content", alignItems: "center" }, o)); }

// ---- Botones ----
const BTN = {
  primary: { default: ["primary-600", "surface-0", null], pressed: ["primary-700", "surface-0", null], disabled: ["surface-100", "ink-500", "border-200"], loading: ["primary-700", "surface-0", null] },
  secondary: { default: ["surface-0", "primary-700", "primary-600"], pressed: ["primary-50", "primary-700", "primary-700"], disabled: ["surface-100", "ink-500", "border-200"], loading: ["primary-50", "primary-700", "primary-600"] },
  accent: { default: ["accent-500", "ink-900", null], pressed: ["accent-700", "surface-0", null], disabled: ["surface-100", "ink-500", "border-200"], loading: ["accent-700", "surface-0", null] },
  destructive: { default: ["error-700", "surface-0", null], pressed: ["error-800", "surface-0", null], disabled: ["surface-100", "ink-500", "border-200"], loading: ["error-800", "surface-0", null] },
};
function Btn(p, label, o = {}) {
  const v = o.v || "primary", st = o.st || "default";
  const [bg, fg, bd] = BTN[v][st];
  const node = { type: "rectangle", role: "button", name: "Boton " + v + " " + st, layout: "horizontal", alignItems: "center", justifyContent: "center",
    width: o.width || "fill_container", height: o.h || 52, cornerRadius: 12, padding: [0, 20], gap: 8, fill: F(bg) };
  if (bd) node.stroke = ST(bd, 2);
  const b = I(p, node);
  if (st === "loading") IC(b, "loader", 20, fg);
  else if (o.icon) IC(b, o.icon, 20, fg);
  T(b, label, { s: 16, w: 600, c: fg, lh: 1.2 });
  return b;
}

// ---- Campo de texto ----
function Field(p, label, value, o = {}) {
  const st = o.st || "default";
  const g = V(p, { name: "Campo " + label + " " + st, gap: 8 });
  T(g, label, { s: 14, w: 600, lh: 1.3 });
  const box = { type: "rectangle", role: "form-input", name: "Entrada " + label, layout: "horizontal", alignItems: "center", width: "fill_container", height: 52, cornerRadius: 12,
    padding: [0, 16], gap: 10, fill: F("surface-0"), stroke: ST("ink-300", 1.5) };
  let tc = value ? "ink-900" : "ink-500";
  if (st === "focus") { box.stroke = ST("primary-600", 2); box.effects = [{ type: "shadow", offsetX: 0, offsetY: 0, blur: 0, spread: 3, color: "rgba(196,30,36,0.22)" }]; }
  if (st === "error") { box.stroke = ST("error-700", 2); }
  if (st === "disabled") { box.fill = F("surface-100"); box.stroke = ST("border-200", 1); tc = "ink-500"; }
  const b = I(g, box);
  if (o.icon) IC(b, o.icon, 20, st === "disabled" ? "ink-500" : "ink-700");
  T(b, value || o.placeholder || "", { s: 16, c: tc, fill: true, lh: 1.2 });
  if (o.trail) IC(b, o.trail, 20, "ink-700");
  if (st === "focus") I(b, { type: "rectangle", name: "Cursor", width: 2, height: 24, fill: F("primary-600") });
  if (o.help && st !== "error") T(g, o.help, { s: 14, c: "ink-500", fill: true });
  if (st === "error") {
    const r = H(g, { gap: 6, alignItems: "start" });
    IC(r, "circle-alert", 18, "error-700");
    T(r, o.error, { s: 14, w: 500, c: "error-700", fill: true });
  }
  return g;
}

// ---- Insignias de estado (texto + icono) ----
const BADGE = {
  valido: ["Sin usar", "circle-check", "success-100", "success-700"],
  utilizado: ["Utilizado", "check-check", "surface-100", "ink-700"],
  vencido: ["Vencido", "calendar-x", "error-100", "error-700"],
  pendiente: ["Pendiente", "clock", "warning-100", "warning-800"],
  offline: ["Sin conexión", "wifi-off", "ink-900", "surface-0"],
};
function Badge(p, kind) {
  const [txt, ic, bg, fg] = BADGE[kind];
  const b = H(p, { role: "badge", name: "Insignia " + txt, width: "fit_content", height: 28, padding: [0, 10], gap: 6, cornerRadius: 14, fill: F(bg), justifyContent: "center" });
  IC(b, ic, 16, fg);
  T(b, txt, { s: 13, w: 700, c: fg, lh: 1.2 });
  return b;
}

// ---- Avisos ----
function Banner(p, kind, title, body, o = {}) {
  const cfg = { error: ["error-100", "error-700"], offline: ["ink-900", "surface-0"], stale: ["warning-100", "warning-800"], info: ["info-100", "info-700"] }[kind];
  const icn = { error: "circle-alert", offline: "wifi-off", stale: "clock", info: "info" }[kind];
  const b = H(p, { name: "Aviso " + kind, alignItems: "start", padding: 16, gap: 12, cornerRadius: 12, fill: F(cfg[0]) });
  IC(b, icn, 22, cfg[1]);
  const col = V(b, { width: "fill_container", gap: 4 });
  T(col, title, { s: 16, w: 700, c: cfg[1], fill: true, lh: 1.3 });
  if (body) T(col, body, { s: 14, c: cfg[1], fill: true, lh: 1.4 });
  if (o.action) {
    const a = H(col, { type: "rectangle", role: "button", name: "Boton reintentar", width: "fit_content", height: 48, padding: [0, 16], gap: 8, cornerRadius: 12, justifyContent: "center",
      fill: F("surface-0"), stroke: ST(o.actionColor || cfg[1], 2) });
    IC(a, "refresh-cw", 18, o.actionColor || cfg[1]);
    T(a, o.action, { s: 15, w: 700, c: o.actionColor || cfg[1], lh: 1.2 });
  }
  return b;
}

// ---- Tarjetas ----
function TicketCard(p, d) {
  const c = V(p, { role: "card", name: "Tarjeta ticket " + d.from + " a " + d.to, padding: 16, gap: 12, cornerRadius: 16, fill: F("surface-0"), stroke: ST("border-200", 1), effects: SH.sm });
  const top = H(c, { justifyContent: "space_between", alignItems: "start", gap: 8 });
  const t = V(top, { width: "fill_container", gap: 2 });
  T(t, d.from, { s: 17, w: 700, fill: true, lh: 1.25 });
  const r = H(t, { gap: 6, alignItems: "center" });
  IC(r, "arrow-down", 16, "ink-700");
  T(r, d.to, { s: 17, w: 700, fill: true, lh: 1.25 });
  Badge(top, d.st);
  I(c, { type: "rectangle", name: "Separador", width: "fill_container", height: 1, fill: F("border-200") });
  const m = H(c, { gap: 16, alignItems: "start" });
  const a = V(m, { width: "fill_container", gap: 2 }); T(a, "Servicio", { s: 12, w: 600, c: "ink-500" }); T(a, d.serv, { s: 15, w: 600 });
  const b = V(m, { width: "fill_container", gap: 2 }); T(b, "Fecha", { s: 12, w: 600, c: "ink-500" }); T(b, d.fecha, { s: 15, w: 600 });
  const act = H(c, { height: 48, justifyContent: "space_between", name: "Accion mostrar QR" });
  T(act, d.cta || "Mostrar QR", { s: 15, w: 700, c: "primary-700", lh: 1.2 });
  IC(act, "chevron-right", 22, "primary-700");
  return c;
}
function ItinCard(p, d) {
  const direct = d.kind === "Directo";
  const c = V(p, { role: "card", name: "Tarjeta itinerario " + d.kind, padding: 16, gap: 12, cornerRadius: 16, fill: F("surface-0"), stroke: ST("border-200", 1), effects: SH.sm });
  const top = H(c, { justifyContent: "space_between" });
  const tag = H(top, { width: "fit_content", height: 28, padding: [0, 10], gap: 6, cornerRadius: 14, fill: F(direct ? "success-100" : "accent-100") });
  IC(tag, direct ? "route" : "repeat", 16, direct ? "success-700" : "accent-700");
  T(tag, d.kind, { s: 13, w: 700, c: direct ? "success-700" : "accent-700", lh: 1.2 });
  T(top, d.precio, { s: 20, w: 700, lh: 1.2, c: "ink-900" });
  d.legs.forEach((leg, i) => {
    const row = H(c, { gap: 12, alignItems: "start" });
    const tm = V(row, { width: 56, gap: 20 }); T(tm, leg.h1, { s: 16, w: 700, lh: 1.2 }); T(tm, leg.h2, { s: 16, w: 700, lh: 1.2 });
    const dot = V(row, { width: 12, height: 52, alignItems: "center", gap: 0 });
    I(dot, { type: "ellipse", width: 12, height: 12, fill: F("ink-900") });
    I(dot, { type: "rectangle", width: 2, height: 28, fill: F("ink-300") });
    I(dot, { type: "ellipse", width: 12, height: 12, fill: F("surface-0"), stroke: ST("ink-900", 2) });
    const nm = V(row, { width: "fill_container", gap: 20 }); T(nm, leg.a, { s: 16, fill: true, lh: 1.2 }); T(nm, leg.b, { s: 16, fill: true, lh: 1.2 });
    if (i < d.legs.length - 1) {
      const tr = H(c, { gap: 8, padding: [8, 12], cornerRadius: 8, fill: F("surface-50") });
      IC(tr, "clock", 16, "ink-700"); T(tr, d.trans, { s: 14, w: 500, c: "ink-700", fill: true, lh: 1.3 });
    }
  });
  const f = H(c, { gap: 8 });
  IC(f, "timer", 16, "ink-500"); T(f, d.dur, { s: 14, c: "ink-500", fill: true, lh: 1.2 });
  return c;
}

// ---- Navegación ----
const TABS_P = [["Buscar", "search"], ["Tickets", "ticket"], ["Abonos", "id-card"], ["Servicio", "bus"], ["Cuenta", "user"]];
const TABS_C = [["Validar", "scan-line"], ["Cuenta", "user"]];
function HomeIndicator(p, bg) {
  const hi = H(p, { height: 34, justifyContent: "center", alignItems: "end", padding: [0, 0, 8, 0], name: "Area segura inferior", fill: F(bg) });
  I(hi, { type: "rectangle", name: "Indicador de inicio", width: 134, height: 5, cornerRadius: 3, fill: F("ink-900") });
  return hi;
}
function TabBar(p, tabs, active, o = {}) {
  const bar = V(p, Object.assign({ role: "navbar", name: "Barra de pestañas " + (tabs.length === 5 ? "pasajero" : "cobrador"), fill: F("surface-0") }, o));
  I(bar, { type: "rectangle", name: "Borde superior", width: "fill_container", height: 1, fill: F("border-200") });
  const row = H(bar, { height: 56, padding: [0, 4], gap: 0 });
  tabs.forEach(([lb, ic], i) => {
    const on = i === active;
    const t = V(row, { name: "Pestaña " + lb + (on ? " activa" : ""), width: "fill_container", height: 56, alignItems: "center", justifyContent: "center", gap: 2 });
    const pill = H(t, { width: 56, height: 28, cornerRadius: 14, justifyContent: "center", fill: on ? F("accent-500") : [] });
    IC(pill, ic, 22, on ? "ink-900" : "ink-500");
    T(t, lb, { s: 12, w: on ? 700 : 600, c: on ? "ink-900" : "ink-500", a: "center", lh: 1.2 });
  });
  HomeIndicator(bar, "surface-0");
  return bar;
}
function StatusBar(p) {
  const s = H(p, { name: "Area segura superior", height: 44, padding: [0, 24], justifyContent: "space_between", fill: F("ink-900") });
  T(s, "9:41", { s: 15, w: 600, c: "surface-0", lh: 1.2 });
  const r = H(s, { width: "fit_content", gap: 6 });
  IC(r, "signal", 16, "surface-0"); IC(r, "wifi", 16, "surface-0"); IC(r, "battery-full", 18, "surface-0");
  return s;
}
function Header(p, o = {}) {
  const h = H(p, { role: "navbar", name: "Encabezado", height: 56, padding: o.back ? [0, 16, 0, 8] : [0, 16], gap: 8, fill: F("ink-900") });
  if (o.back) { const b = H(h, { role: "icon-button", name: "Volver", width: 48, height: 48, justifyContent: "center" }); IC(b, "arrow-left", 24, "surface-0"); }
  if (o.title) T(h, o.title, { s: 18, w: 700, c: "surface-0", fill: true, lh: 1.2 });
  else T(h, "Berrutti", { s: 22, w: 800, c: "surface-0", fill: true, lh: 1.1, ls: -0.5, name: "Wordmark Berrutti" });
  I(p, { type: "rectangle", name: "Franja naranja", width: "fill_container", height: 4, fill: F("accent-500") });
  return h;
}

// ---- Pantalla móvil 375x812 ----
function Screen(name, x, y, o = {}) {
  const root = I(null, { type: "frame", name, x, y, width: 375, height: 812, layout: "vertical", clipContent: true, fill: F(o.bg || "surface-50") });
  StatusBar(root);
  Header(root, o);
  const body = V(root, { name: "Contenido", height: "fill_container", padding: o.pad === undefined ? 16 : o.pad, gap: o.gap || 16, clipContent: true });
  if (o.tabs) TabBar(root, o.tabs, o.active);
  else HomeIndicator(root, o.bg || "surface-50");
  return body;
}

// ---- QR de relleno (sin código real) ----
function QR(p, size = 240) {
  const q = I(p, { type: "frame", name: "Área QR (marcador de posición)", layout: "none", width: size, height: size, cornerRadius: 12, fill: F("surface-0"), stroke: ST("ink-900", 2), clipContent: true });
  const n = 25, cell = (size - 32) / n, off = 16;
  const finder = (cx, cy) => {
    I(q, { type: "rectangle", x: off + cx * cell, y: off + cy * cell, width: 7 * cell, height: 7 * cell, fill: F("ink-900") });
    I(q, { type: "rectangle", x: off + (cx + 1) * cell, y: off + (cy + 1) * cell, width: 5 * cell, height: 5 * cell, fill: F("surface-0") });
    I(q, { type: "rectangle", x: off + (cx + 2) * cell, y: off + (cy + 2) * cell, width: 3 * cell, height: 3 * cell, fill: F("ink-900") });
  };
  finder(0, 0); finder(n - 7, 0); finder(0, n - 7);
  let s = 7;
  for (let y = 0; y < n; y++) for (let x = 0; x < n; x++) {
    const inF = (x < 8 && y < 8) || (x >= n - 8 && y < 8) || (x < 8 && y >= n - 8);
    s = (s * 1103515245 + 12345) % 2147483648;
    if (!inF && (s >> 8) % 100 < 46) I(q, { type: "rectangle", x: off + x * cell, y: off + y * cell, width: cell, height: cell, fill: F("ink-900") });
  }
  return q;
}
