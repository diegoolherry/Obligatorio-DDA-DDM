// Home B · centro de tareas
function HomeB(name, x) {
const { root, main } = DeskPage(name, x, "inicio", "visitante");
Title(main, "¿Qué necesitás hoy?", "Buscá un viaje, consultá los horarios o seguí un servicio en camino.", { s: 40, ss: 18 });
const row = H(main, { gap: 24, alignItems: "start" });
const big = V(row, { role: "card", name: "Entrada Buscar viaje", clipContent: true, padding: 0, gap: 0, cornerRadius: 16, fill: F("surface-0"), stroke: ST("border-200", 1), effects: SH.sm });
I(big, { type: "rectangle", name: "Franja naranja", width: "fill_container", height: 4, fill: F("accent-500") });
const bin = V(big, { name: "Contenido de la entrada", padding: 32, gap: 12 });
IC(bin, "search", 28, "ink-900");
T(bin, "Buscar viaje", { s: 32, w: 800, lh: 1.19, ls: -0.5, fill: true, name: "H2" });
T(bin, "Elegí origen, destino y fecha. Te mostramos los itinerarios directos y los que tienen transbordo, con el importe de cada tramo.", { s: 18, c: "ink-700", fill: true, lh: 1.45 });
Btn(bin, "Buscar viaje", { width: 200, icon: "search" });
const side = V(row, { name: "Otras entradas", width: 400, gap: 24 });
const c1 = Card(side, { name: "Entrada Horarios", gap: 8 });
IC(c1, "calendar-clock", 28, "ink-900");
T(c1, "Horarios", { s: 20, w: 700, lh: 1.25, fill: true, name: "H2" });
T(c1, "Salidas de un día y catálogo completo del horario de invierno 2026.", { s: 15, c: "ink-700", fill: true, lh: 1.45 });
LinkRow(c1, "Ver horarios");
const c2 = Card(side, { name: "Entrada Ubicación del servicio", gap: 8 });
IC(c2, "bus", 28, "ink-900");
T(c2, "Ubicación del servicio", { s: 20, w: 700, lh: 1.25, fill: true, name: "H2" });
T(c2, "Última posición conocida de un servicio y su llegada aproximada.", { s: 15, c: "ink-700", fill: true, lh: 1.45 });
const lk = H(c2, { gap: 8, padding: [8, 12], cornerRadius: 8, fill: F("surface-50") });
IC(lk, "lock", 16, "ink-700"); T(lk, "Requiere ingresar como pasajero.", { s: 14, w: 500, c: "ink-700", fill: true, lh: 1.3 });
LinkRow(c2, "Ver ubicación");
const lc = Card(main, { name: "Líneas y paradas", padding: [24, 32], gap: 12 });
T(lc, "Líneas y paradas", { s: 24, w: 700, lh: 1.2, fill: true, name: "H2" });
LINEAS.forEach((l, i) => {
  Sep(lc);
  const g = H(lc, { gap: 24, padding: [4, 0] });
  T(g, l[0], { s: 16, w: 700, lh: 1.3, width: 260 });
  T(g, l[1], { s: 15, c: "ink-700", lh: 1.4, fill: true });
});
Footer(root, false);
}
HomeB("H-B-centro-de-tareas", 1400);
HomeB("W01-home", 4200);
