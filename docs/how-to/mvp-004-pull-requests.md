# MVP-004 — borradores locales de PR en cadena

**Estado:** hay 27 borradores encadenados previos; el corte 28 de preparación aislada está en implementación en `test/mvp-004-entorno-aislado`, basado en `d7b0a46`, sin commit todavía. El corte de reintentos incluye `00fc202`, `13858ba` y `fd80d0b`; aliases e historial se conservan. Los cuerpos son locales y no publicados: no hay PRs remotos, números ni URLs.

## Base de cadena y límites

- Tracker: feat/mvp-004-base-compra, base main, head da5282c. Su diff real es 31 archivos, +2.876/−21 = 2.897 líneas; el usuario aprobó explícitamente esa excepción de tamaño. Sigue bloqueado para merge hasta revisión e integración verificables; abrir solo como borrador/no-merge.
- feat/mvp-004-attempt-hold en c1e3605 es una rama hermana, no ancestro de la cadena activa; no usarla como base. feat/mvp-004 sigue en main. Alias antiguos y originales permanecen intactos.
- Cada cuerpo indica base, head, conteo Git, dependencia anterior, siguiente corte, alcance, evidencia y reversión acotada. Los resultados históricos se citan como tales; esta preparación no repitió suites.
- El orden de los borradores forma una cadena: cada base es el alias anterior seleccionado; el head aparece con el marcador 📍. Las dos combinaciones autorizadas son f599a70..0ee5507 (intentos HTTP + política) y 08004ca..d6b1500 (flujo Mobile + evidencia).

## Borradores completos

### 01 — MVP-004: incorporar contratos de compra e inventario de salida

~~~~markdown
Cadena: main → 📍 feat/mvp-004-base-compra (tracker, borrador sin merge).
Base: main. Head: feat/mvp-004-base-compra. Diff: 31 archivos, +2.876/−21 (2.897).
Alcance: base común de contratos, inventario y secuencia de paradas necesaria para la cadena.
Verificación: evidencia histórica previa; no se repitieron suites. Sin aprobación de excepción para este diff.
Dependencia: raíz. Siguiente: feat/mvp-004-esquema-intentos. Mantener no-merge; no retirar commits previos.
Reversión: retirar únicamente este tracker; no combinarlo ni modificar main.
~~~~

### 02 — MVP-004: preparar persistencia de intentos y holds

~~~~markdown
Cadena: feat/mvp-004-base-compra → 📍 feat/mvp-004-esquema-intentos.
Base: feat/mvp-004-base-compra. Head: feat/mvp-004-esquema-intentos. Diff: 12 archivos, +1.994/−3 (1.997).
Alcance: modelos, migración y checks offline de esquema.
Verificación: el tracker registra comprobaciones metadata/migración; no prueba MySQL.
Dependencia: feat/mvp-004-base-compra. Siguiente: feat/mvp-004-gestion-reservas. Revertir solo este corte.
~~~~

### 03 — MVP-004: implementar la gestión de reservas temporales

~~~~markdown
Cadena: feat/mvp-004-esquema-intentos → 📍 feat/mvp-004-gestion-reservas.
Base: feat/mvp-004-esquema-intentos. Head: feat/mvp-004-gestion-reservas. Diff completo: 14 archivos, +1.056/−16 (1.072).
Alcance runtime: 1.055 líneas; el delta restante es +10/−7 del tracker. Mantener esa distinción.
Verificación: PurchaseHolds 74/74 y checks offline registrados; no prueba MySQL.
Dependencia: feat/mvp-004-esquema-intentos. Siguiente: feat/mvp-004-intentos-y-plazos. Revertir solo runtime.
~~~~

### 04 — MVP-004: exponer intentos y respetar el plazo del hold

~~~~markdown
Cadena: feat/mvp-004-gestion-reservas → 📍 feat/mvp-004-intentos-y-plazos.
Base: feat/mvp-004-gestion-reservas. Head: feat/mvp-004-intentos-y-plazos. Diff: 8 archivos, +329/−7 (336).
Alcance: HTTP autenticado de intentos y política pura de confirmación antes del vencimiento.
Verificación: contratos/política determinista según tracker; no DB ni servicio HTTP iniciado.
Dependencia: feat/mvp-004-gestion-reservas. Siguiente: feat/mvp-004-ordenes-test. Revertir solo esta combinación.
~~~~

