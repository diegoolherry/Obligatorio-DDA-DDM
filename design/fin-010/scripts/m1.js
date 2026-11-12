// Pantallas 1-5: acceso, acceso con error, buscar, resultados, mis tickets
const DX = 455, DY = 900;
function title(b, t, sub) { const g = V(b, { gap: 4 }); T(g, t, { s: 24, w: 700, lh: 1.2, fill: true }); if (sub) T(g, sub, { s: 14, c: "ink-500", fill: true }); return g; }

// 1. Acceso
let b = Screen("M01 Acceso", 0 * DX, 0);
title(b, "Ingresá a tu cuenta", "Usá el correo con el que te registraste.");
Field(b, "Correo", "lucia.pereyra@ejemplo.com", { icon: "mail" });
Field(b, "Contraseña", "••••••••••", { icon: "lock", trail: "eye" });
Btn(b, "Ingresar", { icon: "log-in" });

// 2. Acceso con error
b = Screen("M02 Acceso - error", 1 * DX, 0);
title(b, "Ingresá a tu cuenta", "Usá el correo con el que te registraste.");
Banner(b, "error", "No pudimos iniciar tu sesión", "Revisá el correo y la contraseña e intentá de nuevo.");
Field(b, "Correo", "lucia.pereyra@ejemplo.com", { icon: "mail" });
Field(b, "Contraseña", "••••••••", { icon: "lock", trail: "eye", st: "error", error: "La contraseña no coincide con la cuenta." });
Btn(b, "Ingresar", { icon: "log-in" });
T(b, "Tus datos se conservan para que no tengas que escribirlos de nuevo.", { s: 14, c: "ink-500", fill: true, a: "center" });

// 3. Buscar viaje
b = Screen("M03 Buscar viaje", 2 * DX, 0, { tabs: TABS_P, active: 0 });
title(b, "Buscar viaje", "Elegí origen, destino y fecha.");
Field(b, "Origen", "Ombúes de Lavalle", { icon: "map-pin", trail: "chevron-down" });
Field(b, "Destino", "Colonia", { icon: "flag", trail: "chevron-down" });
Field(b, "Fecha", "Vie 13/11/2026", { icon: "calendar", trail: "chevron-down" });
Btn(b, "Buscar viajes", { icon: "search" });
Banner(b, "info", "Horarios sujetos a cambios", "Confirmá el servicio antes de viajar.");

// 4. Resultados
b = Screen("M04 Resultados de búsqueda", 3 * DX, 0, { back: true, title: "Resultados", tabs: TABS_P, active: 0 });
const sm = V(b, { gap: 2 });
T(sm, "Ombúes de Lavalle a Colonia", { s: 18, w: 700, fill: true, lh: 1.25 });
T(sm, "Vie 13/11/2026 · 2 opciones", { s: 14, c: "ink-500", fill: true });
ItinCard(b, { kind: "Directo", precio: "$ 320", legs: [{ h1: "06:15", h2: "08:20", a: "Ombúes de Lavalle", b: "Colonia" }], dur: "2 h 05 min · sin transbordos" });
ItinCard(b, { kind: "2 tramos", precio: "$ 280", trans: "Transbordo en Radial · 15 min de espera",
  legs: [{ h1: "07:00", h2: "07:40", a: "Ombúes de Lavalle", b: "Radial" }, { h1: "07:55", h2: "08:45", a: "Radial", b: "Colonia" }], dur: "1 h 45 min en total · 2 tickets, uno por tramo" });

// 5. Mis tickets
b = Screen("M05 Mis tickets", 4 * DX, 0, { tabs: TABS_P, active: 1 });
title(b, "Mis tickets", "Un ticket por cada tramo.");
TicketCard(b, { from: "Ombúes de Lavalle", to: "Radial", st: "valido", serv: "07:00", fecha: "Vie 13/11/2026" });
TicketCard(b, { from: "Radial", to: "Colonia", st: "valido", serv: "07:55", fecha: "Vie 13/11/2026" });
TicketCard(b, { from: "Nueva Palmira", to: "Carmelo", st: "utilizado", serv: "09:15", fecha: "Mié 11/11/2026" });
