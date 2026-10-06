---
name: ctc-ddm-mobile
description: "Trigger: React Native, TypeScript, Expo, NativeWind, Tailwind. Aplicar criterios DDM y consultar conceptos móviles no demostrados."
license: Apache-2.0
metadata:
  author: "Equipo CTC"
  version: "1.0"
---

## Contrato de activación

Aplica al planificar, implementar, depurar o revisar móvil, Expo y estilos NativeWind/Tailwind. Lee `references/curso.md`. El flujo y UX de `../../../skills/ctc-project-conventions/SKILL.md` siguen siendo autoritativos; no vuelvas a activar esa skill desde aquí.

## Reglas obligatorias

- Prioriza requisitos y configuración real; informa conflictos. Conserva contratos, autenticación, QR y pagos exigidos.
- Atribuye evidencia a extractos NotebookLM aportados, no a lectura propia de los originales.
- Usa componentes nativos y estado tipado/inmutable; no ocultes errores con `any`.
- Respeta el sistema de estilos elegido: StyleSheet también fue enseñado; NativeWind no impone migración ni instalación.
- No impongas versiones del curso ni construyas clases dinámicas no verificadas. Usa alternativas con conjuntos literales completos.
- No agregues stores globales, animaciones, librerías ni dependencias por defecto; una dependencia transitiva no acredita enseñanza.

## Puertas de decisión

| Evidencia del concepto | Acción |
| --- | --- |
| Enseñado/aplicado | Usa el ejemplo mínimo compatible. |
| Solo mencionado | No presupongas demostración práctica. |
| Exigido por proyecto, no demostrado | Explica la solución más simple y pide aprobación antes de implementar lo dependiente. |
| No verificado | Detén la implementación dependiente; solicita evidencia o aprobación informada. |

## Pasos de ejecución

1. Inspecciona versiones, árbol de rutas y estilos actuales sin instalar paquetes.
2. Clasifica conceptos y cita fuente/localización, o su ausencia.
3. Mantén el enfoque de navegación existente; elige props/estado local y utilidades o StyleSheet según el proyecto.
4. Consulta extensiones no demostradas, incluidas HTTP práctico, almacenamiento, auth o animaciones; registra aprobación explícita.
5. Implementa el corte pequeño y valida con herramientas disponibles, declarando lo no probado.

## Contrato de salida

Informa archivos, clasificación docente, estilos/navegación elegidos, aprobaciones, verificaciones, conflictos y pendientes; no afirma compatibilidad ni pruebas no ejecutadas.

## Referencias

- [Curso y límites](references/curso.md)
- [Uso en Pi y Codex](../ctc-dda-blazor/references/uso-hosts.md)