### 05 — MVP-004: verificar órdenes TEST de Checkout Pro

~~~~markdown
Cadena: feat/mvp-004-intentos-y-plazos → 📍 feat/mvp-004-ordenes-test.
Base: feat/mvp-004-intentos-y-plazos. Head: feat/mvp-004-ordenes-test. Diff: 9 archivos, +390/−1 (391).
Alcance: adaptador de consulta de órdenes y pruebas HTTP simuladas.
Verificación: pruebas del cliente falso registradas; no se consultó Mercado Pago real ni se usaron credenciales.
Dependencia: feat/mvp-004-intentos-y-plazos. Siguiente: feat/mvp-004-checkout-devoluciones. Revertir solo el adaptador.
~~~~

### 06 — MVP-004: crear checkout TEST y solicitar devoluciones

~~~~markdown
Cadena: feat/mvp-004-ordenes-test → 📍 feat/mvp-004-checkout-devoluciones.
Base: feat/mvp-004-ordenes-test. Head: feat/mvp-004-checkout-devoluciones. Diff: 7 archivos, +175/−9 (184).
Alcance: creación Checkout Pro TEST y solicitud de devolución completa.
Verificación: HTTP simulado; una solicitud no acredita que el proveedor completó la devolución.
Dependencia: feat/mvp-004-ordenes-test. Siguiente: feat/mvp-004-persistencia-pagos. Revertir solo este corte.
~~~~

### 07 — MVP-004: persistir pagos, precios aceptados y tickets

~~~~markdown
Cadena: feat/mvp-004-checkout-devoluciones → 📍 feat/mvp-004-persistencia-pagos.
Base: feat/mvp-004-checkout-devoluciones. Head: feat/mvp-004-persistencia-pagos. Diff: 15 archivos, +2.656/−8 (2.664).
Alcance: persistencia de pagos/precios y modelos de compra/ticket; conservar la excepción de tamaño ya aprobada.
Verificación: checks offline y build registrados; no afirma enforcement ni ejecución MySQL.
Dependencia: feat/mvp-004-checkout-devoluciones. Siguiente: fix/mvp-004-prefijos-contador. Revertir solo este corte aprobado.
~~~~

### 08 — MVP-004: alinear metadatos de prefijos de contador

~~~~markdown
Cadena: feat/mvp-004-persistencia-pagos → 📍 fix/mvp-004-prefijos-contador.
Base: feat/mvp-004-persistencia-pagos. Head: fix/mvp-004-prefijos-contador. Diff: 5 archivos, +31/−3 (34).
Alcance: coherencia del allowlist EF y regresión de metadatos.
Verificación: suite metadata/migración 22/22 registrada; sin conexión de DB.
Dependencia: feat/mvp-004-persistencia-pagos. Siguiente: feat/mvp-004-recotizacion. Revertir solo la corrección de prefijos.
~~~~

### 09 — MVP-004: recotizar sin renovar el hold

~~~~markdown
Cadena: fix/mvp-004-prefijos-contador → 📍 feat/mvp-004-recotizacion.
Base: fix/mvp-004-prefijos-contador. Head: feat/mvp-004-recotizacion. Diff: 7 archivos, +292/−1 (293).
Alcance: recotización por fecha de viaje conservando el vencimiento original.
Verificación: checks deterministas de cotización registrados; el pasajero debe aceptar cualquier cambio.
Dependencia: fix/mvp-004-prefijos-contador. Siguiente: feat/mvp-004-recotizacion-http. Revertir solo el servicio puro.
~~~~

### 10 — MVP-004: exponer recotización autenticada

~~~~markdown
Cadena: feat/mvp-004-recotizacion → 📍 feat/mvp-004-recotizacion-http.
Base: feat/mvp-004-recotizacion. Head: feat/mvp-004-recotizacion-http. Diff: 6 archivos, +124/−4 (128).
Alcance: endpoint/contrato de cotización autenticada; no inicia pago.
Verificación: PaymentContracts y build API registrados en tracker; no se repitieron.
Dependencia: feat/mvp-004-recotizacion. Siguiente: feat/mvp-004-inicio-pago. Revertir solo este endpoint.
~~~~

### 11 — MVP-004: reservar pagos antes del checkout TEST

