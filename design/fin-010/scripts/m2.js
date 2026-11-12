// Pantallas 6-13: detalle de ticket (en línea / sin conexión), cobrador, estado del servicio
const DX = 455, DY = 900;
function title(b, t, sub) { const g = V(b, { gap: 4 }); T(g, t, { s: 24, w: 700, lh: 1.2, fill: true }); if (sub) T(g, sub, { s: 14, c: "ink-500", fill: true }); return g; }
function kv(p, k, v) { const g = V(p, { width: "fill_container", gap: 2 }); T(g, k, { s: 12, w: 600, c: "ink-500" }); T(g, v, { s: 15, w: 600, fill: true, lh: 1.3 }); return g; }

function ticketDetail(name, x, y, offline) {
  const b = Screen(name, x, y, { back: true, title: "Ticket", pad: [12, 16], gap: 12 });
  if (offline) Banner(b, "offline", "Sin conexión", "Última actualización 12/11 14:30 · puede no estar al día");
  const top = H(b, { justifyContent: "space_between", alignItems: "start", gap: 8 });
  const t = V(top, { width: "fill_container", gap: 2 });
  T(t, "Ombúes de Lavalle", { s: 18, w: 700, fill: true, lh: 1.25 });
  T(t, "a Radial", { s: 18, w: 700, fill: true, lh: 1.25 });
  Badge(top, offline ? "offline" : "valido");
  const card = V(b, { role: "card", name: "Panel QR", alignItems: "center", padding: 16, gap: 12, cornerRadius: 16, fill: F("surface-0"), stroke: ST("border-200", 1), effects: SH.sm });
  QR(card, offline ? 196 : 248);
  T(card, offline ? "No se puede validar sin conexión. Reconectá para confirmar el estado." : "Presentá este código al cobrador", { s: 14, w: 500, c: "ink-700", a: "center", fill: true });
  const g = H(card, { gap: 12, alignItems: "start" });
  kv(g, "Servicio", "07:00 · Ombúes de Lavalle – Colonia");
  kv(g, "Fecha", "Vie 13/11/2026");
  if (!offline) Btn(b, "Ver el tramo Radial a Colonia", { v: "secondary", icon: "arrow-right-left" });
  else Btn(b, "Reintentar conexión", { v: "secondary", icon: "refresh-cw" });
}
ticketDetail("M06 Ticket - detalle con QR", 0 * DX, DY, false);
ticketDetail("M07 Ticket - sin conexión", 1 * DX, DY, true);

// 8. Cobrador: Validar
let b = Screen("M08 Cobrador - Validar", 2 * DX, DY, { tabs: TABS_C, active: 0 });
title(b, "Validar ticket", "Elegí el tramo del servicio actual.");
Field(b, "Servicio", "07:00 · Ombúes de Lavalle – Colonia", { icon: "bus", trail: "chevron-down" });
Field(b, "Tramo", "Ombúes de Lavalle a Radial", { icon: "route", trail: "chevron-down" });
const cam = V(b, { name: "Visor de cámara (marcador de posición)", height: 150, alignItems: "center", justifyContent: "center", gap: 8, cornerRadius: 16, fill: F("ink-900") });
IC(cam, "qr-code", 40, "surface-0");
T(cam, "La cámara se activa al tocar Escanear", { s: 14, c: "surface-0", a: "center", fill: true });
Btn(b, "Escanear", { v: "accent", icon: "scan-line", h: 64 });

function resultScreen(name, x, y, o) {
  const b = Screen(name, x, y, { tabs: TABS_C, active: 0, gap: 16 });
  const p = V(b, { name: "Resultado " + o.t, alignItems: "center", padding: [24, 20], gap: 12, cornerRadius: 16, fill: F(o.bg), stroke: ST(o.fg, 2) });
  IC(p, o.icon, 64, o.fg);
  T(p, o.t, { s: 28, w: 800, c: o.fg, a: "center", fill: true, lh: 1.15 });
  T(p, o.d, { s: 16, w: 500, c: o.fg, a: "center", fill: true, lh: 1.4 });
  const info = V(b, { padding: 16, gap: 12, cornerRadius: 12, fill: F("surface-0"), stroke: ST("border-200", 1) });
  kv(info, "Tramo", "Ombúes de Lavalle a Radial");
  kv(info, "Servicio", "07:00 · Vie 13/11/2026");
  kv(info, o.k, o.v);
  return b;
}
let r = resultScreen("M09 Cobrador - Ticket válido", 3 * DX, DY, { bg: "success-100", fg: "success-700", icon: "circle-check", t: "Ticket válido", d: "Confirmado por el servidor. Puede viajar.", k: "Validado", v: "13/11 07:02" });
Btn(r, "Escanear otro", { v: "accent", icon: "scan-line", h: 56 });
r = resultScreen("M10 Cobrador - Ya utilizado", 4 * DX, DY, { bg: "error-100", fg: "error-700", icon: "circle-x", t: "Ya utilizado", d: "Este ticket ya se validó. No es válido para viajar.", k: "Primer uso", v: "13/11 06:58" });
Btn(r, "Escanear otro", { v: "accent", icon: "scan-line", h: 56 });
r = resultScreen("M11 Cobrador - No se pudo confirmar", 0 * DX, 2 * DY, { bg: "warning-100", fg: "warning-800", icon: "triangle-alert", t: "No se pudo confirmar", d: "No hubo respuesta del servidor. El ticket no se validó.", k: "Estado", v: "Sin confirmar" });
Btn(r, "Reintentar", { v: "primary", icon: "refresh-cw", h: 56 });
Btn(r, "Escanear otro", { v: "secondary", h: 56 });

