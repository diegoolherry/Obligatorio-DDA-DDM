# FIN-001 — Catálogo de rutas dirigidas y calendarios verificados
Rama `feature/fin-001-route-catalog` (base `main` c9d90ff). Asignado: Diego; revisor: Enzo. Backlog: `docs/tasks.json` FIN-001 (in_progress desde 2026-11-11T13:29:06-03:00), FIN-008 creado para el ABM.
Fuente: http://www.berruttiturismo.com.uy/horarios.html — imagen «HORARIOS DE INVIERNO 2026».

## Specs
- **S1 Fuente.** Usuario: «http://www.berruttiturismo.com.uy/horarios.html … tomamos esta de referencia porque es la real y veridica». La transcripción se versiona como datos; la imagen no se copia al repo.
- **S2 Vínculo con Servicio.** Elegido: «Catálogo aparte que genera Servicios (Recomendado)» — «Tablas nuevas: Ruta, ParadaRuta (con orden), SalidaPatron (hora por parada) y Calendario. Un generador crea un Servicio y su Tramo por cada par de paradas de cada salida, en un rango de fechas. No rompe la búsqueda, el admin, Mobile ni la compra de Enzo (MVP-004). La disponibilidad por intervalo se rediseña en FIN-003.»
- **S3 Feriados.** Elegido: «Tabla de feriados cargada por año (Recomendado)» — «Una tabla Feriado(fecha, tipo) con el seed de 2026: los no laborables 1/1, 1/5, 18/7, 25/8 y 25/12. Los días sin servicio quedan como excepción explícita. Turismo 2026 (30/3–5/4) se marca como «consultar en la agencia», sin horarios inventados.»
- **S4 Ombúes/Conchillas.** Elegido: «Dos alimentadores separados» — «Ombúes→Radial y Conchillas→Radial son dos rutas cortas, cada una con su hora de salida, que combinan en Radial. El horario solo da la salida, así que no sabemos a qué hora llegan a Radial.»
- **S5 Web.** Usuario: «El alcance seria solo el catalogo, pero podes registrar eso de ABM para capaz hacerlo dsp.» → consulta de solo lectura del catálogo; ABM registrado como FIN-008.
- **S6 Criterios FIN-001 (autoritativos, `docs/tasks.json`).**
  1. «Las cinco familias aprobadas y variantes Polancos tienen dirección, paradas ordenadas y salidas válidas; órdenes de distintas rutas no se comparan.»
  2. «Se validan días hábiles, fines de semana, feriados y excepciones por bloque; 02/03/26–20/12/26 solo se aplica al bloque principal Palmira–Colonia cuando corresponde.»
  3. «La transcripción completa y sus pruebas conservan llegadas aproximadas, documentan una fuente reproducible autorizada y no infieren tarifas, capacidades ni flota real.»

### Reglas derivadas (propuestas por el agente, a confirmar en revisión)
- R1 Solo genera Servicio un par de paradas con hora publicada en ambas; paradas sin hora (Polancos, bloques sin llegadas) no generan. Capacidad y política de asiento las da quien genera: no hay valor por defecto.
- R2 Calendario: fuera de vigencia del bloque → nada; excepción «sin servicio» → nada; «consultar agencia» → estado sin salidas; domingo o feriado → salidas «Domingos y feriados»; sábado → «Sábados» y «Lunes a sábado»; lunes a viernes → «Lunes a viernes», «Lunes a sábado» y, lunes y viernes, «Lunes y viernes». Las salidas de días hábiles no corren en feriados.
- R3 La primera hora de cada salida es la publicada; las siguientes son aproximadas.
- R4 Polancos se ubica entre Nueva Palmira y Carmelo, sin hora publicada (supuesto a confirmar).

