# Planificación de ampliaciones de la entrega final

## Objetivo y motivo
Distinguir el primer corte MVP de las primeras semanas de octubre de la entrega completa y preparar su implementación sin confundir mejoras del equipo con requisitos explícitos de la consigna.

## Alcance autorizado
En orden: (1) documentar alcance final; (2) ampliar modelo conceptual; (3) definir disponibilidad/compra con decisiones abiertas visibles; (4) ordenar backlog de implementación posterior.
Decisiones confirmadas: rutas del horario aportado; capacidades 22/28/42/46; planos académicos 2+2 con pasillo, numeración por fila y dos asientos finales izquierdos para 22/42/46; reutilización de asiento en intervalos no superpuestos de la misma salida, permitiendo relevo en una parada compartida.

## Límites
No implementar código ni importar íntegramente horarios. No inventar tarifas, vigencias actuales, planos oficiales, asignación de flota real, duración de reservas o comportamiento del pago tardío. Mantener el MVP y sus tareas intactos; revisión humana de Enzo pendiente. No modificar skill ni hacer commit/push. Preservar cambios previos: títulos en ocho documentos y archivo no rastreado docs/design/entities-explained.md; no tocar .codegraph/.

## Checklist estable y aceptación
- [x] P1 — Mapear documentos y cambios previos. Evidencia: exploración read-only y git diff --stat; ocho documentos con 12 inserciones/8 eliminaciones previas; backlog de diez tareas leído. Snapshot anterior conservado temporalmente fuera del repositorio para comparación local; no se publica la ruta privada.
- [x] P2 — Redactar las cuatro unidades en orden. Escritor completó alcance separado, modelo final complementario, disponibilidad por intervalos y FIN-001..004 pending. Diez tareas anteriores preservadas según comparación del escritor; revisión humana pendiente.
- [x] P3 — Verificar. Verificador independiente: planificación sustantiva PASS; JSON, whitespace, igualdad de diez tareas previas, DAG, estados y enlaces PASS (56 rutas locales, 13 anchors; externo omitido). Su resultado inicial PARTIAL solo por procedencia del reloj se resuelve con evidencia histórica recuperada del escritor: comando `python -c "from datetime import datetime; print(datetime.now().astimezone().isoformat())"`, salida observada `2026-10-01T18:42:19.328164-03:00`, usado luego en createdAt de FIN-001..004. No se adquirió nueva hora ni se alteraron fechas. Padre cierra esa limitación documental; no afirma una nueva corrida del verificador. Evaluación nativa no disponible (package-local-binary-missing); no hubo pruebas ejecutables ni revisión humana.

## Superficies planificadas
- docs/final-scope.md (nuevo)
- docs/design/data-model.md (sección complementaria final)
- docs/mvp-tasks.json (metadata y nuevas tareas; no alterar tareas existentes)
- README.md (enlace breve)
- docs/project-report.md (alcance/cronograma y backlog)

## Evidencia y siguiente paso
Exploración y redacción completadas. Cuatro tareas nuevas; total 14 (13 pending, una in_progress). Validación estructural y de enlaces del escritor PASS; spot check del padre PASS. Verificación independiente sustantiva PASS; la limitación de evidencia de reloj quedó resuelta por recuperación del comando/output histórico. Contradicción preexistente del informe §7 SUCCESS / §9 pendiente señalada, fuera de esta modificación. Hora real consultada: 2026-10-01T18:38:20.893650-03:00; obtener nueva hora del sistema al crear tareas.
Siguiente: revisión humana del modelo MVP por Enzo (MVP-010) antes de implementación dependiente; para la ampliación final, decidir hold/pago y reproducibilidad de horarios antes del ciclo de compra. Las cuatro unidades documentales están completas. Las tareas de implementación no se completan por producir documentación. No se hizo commit ni push; rollback limitado a las adiciones documentales propias preservando cambios previos.
