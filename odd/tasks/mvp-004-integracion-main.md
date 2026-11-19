# MVP-004: integrar la cadena con main actualizado

## Objetivo

Preparar ocho PR encadenadas basadas en `origin/main` 60f9142b64181ecb3ed084801a46c2bdd5b91892, conservando los 30 cortes MVP-004 y los commits `412bee7`/`7687494` sin perder los cambios FIN-005/JWT de Diego. No merge a main ni borrado de ramas. Cerrar las 30 PR anteriores solo después de verificar la nueva cadena y que no existan dependencias activas de Diego.

## Alcance y restricciones

- Ocho grupos aprobados y excepciones de tamaño autorizadas. Publicar solo si la integración, las pruebas y la comprobación remota final son correctas.
- No modificar las ramas antiguas ni realizar pagos, devoluciones o escrituras MySQL.
- Resolver conflictos preservando JWT y compra. Detener ante una resolución no demostrable con seguridad.
- Estrategia: cadena de ramas nuevas sobre main, con corte 1 en borrador; cada PR posterior apunta a la rama anterior.

## Tareas

- [x] I1: Integrar base de compra e inventario sobre main.
- [x] I2: Integrar intentos, persistencia y holds.
- [x] I3: Integrar Checkout Pro TEST.
- [x] I4: Integrar pagos, cotizaciones y reintentos.
- [ ] I5: Integrar confirmación, tickets y conciliación.
- [ ] I6: Integrar flujo Web con JWT vigente.
- [ ] I7: Integrar flujo Mobile con JWT vigente.
- [ ] I8: Integrar pruebas, correcciones Mercado Pago y estilo de clase hasta `7687494`.
- [ ] V1: Verificar ascendencia de todos los commits anteriores, código FIN-005 preservado, migraciones/snapshot y pruebas enfocadas.
- [ ] P1: Verificar de nuevo refs remotas y dependencias de Diego; hacer push/crear ocho PR; confirmar bases, títulos y cuerpos; cerrar 30 PR anteriores sin borrar ramas.

## Verificación proporcional

Primero revisar manualmente los ocho conflictos detectados en la simulación de merge: `docs/reference/api.md`, `docs/tasks.json`, snapshot EF, `appsettings.json`, `BuscarItinerario.razor`, Web `Program.cs`, `tests/AuthenticationSessions/Program.cs` y `tests/OperationalAdministration/Program.cs`. Revisar también cambios superpuestos en `BerrutiDbContext`, API `Program.cs`, autenticación y clientes. Ejecutar solo los builds y pruebas de JWT/compra/API/Web/Mobile que protejan el código integrado; no repetir suites previas si ninguna integración las afecta.

## Ruta y estado

Ruta delegada directa: integración multifichero con un escritor. La exploración solo lectura ya encontró ocho conflictos y 14 archivos superpuestos entre main y MVP-004. La primera rama nueva `mvp-004-main/01-base-compra` apunta inicialmente a main.

I1: merge local `--no-ff --no-commit` desde `mvp-004/01-base-compra` (`da5282c`) sobre `60f9142`. Se resolvieron las dos pruebas en conflicto conservando el bloque JWT sin estado y combinando los checks independientes del inventario/seed. Verificación: restauración API y `AuthenticationSessions` correcta; `dotnet build src/Api/API-Berruti/API-Berruti/API-Berruti.csproj --no-restore` PASS; `dotnet build tests/AuthenticationSessions/AuthenticationSessions.csproj --no-restore` PASS; `dotnet tests/AuthenticationSessions/bin/Debug/net10.0/AuthenticationSessions.dll --unit` PASS (68/68, incluye metadata EF sin conectar); `dotnet build tests/OperationalAdministration/OperationalAdministration.csproj --no-restore` PASS; `dotnet tests/OperationalAdministration/bin/Debug/net10.0/OperationalAdministration.dll` PASS (150 checks estructurales). Sin MySQL ni HTTP público. Commit de integración local: `a04c8a8`; sin push ni PR.

I2: rama local `mvp-004-main/02-reservas` desde `f1db45d`; merge de `mvp-004/02-reservas` (`0ee5507`), commit `b963635`. Los cuatro conflictos se reconciliaron preservando JWT y las reglas actuales: configuración `Jwt` + `Compra`; el snapshot final omite `Sesion` porque `20261118162624_QuitarSesion` la elimina, mientras el Designer de la migración I2 conserva su modelo histórico del 2026-11-16. En `docs/tasks.json` se conserva el ID `MVP-004`, el inicio histórico `in_progress` (dependencia `MVP-003` done) y la aprobación conceptual de la versión actual; la revisión cruzada y el resto de la compra siguen pendientes. Verificación: `dotnet build src/Api/API-Berruti/API-Berruti/API-Berruti.csproj --no-restore` PASS; `dotnet build tests/PurchaseHolds/PurchaseHolds.csproj --no-restore` PASS y su DLL PASS 74/74; `dotnet build tests/PurchaseHoldSchema/PurchaseHoldSchema.csproj --no-restore` PASS con advertencia NU1900 del índice de vulnerabilidades y su DLL PASS 12/12; `dotnet build tests/AuthenticationSessions/AuthenticationSessions.csproj --no-restore` PASS y DLL `--unit` PASS 68/68. Las pruebas usan fakes/metadatos y no demuestran HTTP integrado ni enforcement/concurrencia real de MySQL. No se ejecutaron `PaymentContracts`, MySQL ni pagos. Commit de integración `b963635`; evidencia local `7819272`; sin push ni PR.

I3: rama local `mvp-004-main/03-checkout-test` desde `7819272`; merge limpio de `mvp-004/03-checkout-test` (`41ad3a6`), commit `0b828bd`. `Program.cs` agrega el cliente TEST sin alterar `AgregarAutenticacionLocal` ni el pipeline JWT. Verificación: `dotnet build src/Api/API-Berruti/API-Berruti/API-Berruti.csproj --no-restore` PASS (0 warnings); `dotnet build tests/PaymentContracts/PaymentContracts.csproj --no-restore` PASS con una advertencia NU1900 del índice de vulnerabilidades; DLL PASS (114 checks, 0 fallos). El test usa `MockHttpMessageHandler` y credencial `TEST-contract-token`; no hizo solicitudes de red ni pagos reales. No se repitieron suites ajenas a I3. Sin push ni PR.

I4: rama local `mvp-004-main/04-pagos-cotizaciones` desde I3 (`4548a16`); merge local de `mvp-004/04-pagos-cotizaciones` (`7ec6c0d`), commit de integración `0d39489`. Se resolvió el conflicto del snapshot conservando el modelo de pagos y `TramoPago`, omitiendo la relación histórica `Sesion` eliminada por `20261118162624_QuitarSesion`; el Designer de I4 mantiene su modelo histórico. `Program.cs` conserva `AgregarAutenticacionLocal` y agrega los servicios de cotización/pago. `docs/reference/api.md` mantiene FIN-005 JWT sin estado y distingue el inicio/consulta de pago implementados de la conciliación, confirmación, tickets/QR y reintegros aún pendientes. Verificación: build API PASS (0 advertencias/errores); build PaymentContracts PASS (NU1900 al consultar el índice NuGet) y DLL PASS (203 checks); build PurchaseHoldSchema PASS (NU1900) y DLL PASS (22/22 checks, metadata/migraciones sin conectar). `git diff --cached --check` solo informa una línea en blanco final en el SQL generado; no se modificó a mano. Los harnesses no ejecutaron HTTP integrado, MySQL ni llamadas reales al proveedor. Sin push ni PR.
