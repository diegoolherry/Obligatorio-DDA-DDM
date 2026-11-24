# MVP-005: visualización QR y validación de ticket

Owner Diego, reviewer Enzo. Branch `feature/mvp-005-qr-validacion` (from `origin/main` e855b26, worktree `.worktrees/mvp005`). Implementation slices are planned after the team reviews the mockups.
High-risk units for later slices: atomic single-use consumption (item 4, concurrency) and the camera dependency (item 5).

## Specs

- S1 (backlog evidence): "Antes de implementar, diseñar los mockups de sus pantallas (incluido el permiso de cámara denegado) en OpenPencil con el sistema de diseño de FIN-010 (design/fin-010, decisión de Diego del 2026-11-16)."
- S2 (AC): "El pasajero puede consultar tickets vigentes o utilizados y mostrar el QR opaco de cada tramo."
- S3 (AC): "El cobrador recibe resultados claros para ticket válido, ya utilizado, vencido, tramo incorrecto y código inexistente."
- S4 (AC): "El primer escaneo válido marca el ticket como utilizado y un segundo escaneo del mismo ticket es rechazado."
- S5 (AC): "Un ticket de otro tramo no se consume."
- S6 (description): "Permitir que el pasajero consulte y muestre cada QR de ticket y que el cobrador seleccione el tramo actual, escanee el código y reciba el resultado de la validación desde la API. La validación debe consumir el ticket de forma atómica."

## Tasks

| ID | Specs | Route | Commit |
|---|---|---|---|
| T1 Mockups M14–M27 in `design/fin-010` (passenger filter/states, used-ticket detail, collector segment picker, camera permission pre-prompt and denied, scanner, validating, vencido, tramo incorrecto, código inexistente) + README + exports | S1, S2, S3, S5 | inline; refine 14/14 without fixes, M01–M13 unchanged, visual review of PNGs; lint not run | 3b436f3 |
| T2 API: ticket consumption state, passenger tickets query (vigentes/anteriores), collector validation endpoint with atomic single use, migration + SQL script, tests, docs/reference/api.md | S2–S6 | gentle-ai-worker (item 4 concurrency, item 1 migration); verify mwup4rip PASS by design (TicketValidation 60/60, build 0/0); real-MySQL harness NOT RUN (Diego runs it, L9) | ec38955 |
| T2b API: require the collector to be assigned to the service (L9) | S3, S6 | gentle-ai-worker; verify mwuymq3s PASS (build 0/0, TicketValidation 73/73); minor: no L9 test for malformed/vencido codes | e5fa458 |
| T3 Mobile passenger: ticket list with filter and QR detail | S2 | gentle-ai-worker; MobileTickets 13/13, Purchase 21, Auth 26, Location 14; typecheck via main tsc only; QR is a placeholder pending a library decision | a5ed5ca |
| T4 Mobile collector: segment selection, camera scan, results | S3–S6 | first worker stopped on quota after services/contract/deps (expo-camera ~58.0.14); continuation worker; MobileTicketValidation 22/22, regressions green, typecheck OK; real camera NOT RUN | bdc90c0 |
| T5 Real QR with react-native-qrcode-svg (L10) | S2 | inline; typecheck OK, Mobile suites green, `expo export --platform android` OK | bcf894a |

## Log

- L1 (Diego, 2026-11-24): «sigamos con alguna tarea de @docs/tasks.json» → elegido: «Arrancar MVP-005 por los mockups».
- L2 Exploration: FIN-010 already has M05 (Mis tickets), M06 (QR), M07 (offline), M08 (Validar), M09 (válido), M10 (ya utilizado), M11 (no se pudo confirmar). No document defines exact result wording; `ux-design.md:64` lists the categories and «Validando…»; `ux-design.md:91` asks for in-context permission, explained denial and an exit without invented manual validation. API has `Ticket` without state; no ticket/validation endpoints; Mobile has no ticket/QR/scan screens; `expo-camera` not installed.
- L3 Design proposals open for team review (README question 6): passenger filter labelled «Vigentes · Anteriores» (used + expired) instead of «Utilizados»; collector picks transfer segments of the service, not every stop pair from FIN-017. Result copy is a proposal.
- L4 (Diego, 2026-11-24): filtro «Anteriores»: utilizados y vencidos (como en M15); el cobrador elige «Los tramos de transbordo del servicio (como en M20)».
- L5 (Diego, 2026-11-24) on the base branch: «crea una rama para esta feature» → branch renamed to `feature/mvp-005-qr-validacion` (from origin/main).
- L6 (Diego, 2026-11-24) on expo-camera: «te paso las precentaciones del profe: C:\Users\User\Downloads\Guía_UX_de_Hardware_Móvil-1.pdf C:\Users\User\Downloads\Arquitectura_de_Permisos_Móviles-1.pdf . Poruqe no me suena el expo-camera. Aca tenes el commit donde se subio el codigo de la clase: https://github.com/xalejandrow/ctc-ddm/commit/0d4e0357ab87572afc944017b4d6b27a6f84f2cd». Evidence: the commit adds `clase09/Ejemplo_SIGMA_Camara_Local` using `expo-camera ~58.0.12` (`CameraView`, `useCameraPermissions`, app.json plugin `cameraPermission`, Android `CAMERA`, iOS `NSCameraUsageDescription`). Its README says: request only when needed, explain on denial, allow asking again, and use the phone settings when the dialog no longer shows. QR reading (`onBarcodeScanned`) and opening settings are not shown. Both PDFs are scanned images: no text could be extracted.
- L8 (Diego, 2026-11-24): approved «Sí, aprobado» to read the QR with `onBarcodeScanned` of `expo-camera ~58` (class 09 example) installed with `npx expo install`, and `Linking.openSettings()` for «Abrir Ajustes».
- L7 T2 working decisions (agent, to confirm in review): the collector's tramo is the purchased segment service (the ticket's `CompraServicio` service); «vencido» = the ticket's service day is earlier than the validation day in Uruguay time; a wrong-segment scan leaves the ticket untouched; consumption is a conditional update so only one concurrent scan wins.
- L9 (Diego, 2026-11-24): «Sí, solo en sus servicios asignados (recomendado)» for collectors; «No por ahora» for logging rejected scans; real-MySQL concurrency run: «La corro yo y te paso el resultado».
- L10 (Diego, 2026-11-24): QR rendering library «react-native-qrcode-svg + react-native-svg» (react-native-svg via npx expo install). Applied after T4 releases package.json.
- L11 (Diego, 2026-11-24): «bueno, la concurrencia en MySql la podes hacer vos?. Dsp pruebo yo cuando llegue del parcial. Dsp eso de la barra inferior, seguramente quede apretada deberiamos buscar iconos para utilizae». Real-MySQL run needs a fresh empty DB and private credentials (docs/how-to/purchase-test-checks.md), so it stays with Diego. Icons: «lucide-react-native (recomendado)», applied «En FIN-015, con la barra abajo (recomendado)», not in MVP-005.
