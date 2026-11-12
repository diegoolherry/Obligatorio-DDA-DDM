# FIN-010-01 · Sistema de diseño y mockups Mobile de Berrutti

Archivo OpenPencil con tokens, componentes base y 13 pantallas Mobile (375 × 812). Es una propuesta de diseño: no implica código implementado. La identidad usa el nombre **Berrutti** como wordmark de texto; no usa logo ni fotografías de la empresa. Cuentas, precios, horarios y posiciones de las pantallas son datos de ejemplo; el texto visible para la persona usuaria no menciona «demo» ni «ficticio».

## Cómo abrirlo

```bash
op start --file design/fin-010/berrutti.op              # editor de escritorio
op start --headless --file design/fin-010/berrutti.op   # servidor sin ventana (CLI)
op stop
```

- Página **Sistema** (`S1`–`S5`): colores y contrastes, tipografía/espaciado/radios/sombras, botones y campos, insignias/tarjetas/avisos, encabezado y pestañas.
- Página **Mobile** (`M01`–`M13`): pantallas.
- Los colores, espaciados, radios y tamaños de texto son variables (`op vars`); los nodos las referencian con `$nombre`.
- `scripts/` contiene los generadores usados para construir el archivo (`scripts/build.sh` lo reconstruye con el servidor iniciado). Los PNG se regeneran con `op export-frames --output-dir <ruta absoluta> --format png`, una vez por página (`op set_active_page index=0` y `index=1`).

## Tokens

Nombres compatibles con Tailwind/NativeWind (`primary-600`, `accent-500`, `ink-900`, `surface-50`...). Los ratios se calcularon con la fórmula de luminancia relativa WCAG 2.1 (`scripts/tokens.py`, `scripts/readme_tables.py`); no son estimaciones. «Blanco» = `#FFFFFF`; «ink-900» = `#141414`.

| Token | Hex | Uso | Blanco | ink-900 |
|---|---|---|---|---|
| `primary-50` | `#FDECEC` | Fondo de aviso/botón secundario pulsado | 1,14:1 | 16,13:1 |
| `primary-100` | `#FAD2D3` | Fondo suave rojo | 1,38:1 | 13,36:1 |
| `primary-500` | `#D93036` | Rojo intermedio (iconos, gráficos) | 4,74:1 | 3,88:1 |
| `primary-600` | `#C41E24` | Botón primario, foco | 5,90:1 | 3,12:1 |
| `primary-700` | `#A3171C` | Enlaces, botón primario pulsado, texto rojo | 7,79:1 | 2,37:1 |
| `primary-900` | `#5C0B0E` | Rojo muy oscuro (reserva) | 13,91:1 | 1,32:1 |
| `accent-100` | `#FCE7D6` | Fondo de etiqueta naranja | 1,20:1 | 15,39:1 |
| `accent-500` | `#E77723` | Relleno naranja con texto ink-900 (acento, pestaña activa, franja). Nunca texto sobre blanco | 2,97:1 | 6,19:1 |
| `accent-700` | `#9A4A0C` | Texto naranja sobre blanco / accent-100; acento pulsado | 6,25:1 | 2,95:1 |
| `ink-900` | `#141414` | Texto principal, encabezado | 18,42:1 | 1,00:1 |
| `ink-700` | `#3D3D3A` | Texto secundario fuerte | 10,90:1 | 1,69:1 |
| `ink-500` | `#5E5E59` | Texto de ayuda, iconos inactivos | 6,52:1 | 2,83:1 |
| `ink-300` | `#8A8A84` | Solo bordes de campos (UI 3:1), no texto | 3,47:1 | 5,31:1 |
| `surface-0` | `#FFFFFF` | Tarjetas, campos | 1,00:1 | 18,42:1 |
| `surface-50` | `#F6F6F3` | Fondo de pantalla | 1,08:1 | 17,01:1 |
| `surface-100` | `#ECECE8` | Deshabilitado, Utilizado | 1,18:1 | 15,55:1 |
| `border-200` | `#D6D6D0` | Separadores decorativos (no señal única) | 1,46:1 | 12,62:1 |
| `success-100` | `#E2F3E7` | Fondo «Sin usar» y éxito | 1,15:1 | 15,97:1 |
| `success-700` | `#1B6B38` | Texto/icono éxito | 6,55:1 | 2,81:1 |
| `error-100` | `#FCE8E6` | Fondo de error | 1,18:1 | 15,64:1 |
| `error-700` | `#B3261E` | Texto/icono error, botón destructivo | 6,54:1 | 2,82:1 |
| `error-800` | `#8A1C16` | Destructivo pulsado | 9,30:1 | 1,98:1 |
| `warning-100` | `#FFF0C2` | Fondo Pendiente / desactualizado | 1,14:1 | 16,23:1 |
| `warning-500` | `#F2B01E` | Relleno/icono (texto ink-900) | 1,91:1 | 9,65:1 |
| `warning-800` | `#6E4400` | Texto advertencia | 8,43:1 | 2,18:1 |
| `info-100` | `#E3EEFB` | Fondo informativo | 1,17:1 | 15,69:1 |
| `info-700` | `#1D4F91` | Texto/icono informativo | 8,14:1 | 2,26:1 |

