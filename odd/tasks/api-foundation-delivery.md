# Corte API y exclusiones .NET — PUB-04

## Alcance y procedencia

Candidato `chore/api-foundation-slice` sobre `feat/mobile-foundation-slice`, base exacta `109f2534bb255fca8b724b43bfbbfdf3613cfae8` (PR #18), encadenado al tracker #17; `Refs #15`, `type:chore`. El padre copió nueve archivos API byte a byte desde el checkout original y aplicó sus seis líneas de exclusiones .NET. Este escritor solo modifica README, informe y este registro; no modifica fuentes ni ignore, no incorpora Web ni las rutas Diataxis previstas para PUB-06.

Mobile conserva su unidad anterior de 14 archivos y su evidencia parcial. El informe conserva sus nueve secciones, auditoría, timeouts Metro y atribución del arranque en celular al usuario. No se cambian estados, timestamps ni finalización del backlog académico. El array canónico `tasks` permanece en SHA-256 `bf1ceafc7402d1c809c47695affab95c069dbba543b0b7ff06a5c7a542ea8455`; metadata y contenido de `docs/tasks.json` coinciden con la base Mobile (bytes normalizados LF por el checkout CRLF).

## Evidencia y límites

- La inspección del proyecto, solución, Program, controller y perfiles respalda `net10.0`, OpenAPI `10.0.12`, WeatherForecast y los perfiles Development. No acredita funcionalidades del dominio, auth, persistencia ni integración.
- Build API con SDK `10.0.401`, cero errores/advertencias y HTTP 200 (cinco registros JSON y OpenAPI `3.1.1`) son observaciones del verificador de la sesión original, conservadas en [informe §7](../../docs/project-report.md#7-calidad-pruebas-y-gestión-de-configuración). El arranque histórico anuló perfiles mediante `--no-build --no-launch-profile --urls http://127.0.0.1:5095`; mantuvo la advertencia de puerto HTTPS. No son checks actuales del candidato.
- La limitación DH-02 original sigue abierta: el escritor anterior afirmó preservación de 96 archivos, pero el verificador independiente no recibió el manifiesto original y no pudo demostrarla. Una comparación actual no recupera esa prueba histórica ni reescribe DH-02.
- Documentación pasiva y fuentes copiadas sin cambio de comportamiento: no hay RED significativo. Las comprobaciones estructurales no sustituyen el build/smoke separado del nuevo candidato.
- La autoridad nativa Mobile anterior ya fue consumida; este candidato API necesita su propia revisión. No se afirma aprobación, revisión de Enzo ni finalización de MVP-001-01.

## Checks de este escritor

- PASS `git diff --check`; solo aviso LF→CRLF del ignore.
- PASS nueve archivos API byte-idénticos al origen; 14 archivos Mobile versionados byte-idénticos al origen y a la base exacta. El conteo inicial indiscriminado encontró 20.167 archivos Mobile por outputs/dependencias presentes y se corrigió a la lista versionada de la base, sin modificar esos archivos.
- PASS JSON completo/metadata y bytes normalizados LF del backlog contra la base, array canónico contra origen y hash indicado arriba. La comparación inicial de bytes crudos con el blob falló por CRLF del checkout, no por diferencia JSON.
- PASS `.gitignore` byte-idéntico al origen, SHA-256 `6df8297640ec151e4f2f00e59303b75b0c50701be667299db084f3ac9f1f9e82`, seis líneas añadidas frente a la base.
- PASS 71 enlaces locales, 16 anchors y tres rutas inline fuente/documento en README/informe/este registro. Cuatro enlaces externos no consultados.
- PASS `git check-ignore --no-index --stdin`: diez positivos (bin/Bin, obj/Obj, .vs, csproj.user y tmpuser) y ocho negativos (.cs, .csproj, .slnx, .razor). Sin fixtures. El primer intento con stdin textual Windows introdujo CR y falló; repetir con bytes LF confirmó los casos previstos.
- PASS nueve secciones del informe y límites históricos retenidos; Web y las cuatro carpetas Diataxis ausentes. Presupuesto fuente API comprobado: 125 líneas + 6 de ignore + documentación; separado de Mobile.

Omitidos por contrato: build/restore, launch/HTTP, auditoría, instalación, servidor, checks de dispositivo, enlaces externos/renderizado, lifecycle/revisión nativa, stage/commit/push y escrituras GitHub. No se refresca CodeGraph.

## Reversión y entrega

El padre posee commit y entrega. La reversión de esta unidad se limita a los nueve archivos añadidos bajo `src/Api/API-Berruti`, las seis líneas .NET de `.gitignore`, el delta API de README/informe y este registro; conserva la base Mobile, el backlog y todos los cambios ajenos. No se ejecutó rollback ni operación destructiva. Publicación y aprobación del candidato permanecen pendientes.
