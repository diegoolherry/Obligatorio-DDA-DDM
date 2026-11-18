# Preparar verificaciones de compra MVP-004

Esta guía separa evidencia local ya observada de pruebas futuras que requieren una base aislada o una operación real de Checkout Pro TEST. **No ejecutes los pasos de escritura sin autorización explícita para ese destino.** Ningún paso de esta guía autoriza usar producción.

## Estado de evidencia

- **Observado, offline/fake:** PaymentContracts 269/269; API build sin errores; WebPurchase 164/164 y build Web sin advertencias/errores; MobilePurchase 14/14 y `npm run typecheck` exit 0. Son harnesses deterministas, no prueban una base MySQL, HTTP integrado ni Checkout Pro real.
- **Preparado, no ejecutado:** `tests/MySqlPurchase` compila; `--plan` es de solo lectura y `--prepare` exige opt-in. No se ejecutaron consultas SQL, migraciones, seeds, API iniciada, transacción MySQL ni pago del proveedor.
- **Pendiente:** provisión externa de DB/cuenta, fixture nuevo, verificación MySQL, recorrido manual Web/Mobile, una transacción real TEST y revisión humana cruzada.

## Bloqueos para ejecución real

- `ConfiguracionAutenticacion.ValidarConexion` permite `PurchaseTests/berruticomprastests/berruti_purchase_test` solo en loopback:3306, además de los dos pares existentes. La DB y la cuenta deben crearse externamente por un DBA; el harness nunca las crea.
- `--prepare --allow-test-db-writes` requiere una base vacía (cero tablas, incluida la historia de migraciones), aplica migraciones versionadas y siembra un pasajero de prueba más el escenario operativo existente. Si falla a mitad, no limpia ni reintenta: detente y pide revisión DBA. `scripts/reset-local-db.ps1` sigue siendo destructivo y no debe usarse.
- La preparación lee `DemoSeed__Contrasena` solo tras opt-in. El flujo académico actual guarda y compara literalmente la contraseña; usa un valor de prueba desechable, nunca una contraseña real. No se imprime la contraseña; solo subject público e IDs de servicio.
- Para API, configura `ConnectionStrings__Berruti` en el proceso y usa `ASPNETCORE_ENVIRONMENT=PurchaseTests` con el listener HTTPS existente; el arranque normal no migra ni siembra. Web usa InteractiveServer, así que sus llamadas al API son server-side y no requieren CORS de navegador. El listener/certificado TLS no se validó. El túnel Cloudflare para Mobile requiere autorización separada.
- El review nativo sigue `correction_required`: `repair --preflight` fue de solo lectura y devolvió `unsupported`, una lineage, cero candidatos elegibles y ningún input requerido; no ofreció recuperación ni produjo ACK.

## Harness MySQL: solo con fixture aislado autorizado

El modo `--run` no migra, crea esquema, reinicia ni limpia datos. Una corrida autorizada escribe compras, pagos, tickets y asientos y los conserva; no se debe repetir sobre el mismo fixture. El modo separado `--prepare` solo aplica migraciones/seed tras opt-in explícito, provisión DBA previa y confirmación de base vacía. Si encuentra tablas o falla a mitad, rehúsa y no borra datos.

Un DBA debe crear externamente la base fresca `berruticomprastests` y el usuario no-root `berruti_purchase_test` con acceso solo de loopback y permisos requeridos; el harness nunca crea la base ni el usuario. La preparación aplica migraciones versionadas y siembra el pasajero activo `pasajero.purchase@example.test` más el escenario existente de mañana: primer tramo `sinAsiento`, segundo `asignacionAutomatica`, salida con 46 asientos. Comprueba cero tablas antes de migrar. Ante cualquier error no limpia ni reintenta. Imprime solo el subject público y los IDs de ambos servicios para configurar el harness; nunca imprime la contraseña desechable.

La preparación lee `BERUTI_MYSQL_PURCHASE_TEST_CONNECTION` y `DemoSeed__Contrasena` solo después de ambos flags. El modo `--run` también requiere `BERUTI_MYSQL_PURCHASE_TEST_PASSENGER_SUBJECT`, `BERUTI_MYSQL_PURCHASE_TEST_SERVICE_NO_SEAT` y `BERUTI_MYSQL_PURCHASE_TEST_SERVICE_ASSIGNED`, configurados privadamente desde la salida segura. No leas valores de `.env` ni los pegues en comandos, documentación o chat. La conexión debe coincidir con la tupla exacta de API para `PurchaseTests` en loopback:3306.