~~~~markdown
Cadena: feat/mvp-004-recotizacion-http → 📍 feat/mvp-004-inicio-pago.
Base: feat/mvp-004-recotizacion-http. Head: feat/mvp-004-inicio-pago. Diff: 16 archivos, +1.026/−4 (1.030).
Alcance: reserva durable y servicio de inicio; mantener la excepción de tamaño ya aprobada.
Verificación: harness de contratos/build offline registrados; no proveedor, MySQL ni API iniciada.
Dependencia: feat/mvp-004-recotizacion-http. Siguiente: feat/mvp-004-rechazo-y-reintento-http. Revertir solo este corte.
~~~~

### 12 — MVP-004: permitir reintento tras rechazo definitivo

~~~~markdown
Cadena: feat/mvp-004-inicio-pago → 📍 feat/mvp-004-rechazo-y-reintento-http.
Base: feat/mvp-004-inicio-pago. Head: feat/mvp-004-rechazo-y-reintento-http. Diff: 7 archivos, +347/−5 (352).
Alcance: rechazo definitivo permite reintento; pendiente/desconocido mantiene el bloqueo.
Verificación: contratos/API offline documentados en tracker; sin MySQL ni compra integrada.
Dependencia: feat/mvp-004-inicio-pago. Siguiente: feat/mvp-004-emision-tickets. Revertir solo reglas de retry HTTP.
~~~~

### 13 — MVP-004: preparar tickets desde un pago aprobado

~~~~markdown
Cadena: feat/mvp-004-rechazo-y-reintento-http → 📍 feat/mvp-004-emision-tickets.
Base: feat/mvp-004-rechazo-y-reintento-http. Head: feat/mvp-004-emision-tickets. Diff: 6 archivos, +375/−0 (375).
Alcance: materializador puro por tramo, precios congelados y QR opacos.
Verificación: comprobaciones deterministas registradas; no representa persistencia SQL.
Dependencia: feat/mvp-004-rechazo-y-reintento-http. Siguiente: fix/mvp-004-ocupacion-asientos. Revertir solo materialización y checks.
~~~~

### 14 — MVP-004: respetar ocupación permanente de asientos

~~~~markdown
Cadena: feat/mvp-004-emision-tickets → 📍 fix/mvp-004-ocupacion-asientos.
Base: feat/mvp-004-emision-tickets. Head: fix/mvp-004-ocupacion-asientos. Diff: 4 archivos, +181/−3 (184).
Alcance: tickets confirmados bloquean asiento en la misma salida e intervalos solapados.
Verificación: query seam tests offline; no claim de traducción/ejecución MySQL.
Dependencia: feat/mvp-004-emision-tickets. Siguiente: feat/mvp-004-confirmacion-atomica. Revertir solo ocupación confirmada.
~~~~

### 15 — MVP-004: persistir compra y tickets de forma atómica

~~~~markdown
Cadena: fix/mvp-004-ocupacion-asientos → 📍 feat/mvp-004-confirmacion-atomica.
Base: fix/mvp-004-ocupacion-asientos. Head: feat/mvp-004-confirmacion-atomica. Diff: 7 archivos, +367/−0 (367).
Alcance: transacción de confirmación, compra, tramos y tickets.
Verificación: PaymentContracts fake/build offline registrados; transacción/concurrencia real MySQL no ejecutada.
Dependencia: fix/mvp-004-ocupacion-asientos. Siguiente: feat/mvp-004-recuperacion-orden. Revertir solo confirmación persistida.
~~~~

### 16 — MVP-004: recuperar orden sin duplicar checkout

~~~~markdown
Cadena: feat/mvp-004-confirmacion-atomica → 📍 feat/mvp-004-recuperacion-orden.
Base: feat/mvp-004-confirmacion-atomica. Head: feat/mvp-004-recuperacion-orden. Diff: 4 archivos, +141/−0 (141).
Alcance: búsqueda autoritativa de orden por referencia para recuperar respuesta perdida sin crear otra orden.
Verificación: HTTP simulado; no operación real del proveedor.
Dependencia: feat/mvp-004-confirmacion-atomica. Siguiente: feat/mvp-004-conciliacion-pagos. Revertir solo recuperación de orden.
~~~~

### 17 — MVP-004: conciliar pago y aprobación tardía

