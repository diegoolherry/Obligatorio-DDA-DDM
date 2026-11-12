# Offline-first en Mobile: análisis del material del curso

Este documento cumple con FIN-006. Analiza el material de arquitectura offline-first que se trabaja en Desarrollo para Dispositivos Móviles y evalúa qué partes aplican a la app Expo del proyecto. **Es un análisis con una propuesta: no autoriza implementación, dependencias, caché ni sincronización.** Cualquier cambio posterior requiere una tarea nueva y que el equipo lo apruebe.

## Lectura rápida

- El material enseña un patrón concreto: escribir primero en el dispositivo, registrar la intención en una cola durable y sincronizar cuando hay red. Los manuales lo muestran con código (AsyncStorage + NetInfo); las presentaciones lo amplían a nivel de concepto (SQLite, conflictos, BaaS).
- En Berrutti, las operaciones con valor (compra, pago, validación de QR, asignación de asientos) son **autoritativas en el servidor y requieren conexión**: [SUP-02](../reference/requirements.md) y el [alcance final §5](../reference/final-scope.md#5-compra-y-estados-de-experiencia) prohíben prometer compra, reserva o validación offline. El propio material clasifica la «reserva de asientos» como caso *server-authoritative*.
- Por eso el núcleo del patrón, la cola de mutaciones, no tiene hoy ninguna mutación legítima en el proyecto. Lo aplicable es más chico: **lectura de la última copia conocida, estado visible de conectividad y reintentos idempotentes**, que ya coinciden con reglas vigentes.
- Propuesta mínima, aprobada por Diego el 2026-11-12 (§4): una caché de solo lectura de los tickets del pasajero con la etiqueta «última actualización», el QR visible sin red y un indicador de sin conexión. Sin cola de escritura. Se implementa en `FIN-009`.

## 1. Material analizado y nivel de evidencia

El material lo aportó Diego el 2026-11-12. No se versiona en el repositorio; se cita por nombre de archivo y diapositiva o página. Las cinco presentaciones son imágenes, sin texto seleccionable, y llevan la marca «Gemini Notebook»: se transcribieron leyendo cada diapositiva.

| Ref. | Archivo | Tipo | Contenido principal |
| --- | --- | --- | --- |
| D1 | `1-Arquitectura_Offline-First_en_React_Native.pdf` (15 diap.) | Presentación | Patrón offline-first, cola, compactación, idempotencia, conflictos. |
| D2 | `2-Arquitectura_Offline_SIGMA.pdf` (13 diap.) | Presentación | Cuatro opciones de arquitectura (A–D) y recomendación para el caso SIGMA. |
| D3 | `3-SIGMA_Offline_Data_Architecture.pdf` (11 diap.) | Presentación | Modelo local SQLite (`tasks`, `sync_queue`, `attachments`) y transacciones. |
| D4 | `4-SIGMA_Data_Architecture_Blueprint.pdf` (9 diap.) | Presentación | ORMs y herramientas por entorno (Prisma, TypeORM, Drizzle, WatermelonDB). |
| D5 | `5-Arquitectura_Backend_Móvil_SIGMA.pdf` (13 diap.) | Presentación | Firebase frente a Supabase; decisión «SQLite + API propia». |
| M1 | `Manual Tecnico - Persistencia Local y Estrategia Offline en React Native.pdf` (3 p.) | Manual | Tipos, `storageService`, `syncEngine`, hook `useOfflineTodos`. |
| M2 | `Manual_Persistencia_Offline_SIGMA_React_Native.pdf` (8 p.) | Manual | Ejemplo SIGMA completo con AsyncStorage, NetInfo y axios; pruebas manuales. |
| M3 | `Referencia_Funciones_Persistencia_y_Sincronizacion_SIGMA.pdf` (7 p.) | Manual | Firmas y código de `getJson`, `createTask`, `compact`, `isOnline`, `syncPendingOperations`. |

Además, Diego aportó el código de ejemplo de la clase: [`xalejandrow/ctc-ddm/clase07`](https://github.com/xalejandrow/ctc-ddm/tree/main/clase07) (commit `0d4e035`, 2026-11-03), con cinco proyectos Expo 57:

| Ref. | Proyecto | Mobile usa |
| --- | --- | --- |
| C0 | `SIGMA_Persistencia_Local_Inicial` | AsyncStorage (`src/storage.ts`). |
| C1 | `SIGMA_Opcion_1_Express_PostgreSQL_AsyncStorage` | AsyncStorage, NetInfo (`NetInfo.addEventListener`, `NetInfo.fetch` en `App.tsx`), axios, cola con compactación. |
| C2 | `SIGMA_Opcion_2_Express_MySQL_SQLite_OfflineFirst` | `expo-sqlite`, NetInfo, axios. |
| C3 | `SIGMA_Opcion_2_NestJS_PostgreSQL_SQLite_OfflineFirst` | `expo-sqlite`, NetInfo, axios. |
| C4 | `SIGMA_Opcion_C_Supabase_PostgreSQL_SQLite_Local` | `expo-sqlite`, NetInfo, `@supabase/supabase-js`. |

Clasificación usada, alineada con la [skill Mobile del proyecto](../../.agents/skills/ctc-ddm-mobile/SKILL.md):

| Concepto | Nivel | Evidencia |
| --- | --- | --- |
| Persistir JSON con AsyncStorage (`getJson`/`setJson`) | **Demostrado con código** | M2 p. 4–5 §7.2; M3 p. 2; M1 p. 3; C0 `src/storage.ts`; C1. |
| Detectar red con NetInfo (`NetInfo.fetch`, `isInternetReachable`) | **Demostrado con código** | M3 p. 5 `isOnline`; M1 p. 3; C1 `App.tsx`. |
| Cola durable CREATE/UPDATE/DELETE y compactación | **Demostrado con código** | M3 p. 4 `compact`; M2 p. 6 §7.3; D1 diap. 9. |
| Sincronización secuencial que se detiene ante el primer fallo | **Demostrado con código** | M3 p. 6 `syncPendingOperations`; M2 p. 6 §7.4; D1 diap. 10. |
| Id local estable + `serverId` asignado por la API | **Demostrado con código** | M2 p. 5 §7.1; D1 diap. 6 y 13. |
| Clave de idempotencia (`X-Idempotency-Key` / `X-Operation-Id`) | Código parcial (encabezado) | M1 p. 3; M3 p. 5; D1 diap. 11. |
| Conflictos: LWW, server-authoritative (409), merge | Concepto | M1 p. 2; M2 p. 7 §9; D1 diap. 12. |
| SQLite local con `sync_queue` y transacción | Concepto en las presentaciones; código en C2–C4 (`expo-sqlite`), no revisado en detalle | D3 diap. 4–7 y 10; D2 diap. 6; C2–C4. |
| `expo-secure-store` para tokens | Mención | D1 diap. 5; M2 p. 3; C0 `README.md` («Para tokens se utilizará `expo-secure-store`»), sin código ni dependencia en ningún proyecto. |
| WatermelonDB, MMKV, Drizzle, TypeORM en Mobile | Mención | D4 diap. 5–9; D1 diap. 14. |
| Firebase y Supabase | Concepto comparativo, sin código | D5; D2 diap. 7–8. |
| Backoff exponencial y cola de subida de fotos | Mención | M1 p. 3; D1 diap. 14; D3 diap. 11. |

**No verificado:** ninguno de los ejemplos (manuales ni C0–C4) se ejecutó. Los proyectos de clase usan Expo 57 y el proyecto usa Expo 58: las versiones de AsyncStorage y NetInfo se deben resolver con `npx expo install` al implementar, no copiar de clase.

## 2. Qué es offline-first según el material

1. **La pantalla lee y escribe contra el almacenamiento local**, nunca espera al servidor (D1 diap. 2 y 15; D3 diap. 2).
2. **Cada acción produce dos escrituras locales: el dato y la intención** (D1 diap. 8; D3 diap. 8). La intención va a una cola durable que sobrevive al cierre de la app (M2 p. 6).
3. **La cola se compacta** para enviar solo la intención final (D1 diap. 9; M3 p. 4).
4. **La sincronización es secuencial y conservadora**: quita una operación solo tras una respuesta exitosa y se detiene ante el primer error para no romper el orden (D1 diap. 10; M3 p. 6).
5. **Reintentar es seguro** gracias a una clave de idempotencia por operación (D1 diap. 11; M3 p. 5).
6. **Los conflictos dependen del dominio**: LWW para datos personales y server-authoritative para saldos, inventario y «reserva de asientos» (M1 p. 2; D1 diap. 12).
7. **El estado tiene que ser visible**: Pendiente, Sincronizada o Error, junto con la última sincronización (D2 diap. 13; D3 diap. 11; M2 p. 8).
8. **Seguridad**: no se guardan tokens ni contraseñas en AsyncStorage ni en tablas sin cifrar (M2 p. 3; D3 diap. 11).

Para el proyecto integrador SIGMA, el material recomienda una API propia + SQLite local con `sync_queue`, en fases: primero AsyncStorage, después SQLite, después la cola (D2 diap. 10–12; D5 diap. 12–13).

## 3. Contraste con Berrutti

### Requisitos vigentes

| Regla del proyecto | Fuente | Relación con offline-first |
| --- | --- | --- |
| La validación de QR requiere conexión con la API; sin modo offline en el MVP. | [requirements.md SUP-02](../reference/requirements.md), [mvp.md](mvp.md) | **Conflicto directo** con validar sin red. Se mantiene la regla. |
| Sin conexión: conservar datos recuperables y ofrecer reintento; no prometer reserva ni compra offline. | [final-scope.md §5](../reference/final-scope.md#5-compra-y-estados-de-experiencia) | Coincide con «conservar y reintentar», pero **excluye** la UI optimista para compras. |
| Confirmación de compra atómica e idempotente; ante un timeout, consultar el estado autoritativo antes de repetir. | [final-scope.md](../reference/final-scope.md) | Coincide con la idempotencia del material (D1 diap. 11). Ya es un requisito, no algo nuevo. |
| Pago TEST verificado por la API antes de mostrar tickets. | [final-scope.md §5](../reference/final-scope.md#5-compra-y-estados-de-experiencia) | Incompatible con mostrar un ticket «Pendiente» como si estuviera emitido. |
| Ubicación: última posición conocida, con un indicador de desactualizada. | [requirements.md](../reference/requirements.md) | Coincide con «mostrar la última copia + fecha» (D1 diap. 13). |

### Arquitectura Mobile actual (`src/Mobile`)

- Expo 58 con `expo-location` y `expo-status-bar`. **No tiene AsyncStorage, NetInfo, SQLite ni SecureStore.** Ninguna pantalla persiste datos entre aperturas.
- `AuthenticationService` guarda el token **solo en memoria** (`private token`) y nunca lo expone. Esto cumple con la regla del material de no persistir tokens. Persistir la sesión requeriría `expo-secure-store` (mención en D1 diap. 5), algo que hoy no se pidió.
- Los servicios (`services/Itinerary`, `services/Location`) ya separan la UI del acceso HTTP, que es la capa «repositorio» que pide el material (M2 p. 3; D1 diap. 3). Un cambio futuro encajaría ahí sin tocar componentes.
- El backend es ASP.NET Core + MySQL, no NestJS/PostgreSQL como en SIGMA. Los conceptos de servidor (idempotencia, 409, `updatedSince`) se pueden trasladar; las herramientas de D4 no aplican.

### Por qué el caso SIGMA no se traslada tal cual

SIGMA registra trabajo de campo propio del técnico, donde la última escritura gana sin perjuicio para nadie. En Berrutti, en cambio, las mutaciones relevantes compiten por recursos compartidos (asientos, pagos, usos de abono) o cumplen funciones de control (validación). El material las ubica explícitamente en la estrategia server-authoritative (M1 p. 2), que exige conexión. **La cola de mutaciones solo tendría sentido para datos que no compiten**, y hoy el único candidato es la publicación de posiciones (ver D-4).

## 4. Propuesta mínima y límites

Diego aprobó los tres puntos el 2026-11-12 («Me gusta, estoy de acuerdo con los 3 puntos»). Se implementan en `FIN-009` del [backlog](../tasks.json), que depende de MVP-005.

1. **Caché de solo lectura de «Mis tickets»** (cuando exista MVP-005): guardar en AsyncStorage la última lista recibida y mostrarla sin red con «Última actualización: <fecha>» y un aviso de que el estado puede haber cambiado. La caché se reemplaza en cada respuesta exitosa y se borra al cerrar sesión. Usa solo conceptos demostrados (M2 §7.2; M3 `getJson`).
2. **QR visible sin red** (decisión D-2): desde la copia, el pasajero puede mostrar el QR. La seguridad no cambia porque el cobrador valida siempre contra la API (SUP-02) y un ticket usado o anulado se rechaza ahí.
3. **Indicador de conectividad** con NetInfo (demostrado, M3 `isOnline`) en las pantallas que dependen de la API, con reintento manual. Sustituye mensajes de error genéricos por «Sin conexión» cuando corresponda.
4. **Nada de cola de escritura**: compra, pago, validación y abonos siguen siendo en línea y autoritativos.

Límites explícitos:

- Agrega dos dependencias (`@react-native-async-storage/async-storage`, `@react-native-community/netinfo`). Instalar dependencias requiere aprobación, según la [skill del proyecto](../../.agents/skills/ctc-ddm-mobile/SKILL.md).
- No se cachean tokens ni datos de pago.
- La caché no sirve como prueba de validez: un ticket cacheado puede haber sido usado o anulado.
- No se promete funcionamiento offline en el informe ni en la UI.

## 5. Decisiones

Todas las tomó Diego el 2026-11-12. Enzo las confirma en la revisión cruzada.

| ID | Pregunta | Decisión |
| --- | --- | --- |
| D-1 | ¿Se aprueba la propuesta mínima de la §4? | **Aprobada**, los tres puntos, como tarea nueva. |
| D-2 | ¿El pasajero puede mostrar el QR de un ticket cacheado sin red? | **Sí**, con el aviso de última actualización. La validación sigue requiriendo API. |
| D-3 | ¿Persistir la sesión con `expo-secure-store`? | **No por ahora.** El repo no tiene nada implementado (el token vive solo en memoria en `AuthenticationService`) y el curso solo lo menciona. |
| D-4 | ¿Qué pasa con las posiciones del cobrador sin red? | **Se descartan.** Al recuperar la red se envía la posición de ese momento. Sin cola de ubicación. |
| D-5 | ¿Se adopta SQLite? | **Diferida**, se evalúa más adelante. |

## 6. Qué quedó afuera

- No se implementó nada ni se instalaron dependencias.
- No se ejecutaron los ejemplos del material.