Consulta de plan, sin leer credenciales ni crear DbContext:

```powershell
dotnet run --no-restore --project tests/MySqlPurchase/MySqlPurchase.csproj -- --plan
```

Solo después de provisión DBA y autorización explícita, prepara la base vacía una vez:

```powershell
dotnet run --no-restore --project tests/MySqlPurchase/MySqlPurchase.csproj -- --prepare --allow-test-db-writes
```

Luego configura privadamente los tres valores públicos impresos. La verificación que escribe compras es otra operación y también requiere autorización explícita:

```powershell
dotnet run --no-restore --project tests/MySqlPurchase/MySqlPurchase.csproj -- --run --allow-test-db-writes
```

El harness actual cubre aprobación con dos tickets (un tramo sin asiento y otro asignado), idempotencia concurrente del mismo pago, asignación concurrente sin colisión/reuso de asiento confirmado, estado desconocido y rechazo con reintento dentro del hold original, aprobación tardía con devolución pendiente/rollback de contadores y fallo inyectado con rollback. Los órdenes de proveedor son DTOs controlados: esta prueba **no llama a Mercado Pago**. No agregues `--run` solo ni repitas sobre datos ya escritos; ambos son una condición de rechazo o de fixture no fresco.

| Escenario por verificar | Resultado requerido | Evidencia disponible / pendiente |
| --- | --- | --- |
| Aprobación en ambos clientes | API confirma; una compra, dos tickets distintos, primer tramo sin asiento y segundo con asignación automática. | Harness cubre persistencia con DTO aprobado; falta Checkout Pro TEST y recorrido UI real. |
| Rechazo y resultado desconocido | Cero tickets; desconocido bloquea otro pago; solo rechazo definitivo permite reintento con el mismo vencimiento original. | Harness usa órdenes controladas; falta transacción real TEST. |
| Aprobación tardía | Sin compra/tickets; devolución pendiente con clave estable y sin avance de contadores de emisión. | Harness controla el reloj y persiste contra MySQL; no acredita que Mercado Pago complete una devolución. |
| Reconciliación repetida/concurrente y asientos | Mismo pago conserva compra/tickets/contadores; reservas simultáneas no solapan ni reutilizan asiento confirmado tras vencer el hold. | Harness preparado; no ejecutado. |
| Propiedad y sesión | Pasajero B no puede consultar ni conservar en UI la compra/selección en caché de A tras cerrar sesión e ingresar como B. | API/servicios tienen pruebas fake; invalidación de UI se verificó estructuralmente, no con render ni servidor integrado. |
| Cotización cambiada | Mostrar precio actualizado, exigir nueva aceptación explícita y conservar el vencimiento original del intento. | Cliente/controlador cuentan con pruebas deterministas; el recorrido real queda pendiente. |

## Comprobaciones SQL posteriores de solo lectura

Ejecuta estas consultas únicamente con autorización de lectura al fixture y reemplaza `<IC>` y `<PG>` por identificadores observados. No selecciones ni copies `qr_opaco`; informa solo cantidades/identificadores de ticket. Son comprobaciones manuales y no forman parte de una corrida aún no autorizada.

```sql
SELECT ic.intento_id, ic.usuario_id, ic.hold_vence_utc,
       p.pago_id, p.estado, p.importe_esperado, p.importe_pagado, p.moneda,
       p.orden_proveedor_id, p.clave_devolucion,
       COUNT(DISTINCT c.compra_id) AS compras,
       COUNT(DISTINCT cs.compra_servicio_id) AS tramos_compra,
       COUNT(DISTINCT t.ticket_id) AS tickets,
       COUNT(DISTINCT t.qr_opaco) AS qr_distintos
FROM intento_compra ic
LEFT JOIN pago p ON p.intento_id = ic.intento_id
LEFT JOIN compra c ON c.intento_id = ic.intento_id AND c.pago_id = p.pago_id
LEFT JOIN compra_servicio cs ON cs.compra_id = c.compra_id
LEFT JOIN ticket t ON t.compra_servicio_id = cs.compra_servicio_id
WHERE ic.intento_id = '<IC>' AND p.pago_id = '<PG>'
GROUP BY ic.intento_id, ic.usuario_id, ic.hold_vence_utc, p.pago_id,
         p.estado, p.importe_esperado, p.importe_pagado, p.moneda,
         p.orden_proveedor_id, p.clave_devolucion;
```

