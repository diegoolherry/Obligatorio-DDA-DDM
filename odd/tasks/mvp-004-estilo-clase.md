# MVP-004: claridad del código

## Objetivo y alcance autorizado

Aplicar únicamente constructores explícitos, legibilidad y eliminación del async artificial del marcador de devolución. Conservar comportamiento, firmas públicas, contratos, validaciones, orden de evaluación, transacciones, bloqueos y protecciones de pagos. No mover responsabilidades entre capas ni modificar MySQL, proveedor de pagos o GitHub.

Base: `8eef088853c71bdad3eadd9957f0702b2d1112f7`. Rama local: `refactor/mvp-004-estilo-clase`. Las ramas publicadas se conservan.

## Plan

- [x] C1: Constructores normales y campos readonly en los cuatro controllers, cuatro services, tres repositories y cliente Web de compra. Se conservaron los nombres y tipos de parámetros; ver los 12 archivos de capa en el diff.
- [x] C2: Expandir instrucciones compactadas en confirmación, política y componente Web, conservando evaluación y flujo. Ver `ConfirmacionesCompraRepository.cs`, `PoliticaConfirmacionCompra.cs` y `CompraItinerario.razor`.
- [x] C3: Reemplazar `IniciarDevolucionAsync` por `MarcarDevolucionPendiente` síncrono y actualizar sus tres llamadas internas; se quitaron el `await` y `Task.CompletedTask` artificiales.
- [x] V1: Compilación API/Web, regresiones existentes y `git diff --check`; resultados observados abajo.

Ruta: delegado directo (más de dos archivos no triviales); un único escritor. No hay funcionalidad nueva ni un fallo esperado para un RED: se conserva la conducta cubierta por las regresiones existentes. No se modifican tests para acomodar un cambio de comportamiento.

## Verificación prevista

Ejecutar una vez PurchaseHolds, PaymentContracts y WebPurchase: cubren precisamente los services/controllers/política/cliente afectados. El filtro existente de PaymentContracts solo prueba identidad del proveedor y no cubre este refactor, por lo que no lo sustituye. No ejecutar Mobile, MySQL ni pagos reales. Compilar API/Web mediante sus referencias de proyecto en los harnesses. Usar dotnet DLL para evitar problemas previos de apphost.

## Entrega y revisión

Unidades locales de trabajo; publicación y reorganización de PR fuera de alcance. Tamaño previsto: varios cientos de líneas por expansión de sintaxis, sin cambios de modelo ni migraciones. Conservar la estrategia de cadena ya acordada; no publicar cortes ni solicitar excepciones remotas en este trabajo. Modo RDD consultado: on; cualquier consentimiento es específico del candidato.

## Evidencia y pendientes

Verificaciones finales observadas tras normalizar finales de línea CRLF: `dotnet build tests/PurchaseHolds/PurchaseHolds.csproj --no-restore` (0; 1 advertencia NU1900 por índice NuGet inaccesible), su DLL (0; 83/83); `dotnet build tests/PaymentContracts/PaymentContracts.csproj --no-restore` (0; 2 advertencias NU1900), su DLL (0; 285 checks, 0 fallas); `dotnet build tests/WebPurchase/WebPurchase.csproj --no-restore` (0; 0 advertencias), su DLL (0; 164/164); `git diff --check` (0). Los seis comandos se ejecutaron una vez antes y una vez después de la normalización para comprobar los bytes finales. Copia Engram pendiente por bloqueo conocido de identidad del host. No se atribuye aprobación nativa ni commit. El intento de `git add`/commit/revisión fue rechazado antes de ejecutarse porque el revisor automático de permisos alcanzó su límite de uso; no se intentó eludirlo. Los 14 cambios de código y este documento permanecen locales, sin stage ni commit. Revisión RDD pendiente.
