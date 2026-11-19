# Cómo defender los conceptos de MVP-004 que van más allá de la clase

Guía para explicar en la defensa las partes de compra y pago que no aparecen en el ejemplo del curso (`Api-Maquinaria`). Para cada concepto: qué problema resuelve, por qué el patrón de clase no alcanza y una explicación corta para decir en voz alta.

> Esta guía explica el código tal como está. No registra aprobaciones nuevas ni reemplaza las pruebas automatizadas que exige `AGENTS.md`.

## Punto de partida: qué sí sigue el patrón de clase

Antes de hablar de lo nuevo, conviene mostrar que la base es la misma que la del profe:

| Patrón de clase (`Api-Maquinaria`) | Dónde está en MVP-004 |
|---|---|
| Controller → Service → Repository → DbContext | `PagosController` → `PagosService` → `PagosRepository` → `BerrutiDbContext` |
| Constructores explícitos con campos `private readonly` | Controllers y services de `Controllers/Operational` y `Services/Operational` |
| `AddScoped<IServicio, Servicio>()` en `Program.cs` | Bloque de registros de compra y pago en `Program.cs` |
| `try/catch` en el controller: `ArgumentException` → 400, `KeyNotFoundException` → 404, `Exception` → 500 «Error interno del servidor» | `IntentosCompraController`, `PagosController`, `ConciliacionesPagoController` |
| Validación en el service lanzando `ArgumentException` | `PagosService`, `IntentosCompraService` |
| Mapeo manual entidad → DTO | `PagosService.CrearRespuesta` |
| `AddHttpClient` para Mercado Pago y `X-Idempotency-Key` | `MercadoPagoCheckoutClient` (el profe usa `MercadoPagoService` con `Guid.NewGuid()`) |

Frase de apertura sugerida: *«La arquitectura es la de clase. Lo que agregamos responde a dos requisitos que el ejemplo no tiene: varios pasajeros compitiendo por el mismo asiento y un proveedor de pagos que puede fallar o responder tarde.»*

## 1. Transacciones con `SELECT … FOR UPDATE`

**Dónde:** `PagosRepository` (bloquea `intento_compra`), `IntentosCompraRepository` (bloquea `salida`), `ConfirmacionesCompraRepository` y `ContadoresRepository`.

**Problema:** en el ejemplo de clase, crear un equipo hace «¿existe el código? → si no, lo agrego». Si dos pedidos llegan al mismo tiempo, los dos ven «no existe» y los dos insertan. Con equipos es un detalle; con asientos y pagos significa vender el mismo asiento dos veces o cobrar dos veces el mismo intento.

**Qué hace:** abre una transacción y bloquea la fila que se va a decidir (la salida, el intento o el contador). El segundo pedido espera hasta que el primero termina y entonces vuelve a leer el estado ya actualizado.

**Para decir:** *«`FOR UPDATE` es un candado sobre una fila: mientras una compra decide qué asiento toma, otra compra de la misma salida espera. Sin eso, el chequeo y la escritura pueden intercalarse y dos personas quedarían con el mismo asiento.»*

**Evidencia:** suite `tests/MySqlPurchase` (asignación concurrente sin repetir asiento) y el E2E del 2026-11-19, donde la segunda compra recibió el asiento 2 porque el 1 ya estaba vendido.

## 2. Savepoint al confirmar la compra

**Dónde:** `ConfirmacionesCompraRepository`, punto `antes_contadores_compra`.

**Problema:** al confirmar, primero se reservan los números de compra y tickets (`CO-`, `CS-`, `TI-`) y recién después se valida por última vez que el hold siga vigente y el asiento libre. Si esa última validación falla, hay que deshacer los números reservados, pero **conservar** que el pago pasó a devolución pendiente.

**Qué hace:** el savepoint es un «punto de guardado» dentro de la transacción. Si la validación final falla, se vuelve a ese punto (se descartan los contadores) y se guarda solo el cambio de estado del pago.

**Para decir:** *«Es como un Ctrl+Z parcial: deshacemos la emisión de tickets pero no perdemos el registro de que hay que devolver la plata.»*

**Si preguntan por qué no dos transacciones:** porque entre una y otra podría entrar otra confirmación y ver un estado intermedio.

## 3. Respuesta 202 cuando no se sabe si Mercado Pago cobró

**Dónde:** `PagosController` y `ConciliacionesPagoController`; estado `Desconocido` en `PagosService`.

**Problema:** si el pedido a Mercado Pago se envía pero la respuesta no llega (timeout, corte de red), no sabemos si la orden se creó. Si respondiéramos error y el cliente reintentara, podríamos crear un segundo cobro.

**Qué hace:** el pago queda `Desconocido`, la API responde `202 Accepted` («recibido, todavía sin resultado») y bloquea otro pago para ese intento hasta resolverlo consultando a Mercado Pago (`POST …/pagos/sincronizar`). Si el fallo ocurrió **antes** de enviar nada (por ejemplo, credencial TEST incorrecta), no hay ambigüedad: el pago queda `Rechazado` y se puede reintentar.

**Para decir:** *«Preferimos decir "no sé todavía" antes que arriesgar un doble cobro.»*

