// W03 · Horarios (salidas del día + catálogo)
const { root, main } = DeskPage("W03-horarios", 1400, "horarios", "visitante");
Title(main, "Horarios", "Transcripción del horario publicado por la empresa. La primera hora de cada salida es la publicada; las demás (marcadas con ≈) son aproximadas.");
// --- Salidas de un día
const s1 = V(main, { name: "Salidas de un día", gap: 16 });
H2(s1, "Salidas de un día");
const fr = H(s1, { gap: 16, alignItems: "end" });
FW(fr, 240, "Fecha", "13/11/2026", { trail: "calendar" });
Btn(fr, "Consultar", { width: 160 });
const sd = H(s1, { gap: 8 }); IC(sd, "calendar", 20, "ink-700"); T(sd, "viernes 13/11/2026", { s: 16, w: 600, lh: 1.3, name: "Estado del día" });
const cols = H(s1, { gap: 24, alignItems: "start" });
const colL = V(cols, { name: "Columna 1", gap: 24 });
const colR = V(cols, { name: "Columna 2", gap: 24 });
const b1 = Card(colL, { name: "Bloque Línea Nueva Palmira / Colonia", gap: 4 });
T(b1, "Línea Nueva Palmira / Colonia", { s: 20, w: 700, lh: 1.25, fill: true, name: "H3" });
SalidaRow(b1, "05:20", "Nueva Palmira → Colonia", "Nueva Palmira 05:20 → Carmelo ≈05:50 → Radial de Conchillas ≈06:15 → Colonia ≈07:05", "C-O");
Sep(b1);
SalidaRow(b1, "06:55", "Nueva Palmira → Colonia", "Nueva Palmira 06:55 → Carmelo ≈07:30 → Radial de Conchillas ≈08:05 → Colonia ≈09:00", "C-O");
Sep(b1);
SalidaRow(b1, "09:40", "Nueva Palmira → Colonia", "Nueva Palmira 09:40 → Carmelo ≈10:15 → Radial de Conchillas ≈10:45 → Colonia ≈11:40", "C-O");
const b2 = Card(colR, { name: "Bloque Línea Carmelo / Mercedes", gap: 4 });
T(b2, "Línea Carmelo / Mercedes", { s: 20, w: 700, lh: 1.25, fill: true, name: "H3" });
SalidaRow(b2, "06:00", "Carmelo → Mercedes", "Carmelo 06:00 → Nueva Palmira ≈06:30 → Dolores ≈07:10 → Mercedes ≈07:50", null);
Sep(b2);
SalidaRow(b2, "13:00", "Carmelo → Mercedes", "Carmelo 13:00 → Nueva Palmira ≈13:30 → Dolores ≈14:15 → Mercedes ≈15:00", null, "Combina desde Colonia");
const b3 = Card(colR, { name: "Bloque Línea Nueva Palmira / Agraciada", gap: 4 });
T(b3, "Línea Nueva Palmira / Agraciada", { s: 20, w: 700, lh: 1.25, fill: true, name: "H3" });
SalidaRow(b3, "12:00", "Nueva Palmira → Agraciada", "Nueva Palmira 12:00 → Agraciada", null);
// --- Catálogo
const s2 = V(main, { name: "Catálogo completo", gap: 16 });
H2(s2, "Catálogo completo: horario de invierno 2026");
function Summ(p, text, open) {
  const r = H(p, { name: "Resumen " + text, height: 56, padding: [0, 20], gap: 12, cornerRadius: 12, fill: F("surface-0"), stroke: ST(open ? "ink-300" : "border-200", open ? 1.5 : 1) });
  IC(r, open ? "chevron-down" : "chevron-right", 22, "ink-900");
  T(r, text, { s: 18, w: 700, lh: 1.2, fill: true });
  return r;
}
Summ(s2, "Línea Nueva Palmira / Colonia", false);
const open = V(s2, { name: "Bloque abierto", gap: 12 });
Summ(open, "Línea Carmelo / Mercedes", true);
const body = Card(open, { name: "Detalle del bloque", gap: 12 });
const meta = H(body, { gap: 6 });
T(meta, "Berrutti S.R.L. · vigente desde el 02/03/2026 ·", { s: 14, c: "ink-700", lh: 1.3 });
T(meta, "fuente", { s: 14, w: 700, c: "primary-700", lh: 1.3 });
IC(meta, "external-link", 14, "primary-700");
function Tabla(p, ruta, paradas, filas) {
  T(p, ruta, { s: 16, w: 700, lh: 1.3, fill: true, name: "H4" });
  T(p, "Paradas: " + paradas, { s: 14, c: "ink-700", fill: true, lh: 1.4 });
  const t = V(p, { name: "Tabla " + ruta, gap: 0, cornerRadius: 8, stroke: ST("border-200", 1), clipContent: true });
  const hd = H(t, { height: 40, padding: [0, 16], gap: 16, fill: F("surface-100") });
  [["Días", 200], ["Recorrido", "fill_container"], ["Códigos", 90], ["Nota", 200]].forEach((c) => { T(hd, c[0], { s: 14, w: 700, c: "ink-900", lh: 1.2, width: c[1] === "fill_container" ? undefined : c[1], fill: c[1] === "fill_container" }); });
  filas.forEach((f) => {
    Sep(t);
    const r = H(t, { alignItems: "start", padding: [12, 16], gap: 16 });
    T(r, f[0], { s: 14, lh: 1.4, width: 200 }); T(r, f[1], { s: 14, lh: 1.4, fill: true }); T(r, f[2], { s: 14, lh: 1.4, width: 90 }); T(r, f[3], { s: 14, c: "ink-700", lh: 1.4, width: 200 });
  });
}
Tabla(body, "Carmelo → Mercedes", "Carmelo → Nueva Palmira → Dolores → Mercedes", [
  ["Lunes a viernes", "Carmelo 06:00 → Nueva Palmira ≈06:30 → Dolores ≈07:10 → Mercedes ≈07:50", "", ""],
  ["Lunes a sábado", "Carmelo 13:00 → Nueva Palmira ≈13:30 → Dolores ≈14:15 → Mercedes ≈15:00", "", "Combina desde Colonia"]]);
