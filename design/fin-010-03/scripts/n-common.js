// Componentes angostos (390)
function NSalida(p, s) {
  const r = V(p, { name: "Salida " + s[0] + " " + s[1], gap: 4, padding: [12, 0] });
  const h = H(r, { gap: 12 });
  T(h, s[0], { s: 20, w: 700, lh: 1.2 });
  T(h, s[1], { s: 16, w: 600, lh: 1.3, fill: true });
  if (s[3]) Tag(h, s[3], "n");
  T(r, s[2], { s: 14, c: "ink-700", lh: 1.4, fill: true });
  if (s[4]) T(r, s[4], { s: 14, c: "ink-500", lh: 1.4, fill: true });
  return r;
}
function NSearch(p, ttl) {
  const c = Card(p, { name: "Formulario Buscar viaje", padding: 16, gap: 16, effects: SH.md });
  if (ttl) T(c, ttl, { s: 20, w: 700, lh: 1.25, fill: true, name: "H2" });
  Field(c, "Origen", "Carmelo", { trail: "chevron-down" });
  Field(c, "Destino", "Colonia", { trail: "chevron-down" });
  Field(c, "Fecha", "13/11/2026", { trail: "calendar" });
  Btn(c, "Buscar", { icon: "search" });
  return c;
}
function NTitle(p, t, sub) { return Title(p, t, sub, { s: 32, ss: 16 }); }