// Estado del servicio
function timeline(p, stops) {
  const c = V(p, { name: "Recorrido", gap: 0 });
  stops.forEach((s, i) => {
    const last = i === stops.length - 1, rh = last ? 36 : 60;
    const row = H(c, { gap: 12, alignItems: "start", height: rh });
    const rail = V(row, { width: 20, height: rh, alignItems: "center", gap: 0 });
    if (s.here) { const m = H(rail, { width: 20, height: 20, cornerRadius: 10, justifyContent: "center", fill: F("accent-500"), stroke: ST("ink-900", 2) }); IC(m, "bus", 12, "ink-900"); }
    else I(rail, { type: "ellipse", width: 14, height: 14, fill: F(s.done ? "ink-900" : "surface-0"), stroke: ST(s.done ? "ink-900" : "ink-700", 2) });
    if (i < stops.length - 1) I(rail, { type: "rectangle", width: 2, height: s.here ? 40 : 46, fill: F("ink-300") });
    const col = V(row, { width: "fill_container", gap: 2 });
    T(col, s.n, { s: 16, w: s.here ? 700 : 600, fill: true, lh: 1.2 });
    T(col, s.d, { s: 14, c: s.dc || "ink-500", w: s.here ? 600 : 400, fill: true, lh: 1.3 });
  });
  return c;
}
function serviceScreen(name, x, y, stale) {
  const b = Screen(name, x, y, { tabs: TABS_P, active: 3, gap: 12 });
  title(b, "Estado del servicio", "07:00 · Ombúes de Lavalle – Colonia");
  if (stale) Banner(b, "stale", "Información desactualizada", "Última actualización 07:04 · hace 18 min. La posición puede haber cambiado.");
  const card = V(b, { role: "card", name: "Última posición", padding: 16, gap: 12, cornerRadius: 16, fill: F("surface-0"), stroke: ST("border-200", 1), effects: SH.sm });
  const top = H(card, { justifyContent: "space_between", alignItems: "start", gap: 8 });
  const t = V(top, { width: "fill_container", gap: 2 });
  T(t, "Última posición conocida", { s: 12, w: 600, c: "ink-500" });
  T(t, "Cerca de Polancos", { s: 20, w: 700, fill: true, lh: 1.25 });
  T(t, stale ? "Actualizada 07:04" : "Actualizada hoy 07:21 · hace 1 min", { s: 14, c: "ink-500", fill: true });
  const bd = H(top, { width: "fit_content", height: 28, padding: [0, 10], gap: 6, cornerRadius: 14, fill: F(stale ? "warning-100" : "success-100") });
  IC(bd, stale ? "clock" : "circle-check", 16, stale ? "warning-800" : "success-700");
  T(bd, stale ? "Desactualizada" : "En horario", { s: 13, w: 700, c: stale ? "warning-800" : "success-700", lh: 1.2 });
  I(card, { type: "rectangle", name: "Separador", width: "fill_container", height: 1, fill: F("border-200") });
  timeline(card, [
    { n: "Ombúes de Lavalle", d: "Salida 07:00", done: true },
    { n: "Polancos", d: stale ? "Última posición recibida" : "Posición actual · 07:21", here: true, dc: "ink-900" },
    { n: "Radial", d: stale ? "Llegada aproximada no disponible" : "Llegada aproximada 07:40" },
    { n: "Colonia", d: stale ? "Estimación no disponible" : "Llegada aproximada 08:45" },
  ]);
  if (stale) Btn(b, "Actualizar", { v: "secondary", icon: "refresh-cw" });
  else T(b, "La llegada es una estimación y puede variar.", { s: 14, c: "ink-500", fill: true, a: "center" });
}
serviceScreen("M12 Estado del servicio", 1 * DX, 2 * DY, false);
serviceScreen("M13 Estado del servicio - desactualizado", 2 * DX, 2 * DY, true);
