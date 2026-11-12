// W06 · Administración operativa (vista representativa: Paradas, con error de campo y confirmación de eliminación)
const { root, main } = DeskPage("W06-administracion-operativa", 5600, "admin", "admin");
Title(main, "Paradas y tramos", "Configuración operativa: paradas, tramos, servicios y tarifas.");
StateBox(main, "ok", "Parada guardada.");
const row = H(main, { gap: 32, alignItems: "start" });
// índice de secciones (anclas dentro de la página)
const nav = V(row, { name: "Secciones", width: 220, gap: 4 });
T(nav, "En esta página", { s: 14, w: 700, c: "ink-500", fill: true });
[["Paradas", true], ["Tramos", false], ["Servicios", false], ["Tarifas", false]].forEach((s) => {
  const l = H(nav, { role: "nav-link", name: "Ancla " + s[0], height: 44, padding: [0, 14], cornerRadius: 12, fill: s[1] ? F("accent-500") : [] });
  T(l, s[0], { s: 16, w: s[1] ? 700 : 600, c: s[1] ? "ink-900" : "ink-700", fill: true, lh: 1.2 });
});
const sec = V(row, { name: "Sección Paradas", gap: 24 });
H2(sec, "Paradas");
const two = H(sec, { gap: 24, alignItems: "start" });
// lista
const list = Card(two, { name: "Lista de paradas", padding: 8, gap: 0 });
const th = H(list, { height: 44, padding: [0, 16], fill: F("surface-100"), cornerRadius: 8 });
T(th, "Nombre", { s: 14, w: 700, lh: 1.2, fill: true }); T(th, "Acciones", { s: 14, w: 700, lh: 1.2, width: 190 });
const paradas = ["Carmelo", "Colonia", "Dolores", "Mercedes", "Nueva Palmira", "Radial de Conchillas"];
paradas.forEach((n, i) => {
  const sel = n === "Dolores";
  const r = H(list, { name: "Fila " + n, height: 56, padding: [0, 16], gap: 8, cornerRadius: 8, fill: sel ? F("warning-100") : [] });
  T(r, n, { s: 16, w: sel ? 700 : 400, c: sel ? "warning-800" : "ink-900", fill: true, lh: 1.3 });
  const e = H(r, { type: "rectangle", role: "button", name: "Editar " + n, width: 88, height: 44, gap: 6, cornerRadius: 12, justifyContent: "center" });
  IC(e, "pencil", 18, "primary-700"); T(e, "Editar", { s: 15, w: 700, c: "primary-700", lh: 1.2 });
  const d = H(r, { type: "rectangle", role: "button", name: "Eliminar " + n, width: 100, height: 44, gap: 6, cornerRadius: 12, justifyContent: "center" });
  IC(d, "trash-2", 18, "error-700"); T(d, "Eliminar", { s: 15, w: 700, c: "error-700", lh: 1.2 });
  if (i < paradas.length - 1) Sep(list);
});
// formulario con error
const f = Card(two, { name: "Formulario de parada", width: 360, gap: 16, effects: SH.md });
T(f, "Nueva parada", { s: 20, w: 700, lh: 1.25, fill: true, name: "H3" });
StateBox(f, "error", "Revisá el formulario", "Hay 1 campo con error.");
Field(f, "Nombre", "", { st: "error", error: "Ingresá el nombre de la parada.", icon: "map-pin" });
Btn(f, "Crear parada", {});
// confirmación de eliminación
const cf = H(sec, { name: "Confirmar eliminación", alignItems: "start", padding: 20, gap: 16, cornerRadius: 12, fill: F("warning-100") });
IC(cf, "triangle-alert", 24, "warning-800");
const cc = V(cf, { gap: 12 });
T(cc, "Eliminar pendiente", { s: 16, w: 700, c: "warning-800", fill: true, lh: 1.3 });
T(cc, "¿Confirmás eliminar la parada Dolores? Esta acción no se puede deshacer.", { s: 14, c: "warning-800", fill: true, lh: 1.4 });
const bb = H(cc, { gap: 12 });
Btn(bb, "Confirmar", { v: "destructive", width: 160, icon: "trash-2" });
Btn(bb, "Cancelar", { v: "secondary", width: 140 });
Footer(root, false);