~~~~markdown
Cadena: feat/mvp-004-recuperacion-orden → 📍 feat/mvp-004-conciliacion-pagos.
Base: feat/mvp-004-recuperacion-orden. Head: feat/mvp-004-conciliacion-pagos. Diff: 3 archivos, +447/−7 (454).
Alcance: conciliación fuera de transacción de red, expiración y seguimiento de devolución; conservar excepción de tamaño registrada.
Verificación: PaymentContracts/API offline según tracker; no MySQL ni proveedor real.
Dependencia: feat/mvp-004-recuperacion-orden. Siguiente: feat/mvp-004-sincronizacion-pagos. Revertir solo el servicio.
~~~~

### 18 — MVP-004: exponer sincronización autenticada de pago

~~~~markdown
Cadena: feat/mvp-004-conciliacion-pagos → 📍 feat/mvp-004-sincronizacion-pagos.
Base: feat/mvp-004-conciliacion-pagos. Head: feat/mvp-004-sincronizacion-pagos. Diff: 5 archivos, +132/−0 (132).
Alcance: endpoint, DI y respuesta tipada para sincronización.
Verificación: pruebas directas de controller y build registrados; no API iniciada.
Dependencia: feat/mvp-004-conciliacion-pagos. Siguiente: feat/mvp-004-cliente-web. Revertir solo el límite HTTP.
~~~~

### 19 — MVP-004: agregar cliente autenticado de compra Web

~~~~markdown
Cadena: feat/mvp-004-sincronizacion-pagos → 📍 feat/mvp-004-cliente-web.
Base: feat/mvp-004-sincronizacion-pagos. Head: feat/mvp-004-cliente-web. Diff: 6 archivos, +319/−0 (319).
Alcance: cliente/contratos tipados y pruebas fake.
Verificación: pruebas de contrato offline registradas; UI/E2E y API integrada pendientes.
Dependencia: feat/mvp-004-sincronizacion-pagos. Siguiente: fix/mvp-004-validacion-pagos-web. Revertir solo el cliente.
~~~~

### 20 — MVP-004: validar respuestas de pago Web

~~~~markdown
Cadena: feat/mvp-004-cliente-web → 📍 fix/mvp-004-validacion-pagos-web.
Base: feat/mvp-004-cliente-web. Head: fix/mvp-004-validacion-pagos-web. Diff: 5 archivos, +316/−29 (345).
Alcance: validar estados/respuestas y asegurar salida controlada de los harnesses.
Verificación: harnesses Web/PaymentContracts 164/164 y 269/269, apphosts terminan con código esperado; no atribuir diálogos históricos.
Dependencia: feat/mvp-004-cliente-web. Siguiente: feat/mvp-004-flujo-web. Revertir solo guardias y salida de tests.
~~~~

### 21 — MVP-004: conectar checkout y confirmación Web

~~~~markdown
Cadena: fix/mvp-004-validacion-pagos-web → 📍 feat/mvp-004-flujo-web.
Base: fix/mvp-004-validacion-pagos-web. Head: feat/mvp-004-flujo-web. Diff: 5 archivos, +305/−28 (333).
Alcance: integrar compra en itinerario, abrir la URL HTTPS de API y sincronizar el mismo intento.
Verificación: harnesses fake/build registrados; no recorrido manual, browser E2E, API ni proveedor.
Dependencia: fix/mvp-004-validacion-pagos-web. Siguiente: feat/mvp-004-cliente-mobile. Revertir solo UI de este corte.
~~~~

### 22 — MVP-004: agregar cliente tipado de compra Mobile

~~~~markdown
Cadena: feat/mvp-004-flujo-web → 📍 feat/mvp-004-cliente-mobile.
Base: feat/mvp-004-flujo-web. Head: feat/mvp-004-cliente-mobile. Diff: 6 archivos, +282/−3 (285).
Alcance: contratos/servicio HTTP autenticado; no UI.
Verificación: harness Node M1 y typecheck registrados; sin render/API integrada.
Dependencia: feat/mvp-004-flujo-web. Siguiente: feat/mvp-004-compra-mobile. Revertir solo cliente/contratos M1.
~~~~

### 23 — MVP-004: conectar compra Mobile y registrar evidencia

~~~~markdown
Cadena: feat/mvp-004-cliente-mobile → 📍 feat/mvp-004-compra-mobile.
Base: feat/mvp-004-cliente-mobile. Head: feat/mvp-004-compra-mobile. Diff: 7 archivos, +321/−33 (354).
Alcance: combinar flujo Mobile, preparación de respuesta de compra antes del commit y evidencia local de límites.
Verificación: pruebas Mobile fake/typecheck están registradas; sin render nativo, API, proveedor o DB.
Dependencia: feat/mvp-004-cliente-mobile. Siguiente: fix/mvp-004-recuperacion-mobile. Revertir solo M2 y evidencia acompañante.
~~~~

