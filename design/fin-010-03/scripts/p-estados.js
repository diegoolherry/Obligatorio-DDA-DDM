// W09 · Estados de carga, vacío, error y éxito de cada página
const { root, main } = DeskPage("W09-estados", 9800, "estados", "visitante");
Title(main, "Estados de las páginas", "Carga, vacío, error y éxito de cada página. El error de red ofrece Reintentar; un dato anterior nunca se presenta como vigente.");
function Col(p, titulo, items) {
  const c = Card(p, { name: "Estados " + titulo, gap: 12, padding: 20 });
  T(c, titulo, { s: 20, w: 700, lh: 1.25, fill: true, name: "H2" });
  items.forEach((it) => { T(c, it[0], { s: 13, w: 700, c: "ink-500", fill: true, lh: 1.2 }); StateBox(c, it[1], it[2], it[3], it[4]); });
  return c;
}
const g = H(main, { gap: 24, alignItems: "start" });
Col(g, "Buscar viaje", [
  ["Cargando paradas", "load", "Cargando paradas…"],
  ["Error de paradas", "error", "No se pudieron cargar las paradas.", "Revisá tu conexión e intentá de nuevo.", "Reintentar"],
  ["Buscando", "load", "Buscando itinerarios…"],
  ["Sin resultados", "empty", "No hay viajes de Carmelo a Colonia el 13/11/2026.", "Probá con otra fecha u otras paradas."],
  ["Error de búsqueda", "error", "No se pudo completar la búsqueda.", null, "Reintentar"],
]);
Col(g, "Horarios", [
  ["Consultando el día", "load", "Consultando salidas…"],
  ["Día sin servicio", "warn", "No hay servicio", "25 de diciembre: no hay servicios."],
  ["Consultar en la agencia", "warn", "Consultá en la agencia", "Semana de Turismo: consulte en agencias por horarios especiales."],
  ["Catálogo vacío", "empty", "Todavía no hay horarios cargados."],
  ["Error de catálogo", "error", "No se pudo cargar el catálogo.", null, "Reintentar"],
]);
Col(g, "Cuenta, servicio y administración", [
  ["Ingreso: verificando", "load", "Verificando tus datos…"],
  ["Ingreso: sesión iniciada", "ok", "Sesión iniciada."],
  ["Ubicación sin sesión", "info", "Ingresá con la cuenta de pasajero para consultar la ubicación.", null, "Ir al ingreso"],
  ["Ubicación sin posiciones", "empty", "Todavía no hay posiciones para este servicio."],
  ["Administración sin permiso", "error", "No tenés permiso de administrador.", "Si cambió tu rol, comprobá el acceso de nuevo.", "Comprobar acceso"],
]);
Footer(root, false);
