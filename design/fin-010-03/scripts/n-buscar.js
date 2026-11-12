// N4 · Buscar viaje angosto · N5 · menú desplegado
let { root, main } = NarPage("N4-buscar-viaje", 1500, "buscar", "visitante");
NTitle(main, "Buscar viaje", "Elegí el origen, el destino y la fecha.");
NSearch(main);
let st = H(main, { gap: 8, alignItems: "start", padding: [0, 16, 0, 0] }); IC(st, "circle-check", 20, "success-700");
T(st, "2 itinerarios encontrados de Carmelo a Colonia el 13/11/2026.", { s: 16, w: 600, width: 312, lh: 1.3, name: "Estado de resultados" });
ItinCard(main, { kind: "2 tramos", precio: "$ 170,00", trans: "Transbordo en Radial de Conchillas: 25 min", dur: "1 h 50 min en total",
  legs: [{ h1: "07:30", h2: "08:05", a: "Carmelo", b: "Radial de Conchillas" }, { h1: "08:30", h2: "09:20", a: "Radial de Conchillas", b: "Colonia" }] });
ItinCard(main, { kind: "Directo", precio: "$ 190,00", dur: "1 h 25 min · sin transbordos", legs: [{ h1: "10:15", h2: "11:40", a: "Carmelo", b: "Colonia" }] });
Footer(root, true);

({ root, main } = NarPage("N5-menu-desplegado", 2000, "buscar", "pasajero", true));
NTitle(main, "Buscar viaje", "Elegí el origen, el destino y la fecha.");
Footer(root, true);
