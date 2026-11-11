# MVP-009 — Geolocalización básica de demostración

Worktree `.worktrees/mvp-009`, rama `feature/mvp-009-geolocation` (base `main` 4afafc7). Coordinadora: sesión 01a1c835. Revisa Enzo.
Backlog: hijos MVP-009-01 (API), MVP-009-02 (Mobile), MVP-009-03 (Web) en `docs/tasks.json`; sus `acceptanceCriteria` son autoritativos.

## Specs

- S1 (backlog MVP-009, criterio 1): "La API acepta una posición básica solo de un dispositivo o celular embarcado autorizado y asociado a un servicio de demostración existente; rechaza dispositivos no autorizados, servicios inexistentes y posiciones incompletas sin reemplazar la última posición."
- S2 (criterio 2): "Cada publicación aceptada conserva dispositivo, servicio, posición y fecha/hora de recepción; la periodicidad se contrasta con el parámetro académico configurado."
- S3 (criterio 3): "El pasajero consulta la última posición y su actualización; la información que supera el umbral configurable se marca como desactualizada y la ausencia de posiciones o un error se comunican con opción de reintento."
- S4 (criterio 4): "Con datos vigentes y referencias configuradas se muestran llegada aproximada y estado básico de demora (en horario o con demora); con datos insuficientes o desactualizados se informa indisponibilidad sin inventar precisión."
- S5 (criterio 5): "La periodicidad de publicación y el umbral de desactualización son parámetros académicos visibles en la configuración y configurables; las referencias de llegada y demora son configurables y los datos se identifican como ficticios."
- S6 (decisión de Diego): el dispositivo es el celular del cobrador: "mientras con su celu cobra, tambien se sepa la ubicacion de obmnibus". Sesión de rol Cobrador; asociación cobrador↔servicio sembrada; expo-location aprobado.
- S8 (material de clase aportado por Diego, 2026-11-11): «Arquitectura de Permisos Móviles» (s5: ubicación en primer plano = When In Use; segundo plano «solo si es esencial»; s9 Camino Dorado: pedir en contexto, explicar antes, pedir lo mínimo, prever el rechazo y guiar a Ajustes), «Guía UX de Hardware Móvil» (s7: «Jamás pidas permisos apenas se abre la app»; s9: tras rechazo no repetir el diálogo, si está bloqueado botón a Ajustes; s10/s14: GPS es observación con precisión, re-verificar el permiso antes de cada acción) y el ejemplo de clase `xalejandrow/ctc-ddm@0d4e035` `clase09/Ejemplo_SIGMA_Camara_Local` (hook `useCameraPermissions`, rama de carga mientras `permission` es null, explicación + botón que llama `requestPermission`, plugin en `app.json` con texto de propósito, versiones `~58.0.x` con `npx expo install`).
- S7 (decisión de Diego): cálculo = distancia al destino / velocidad; parámetros en appsettings.json de la API; consulta del pasajero en "App Expo y también Blazor".

## Tasks

- T1 [S1-S2,S5-S7] API: migración `UbicacionDemo` (cobrador_servicio, posicion_recibida) + SQL, seed, parámetros `Ubicacion`, POST publicación (Cobrador asignado), GET consulta (Pasajero), pruebas de invariantes de autorización — inline — MVP-009-01 — commits: bde0a7a (esquema), cdc6481 (API, pruebas y docs), + fix UTC al leer recibida_utc; verificación independiente sin bloqueantes (S1,S2,S5-S7 PASS); `has-pending-model-changes` limpio y SQL idéntico al regenerado; falta `update-local-db.ps1 -Apply` (Diego).
- T2 [S3-S6, S8] Mobile: cobrador comparte ubicación en primer plano; pasajero consulta — worker — rama `feature/mvp-009-02-mobile` — MVP-009-02 — commits: b67367d, d53f83c, 332085e (fixes del verify), a17d7eb (sesión entre pestañas), 13cc05e (envío entre pestañas); typecheck OK, node --test 39/39; verificación independiente PASS; falta prueba en iPhone (Diego).
- T3 [S3-S5,S7] Web: página de pasajero en Blazor (`/pasajero/ubicacion`) — worker — MVP-009-03 — commit: 57ca3a5; build Web 0/0, WebAuthentication 98/98, AuthenticationContracts 50/50; sin recorrido en navegador todavía.

Corte de PR: el primer PR es MVP-009-01 (bde0a7a..cdc6481, ~1.6k líneas con la migración generada). Web y Mobile van en cortes separados encima.

## Log

- L1 (coordinadora, por pedido de Diego): trabajar MVP-009 «Geolocalización básica de demostración» en el worktree; avisar antes de migraciones; no correr reset-local-db.ps1; partir en hijos si cruza capas.
- L2 (Diego): "La idea es que sea desde el celular del cobrador, tipo que mientras con su celu cobra, tambien se sepa la ubicacion de obmnibus." / "La geolocalizacion la queremos para saber la ubicacion del obmnibus, osea q por web no seria." Luego: asignación sembrada; expo-location sí ("en clase vimos cosas de permisos, tipo ciclo de vida y demas... te puedo pasar y mostrar la presentacion y creo que un repo de github"); consulta en "App Expo y también Blazor".
- L3 (coordinadora): OK a la migración; script con `dotnet ef migrations script <anterior> <nueva>`; solo se permite quitar la línea en blanco final; probar con `update-local-db.ps1 -Apply`.
- L4 (verify T1): sin bloqueantes; observó que `recibidaUtc` leído de MySQL salía sin `Z` → corregido con conversión a UTC en el DbContext.
- L5 (2026-11-11, recorrido Web con ego-browser tras reset de Diego y rebase sobre main 6062baa): sin sesión → «Ir al ingreso»; pasajero lista 2 servicios; posición fresca → llegada 01:18 «en horario»; >120 s → «desactualizada» y «no disponibles»; servicio editado por admin a llegada 01:05 + publicación → llegada 01:29 «con demora»; API caída → error y «Reintentar» recupera. Script del cobrador: 201/201/201, periodicidad 30,09 s cumple; corregido para PowerShell 5.1 (06bb50e, 206ce7a). Post-rebase: has-pending-model-changes limpio, AuthenticationContracts 50/50, WebAuthentication 105/105, Geolocation 27/27. Observación: una publicación previa no identificada (~00:14) dejó intervalo=2094 s.
- L6 (Diego, 2026-11-11): aportó 'C:\Users\User\Downloads\Arquitectura_de_Permisos_Móviles.pdf', 'C:\Users\User\Downloads\Guía_UX_de_Hardware_Móvil.pdf' y https://github.com/xalejandrow/ctc-ddm/commit/0d4e0357ab87572afc944017b4d6b27a6f84f2cd; #72 y #73 mergeados (2053f28, 5372688), MVP-009-01 y -03 cerrados (4db4520).
- L7 (Diego, prueba en iPhone 2026-11-11): «va todo bien»; la sesión se borraba al cambiar de pestaña (corregido a17d7eb); pidió que el envío siga en segundo plano → decidió dejarlo para FIN-007; al cambiar de pestaña se cortaba el envío → eligió «que siga con cualquier pestaña» (13cc05e).
