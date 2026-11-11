MVP-002-01 — Horario de servicio como instantes y datos demo como seed (plan B)
Rama `feature/mvp-002-01-schedule-seed`, PR de corrección posterior al merge de la pila MVP-002 (#61-#66). Responsable Diego, revisor Enzo.

## Specs

- S1. "Salida + Llegada (DateTime)" — `Servicio` reemplaza `Fecha` + `HoraSalida`/`HoraLlegada` (`TimeSpan`) por `Salida` y `Llegada` `DateTime`, con check constraint `llegada > salida`.
- S2. "Hora local Uruguay" — los instantes se guardan en hora local de Uruguay, sin conversión UTC.
- S3. El cambio se aplica con una migración EF Core **nueva** (la anterior ya está en `main`), con su script SQL generado en `scripts/database/`, sin editar a mano el SQL generado; los servicios existentes en una base local se convierten sin perderse.
- S4. "Seed fuera de la API (recomendada): se corre después de dotnet ef database update, con un script o un proyecto de consola [...] La API queda limpia y recrear la base es un solo comando." — se quita el endpoint `POST /api/administracion/escenario-demo/preparar-demo` (controller, service, repository, DTOs) y su formulario/botón Blazor.
- S5. Los datos demo (3 paradas Ombúes, Radial de Conchillas, Colonia; 2 tramos; servicio Ombúes→Radial `SinAsiento` y Radial→Colonia `AsignacionAutomatica`; 2 tarifas) se cargan al recrear la base local con el comando existente `scripts/reset-local-db.ps1`.
- S6. El CRUD de paradas, tramos, servicios y tarifas (#62/#64) se conserva; el formulario de servicios usa salida y llegada con fecha y hora.
- S7. Backlog: hijo `MVP-002-01` con `parentId` `MVP-002`; MVP-002 no pasa a `done` hasta mergear este PR.

## Tasks

- T1 (S1-S3, S6) Modelo, mapeo, migración + SQL, DTO/servicio/repositorio API, DTO/formulario Web, checks. Route: worker. Commit: 8a8a397.
- T2 (S4-S5) Quitar endpoint demo y su UI/cliente/scripts/checks; sembrar escenario operativo en `ReinicioLocal`. Route: inline + worker. Commit: e827b09 (retiro), 8a8a397 (seed).
- T3 (S7) Backlog hijo y docs (`api.md`, guía de reinicio, registro MVP-002). Route: inline. Commit: este commit de backlog.

## Log

- L1 (usuario, 2026-11-10): «dale. Mergea lso PR y dale con la B. El esta de acuerdo ya, se lo comente por wasap»
- L2 (usuario, decisiones previas): horario «Salida + Llegada (DateTime)», zona «Hora local Uruguay»; seed «Seed fuera de la API (recomendada)». Enzo de acuerdo según reporte del usuario (WhatsApp).
- L3 (evidencia): pila #61-#66 mergeada en `main` (`1a75964`); builds API/Web 0/0; checks 107+46, 4/4, 50/50, 92/92, 46/46 PASS antes del merge.
- L4 (decisión técnica): el repo ya tiene el comando único de recreación `scripts/reset-local-db.ps1` → `dotnet run -- --reset-local` (`Data/ReinicioLocal.cs`), que siembra cuentas demo sin exponer HTTP. El escenario operativo se siembra ahí en lugar de un `.sql` aparte, para mantener un solo comando. Valores tomados de `scripts/check-operational-demo-runtime.ps1` (08:00-08:30 y 08:45-10:00, capacidades 30/40, tarifas 100.00/250.00); fecha relativa (día siguiente al reinicio) para que la demo no quede en el pasado.
- L5 (evidencia): builds API/Web 0/0; OperationalAdministration 84 PASS (RED de horario observado como fallo de compilación); OperationalControllerValidation 4/4; WebAuthentication 92/92; AuthenticationSessions 46/46; AuthenticationContracts 49/50 por residuo local no versionado src/Shared/ (bin/obj), ajeno al cambio. Migración HorarioServicio y seed NO ejecutados contra MySQL: el reset AuthenticationTests fue rechazado por el guard de conexión; pendiente prueba local del usuario.
- L6 (reporte del usuario, MySQL local): update-local-db.ps1 -Apply aplicó 20261109231033_EsquemaOperativa y 20261110184629_HorarioServicio, Done (servicio vacío: conversión de filas no ejercitada). reset-local-db.ps1: 10 tablas borradas, esquema recreado, 3 cuentas demo, escenario operativo 2026-11-11 (3 paradas, 2 tramos, 2 servicios, 2 tarifas).
- L7 (evidencia, recorrido ego browser como admin.demo, contraseña ingresada por el usuario): /admin/operativa muestra 3 paradas, 2 tramos, servicios 2026-11-11 08:00-08:30 cap. 30 Sin asiento y 08:45-10:00 cap. 40 Asignación automática, tarifas 100,00 y 250,00 vigentes 2026-11-11 a 2027-11-11. Llegada 09:00 < salida 10:00: mensaje "La llegada debe ser posterior a la salida." y 2 filas sin cambio (validación del formulario). Servicio 2026-11-12 23:30 → 2026-11-13 01:15 creado, editado (capacidad 25, horario conservado) y eliminado con confirmación; quedan los 2 servicios del seed. Web requiere Api__BaseUrl=https://localhost:7053/.
- L8 (CI #67 + decisión del usuario): `validate` falló en `git diff --check` por una línea en blanco al final de `20261110184629_HorarioServicio.sql`, que EF genera así (se confirmó regenerándolo). El usuario eligió «Recortar la línea final»: el script quedó idéntico a la salida de EF salvo esa línea, se documentó en `local-database-reset.md` y `git diff --check` local da OK. Commit de este cambio, sin push.
