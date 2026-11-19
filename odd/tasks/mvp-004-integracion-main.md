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
- [ ] I2: Integrar intentos, persistencia y holds.
- [ ] I3: Integrar Checkout Pro TEST.
- [ ] I4: Integrar pagos, cotizaciones y reintentos.
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
