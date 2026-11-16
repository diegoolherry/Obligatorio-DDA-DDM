// W02 · Buscar viaje (formulario + resultados)
const { root, main } = DeskPage("W02-buscar-viaje", 0, "buscar", "visitante");
Title(main, "Buscar viaje", "Elegí el origen, el destino y la fecha.");
const c = Card(main, { name: "Formulario Buscar viaje", gap: 16 });
const row = H(c, { gap: 16, alignItems: "end" });
FW(row, "fill_container", "Origen", "Carmelo", { trail: "chevron-down" });
FW(row, "fill_container", "Destino", "Colonia", { trail: "chevron-down" });
FW(row, 200, "Fecha", "13/11/2026", { trail: "calendar" });
Btn(row, "Buscar", { width: 160, icon: "search" });
const res = V(main, { name: "Resultados", gap: 16 });
const st = H(res, { gap: 8 }); IC(st, "circle-check", 20, "success-700");
T(st, "2 itinerarios encontrados de Carmelo a Colonia el 13/11/2026.", { s: 16, w: 600, c: "ink-900", fill: true, lh: 1.3, name: "Estado de resultados" });
ItinDesk(res, ITIN_2);
ItinDesk(res, ITIN_1);
Footer(root, false);
