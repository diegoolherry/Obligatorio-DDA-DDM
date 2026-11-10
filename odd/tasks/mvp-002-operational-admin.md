# MVP-002 — Administración operativa y datos de demostración

## Objetivo y estado

Implementar la administración de paradas, tramos, servicios y tarifas ficticias, y preparar de forma repetible dos servicios de demostración entre Ombúes, Radial de Conchillas y Colonia. La tarea `MVP-002` permanece **blocked**: la interfaz interactiva aún no fue comprobada y Diego debe completar la revisión cruzada. `completedAt` y `completedBy` continúan en `null`.

## Alcance y aceptación

- Un administrador gestiona las cuatro entidades desde Blazor.
- La preparación demo acepta valores ingresados por el administrador y es idempotente; no actualiza datos existentes y reporta conflictos.
- El servicio Ombúes–Radial no requiere asiento; Radial–Colonia usa asignación automática.
- No se agregó administración de cuentas ni compra/reserva/asignación real de asientos.

## Cortes apilados

| Corte | Rama | Base | Commit principal | Contenido |
|---|---|---|---|---|
| Esquema | `feature/mvp-002-01-schema` | `main` | `14eb492` | Entidades, migración EF y SQL generado; borrador PR #61. |
| API CRUD | `feature/mvp-002-02-api-crud` | `feature/mvp-002-01-schema` | `cc3d9c1`, `776921b` | CRUD protegido por rol, DTOs, servicios/repositorios y validación. |
| API demo | `feature/mvp-002-03-api-demo` | `feature/mvp-002-02-api-crud` | `49461cf` | Endpoint idempotente de preparación y sus contratos. |
| Administración Web | `feature/mvp-002-04-web-admin` | `feature/mvp-002-03-api-demo` | `6cb1072` | Formularios operativos, cliente HTTP y confirmación antes de borrar. |
| Tooling local | `feature/mvp-002-05-local-tooling` | `feature/mvp-002-04-web-admin` | `17cfddc` | Diagnóstico y guía segura de actualización de DB local. |
| Evidencia runtime | `feature/mvp-002-06-runtime-evidence` | `feature/mvp-002-05-local-tooling` | Este corte | Scripts de autorización/runtime y registro de estado verificable. |

Se autorizó una excepción de tamaño para mantener cohesionados estos cortes. Para esta publicación el usuario pidió no abrir revisiones opcionales nuevas ni repetir pruebas ya aprobadas; solo los cortes restantes de esta pila quedan sin revisión nativa. La preferencia de RDD para trabajos futuros no se modificó.

## Verificación y límites

- Evidencia local previa: suites operativas `107/107`, validación de controladores `4/4`, autenticación Web `92/92`; build API con cero warnings y errores.
- Evidencia real previa: API anónima `401`, roles insuficientes `403`, administrador `200`; preparación demo repetida sin duplicar datos, conflicto `409` y solicitud inválida `400`; lectura de las filas confirmada por API/DB.
- La comprobación de UI en navegador sigue pendiente. Se verificaron HTML HTTPS y negociación SignalR, no el recorrido interactivo renderizado.
- No se ejecutó DELETE real ni limpieza; las filas nuevas permanecen para revisión. Los scripts de runtime documentan escenarios para futuras verificaciones autorizadas.
- El harness API-only intermedio del corte demo no tuvo assets NuGet locales disponibles al primer intento; no se afirma resultado hasta completar la comprobación cache-only prevista en la rama.

## Próximos pasos

1. Publicar la pila en orden, manteniendo cada PR contra su rama padre inmediata.
2. Diego realiza la revisión cruzada en los PRs.
3. Verificar el recorrido interactivo de administración; no ejecutar DELETE sin autorización específica.
4. Actualizar el estado del backlog solo cuando se cumplan todos los criterios con evidencia y revisión.
