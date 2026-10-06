# Uso de las skills canónicas

Las carpetas `.agents/skills/ctc-dda-blazor/` y `.agents/skills/ctc-ddm-mobile/` son la única copia canónica para Diego (Pi) y Enzo (Codex Desktop). El orquestador informó haber consultado `docs/skills.md` de Pi y la documentación oficial de skills de Codex: ambos admiten descubrimiento recursivo en `.agents/skills/` del repositorio. No requiere instalación global, symlinks, copias por host ni cambios de settings.

## Camino rápido

- **Diego / Pi:** abre el proyecto y usa `/reload` tras cambios. Activa `/skill:ctc-dda-blazor` o `/skill:ctc-ddm-mobile` según la capa.
- **Enzo / Codex Desktop:** permite detectar cambios del repositorio; si la skill no aparece, reinicia la sesión/aplicación y vuelve a comprobar. En **Codex CLI** se puede invocar `$ctc-dda-blazor` o `$ctc-ddm-mobile`; esto no acredita comandos equivalentes probados en la UI Desktop.
- **Ambos:** sigue [AGENTS.md](../../../../AGENTS.md): skill compartida más especializada relevante; ambas especializadas si el trabajo cruza capas. La compartida carga las especializadas y estas no la reactivan, evitando ciclos.

## Comprobaciones manuales pendientes

- Confirmar descubrimiento y activación de ambas skills tras `/reload` en Pi.
- Confirmar descubrimiento en la sesión de Codex Desktop de Enzo y la lectura de referencias locales.
- Probar una consulta por capa y otra transversal: clasificar evidencia y pedir aprobación cuando exceda lo demostrado, sin omitir requisitos del proyecto.

Son instrucciones de incorporación, no resultados de smoke tests ejecutados. El alcance y seguimiento están en [la tarea documental](../../../../odd/tasks/course-grounded-skills.md).