**Reglas de uso**

- `accent-500` (`#E77723`) es solo relleno o acento con texto `ink-900` (6,19:1). Sobre blanco da 2,97:1 y sobre `surface-50` 2,75:1: nunca es texto.
- `ink-300` (3,47:1) y `border-200` (1,46:1) no son texto. `ink-300` sirve para el borde de campos (componente de interfaz, 3:1); `border-200` solo separa y nunca es la única señal de un control.
- Los controles deshabilitados usan `ink-500` sobre `surface-100` (5,50:1), con borde `border-200`.
- Los estados se comunican con texto + icono + color, nunca solo con color.

**Otros tokens**

| Grupo | Tokens |
|---|---|
| Tipografía (`font-sans` = Inter; en Mobile puede usarse la fuente del sistema) | `text-display` 32/38 · 800 · `text-title` 24/29 · 700 · `text-heading` 20/25 · 700 · `text-subtitle` 18/23 · 700 · `text-body` 16/22 · 400/600 · `text-body-sm` 14/20 · `text-caption` 12/16 · 600 |
| Espaciado (base 8, subdivisión 4; igual a la escala de Tailwind) | `spacing-1` 4 · `-2` 8 · `-3` 12 · `-4` 16 · `-5` 20 · `-6` 24 · `-8` 32 · `-10` 40 · `-12` 48 |
| Radios | `radius-sm` 8 · `radius-md` 12 · `radius-lg` 16 · `radius-xl` 24 · `radius-full` 999 |
| Sombras (variables de texto) | `shadow-sm` `0 1 3 0 rgba(20,20,20,.08)` · `shadow-md` `0 4 12 0 … .12` · `shadow-lg` `0 8 24 -4 … .18` |

Texto grande WCAG (3:1): ≥24 px o ≥18,66 px en negrita. Los demás tamaños exigen 4,5:1; en este diseño todos los pares de texto cumplen 4,5:1.

## Pares de texto verificados

| Texto | Fondo | Ratio | Resultado |
|---|---|---|---|
| `ink-900` #141414 | `surface-0` #FFFFFF | 18,42:1 | AA (4,5:1) |
| `ink-900` #141414 | `surface-50` #F6F6F3 | 17,01:1 | AA (4,5:1) |
| `ink-700` #3D3D3A | `surface-0` #FFFFFF | 10,90:1 | AA (4,5:1) |
| `ink-500` #5E5E59 | `surface-0` #FFFFFF | 6,52:1 | AA (4,5:1) |
| `ink-500` #5E5E59 | `surface-50` #F6F6F3 | 6,02:1 | AA (4,5:1) |
| `ink-500` #5E5E59 | `surface-100` #ECECE8 | 5,50:1 | AA (4,5:1) |
| `ink-700` #3D3D3A | `surface-100` #ECECE8 | 9,20:1 | AA (4,5:1) |
| `surface-0` #FFFFFF | `primary-600` #C41E24 | 5,90:1 | AA (4,5:1) |
| `surface-0` #FFFFFF | `primary-700` #A3171C | 7,79:1 | AA (4,5:1) |
| `primary-700` #A3171C | `surface-0` #FFFFFF | 7,79:1 | AA (4,5:1) |
| `primary-700` #A3171C | `primary-50` #FDECEC | 6,82:1 | AA (4,5:1) |
| `ink-900` #141414 | `accent-500` #E77723 | 6,19:1 | AA (4,5:1) |
| `surface-0` #FFFFFF | `accent-700` #9A4A0C | 6,25:1 | AA (4,5:1) |
| `accent-700` #9A4A0C | `surface-0` #FFFFFF | 6,25:1 | AA (4,5:1) |
| `accent-700` #9A4A0C | `accent-100` #FCE7D6 | 5,22:1 | AA (4,5:1) |
| `surface-0` #FFFFFF | `error-700` #B3261E | 6,54:1 | AA (4,5:1) |
| `surface-0` #FFFFFF | `error-800` #8A1C16 | 9,30:1 | AA (4,5:1) |
| `error-700` #B3261E | `error-100` #FCE8E6 | 5,55:1 | AA (4,5:1) |
| `success-700` #1B6B38 | `success-100` #E2F3E7 | 5,68:1 | AA (4,5:1) |
| `warning-800` #6E4400 | `warning-100` #FFF0C2 | 7,43:1 | AA (4,5:1) |
| `info-700` #1D4F91 | `info-100` #E3EEFB | 6,94:1 | AA (4,5:1) |
| `surface-0` #FFFFFF | `ink-900` #141414 | 18,42:1 | AA (4,5:1) |
| `ink-900` #141414 | `warning-500` #F2B01E | 9,65:1 | AA (4,5:1) |

