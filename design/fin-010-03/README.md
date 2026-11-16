# FIN-010-03 · Mockups Web de Berrutti (escritorio y angosto)

Archivo OpenPencil con el diseño Web de las páginas existentes de la Web Blazor y **Home elegido: dirección B** (`W01-home`), más las direcciones A y C conservadas como historial. Es una propuesta de diseño: no implica código. Reutiliza los mismos tokens y componentes del sistema Mobile aprobado (`design/fin-010/`: paleta, Inter, espaciado base 8, radios, botones, campos, avisos, insignias, tarjetas). La identidad es el wordmark de texto **Berrutti**; no hay logo, fotografías ni ilustraciones. El texto visible no menciona «demo» ni «ficticio».

## Cómo abrirlo

```bash
op start --file design/fin-010-03/berrutti-web.op              # editor de escritorio
op start --headless --file design/fin-010-03/berrutti-web.op   # servidor sin ventana (CLI)
op stop
```

Páginas del archivo: **Base** (navegación), **Home** (3 direcciones), **Paginas** (desktop 1280), **Narrow** (390). Los PNG están en `exports/` (desktop a escala 1, angostos a escala 2; ≈ 2,1 MB). `scripts/` contiene los generadores: `run.sh <página> <script>` los ejecuta con el servidor iniciado y `fin.sh <página>` corre `design:refine` y restablece `height: fit_content` (refine lo fija en un valor). Los tokens se cargan con `op vars:set @scripts/vars.json --replace` (copia de `design/fin-010/scripts/vars.json`).

## Home: dirección elegida y alternativas

**Decisión (Diego, 2026-11-12): «me gusta la B»; aprobó además el ajuste «me gusta lo que propones, dale nomas»:** la tarjeta Buscar viaje deja de ser roja y pasa a blanca (`surface-0`, borde `border-200`) con franja superior naranja de 4 px, icono de búsqueda, título, descripción y un único botón primario rojo «Buscar viaje». Motivo: el rojo se reserva para acciones primarias (antislop R-01, color con propósito) y las tres entradas del mismo rol deben verse como el mismo tipo de objeto. Ese botón es la única acción primaria roja del contenido de la página (aparte de «Ingresar» en la barra). La tarjeta ocupa el alto de su contenido y queda alineada arriba con la columna derecha. `W01-home` es el Home elegido (desktop); `N2-home-B-centro-de-tareas` es su versión angosta; `H-B-centro-de-tareas` es una copia idéntica de `W01-home`; `H-A` y `H-C` se conservan sin cambios.

Lectura de diseño: portal web de transporte interurbano para pasajeros, estilo sobrio de servicio público, **ENERGY 1 / RHYTHM 2 / MOTION 1**. Todas usan contenido real (horarios y líneas del catálogo `horarios-invierno-2026.json`); sin estadísticas, testimonios ni promesas.

| | A · Búsqueda primero (`H-A`, `N1`) | B · Centro de tareas (`H-B`, `N2`) | C · Salidas de hoy (`H-C`, `N3`) |
|---|---|---|---|
| Idea | Título «¿A dónde viajás?» y el formulario Origen/Destino/Fecha arriba; debajo, salidas de hoy y líneas. | **Elegida.** Título «¿Qué necesitás hoy?» y tres entradas blancas: Buscar viaje (más ancha, franja naranja y botón rojo), Horarios y Ubicación del servicio; debajo, líneas y paradas. | Salidas de hoy por línea con selector de fecha; a la derecha, búsqueda compacta y avisos del catálogo. |
| A favor | La tarea principal es inmediata y no exige cambiar de página; fácil de explicar en la defensa. | Mapea 1 a 1 las páginas existentes; casi no necesita datos de la API; es la más simple de implementar; muestra que Ubicación requiere ingreso. | Responde la pregunta más común (¿a qué hora sale?); los avisos (25/12 sin servicio, vigencia hasta el 20/12) vienen del catálogo real. |
| En contra | Repite el formulario de Buscar viaje (componente compartido); la lista de salidas pide cargar la API en Home. | Es una página de paso: un clic más antes de hacer algo; menos útil para quien vuelve seguido. | Duplica Horarios; dos campos de fecha en una pantalla; más estados de carga y error en Home. |
| Esfuerzo | Medio | Bajo | Alto |


## Pantallas y archivos

