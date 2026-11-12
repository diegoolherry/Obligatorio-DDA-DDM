// W07 / W08 · Ubicación del servicio (vigente y desactualizada)
function Dato(p, k, v, kw) {
  const r = H(p, { gap: 16, alignItems: "start" });
  T(r, k, { s: 14, c: "ink-500", width: kw || 120, lh: 1.4 });
  T(r, v, { s: 16, w: 600, fill: true, lh: 1.4 });
  return r;
}
function Ubic(name, x, stale) {
  const { root, main } = DeskPage(name, x, "ubic", "pasajero");
  Title(main, "Ubicación del servicio", "Última posición conocida de un servicio y su llegada aproximada.");
  StateBox(main, "info", "La posición es aproximada", "Puede no coincidir con la ubicación real del ómnibus.");
  const row = H(main, { gap: 32, alignItems: "start" });
  const lc = Card(row, { name: "Servicios", width: 400, gap: 12 });
  const th = H(lc, { gap: 8 });
  T(th, "Servicios", { s: 20, w: 700, lh: 1.25, fill: true, name: "H2" });
  const ref = H(th, { type: "rectangle", role: "button", name: "Boton Actualizar lista", width: "fit_content", height: 44, padding: [0, 14], gap: 6, cornerRadius: 12, justifyContent: "center", stroke: ST("primary-600", 2), fill: F("surface-0") });
  IC(ref, "refresh-cw", 18, "primary-700"); T(ref, "Actualizar", { s: 15, w: 700, c: "primary-700", lh: 1.2 });
  [["Nueva Palmira → Colonia", "Sale 09:40 · llega 11:40", true], ["Colonia → Nueva Palmira", "Sale 11:25 · llega 13:30", false], ["Carmelo → Mercedes", "Sale 13:00 · llega 15:00", false]].forEach((s) => {
    const it = H(lc, { type: "rectangle", role: "button", name: "Servicio " + s[0] + (s[2] ? " seleccionado" : ""), height: 68, padding: [0, 16], gap: 12, cornerRadius: 12, fill: s[2] ? F("primary-50") : F("surface-0"), stroke: s[2] ? ST("primary-600", 2) : ST("border-200", 1) });
    const t = V(it, { gap: 2 });
    T(t, s[0], { s: 16, w: 700, lh: 1.3, fill: true }); T(t, s[1], { s: 14, c: "ink-700", fill: true, lh: 1.3 });
    if (s[2]) IC(it, "circle-check", 22, "primary-700");
  });
  const dc = Card(row, { name: "Última posición", gap: 16 });
  H2(dc, "Última posición");
  T(dc, "Nueva Palmira → Colonia", { s: 18, w: 700, lh: 1.25, fill: true, name: "Servicio seleccionado" });
  if (stale) StateBox(dc, "warn", "Posición desactualizada", "Supera el umbral de 120 s. No la tomes como ubicación actual.");
  else {
    const b = H(dc, { name: "Insignia vigente", width: "fit_content", height: 28, padding: [0, 10], gap: 6, cornerRadius: 14, fill: F("success-100") });
    IC(b, "circle-check", 16, "success-700"); T(b, "Posición vigente", { s: 13, w: 700, c: "success-700", lh: 1.2 });
  }
  const dl = V(dc, { gap: 8, name: "Datos de la posición" });
  Dato(dl, "Latitud", "-33,995410");
  Dato(dl, "Longitud", "-58,215320");
  Dato(dl, "Actualizada", stale ? "13/11/2026 10:34:05 (hora de Uruguay, hace 8 min 20 s)" : "13/11/2026 10:42:10 (hora de Uruguay, hace 25 s)");
  Sep(dc);
  T(dc, "Llegada y demora", { s: 18, w: 700, lh: 1.25, fill: true, name: "H3" });
  if (stale) StateBox(dc, "empty", "Llegada y demora no disponibles", "La última posición supera el umbral de actualización.");
  else {
    const dl2 = V(dc, { gap: 8 });
    Dato(dl2, "Llegada aproximada", "11:40 (hora local de Uruguay)", 160);
    const r2 = H(dl2, { gap: 16 });
    T(r2, "Estado", { s: 14, c: "ink-500", width: 160, lh: 1.4 });
    const b = H(r2, { width: "fit_content", height: 28, padding: [0, 10], gap: 6, cornerRadius: 14, fill: F("success-100") });
    IC(b, "circle-check", 16, "success-700"); T(b, "En horario", { s: 13, w: 700, c: "success-700", lh: 1.2 });
  }
  Sep(dc);
  T(dc, "Parámetros usados en la estimación", { s: 16, w: 700, lh: 1.3, fill: true, name: "H3" });
  const pm = V(dc, { gap: 4 });
  ["Periodicidad de publicación: 15 s", "Umbral de desactualización: 120 s", "Velocidad promedio: 50 km/h", "Tolerancia de demora: 5 min"].forEach((t) => T(pm, t, { s: 14, c: "ink-700", fill: true, lh: 1.4 }));
  Btn(dc, "Actualizar", { v: "secondary", width: 180, icon: "refresh-cw" });
  Footer(root, false);
}
Ubic("W07-ubicacion-del-servicio", 7000, false);
Ubic("W08-ubicacion-desactualizada", 8400, true);