## Tasks
- [x] T1 [S1,S6.3] Transcripción versionada (JSON) + validador + chequeos — inline — 29957fb (datos), 49eab2b (chequeos)
- [x] T2 [S2,S3,S4,S6.1] Entidades, migración `CatalogoHorarios` + SQL, seed — inline — 29957fb (aplicación en MySQL real pendiente: reset local de Diego)
- [x] T3 [S3,S6.2] Calendario + `GET api/catalogo`, `GET api/catalogo/salidas?fecha=` — inline — 49eab2b
- [x] T4 [S2,R1] Generador admin idempotente (`POST api/administracion/catalogo/servicios`) — inline — 49eab2b
- [x] T5 [S5] Página Web `/horarios` de solo lectura — inline — 49ec30e (recorrido manual pendiente)
- [x] T6 [S6] Docs (API, alcance, modelo, entidades, reset) y backlog — inline — commit docs

## Log
- L1 Usuario: «continuamos donde lo dejamos» → eligió «FIN-001 · Catálogo de rutas y calendarios».
- L2 Usuario: «La empresa tiene su web con un catalogo de horarios que es una foto subida ahi a la web.»
- L3 Usuario: «http://www.berruttiturismo.com.uy/horarios.html [Image#1] tomamos esta de referencia porque es la real y veridica»
- L4 Respuestas de diseño: S2, S3, S4 y S5 (texto literal arriba).
- L5 Verificación: `dotnet run --project tests/RouteCatalog/RouteCatalog.csproj --no-launch-profile` → 75/75; mutación (feriado ignorado) → 72/75, revertida. Regresión: OperationalAdministration 125 PASS, OperationalControllerValidation 4/4, Geolocation 27/27; builds API y Web sin advertencias. Las pruebas se escribieron después del código (no hubo RED previo), salvo la mutación.
- L6 Entrega: ~3.3k líneas en 3 commits (incluye Designer generado, JSON y SQL). Propuesta de PRs encadenados: (1) API = 29957fb + 49eab2b + docs; (2) Web = 49ec30e. Push y PR quedan a decisión de Diego.
- L7 Usuario: «podes hacer test adversariales, mientras yo hago lo de mySql q me pediste antes.» Mutaciones en copia aislada (fuera de src/): suite original 27/37 detectadas. Se agregaron 49 pruebas adversariales (AdversarialChecks.cs) y 2 correcciones halladas por ellas: catálogo sin bloques aceptado; tramo A→A cuando una ruta repite parada. Resultado: 124/124; mutaciones 38/39 detectadas (M38 y M39 verificadas a mano); sobrevive M14 (equivalente: un día que no opera ya no tiene tipos de día).
- L8 Hallazgo abierto para Diego: los años sin feriados ni excepciones cargados (p. ej. 2027) se responden como días normales; CAR-MER (sin fin de vigencia) y NP-CAR/NP-AGR (sin vigencia) mostrarían salidas el 1/1/2027.
- L9 Usuario: «dale me gusta el a.» y «para el a seria, que para el 2027 solo mostrar lo de los feriados y que no esta cargado el calendario de este año no?» → estado `CalendarioNoCargado` cuando el año no tiene feriados cargados; NP-COL sigue informando fuera de vigencia. TDD: RED 126/129, GREEN 129/129.
- L10 Usuario: «ya hice el reset». API local + MySQL real: catálogo 4 bloques, 14 rutas, 114 salidas, 5 feriados, 4 excepciones; 11/11 NP-COL 37, NP-CAR 22, CAR-MER 4, NP-AGR 0; 25/08 horario de domingos sin CAR-MER ni AGR; 01/05 sinServicio; 01/01/2027 calendarioNoCargado (NP-COL fueraDeVigencia); fecha mala 400; generar sin sesión 401.
- L11 Concurrencia real (script de Diego con su contraseña, fuera del repo), 16–20/11/2026, esperado 720 calculado desde la API pública: dos POST simultáneos → 200 (720 creados, 21 tramos) y 409; tercer POST → 0 creados, 720 existentes; la base tiene 720 servicios en la semana.
