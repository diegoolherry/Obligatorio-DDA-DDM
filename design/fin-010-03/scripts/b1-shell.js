// W00 · Navegación y pie (Base)
const root = I(null, { type: "frame", name: "W00-navegacion", x: 0, y: 0, width: 1280, height: "fit_content", layout: "vertical", fill: F("surface-50") });
const intro = V(root, { padding: [32, 80, 16, 80], gap: 8, name: "Introducción" });
T(intro, "Navegación Web", { s: 32, w: 800, lh: 1.19, ls: -0.5, fill: true });
T(intro, "Solo enlaces a páginas que existen. Los enlaces de Ubicación del servicio y Administración operativa aparecen según el rol; ocultarlos no autoriza nada: la API sigue comprobando el permiso.", { s: 16, c: "ink-700", fill: true, lh: 1.4 });
const L = (t) => { const b = V(root, { padding: [24, 80, 8, 80] }); T(b, t, { s: 14, w: 700, c: "ink-500", fill: true }); };
L("Visitante · página activa: Buscar viaje");
NavDesktop(root, "buscar", "visitante");
L("Pasajero con sesión · página activa: Ubicación del servicio");
NavDesktop(root, "ubic", "pasajero");
L("Administrador con sesión · página activa: Administración operativa · foco de teclado en Horarios (contorno blanco de 2 px)");
NavDesktop(root, "admin", "admin", "horarios");
L("Pie de página: solo marca y la fuente de los horarios; sin contacto, redes ni enlaces inventados");
Footer(root, false);
const n = V(root, { padding: [32, 80, 40, 80], gap: 8, name: "Notas" });
T(n, "Estados y foco", { s: 20, w: 700, lh: 1.25, fill: true });
T(n, "Enlace activo: relleno naranja con texto negro (6,19:1). Enlaces inactivos: blanco sobre negro (18,42:1). Foco: contorno visible de 2 px. Cada enlace es un <a> nativo, alcanzable con Tab; el orden de tabulación sigue el orden visual.", { s: 15, c: "ink-700", fill: true, lh: 1.45 });