| Archivo | Contenido |
|---|---|
| `exports/W00-navegacion.png` | Barra superior para visitante, pasajero y administrador (enlace activo, foco visible) y pie de página |
| `exports/W01-home.png` | **Home elegido (dirección B)**, 1280 |
| `exports/H-A-busqueda-primero.png`, `H-B-centro-de-tareas.png`, `H-C-salidas-de-hoy.png` | Direcciones de Home (historial; `H-B` = `W01-home`) |
| `exports/W02-buscar-viaje.png` | Buscar viaje: formulario y resultados (un itinerario de 2 tramos y uno directo) |
| `exports/W03-horarios.png` | Horarios: salidas de un día y catálogo (un bloque abierto con tablas, tres cerrados, feriados, leyenda) |
| `exports/W04-ingreso.png`, `W05-ingreso-rechazado.png` | Ingreso normal (campo con foco) y credenciales rechazadas (correo conservado, contraseña vacía) |
| `exports/W06-administracion-operativa.png` | Administración: vista de Paradas con lista, formulario con error de campo, aviso de éxito y confirmación de eliminación |
| `exports/W07-ubicacion-del-servicio.png`, `W08-ubicacion-desactualizada.png` | Ubicación vigente y desactualizada (sin llegada ni demora) |
| `exports/W09-estados.png` | Carga, vacío, error (con Reintentar), sin sesión, sin permiso y éxito de cada página |
| `exports/N2-home-B-centro-de-tareas.png` (Home elegido), `N1-home-A-busqueda-primero.png`, `N3-home-C-salidas-de-hoy.png` | Home angosto (390) |
| `exports/N4-buscar-viaje.png`, `N5-menu-desplegado.png` | Buscar viaje angosto con resultados; menú desplegado (pasajero) |

Counter y Weather (plantilla) no se diseñan: se retiran en la implementación.

## Decisiones de diseño (para revisar)

- **Navegación:** solo enlaces a páginas que existen. Visitante: Inicio, Buscar viaje, Horarios, Ingresar. Pasajero agrega Ubicación del servicio y Salir; administrador agrega Administración operativa. Ocultar enlaces no autoriza: la API sigue comprobando. En angosto, un botón **Menú** despliega la lista.
- **Ingreso** no es un enlace del menú: es el botón rojo «Ingresar» (reemplaza el ítem actual).
- **Pie de página:** solo la marca y la fuente de los horarios. Sin contacto, redes ni enlaces.
- **Administración:** se muestra Paradas; Tramos, Servicios y Tarifas siguen el mismo patrón (lista, formulario con error por campo, confirmación de eliminación). Un índice «En esta página» con anclas reemplaza el apilado largo sin cambiar el comportamiento.
- **Copia:** se reemplaza el texto «demostración académica / ficticia» de las páginas actuales por avisos de comportamiento real (sesión solo en la pestaña, posición aproximada). Los mensajes de carga que mencionaban «la API» pasan a lenguaje de usuario («Verificando tus datos…»).
- **Foco:** contorno blanco de 2 px sobre la barra oscura; campos con borde rojo y halo (como en Mobile). Controles ≥ 44 px de alto en Web (botones 52).

## Datos de ejemplo

- Horarios, rutas, paradas, códigos, feriados, excepciones y vigencias salen del catálogo versionado (`src/Api/.../horarios-invierno-2026.json`). Las horas con ≈ son aproximadas, como en el catálogo.
- **Son de ejemplo** (no hay fuente publicada): importes ($ 90, $ 80, $ 170, $ 190), el itinerario de 2 tramos (08:30 y 09:20 del tramo Radial a Colonia, espera de 25 min), coordenadas, antigüedad de la posición, parámetros de estimación (15 s, 120 s, 50 km/h, 5 min), la cuenta `lucia.pereyra@ejemplo.com` y la fecha vie 13/11/2026 usada en las maquetas.

## Verificaciones

- `op design:refine --root-id <id>` en las 18 raíces (1 + 4 + 8 + 5, con `fin.sh`): solo ajusta la altura de la raíz; se restableció `fit_content` después de cada refine.
- `op design:lint` por raíz: 43 advertencias de estilo, ninguna de contraste: 22 `sibling-inconsistency` (alturas fijas de botones y campos frente a `fit_content`), 9 `mixed-sibling-corner-radius`, 4 `mixed-sibling-padding`, 7 `text-explicit-height` (celdas vacías de la columna «Códigos» que conservan el ancho de columna) y 1 `info`. Se aceptan.
- La revisión visual se hizo sobre los PNG exportados. No se probó en navegador, con teclado real ni con lector de pantalla.
- Contraste: todos los pares son los verificados en `design/fin-010/README.md` (el más bajo, 5,22:1). No hay texto naranja sobre blanco ni `ink-300` como texto.

## Delivery Gate de antislop (modo DURANTE, elegido por Diego)

Resultados sobre los mockups; lo que depende de código queda como pendiente para T2/T3.

**Bloque 1: Hard Gate**

