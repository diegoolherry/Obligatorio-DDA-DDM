---
name: ctc-dda-blazor
description: "Trigger: Blazor, C#, API, Razor, EF Core. Aplicar criterios DDA con evidencia de curso y aprobación de conceptos no demostrados."
license: Apache-2.0
metadata:
  author: "Equipo CTC"
  version: "1.0"
---

## Contrato de activación

Aplica al planificar, implementar, depurar o revisar Blazor/C#/API. Lee `references/curso.md` antes de decidir. El flujo y UX de `../../../skills/ctc-project-conventions/SKILL.md` siguen siendo autoritativos; no vuelvas a activar esa skill desde aquí.

## Reglas obligatorias

- Prioriza consigna, requisitos y configuración real; informa conflictos con los ejemplos del curso.
- Atribuye las citas a extractos NotebookLM aportados por el usuario, no a inspección propia de originales.
- Conserva DTO, autorización, QR y pagos exigidos. No confundas ausencia de evidencia docente con permiso para omitir requisitos.
- No introduzcas repositorios genéricos, AutoMapper, frameworks de middleware ni dependencias por defecto.
- Distingue eventos, copias de objetos e IDs locales de persistencia real; aplica los límites de la referencia.

## Puertas de decisión

| Evidencia del concepto | Acción |
| --- | --- |
| Enseñado/aplicado | Usa el patrón mínimo compatible. |
| Solo mencionado | No presupongas implementación enseñada. |
| Exigido por proyecto, no demostrado | Explica la solución más simple y pide aprobación antes de implementar lo dependiente. |
| No verificado | Detén la implementación dependiente; solicita evidencia o aprobación informada. |

## Pasos de ejecución

1. Inspecciona configuración .NET, modo Blazor, proveedor EF y código cercano.
2. Clasifica cada concepto con fuente/localización o ausencia de evidencia.
3. Define página/componente, modelo y servicio; conserva la cadena Controller → Service → Repository → DbContext cuando corresponda.
4. Explica extensiones al curso al equipo antes de implementarlas; registra la aprobación y no la infieras.
5. Valida el corte pequeño con las comprobaciones disponibles del proyecto.

## Contrato de salida

Informa archivos, conceptos y categoría de evidencia, decisiones aprobadas, conflictos, verificaciones y pendientes. Nunca presentes compatibilidad o pruebas no ejecutadas como resultados.

## Referencias

- [Curso y límites](references/curso.md)
- [Uso en Pi y Codex](references/uso-hosts.md)
