// Datos reales del catálogo (horarios-invierno-2026.json) usados en las direcciones de Home
const SALIDAS_HOY = [
  ["11:05", "Nueva Palmira → Colonia", "Nueva Palmira 11:05 → Carmelo ≈11:40 → Radial de Conchillas ≈12:15 → Colonia ≈13:10", "C-O", null],
  ["11:25", "Colonia → Nueva Palmira", "Colonia 11:25 → Radial de Conchillas ≈12:15 → Carmelo ≈13:00 → Nueva Palmira ≈13:30", "C-O-M", null],
  ["12:30", "Nueva Palmira → Colonia", "Nueva Palmira 12:30 → Carmelo ≈13:05 → Radial de Conchillas ≈13:35 → Colonia ≈14:20", "C-O", null],
  ["13:00", "Carmelo → Mercedes", "Carmelo 13:00 → Nueva Palmira ≈13:30 → Dolores ≈14:15 → Mercedes ≈15:00", null, "Combina desde Colonia"],
];
const LINEAS = [
  ["Nueva Palmira / Colonia", "Nueva Palmira · Polancos · Carmelo · Radial de Conchillas · Colonia"],
  ["Carmelo / Mercedes", "Carmelo · Nueva Palmira · Dolores · Mercedes"],
  ["Nueva Palmira / Carmelo", "Nueva Palmira · Polancos · Carmelo"],
  ["Nueva Palmira / Agraciada", "Nueva Palmira · Agraciada"],
  ["Ombúes y Conchillas", "Conexiones hacia Radial de Conchillas"],
];
function SearchCard(p) {
  const c = Card(p, { name: "Formulario Buscar viaje", padding: 24, gap: 16, effects: SH.md });
  const row = H(c, { gap: 16, alignItems: "end" });
  FW(row, "fill_container", "Origen", "Carmelo", { trail: "chevron-down" });
  FW(row, "fill_container", "Destino", "Colonia", { trail: "chevron-down" });
  FW(row, 200, "Fecha", "13/11/2026", { trail: "calendar" });
  Btn(row, "Buscar", { width: 160, icon: "search" });
  return c;
}
function SalidasCard(p, o = {}) {
  const c = Card(p, { name: "Salidas de hoy", gap: 8 });
  const t = H(c, { gap: 16, alignItems: "end" });
  T(t, "Salidas de hoy", { s: 24, w: 700, lh: 1.2, fill: true, name: "H2" });
  T(t, "viernes 13/11/2026 · desde las 11:00", { s: 14, c: "ink-500", lh: 1.3 });
  SALIDAS_HOY.forEach((s, i) => { Sep(c); SalidaRow(c, s[0], s[1], s[2], s[3], s[4]); });
  Sep(c);
  LinkRow(c, "Ver todas las salidas del día");
  return c;
}
function LineasCard(p, o = {}) {
  const c = Card(p, Object.assign({ name: "Líneas y paradas", gap: 12 }, o));
  T(c, "Líneas y paradas", { s: 24, w: 700, lh: 1.2, fill: true, name: "H2" });
  LINEAS.forEach((l, i) => {
    if (i > 0) Sep(c);
    const g = V(c, { gap: 2 });
    T(g, l[0], { s: 16, w: 700, lh: 1.3, fill: true });
    T(g, l[1], { s: 14, c: "ink-700", lh: 1.4, fill: true });
  });
  return c;
}