No usar como texto: accent-500 sobre surface-0: 2,97 · sobre surface-50: 2,75 · ink-300 sobre surface-0: 3,47 · border-200 sobre surface-0: 1,46


## Pantallas y exportaciones

Todas están en `exports/` (PNG a escala 2: pantallas de 750 × 1624; tableros de 2720 px de ancho). Total ≈ 2,7 MB.

| Archivo | Marco | Contenido |
|---|---|---|
| `S1-colores.png` | S1 | Muestras con hex y contraste, pares aprobados |
| `S2-tipografia-espaciado.png` | S2 | Escala tipográfica, espaciado, radios, sombras |
| `S3-botones-campos.png` | S3 | Botones primario/secundario/acento/destructivo × por defecto, presionado, deshabilitado, cargando; campo por defecto, enfocado, error con mensaje, deshabilitado |
| `S4-insignias-tarjetas-avisos.png` | S4 | Insignias Sin usar (ticket sin utilizar), Utilizado, Vencido, Pendiente, Sin conexión; tarjeta de ticket e itinerario (directo y 2 tramos); avisos de error con reintentar, sin conexión, información desactualizada e informativo |
| `S5-navegacion.png` | S5 | Encabezado raíz y de detalle; pestañas de pasajero (Buscar · Tickets · Abonos · Servicio · Cuenta) y de cobrador (Validar · Cuenta) |
| `M01-acceso.png` | M01 | Acceso |
| `M02-acceso-error.png` | M02 | Acceso con error (credenciales inválidas, valores conservados) |
| `M03-buscar-viaje.png` | M03 | Buscar viaje: origen, destino, fecha |
| `M04-resultados.png` | M04 | Resultados: itinerario directo y de 2 tramos con transbordo |
| `M05-mis-tickets.png` | M05 | Mis tickets: un ticket por tramo |
| `M06-ticket-detalle-qr.png` | M06 | QR grande (marcador de posición, no es un QR real), tramo, servicio, fecha, estado |
| `M07-ticket-sin-conexion.png` | M07 | Variante sin conexión: «Última actualización 12/11 14:30 · puede no estar al día» |
| `M08-cobrador-validar.png` | M08 | Cobrador · Validar: servicio, tramo y Escanear |
| `M09-cobrador-valido.png` | M09 | «Ticket válido» (resultado de validación del cobrador; el pasajero ve «Sin usar») |
| `M10-cobrador-ya-utilizado.png` | M10 | Rechazo «Ya utilizado» |
| `M11-cobrador-no-confirmado.png` | M11 | «No se pudo confirmar» con Reintentar |
| `M12-servicio.png` | M12 | Estado del servicio: última posición, hora de actualización, llegada aproximada |
| `M13-servicio-desactualizado.png` | M13 | «Información desactualizada», sin estimación |

**Convenciones:** áreas seguras de 44 px (superior) y 34 px (inferior); objetivos táctiles ≥48 px (botones 52–64, campos 52, pestañas ≥75 × 56, reintentar 48); sin emoji, logo ni fotos.

## Verificaciones

- `op design:refine --root-id <id>` en las 18 raíces (5 tableros, 13 pantallas): sin cambios en las pantallas. En los tableros reemplazó la altura `fit_content` por 4 px; se restableció `fit_content` después de refinar (`scripts/build.sh`).
- `op design:lint`: 55 advertencias de estilo, ninguna de contraste: 31 `sibling-inconsistency` (alturas fijas de botones y campos frente a `fit_content`, intencionales), 19 `invisible-container` (barras de pestañas y zona segura del tablero S5 sobre fondo blanco, sin borde a propósito) y 5 `mixed-sibling-corner-radius` (caja de transbordo de 8 px dentro de una tarjeta). Se aceptan.
- La revisión visual se hizo sobre los PNG exportados. No se probó en un dispositivo ni con lector de pantalla.

## Preguntas abiertas

1. **Datos de demostración.** Horarios, precios y el itinerario directo Ombúes de Lavalle → Colonia son ilustrativos. ¿Deben coincidir con el seed (FIN-001) y con la ruta Ombúes–Radial–Colonia del MVP?
2. **Fuente.** Inter es la fuente de diseño y el render de OpenPencil puede sustituirla. ¿Se empaqueta Inter en Expo o se usa la fuente del sistema?
3. **Pantallas fuera de este corte.** Faltan Mis abonos, Cuenta, confirmación de pago simulado, permiso de cámara denegado, estados de carga y vacío de cada lista y el portal Blazor (FIN-010-02/03).
4. **Identidad.** Los tonos aproximan el rojo, naranja y negro de Berrutti sin muestrear su marca. Pendiente la confirmación de Enzo (`docs/explanation/ux-design.md` §2).
5. **NativeWind.** Los nombres siguen Tailwind, pero el mapeo a `tailwind.config` y la compatibilidad con Expo 58 no se verificaron.
