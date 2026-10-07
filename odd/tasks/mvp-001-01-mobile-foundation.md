# MVP-001-01 — Corte de fundación Mobile

## Alcance de publicación

Primera porción Mobile de la fundación existente: `feat/mobile-foundation-slice`, base `chore/project-foundations-docs-integration` (`d290450`), tipo `type:chore`, `Refs #15`. La documentación corresponde a #16; el tracker es PR #17. No cierra MVP-001-01 ni incorpora API/Web, autenticación, cliente API o funcionalidades de producto.

El padre copió 14 archivos Mobile desde el checkout original `Obligatorio-DDA-DDM`; ese origen conserva los bytes finales canónicos. Este trabajo solo adapta README, informe y backlog y registra el límite de revisión. No crea comportamiento nuevo ni modifica fuentes Mobile. Commit, publicación y revisión nativa del candidato exacto corresponden al padre; no se declara aprobación.

## Procedencia y preservación

- README conserva los recorridos y rutas documentales del tracker; añade únicamente instrucciones y alcance Mobile, sin importar la reorganización documental posterior del origen.
- Informe conserva sus nueve secciones e historia; incorpora solo el estado y evidencia Mobile del informe original, no builds o arranques API/Web posteriores.
- `docs/tasks.json` conserva exactamente la metadata del tracker y el array `tasks` del origen. SHA-256 canónico del array (UTF-8, `ensure_ascii=False`, `sort_keys=True`, separadores `(',', ':')`): `bf1ceafc7402d1c809c47695affab95c069dbba543b0b7ff06a5c7a542ea8455`.
- El cambio del backlog frente al tracker se limita a `status`, `startedAt` y `evidence` de MVP-001-01. Se conserva el inicio original `2026-10-06T10:10:41.491439-03:00`, sin timestamps nuevos; finalización nula y revisión de Enzo pendiente.
- Este registro sintetiza el ODD Mobile original; no copia como comprobaciones actuales sus decisiones de instalación, autorizaciones anteriores, carpeta vacía ni historial de preservación de archivos ajenos al candidato.

## Evidencia previa y límites

El [informe §7](../../docs/project-report.md#7-calidad-pruebas-y-gestión-de-configuración) distingue resultados históricos: typecheck, compatibilidad Expo y exportación JS Android pasaron de forma independiente; `npm ci` fue reportado por el implementador, no reejecutado por el verificador. La exportación no acredita compilación Android nativa.

La readiness Metro reportada por el implementador no fue confirmada: ambos intentos independientes autorizados agotaron el tiempo. La auditoría previa tuvo exit 1 y 22 paquetes afectados (15 altos, 7 moderados), tres avisos distintos, sin remediación. El reporte posterior del usuario de arranque en celular mediante ngrok se atribuye a la persona, no al agente; no sustituye esos resultados ni verifica visualmente insets.

## Puertas pendientes

- La tarea permanece `in_progress`: los tres criterios originales cubren las tres capas, no solo Mobile.
- API/Web e integración cliente/API quedan para cortes posteriores; no se incorpora evidencia posterior de .NET.
- Verificación funcional independiente del candidato, evaluación de auditoría, revisión nativa y revisión cruzada de Enzo siguen pendientes.
- No se ejecutan instalaciones, servidores ni pruebas funcionales en este corte documental. No hay RED de comportamiento significativo para una copia sin cambios; los checks estructurales se reportan en el handoff.

Límite de rollback documental: README, informe, evidencia de la fundación en backlog y este registro; el padre controla la copia fuente y las acciones Git. Estrategia: corte encadenado Mobile primero, sin reclamar cierre ni aprobación de los cortes siguientes.