### 24 — MVP-004: finalizar compra Mobile con seguridad

~~~~markdown
Cadena: feat/mvp-004-compra-mobile → 📍 fix/mvp-004-recuperacion-mobile.
Base: feat/mvp-004-compra-mobile. Head: fix/mvp-004-recuperacion-mobile. Diff: 5 archivos, +105/−9 (114).
Alcance: validar tramos y conservar sesión/selección en estados no terminales.
Verificación: MobilePurchase 14/14 y typecheck registrados; no render de React Native.
Dependencia: feat/mvp-004-compra-mobile. Siguiente: test/mvp-004-pruebas-mysql. Revertir solo recovery Mobile.
~~~~

### 25 — MVP-004: preparar verificaciones MySQL con opt-in

~~~~markdown
Cadena: fix/mvp-004-recuperacion-mobile → 📍 test/mvp-004-pruebas-mysql.
Base: fix/mvp-004-recuperacion-mobile. Head: test/mvp-004-pruebas-mysql. Diff: 3 archivos, +397/−0 (397).
Alcance: harness que exige fixture externo y opt-in explícito; sin migrar ni limpiar.
Verificación: build offline 0 warnings/errors; --plan exit 0; --run sin opt-in exit 2. Ninguna consulta SQL/MySQL ejecutada.
Dependencia: fix/mvp-004-recuperacion-mobile. Siguiente: fix/mvp-004-reintentos-compra. Revertir solo el harness.
~~~~

### 26 — MVP-004: proteger reintentos y aislar cada selección de compra Mobile

~~~~markdown
Cadena: test/mvp-004-pruebas-mysql → 📍 fix/mvp-004-reintentos-compra.
Base: test/mvp-004-pruebas-mysql. Head: fix/mvp-004-reintentos-compra. Diff: 4 archivos, +87/−5 (92).
Alcance: el estado desconocido bloquea un segundo pago; un rechazo definitivo permite reintentar sin renovar el vencimiento original; una nueva selección de itinerario en la misma sesión recibe una identidad de PurchaseFlow nueva. Ocultar y reabrir conserva la compra activa; el cambio de sesión sigue aislado.
Verificación: el harness MySQL conserva su evidencia previa de build/guards seguros, sin SQL/MySQL ni ejecución con opt-in; el test estructural de identidad se observó RED y luego GREEN, y `npm run typecheck` pasó. No se probó un render React Native.
Dependencia: test/mvp-004-pruebas-mysql. Siguiente: docs/mvp-004-verificacion-compra. Revertir solo el test de retry y el cambio de identidad por selección, conservando el harness base.
~~~~

### 27 — MVP-004: documentar verificación y PRs locales

~~~~markdown
Cadena: fix/mvp-004-reintentos-compra → 📍 docs/mvp-004-verificacion-compra.
Base: fix/mvp-004-reintentos-compra. Head: docs/mvp-004-verificacion-compra. Diff: 5 archivos, +395/−5 (400).
Alcance: guía MySQL/TEST, corrección de correlación por pago, configuración, evidencia del backlog y estos cuerpos de PR. Asunto de este corte: docs: documentar verificaciones y límites de compra.
Verificación: regresión del SQL extraído: Node v24.20 SQLite :memory:, RED antes/GREEN después; PG-R 0/0/0/0 y PG-A 1/2/2/2. No demuestra MySQL.
Dependencia: fix/mvp-004-reintentos-compra. Siguiente: test/mvp-004-entorno-aislado. Revertir solo este corte documental.
~~~~

### 28 — MVP-004: preparar el entorno aislado de compra