## 4. Clave de idempotencia guardada

**Dónde:** `Pago.ClaveCreacion` y `Pago.ReferenciaExterna`; `MercadoPagoCheckoutClient`.

**Problema:** el profe envía `X-Idempotency-Key` con un `Guid.NewGuid()` nuevo en cada llamada. Eso protege un reintento automático del mismo request, pero no un reintento después de reiniciar la API o de un 202.

**Qué hace:** el GUID se genera una vez, se guarda con el pago y se reutiliza. También es el `external_reference` de la orden, así que la API puede buscar la orden en Mercado Pago si perdió la respuesta.

**Para decir:** *«Es la misma idea que vimos en clase, pero el identificador queda guardado para poder reintentar o recuperar la orden sin duplicarla.»*

## 5. Clave de cancelación UUID v5

**Dónde:** `ConciliacionesPagoService.ClaveCancelacion`.

**Problema:** cuando vence el hold y nadie pagó, la API cancela la orden en Mercado Pago. Si esa cancelación se reintenta, tiene que usar la misma clave de idempotencia, pero no queremos agregar una columna nueva a la base solo para eso.

**Qué hace:** calcula una clave fija a partir de `ClaveCreacion` y el texto `"cancel"` con el algoritmo estándar UUID v5 (RFC 4122: un hash SHA-1 recortado con formato de GUID). La misma entrada siempre da la misma clave, distinta de la de creación y de la de devolución.

**Para decir:** *«Es una forma estándar de obtener siempre el mismo GUID a partir de otro dato. Lo usamos para no guardar una clave más en la base.»*

**Si piden algo más simple:** sería equivalente usar el texto `"{ClaveCreacion}-cancel"` como clave, porque Mercado Pago acepta cualquier texto en `X-Idempotency-Key`. Se eligió el formato GUID para mantener el mismo tipo de clave que en la creación.

## 6. Huella SHA-256 de la cotización

**Dónde:** `CotizacionesCompraService.CalcularHuella`.

**Problema:** el precio puede cambiar entre que el pasajero ve la cotización y acepta pagar. El cliente no debe mandar el monto (podría manipularlo), pero la API necesita saber qué cotización aceptó.

**Qué hace:** la API resume la cotización (intento, vencimiento, tramos y precios) en un hash. El cliente devuelve solo ese hash. Si al pagar la cotización actual da otro hash, la API responde `409` con la cotización nueva para que el pasajero la acepte explícitamente.

**Para decir:** *«Es una "firma" de la cotización: si cambió algo del precio, cambia la firma y pedimos que la vuelva a aceptar. El monto siempre lo calcula el servidor.»*

## 7. Otros detalles modernos de C#

| Concepto | Para qué | Para decir |
|---|---|---|
| `TimeProvider` | Las reglas de vencimiento dependen de la hora; en los tests se usa un reloj falso para probar el minuto exacto del vencimiento. | «Inyectamos el reloj igual que inyectamos el repositorio, para poder probarlo.» |
| `CancellationToken` | Si el usuario cierra la pestaña, ASP.NET cancela el request y no seguimos llamando a la base o a Mercado Pago. | «Es el mecanismo estándar de ASP.NET para cortar trabajo que nadie va a recibir.» |
| Excepciones propias (`AsientoNoDisponibleException`, `CotizacionModificadaException`, `FalloPreEnvioProveedorException`) | El controller distingue cada caso con su código HTTP sin convertir toda `InvalidOperationException` en 409, como recomienda la guía del curso. | «Cada excepción tiene un significado y el controller la traduce, igual que en clase con `KeyNotFoundException`.» |
| Clases estáticas de reglas (`ReglasInicioPago`, `PoliticaConfirmacionCompra`) | Reglas de negocio puras, sin base ni red, fáciles de probar. | «Son como los métodos privados `ValidarDatos` del profe, pero separados para reutilizarlos y probarlos.» |
| `record` y `switch` de patrones | Datos inmutables y el mapeo de estados de Mercado Pago a estados de dominio. | «Es sintaxis de C# para escribir lo mismo con menos código.» |

## Preguntas probables

- **¿Por qué no un webhook de Mercado Pago?** Requiere una URL pública y validar la firma de cada notificación; para el MVP local alcanza con que el pasajero pulse «Verificar pago», que consulta el estado real a Mercado Pago.
- **¿Qué pasa si el pasajero paga pero vuelve tarde?** La API compara su hora de verificación con el vencimiento del hold. Si ya venció, no emite tickets y pide la devolución. Es una decisión revisable: está pendiente de discutir si conviene usar la hora de aprobación de Mercado Pago.
- **¿Qué pasa si nadie paga la orden?** Al verificar con el hold vencido, la API cancela la orden en Mercado Pago y el pago queda `Rechazado`, así el pasajero puede iniciar otra compra.
- **¿Cómo saben que funciona?** Pruebas automatizadas (`tests/PaymentContracts`, `tests/PurchaseHolds`, `tests/MySqlPurchase`) y el recorrido E2E con Mercado Pago TEST del 2026-11-19: aprobación, aprobación tardía con devolución y cancelación por vencimiento.
