# Modelado de datos del MVP — unidad documental

**Autorización:** borrador de documentación en español; sin código, migraciones, SQL ejecutable, artefactos visuales, instalaciones ni entrega Git. Fuente canónica: [diseño](../../docs/design/data-model.md). Estado: redactado para revisión humana, no aprobado.

## Trabajo y ruta

- [x] Leer guía, requisitos, historias, MVP, backlog e informe; conservar incertidumbre por falta de consigna oficial.
- [x] Redactar UML de dominio antes de ER MySQL, diccionario, límites transaccionales y decisiones abiertas; actualizar MVP, backlog e informe.
- [ ] Resolver preguntas con el equipo y obtener revisión cruzada de Enzo; solo entonces evaluar MVP-010 `done` y habilitar MVP-001.

**Ampliación autorizada:** corregir cardinalidades detectadas por verificación independiente y sustituir la propuesta de pago determinista local por integración de API de Mercado Pago con credenciales TEST, revisable, sin cobros reales. Sin credenciales, SDK, código, migraciones, diagramas adicionales ni decisión sobre checkout. Hallazgos: servicio reutilizable entre itinerarios; ticket emitido vinculado a compra aprobada; pago rechazado representable sin compra emitida; orden de ruta en §8 del MVP provisional. Pendiente conciliar el ciclo de reintentos y revisar con Enzo.

- [x] Corregir UML/ER/diccionario y referencias de ruta/tramo sin fijar políticas pendientes.
- [x] Reconciliar MVP, RF, historias, backlog e informe con el alcance TEST y la verificación del resultado por la API.
- [x] Ejecutar controles de diffs, JSON, invariantes, enlaces, Mermaid y términos de pago; reportar evidencia.

Ruta prevista: escritura delegada por múltiples documentos; siete archivos autorizados forman una sola unidad documental. Pronóstico orientativo: ~250–380 líneas redactadas, no límite artificial. Estrategia de entrega: **single-pr / size:exception**, aceptada explícitamente para ~444 líneas en lugar de encadenar PRs. El usuario autorizó commit, PR y merge del **borrador**, sin declarar MVP-010 terminado ni sustituir la revisión de Enzo. Issue de alcance aprobado: [#1](https://github.com/diegoolherry/Obligatorio-DDA-DDM/issues/1). Rama de entrega: `docs/data-model-mercado-pago-test`. Un único corte documental; la evidencia del commit y PR queda en la historia Git/GitHub, no implica aprobación del diseño. Límite de reversión: estos siete archivos exclusivamente, preservando otros cambios. Pruebas de runtime/TDD: N/A, solo documentación, sin frontera ejecutable; no se infiere configuración del harness.

## Verificación y pendientes

Controles observados: `git diff --check` sin errores (avisos LF/CRLF); `python -m json.tool docs/mvp-tasks.json` válido; `git diff --stat` muestra cinco archivos rastreados, excluye dos nuevos; Python de solo lectura comprobó diez IDs, fechas/estados, dependencias acíclicas, revisores, enlaces locales y referencias textuales UML/ER. `rg` halló «determinista» solo como negación de la mecánica local vigente o referencia histórica a la ampliación; sin afirmaciones vigentes de simulador local. Total estimado 444 líneas authored añadidas+eliminadas, incluidos 200 renglones de archivos nuevos; aviso de carga para revisión, sin recortar artificialmente. No se ejecutó renderizador Mermaid ni prueba del proveedor; la revisión visual y funcional quedan pendientes. Enzo debe revisar cardinalidades, precios/reintentos, ruta/tramo, abonos en ambos sentidos, intentos desconocidos y unicidad de asientos antes de aprobar; la asignación automática en la demo no está en duda. No hay commit ni identidad de entrega.
