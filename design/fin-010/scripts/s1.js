// Página "Sistema": tablero de colores y pares aprobados (usa TOK de tokens.js)
const lumJ = (h) => { h = h.replace("#", ""); const c = [0, 2, 4].map((i) => parseInt(h.substr(i, 2), 16) / 255).map((x) => (x <= 0.03928 ? x / 12.92 : Math.pow((x + 0.055) / 1.055, 2.4))); return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]; };
const crJ = (a, b) => { let x = lumJ(TOK[a]), y = lumJ(TOK[b]); if (x < y) { const t = x; x = y; y = t; } return (x + 0.05) / (y + 0.05); };
const fmt = (r) => (Math.round(r * 100) / 100).toFixed(2).replace(".", ",");
const lvl = (r) => (r >= 4.5 ? "AA texto" : r >= 3 ? "Solo texto grande / UI" : "No usar para texto");
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

function Swatch(p, name, o = {}) {
  const hex = TOK[name];
  const c = V(p, { name: "Muestra " + name, width: 178, gap: 0, cornerRadius: 12, fill: F("surface-0"), stroke: ST("border-200", 1), clipContent: true });
  I(c, { type: "rectangle", name: "Color " + name, width: "fill_container", height: 64, fill: F(name) });
  const m = V(c, { padding: 12, gap: 2 });
  T(m, name, { s: 14, w: 700, lh: 1.25 });
  T(m, hex, { s: 13, c: "ink-700", lh: 1.3 });
  const rw = crJ(name, "surface-0"), ri = crJ(name, "ink-900");
  T(m, "Blanco: " + fmt(crJ("surface-0", name)) + ":1", { s: 12, c: "ink-500", lh: 1.3 });
  T(m, "Ink-900: " + fmt(ri) + ":1", { s: 12, c: "ink-500", lh: 1.3 });
  if (o.note) T(m, o.note, { s: 12, w: 700, c: "ink-900", lh: 1.3, fill: true });
  return c;
}
function Group(p, title, names, sub, notes = {}) {
  const g = Sec(p, title, sub);
  const r = H(g, { gap: 16, alignItems: "start" });
  names.forEach((n) => Swatch(r, n, { note: notes[n] }));
}

// ---- Tablero 1: colores ----
const b1 = Board("S1 Colores", 0 * BX, 1360, "Colores", "Rojo, naranja, negro y blanco de la identidad, ajustados para cumplir WCAG 2.1 AA. Sin logo ni fotografías: Berrutti es un texto.");
Group(b1, "Primario · rojo", ["primary-50", "primary-100", "primary-500", "primary-600", "primary-700", "primary-900"], "Acción principal. Texto blanco sobre 600/700; texto 700 sobre 50/100.", { "primary-600": "Fondo de botón con texto blanco", "primary-700": "Texto de enlace y pulsado" });
Group(b1, "Acento · naranja", ["accent-100", "accent-500", "accent-700"], "El naranja #E77723 solo es relleno o acento con texto ink-900; sobre blanco da 2,97:1 y nunca es texto.", { "accent-500": "Relleno con texto ink-900. Nunca texto sobre blanco", "accent-700": "Texto naranja sobre blanco / accent-100" });
Group(b1, "Ink (texto) · negro", ["ink-900", "ink-700", "ink-500", "ink-300"], "ink-300 solo para bordes de campos (3,47:1, contraste de componente de interfaz).", { "ink-300": "Solo bordes y marcas, no texto" });
Group(b1, "Superficies y bordes", ["surface-0", "surface-50", "surface-100", "border-200"], "border-200 es separador decorativo (1,46:1); nunca única señal de un control.", { "border-200": "Solo separadores decorativos" });
Group(b1, "Semánticos · éxito y error", ["success-100", "success-700", "error-100", "error-700", "error-800"], "Siempre acompañados de texto e icono; nunca solo color.");
Group(b1, "Semánticos · advertencia e información", ["warning-100", "warning-500", "warning-800", "info-100", "info-700"], null, { "warning-500": "Solo relleno/icono con texto ink-900 (9,65:1)" });

// Pares aprobados
const PAIRS = [
  ["ink-900", "surface-0", "Texto principal sobre blanco"], ["ink-900", "surface-50", "Texto principal sobre fondo de pantalla"],
  ["ink-700", "surface-0", "Texto secundario fuerte"], ["ink-500", "surface-0", "Texto de ayuda sobre blanco"], ["ink-500", "surface-50", "Texto de ayuda sobre fondo"],
  ["surface-0", "primary-600", "Botón primario"], ["surface-0", "primary-700", "Botón primario pulsado"], ["primary-700", "surface-0", "Enlace / botón secundario"],
  ["ink-900", "accent-500", "Botón de acento, pestaña activa"], ["surface-0", "accent-700", "Botón de acento pulsado"], ["accent-700", "accent-100", "Etiqueta naranja"],
  ["surface-0", "error-700", "Botón destructivo"], ["error-700", "error-100", "Aviso y texto de error"], ["success-700", "success-100", "Sin usar / éxito"],
  ["warning-800", "warning-100", "Pendiente / desactualizado"], ["info-700", "info-100", "Aviso informativo"], ["surface-0", "ink-900", "Encabezado, Sin conexión"],
  ["ink-700", "surface-100", "Utilizado"], ["ink-500", "surface-100", "Controles deshabilitados"],
];
const sec = Sec(b1, "Pares de texto aprobados", "Fórmula WCAG 2.1 de luminancia relativa. Mínimo 4,5:1 para texto normal, 3:1 para texto grande (≥24 px, o ≥18,66 px en negrita).");
const grid = H(sec, { gap: 24, alignItems: "start" });
const colA = V(grid, { width: "fill_container", gap: 8 }), colB = V(grid, { width: "fill_container", gap: 8 });
PAIRS.forEach(([fg, bg, use], i) => {
  const r = crJ(fg, bg);
  const row = H(i % 2 === 0 ? colA : colB, { gap: 12, height: 40, padding: [0, 0] });
  const chip = H(row, { width: 56, height: 36, cornerRadius: 8, justifyContent: "center", fill: F(bg), stroke: ST("border-200", 1) });
  T(chip, "Aa", { s: 16, w: 700, c: fg, lh: 1.1 });
  T(row, fg + " sobre " + bg, { s: 13, w: 600, fill: true, lh: 1.2 });
  T(row, fmt(r) + ":1 · " + (r >= 4.5 ? "AA" : "falla"), { s: 13, w: 700, lh: 1.2, width: 96, a: "right" });
});
const bad = H(sec, { gap: 12, padding: 12, cornerRadius: 12, fill: F("surface-50"), stroke: ST("ink-900", 1) });
IC(bad, "ban", 22, "ink-900");
T(bad, "Prohibido: accent-500 #E77723 como texto sobre blanco (2,97:1) o sobre surface-50 (2,75:1). Usalo como relleno con ink-900 (6,19:1) o como acento gráfico.", { s: 14, w: 600, fill: true, lh: 1.4 });
