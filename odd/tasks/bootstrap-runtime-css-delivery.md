# Dependencia CSS mínima de Bootstrap para Web

Refs #15 · type:chore · rama `chore/bootstrap-css-runtime-slice` · base API `5f83c38bf2de0f66a1a43c7d9ca2e2470b1dbe46` (PR #19, `chore/api-foundation-slice`). Esta unidad prepara únicamente Bootstrap CSS 5.3.3 y su licencia MIT completa; todavía no contiene aplicación ni scaffold Web.

## Decisión y procedencia

El usuario aprobó publicar solo el CSS necesario, según la inspección previa de `App.razor`, líneas 9–11, comunicada por el padre: App y reconexión no referencian Bootstrap JS. Es evidencia del candidato anterior, no código presente ni prueba funcional de esta unidad. El objetivo es una dependencia runtime mínima, no reducir líneas arbitrariamente: las variantes opcionales no referenciadas no se incluyen por herencia de la plantilla.

- Runtime: [bootstrap.min.css](../../src/Web/Web-App/Web-App/wwwroot/lib/bootstrap/dist/css/bootstrap.min.css).
- Licencia íntegra: [LICENSE](../../src/Web/Web-App/Web-App/wwwroot/lib/bootstrap/LICENSE), texto upstream suministrado, UTF-8/LF con salto final.
- Procedencia de licencia: [URL raw upstream v5.3.3](https://raw.githubusercontent.com/twbs/bootstrap/v5.3.3/LICENSE). Es una URL sin firma; no se afirma autenticidad criptográfica ni descarga verificada en esta ejecución.
- Original de solo lectura: `src/Web/Web-App/Web-App/wwwroot/lib/bootstrap/dist/css/bootstrap.min.css` en el checkout original, preservado fuera de la publicación.
- Tamaño original y copia: **232803 bytes**. SHA256 de ambos: `3c8f27e6009ccfd710a905e6dcf12d0ee3c6f2ac7da05b0572d3e0d12e736fc8`.

El CSS permanece byte por byte intacto, incluido el encabezado `Bootstrap  v5.3.3`, copyright 2011–2024, MIT y el trailer `sourceMappingURL=bootstrap.min.css.map`. El padre informó que su primera guarda esperaba un espacio y falló antes de escribir; corrigió la guarda a `Bootstrap\s+v5\.3\.3\b` y verificó la copia. No hubo cambio de versión ni reparación de código.

## Alcance y verificación

El original conserva los 44 assets vendor. Aquí se incluye un CSS y LICENSE; se excluyen los otros 43 assets (JS, variantes CSS y mapas de depuración). El mapa referido no se distribuye: no se promete disponibilidad del debugmap en DevTools.

La verificación de esta unidad es estructural: comparación de bytes, hash/tamaño, encabezado MIT/versionado, licencia exacta, empaquetado, enlaces locales, preservación de los archivos tracked de la base API y comprobación limitada de firmas de secretos/privacidad. No hay cambio de comportamiento ni RED significativo para este import pasivo. No se ejecutan build, servidor ni pruebas funcionales Web.

La evidencia funcional previa del padre (incluido 0→1) corresponde al candidato Web completo con todos los assets vendor; no demuestra funcionamiento de Web con esta distribución reducida. Al incorporar el scaffold, la siguiente unidad debe reverificar referencias CSS, ausencia de dependencia JS, compilación y comportamiento funcional con el paquete mínimo.

## Entrega y revisión pendientes

La cadena actual tiene seis hijos: Mobile #18 → API #19 → esta dependencia CSS → Web → dos unidades documentales. El tracker #17 conserva la planificación histórica de cinco; no se autorizan ediciones de cuerpos GitHub existentes. README, informe, backlog y demás tracked permanecen exactamente en la base API, sin enlaces anticipados a Web.

La revisión nativa amplia y la de esta unidad mínima quedaron bloqueadas por el límite de contexto, antes de crear autoridad. El código nativo v4.0.0 inspeccionado fija 204800 bytes para el contexto completo; el CSS original ya mide 232803 bytes. No se encontró un ajuste soportado y no se modificó Bootstrap para forzar ese límite.

El usuario autorizó desactivar RDD exclusivamente para este clon, incluidos sus worktrees. `gentle-ai review mode disable --scope clone` terminó correctamente; la consulta posterior informó `off (decided by clone_local)`, con la configuración global todavía en `on`. Esta decisión no equivale a aprobación nativa. La verificación independiente debe completarse antes de publicar: su primera ejecución quedó parcial por permisos vencidos. El padre conserva Git, commit, push y PR; este documento no es un recibo de aprobación, no acredita commit ni autoriza merge.

## Resultado independiente

El verificador completó 18 controles lógicos: PASS, cero fallos del candidato. Confirmó igualdad byte a byte del CSS original/copia y el hash indicado; LICENSE mide 1093 bytes, conserva el texto MIT completo y su SHA256 es `8c14611ae41ac6fd543c13349f22188eb12c69b3e59105c5eca3925a8e4eca3e`.

Los 69 archivos tracked de la base coinciden con normalización LF; los nueve archivos API y catorce Mobile comparados coinciden con los originales. Hay exactamente tres archivos elegibles nuevos, dos enlaces locales válidos y ningún scaffold Web. Se confirmó la presencia de 44 assets originales y únicamente CSS/LICENSE en el candidato; no se afirma preservación byte a byte de los 44 sin manifest previo. Caches ignoradas quedan fuera de esa garantía.

`git diff --check` y `git diff --cached --check` terminaron con código cero; además se verificó explícitamente whitespace de los archivos untracked. Un error inicial de regex en el script se corrigió antes de completar los controles pendientes. El escaneo limitado de seis firmas produjo 26 falsos positivos de rutas en URLs públicas; todos se clasificaron y el patrón corregido encontró cero coincidencias. No es una auditoría universal de secretos.

No hubo build ni navegador porque esta unidad no contiene aplicación Web; las pruebas funcionales quedan para el hijo siguiente. Esta evidencia es verificación estructural independiente, no aprobación nativa. La versión del ejecutable nativo no fue confirmada por este verificador.

Rollback de esta dependencia: retirar únicamente el CSS y LICENSE de esta unidad, sin tocar API, Mobile ni los originales. Este documento conserva la procedencia y los límites de la decisión.
