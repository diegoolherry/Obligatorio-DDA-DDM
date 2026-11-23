# FIN-017: venta de todos los tramos del catálogo hasta el 31/12/2026

Owner Diego, reviewer Enzo. Branch `feature/fin-017-venta-catalogo`, stacked on `fin-014-expo-router` (PR #133). Includes FIN-002 (vehicles, reassigned to Diego) and FIN-003 (interval availability).
Architecture map: Log L4. High-risk units: migrations (item 1) and concurrent seat sale (item 4) get independent verification.

## Specs

- **S1 Venta de todos los tramos.** «quiero q se puedan comprar cada uno de los tramos, con sus paradas en el medio, etc.» Every pair of stops (boarding before alighting) of every catalog route and departure is searchable and purchasable.
- **S2 Fechas.** «Quiero que se pueda comprar en cualquier dida hasta el ultimo dia del año 2026, se entiende?» Services exist for every operating day from the reset date through 2026-12-31.
- **S3 Asientos por intervalo (FIN-003).** «Resolver FIN-003 dentro de esto». Pair-services of the same departure share one seat inventory; a seat sold for [a,b) is unavailable for any overlapping [c,d) (a<d && c<b); adjacent intervals can reuse it. Concurrent purchases never double-sell. Hold and payment policy stay as in MVP-004 («Mantener la política actual de MVP-004»).
- **S4 Vehículos (FIN-002).** «Hago FIN-002 yo también». Vehicle seat configurations (2+2 layout, 22/28/42/46 seats, FIN-002 criteria) determine each departure's seats. Per-route mapping approved («Sí, esta propuesta»): Nueva Palmira–Colonia and Polancos variants 46; Carmelo–Mercedes 42; short routes (Ombúes/Conchillas–Radial, Carmelo–Nueva Palmira, Nueva Palmira–Agraciada) 28; demo Radial→Colonia 46.
- **S5 Precios estimados.** «No los tengo todos, hace un estimativo, como base los que ya estan.» + «Proporcional al tiempo de viaje». Tarifas for catalog tramos are derived from the 2 demo tarifas proportionally to scheduled travel time and are marked as estimates.
- **S6 Horas intermedias estimadas.** «Estimar horas intermedias (marcadas)». Routes with only a first-stop hour get estimated intermediate hours, marked as estimated wherever shown; documented as an exception to FIN-001 «no infieren».
- **S7 Calendario.** «Extender hasta el 31/12». The Palmira–Colonia block (vigencia 02/03/26–20/12/26) is extended to 31/12/2026, flagged as unverified.
- **S8 Reinicio local.** «Reinicio local genera todo el catálogo» and «nose si me gusta mucho eso poruqe si solo hace mostrar dos». The reset generates services, seats and tarifas for the whole catalog from today through 2026-12-31. The Ombúes→Radial→Colonia transfer flow is kept every day as well («Conservarlo además del catálogo»).
- **S9 Cobrador.** «Osea el cobrardor ve todos los servicios y el eliguee en cual compartir». The demo collector can share location on any service (assigned to all); the Compartir list shows today's services.
- **S10 Ubicación del servicio.** «Solo los servicios de hoy» and «Podes usar paginacion, que eso dimos en clase.» + «Lo de paguinacion lo vimos en Blazor, para mobile una flat list si». The list shows only today's services: paged in Blazor, FlatList in Mobile.
- **S11 Llegada aproximada.** «Sí, todas las paradas». Every catalog stop has coordinates so every service shows an approximate arrival.

## Tasks

| ID | Specs | Route | Commit |
|---|---|---|---|
| T1 Backlog: FIN-002 → Diego, create FIN-017, FIN-015/016 | S4 | inline | |
| T2 Calendar extension NP-COL to 31/12 + RouteCatalog tests | S7 | inline | |
| T3 FIN-002 vehicles: entity, layouts 22/28/42/46, route mapping, migration + SQL | S4 | inline, verify (item 1) | |
| T4 Generator v2: Salida per catalog departure/day, stops, seats from vehicle, stamped pair-services, range to 31/12 | S1, S2, S3 | inline, verify (item 1) | |
| T5 Estimated intermediate hours + estimated tarifas (marked) | S5, S6 | inline | |
| T6 Reset local: whole catalog today→31/12, demo flow kept, collector on all services, coordinates | S8, S9, S11 | inline | |
| T7 FIN-003 interval sale: adjacency/inclusion/separation/concurrent tests, idempotent confirmation | S3 | inline, verify (item 4) | |
| T8 Ubicación and Compartir today-only; API paging, Blazor pager, Mobile FlatList | S9, S10 | inline | |
| T9 Clients: "estimado" labels, docs, manual walkthrough | S5, S6, S7 | inline | |

## Log

- **L1** (2026-11-23, Diego): «aca la captura "C:\Users\User\Downloads\WhatsApp Image 2026-11-23 at 4.52.18 PM.jpeg" Mira vamos a dejar esto claro, esto es un proyecto serio, quiero q se puedan comprar cada uno de los tramos, con sus paradas en el medio, etc. SI tenes la minima duda decime. Quiero que se pueda comprar en cualquier dida hasta el ultimo dia del año 2026, se entiende? Dsp sobre el reinicio local ese, nose si me gusta mucho eso poruqe si solo hace mostrar dos»
- **L2** (2026-11-23, Diego decisions): prices «No los tengo todos, hace un estimativo, como base los que ya estan»; seats «Resolver FIN-003 dentro de esto»; data «Reinicio local genera todo el catálogo»; Ubicación «Solo los servicios de hoy»; FIN-002 «Hago FIN-002 yo también»; calendar «Extender hasta el 31/12»; price rule «Proporcional al tiempo de viaje»; hold «Mantener la política actual de MVP-004»; «Podes usar paginacion, que eso dimos en clase.»
- **L3** (2026-11-23, Diego decisions): hourless routes «Estimar horas intermedias (marcadas)»; demo flow «Conservarlo además del catálogo»; collector «Osea el cobrardor ve todos los servicios y el eliguee en cual compartir»; vehicles «Por ruta (vos me decís cuál)» then «Sí, esta propuesta»; paging «Lo de paguinacion lo vimos en Blazor, para mobile una flat list si»; coordinates «Sí, todas las paradas».
- **L4** Architecture map (explorer, read-only): Servicio is one row per stop pair; `Salida` + `ParadaSalida` + `AsientoSalida` already group pairs and interval overlap/holds already exist in `IntentosCompraRepository` (MVP-004), but catalog-generated services have no `SalidaId`/orders so they cannot be bought with a seat. Generator (`GeneracionServiciosService`) creates no tarifas, no Salida, cap 31 days. NP-COL `vigenteHasta` 2026-12-20 in `Data/Catalogo/horarios-invierno-2026.json:35`, asserted in `tests/RouteCatalog`. Reset-source strings pinned by `tests/OperationalAdministration/Program.cs:125-127`. `GET /api/ubicacion/servicios` has no date filter or paging. Estimated volume to 31/12: ~4,600 services, ~770 departures (SA- limit 50,000). No pagination example found in the repo skills.
- **L5** Pending separately: Mobile banner «No se pudo consultar si tenés una compra pendiente» seen during FIN-014 walkthrough step 4; cause not yet reproduced (asked Diego which account and whether «Reintentar» cleared it).