~~~~markdown
Cadena: docs/mvp-004-verificacion-compra → 📍 test/mvp-004-entorno-aislado.
Base: docs/mvp-004-verificacion-compra (d7b0a46). Head: test/mvp-004-entorno-aislado. Diff: 7 archivos, +180/−46 (226).
Alcance: permitir solo la tupla PurchaseTests/berruticomprastests/berruti_purchase_test y preparar con opt-in una base externa vacía usando migraciones y el escenario existente. No crea DB/usuario ni limpia. La ejecución MySQL posterior está registrada abajo; no se inició API/UI ni se llamó al proveedor.
Verificación: AuthenticationSessions focused RED (9/10, exit 1) y GREEN (10/10, exit 0); build sin advertencias/errores; --plan 0 y negativas sin opt-in 2 (evidencia de preparación, no repetida). Después, corrida autorizada: `--prepare --allow-test-db-writes` exit 0/12 PASS y `--run --allow-test-db-writes` exit 0/40 PASS en la DB aislada, con DTOs de pago controlados y sin llamada al proveedor. Filas/asientos retenidos; no repetir sobre ese fixture. Revisión nativa aprobada solo para `9b204c3`; no prueba Checkout Pro TEST, API/UI ni devolución completada.
Dependencia: borradores locales MVP-004 en d7b0a46. Siguiente: `fix/mvp-004-validacion-mercado-pago-test`, luego autorización separada de API/Web/Mobile y Checkout Pro TEST, y revisión de Diego; no publicar aún. Revertir solo el guard de entorno, preparación y pruebas.
~~~~

### 29 — MVP-004: validar la identidad TEST antes de operar con pagos

~~~~markdown
Cadena: test/mvp-004-entorno-aislado → 📍 fix/mvp-004-validacion-mercado-pago-test.
Base: test/mvp-004-entorno-aislado (2d458ca). Head: fix/mvp-004-validacion-mercado-pago-test. Diff local observado: 7 archivos, +283/−14 (297 líneas cambiadas).
Título: Validar identidad TEST antes de operar con pagos.
Alcance: reemplazar la validación obsoleta por prefijo con confirmación explícita del origen TEST y validar, antes de cada creación/consulta/búsqueda/devolución, la identidad de solo lectura del vendedor, tag `test_user` y `site_id` configurados. Errores, redirects o discrepancias bloquean la operación; sin caché ni fallback.
Verificación: `--mercado-pago-test-identity` 59/59, exit 0 (suite inicial 4/34 RED, exit 1); `PaymentContracts` 269/269, exit 0. HTTP simulado únicamente; no usar el token de origen no confirmado, no llamar al proveedor, no ejecutar MySQL ni iniciar API/UI.
Commit local: `9024601`, `fix(payments): verificar la cuenta TEST antes de operar`. Evaluación nativa: medium, bajo presupuesto; sin nueva revisión ni aprobación nativa. No hay PR remoto ni publicación autorizada.
Dependencia: test/mvp-004-entorno-aislado. Después: autorización humana aparte para credencial privada confirmada y recorrido manual TEST/API/Web/Mobile; Diego cross-review. Revertir solo el guard de credenciales, preflight y sus pruebas/documentación.
~~~~

## Gate compartido y tamaño

- La base main..da5282c (2.897 líneas) tiene la excepción de tamaño aprobada; merge sigue bloqueado por revisión e integración pendientes. Su partición conocida es contratos 269, inventario/migración/SQL/tests 1.262 y artefactos de paradas 1.368; no dividir generados ni inferir otras excepciones.
- RS-02 mide 1.072 incluyendo 17 líneas del tracker (+10/−7); su runtime aprobado es 1.055. Las excepciones existentes aplican solo a RS-01 (1.997), RS-02 (1.055), persistencia (2.664), inicio (1.030) y conciliación (454).
- La lineage nativa review-57b85cc83735366a sigue correction_required: el STATUS nativo entregó el hallazgo R3-001 al preparar los documentos pendientes para versionarlos. Se registró el plan acotado de 80 líneas; la continuación posterior terminó en captured_artifacts_unverifiable. La corrección local y sus pruebas no equivalen a aprobación nativa. No hubo ACK; la recuperación de los artefactos sigue pendiente.
- MVP-004 sigue in_progress. La preparación y suite MySQL autorizadas pasaron en la DB aislada, con filas y asientos retenidos; ese fixture no es reutilizable para otra corrida. No se ejecutaron pagos de Mercado Pago TEST/refunds reales, API startup ni UI/E2E, y la revisión humana de Diego sigue pendiente. La aprobación nativa está limitada a `9b204c3`; la lineage anterior `correction_required` permanece intacta. El usuario aprobó la excepción de tamaño de 2.897 líneas para `main..da5282c`; publicación/merge continúan sujetos a revisión e integración pendientes.
