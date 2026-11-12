// Home A · búsqueda primero
const { root, main } = DeskPage("H-A-busqueda-primero", 0, "inicio", "visitante");
Title(main, "¿A dónde viajás?", "Buscá un viaje entre las paradas de Berrutti.", { s: 40, ss: 18 });
SearchCard(main);
const row = H(main, { gap: 32, alignItems: "start" });
const l = V(row, { name: "Columna izquierda" }); SalidasCard(l);
const r = V(row, { name: "Columna derecha", width: 424 }); LineasCard(r);
Footer(root, false);