La correlación se comprueba por intento **y pago**: un pago rechazado no debe heredar los tickets de otro pago aprobado del mismo intento. Una regresión de la consulta extraída de esta guía, con SQLite en memoria, falló antes del ajuste y pasó después: rechazado `0/0/0/0`, aprobado `1/2/2/2` (compras/tramos/tickets/QR distintos). Esto no acredita compatibilidad ni ejecución en MySQL.

```sql
SELECT ti.orden, ti.servicio_id, ti.tramo_id, ti.importe,
       ti.politica_asiento, ti.numero_asiento,
       t.ticket_id, (t.qr_opaco IS NOT NULL) AS qr_presente
FROM tramo_intento_compra ti
LEFT JOIN compra_servicio cs ON cs.intento_id = ti.intento_id AND cs.orden = ti.orden
LEFT JOIN ticket t ON t.compra_servicio_id = cs.compra_servicio_id
WHERE ti.intento_id = '<IC>'
ORDER BY ti.orden;
```

Para revisar solapamientos confirmados, el conteo debe ser cero cuando se comparen tickets de compras distintas con la misma salida y asiento, usando el intervalo estricto `a.orden_embarque < b.orden_desembarque AND b.orden_embarque < a.orden_desembarque`. No filtres tickets ya confirmados según el vencimiento del hold: la ocupación confirmada no desaparece al vencerlo. Esta consulta de colisión es una verificación pendiente, no un resultado observado.

## Checkout Pro TEST y clientes

Es un recorrido separado del harness MySQL. Requiere API configurada fuera del repositorio con `ConnectionStrings__Berruti` y credencial privada `MercadoPago__AccessToken` de TEST. Para usar la pareja aislada, configura esos valores privadamente en el proceso, configura `Api__BaseUrl` del Web como `https://localhost:7053` y arranca sin el perfil Development usando el listener HTTPS existente:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'PurchaseTests'
dotnet run --no-restore --project src/Api/API-Berruti/API-Berruti/API-Berruti.csproj --no-launch-profile -- --urls https://localhost:7053
```

El puerto HTTPS proviene del perfil existente; este comando futuro no fue ejecutado y el listener/certificado TLS requiere verificación manual tras autorización. El arranque normal no migra ni siembra. Nunca uses estas claves en `EXPO_PUBLIC_*`; no guardes tarjeta/CVV, tokens, credenciales ni QR en logs o capturas. No se validó una URL de retorno común ni deep link: después de cerrar el checkout, vuelve manualmente a la misma pestaña Web o a la app Mobile y pulsa **Verificar pago** para sincronizar el mismo intento. El retorno del navegador no confirma la compra.

En cada cliente, busca el itinerario de dos tramos, acepta explícitamente la cotización vigente, abre solo la URL HTTPS devuelta por la API y luego sincroniza el mismo intento. No crees otro intento/pago para resolver un resultado pendiente o desconocido. Confirma que el estado proviene de una respuesta autoritativa de la API, que la aprobación emite una compra con dos tickets distintos y el asiento esperado, y que rechazo/desconocido no emite tickets. Para aprobación tardía, confirma `DevolucionPendiente` y ausencia de compra/tickets; una solicitud de devolución no demuestra que el proveedor la completó.

El entorno TEST puede comunicarse con el proveedor y persistir cambios: requiere autorización separada, usuario de prueba y medios de pago de prueba indicados por el proveedor. No se ejecutó esta prueba ni una UI/E2E. La confirmación/visualización de QR, wallet y escaneo quedan en MVP-005.

## Referencias

- [API implementada](../reference/api.md#intentos-de-compra-corte-http)
- [Configuración de clientes y credenciales](client-api-configuration.md)
- [Reinicio local destructivo](local-database-reset.md)