| Regla | Resultado | Evidencia |
|---|---|---|
| R-02 raya (em dash) | PASS | Búsqueda de la raya larga en los scripts y en este README: 0 apariciones en el texto visible |
| R-03 móvil | PASS | Cuatro marcos de 390 (N1 a N5) revisados en PNG: sin desborde horizontal; se acortaron dos textos que rozaban el borde (aviso de transbordo, «Requiere ingresar como pasajero.») |
| R-17 estadísticas | PASS | Ninguna. Los importes y datos del punto «Datos de ejemplo» son de maqueta, no afirmaciones |
| R-18 testimonios | PASS | Ninguno |
| R-23 activos sin pedir | PASS | Sin logo, fotos, avatares ni ilustraciones; wordmark de texto. La navegación sale de las páginas existentes |
| R-24 enlaces inexistentes | PASS | Solo Inicio, Buscar viaje, Horarios, Ubicación, Administración, Ingresar/Salir; el enlace «fuente» existe en la página Horarios actual |
| R-25 contraste | PASS | Pares de tokens ya verificados; lint sin advertencias de contraste |
| R-26 controles sin función | PASS con pendiente | Cada botón corresponde a un comportamiento de las páginas actuales o a navegación a ellas. Pendiente de T2: el formulario del Home A y el botón del Home B deben llevar a `/itinerarios` |
| R-27 estados | PASS | `W09`, `W05`, `W08`, `N4`: carga, vacío, error con Reintentar, sin sesión, sin permiso, éxito |
| R-28 FAQ | PASS | No hay FAQ |
| R-32 teclado y foco | Parcial | Foco visible diseñado (`W00`, `W04`). La navegación con Tab/Enter/Escape no se pudo comprobar: no hay código. Pendiente de T2/T3 |
| R-33 parches por script | PASS | Los scripts generan nodos del diseño; no modifican código fuente ni CSS |
| R-34 tema | PASS | Sin interruptor de tema |
| R-35 ejecutado | No aplica | Es un diseño: se exportó y se revisó cada PNG; el recorrido con clics queda para T3 |
| R-36 afirmaciones inventadas | PASS | Los textos sobre fuente, vigencia y feriados provienen del catálogo; la posición se declara aproximada |
| R-37 dirección | PASS | Sale de `docs/explanation/ux-design.md` y `design/fin-010/`; dials declarados arriba |
| R-38 contenido real | PASS | Datos del catálogo; lo inventado está listado en «Datos de ejemplo» |
| R-39 accesibilidad | Parcial | Etiquetas visibles en todos los campos; estados en texto + icono. `lang`, `<title>`, landmarks, `cursor` y estados hover/active quedan para T2 |
| R-40 rendimiento | No aplica | Sin medios ni medición |

**Bloque 2: Purpose-Gate**

| Regla | Resultado | Razón escrita |
|---|---|---|
| R-01 degradados | PASS | Ninguno |
| R-04 iconos | PASS con nota | Son los iconos de OpenPencil con estilo de trazo fino, heredados del sistema Mobile aprobado; cada uno ilustra su etiqueta (buscar, calendario, bus, candado, papelera). Ninguno es genérico de IA |
| R-06 tipografía | PASS | Inter heredada del sistema (legibilidad de horarios y tablas); sin monoespaciada ni mayúsculas espaciadas |
| R-07 cuadrícula de fondo | PASS | Ninguna |
| R-08 flechas | PASS | `→` solo indica sentido del recorrido; el chevron solo en enlaces y acordeones |
| R-09 insignias | PASS | Todas tienen función: tipo de itinerario, códigos del horario, estado de la posición |
| R-10 glassmorphism | PASS | Ninguno |

**Bloque 3: Vivacidad y calidad.** ENERGY 1 / RHYTHM 2 / MOTION 1 (sin animaciones). Un punto focal por pantalla (formulario en A, tarjeta Buscar viaje y su botón rojo en B, lista de salidas en C). Un acento deliberado: la franja naranja bajo la barra y el relleno naranja del enlace activo, igual que en Mobile. Motivo de identidad: wordmark + franja. Consistencia: mismos radios, bordes, campos y botones que Mobile en todas las pantallas.

## Preguntas abiertas

1. **Home:** resuelta, dirección B con tarjeta blanca (ver decisión arriba). Se implementa en T2.
2. **Escala tipográfica Web:** el título del Home usa 40/44 (Mobile llega a 32/38). ¿Se agrega `text-display-xl` al sistema o se mantiene 32?
3. **Rol en el cliente:** el menú por rol supone que la Web conoce el rol tras el ingreso. Si hoy solo lo comprueba la API, ¿se muestran todos los enlaces como ahora y la página explica el rechazo?
4. **Administración:** ¿se acepta el índice de anclas «En esta página» en lugar de las cuatro secciones apiladas, o se prefieren pestañas?
5. **Copia:** ¿se aprueba reemplazar los avisos «demostración académica / ficticio» de las páginas actuales por los textos de este diseño (S5)? Los mensajes de validación por defecto de .NET están en inglés: hay que traducirlos en la implementación.
6. **Importes y itinerarios:** ¿deben coincidir con el seed (FIN-001) y con la ruta Ombúes, Radial, Colonia del MVP?
7. **Ingreso:** la página actual ofrece «Comprobar acceso de administrador» con la sesión abierta; el diseño solo muestra el estado de sesión iniciada en `W09`. ¿Se conserva ese botón?
8. **Fuente:** Inter es la fuente de diseño; el render de OpenPencil puede sustituirla. ¿Se empaqueta en el sitio?