Tabla(body, "Mercedes → Carmelo", "Mercedes → Dolores → Nueva Palmira → Carmelo", [
  ["Lunes a viernes", "Mercedes 08:20 → Dolores ≈08:50 → Nueva Palmira ≈09:40 → Carmelo ≈10:15", "", "Combina a Colonia"],
  ["Lunes a sábado", "Mercedes 17:50 → Dolores ≈18:30 → Nueva Palmira ≈19:20 → Carmelo ≈19:50", "", "Combina a Colonia"]]);
Summ(s2, "Línea Nueva Palmira / Carmelo", false);
Summ(s2, "Línea Nueva Palmira / Agraciada", false);
const fe = Card(s2, { name: "Feriados y excepciones", gap: 8 });
T(fe, "Feriados y excepciones", { s: 16, w: 700, lh: 1.3, fill: true, name: "H3" });
["01/01/2026: Año Nuevo (horario de domingos y feriados)", "01/05/2026: Día de los Trabajadores (horario de domingos y feriados)", "18/07/2026: Jura de la Constitución (horario de domingos y feriados)", "25/12/2026: 25 de diciembre: no hay servicios"].forEach((t) => T(fe, t, { s: 14, lh: 1.4, fill: true }));
Sep(fe);
T(fe, "Leyenda: O = combinan a Ombúes · M = combinan a Mercedes · P = por Polancos · C = combinan con Conchillas · S = solo sale · SE = solo entra.", { s: 14, c: "ink-700", fill: true, lh: 1.45 });
Footer(root, false);
