# Obligatorio Berruti

MVP académico de pasajes y abonos por tramo.

Estamos definiendo un prototipo académico de transporte: permitir que un pasajero busque y compre un itinerario, presente un ticket QR por tramo y consulte sus abonos; que un cobrador valide esos comprobantes; y que un administrador prepare los datos de demostración. Es un proyecto inspirado en una operativa observada, **no un sistema oficial ni afiliado a la empresa**. La propuesta es preliminar y debe contrastarse con la consigna del curso.

## Por dónde empezar

1. Leé el [alcance y los escenarios del MVP](docs/explanation/mvp.md) para distinguir reglas observadas, decisiones académicas y dudas abiertas.
2. Consultá el [backlog del proyecto completo](docs/tasks.json) para estados, responsables, dependencias y criterios de aceptación vigentes. Un único array `tasks` reúne 19 registros: 14 originales (diez MVP y cuatro FIN) y cinco hijos ejecutables de MVP-001 (`MVP-001-01` a `MVP-001-05`, vinculados por `parentId`). En total, 17 están `pending`, MVP-010 está `done` (modelado conceptual) y MVP-001-01 está `in_progress` (fundación parcial Mobile). Los cinco hijos tienen a Diego como responsable confirmado y a Enzo como revisor; todos dependen explícitamente de MVP-010, la estructura precede a los contratos y estos a los tres flujos de sesión independientes. MVP-001 es un hito con criterios originales autoritativos: no se completa automáticamente al completar hijos. Estos conteos no representan funcionalidades independientes ni progreso de implementación. Las áreas `API`, `Web`, `Mobile` y `Docs` permiten varias etiquetas por tarea transversal; los IDs `MVP-*` y `FIN-*` se conservan. Los responsables FIN son propuestos, sin asignación humana confirmada. La aprobación conceptual de Diego y Enzo fue reportada por el usuario; la revisión cruzada de la fundación sigue pendiente. Este resumen no reemplaza el JSON.
3. Para el diseño, mirá el [borrador UML/ER](docs/explanation/data-model.md) y la [guía de experiencia](docs/explanation/ux-design.md). El [informe académico vivo](docs/project-report.md) reúne decisiones, evidencia y pendientes; su Markdown es la fuente para una eventual versión Word.

Este corte añade el **scaffold Web Blazor existente** a las bases API y Mobile anteriores, que permanecen intactas. Las tres fundaciones están presentes, no integradas funcionalmente. Web usa únicamente Bootstrap CSS y su LICENSE heredados del [PR #20](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/20); no incorpora las 43 variantes, scripts y mapas no usados del candidato completo anterior. No hay autenticación, integración cliente/API ni funcionalidades de producto. No hay migraciones, integración con proveedor de pagos ni despliegue verificados. MySQL está instalado y se comprobó una conexión administrativa local, pero no está integrado con la API; el modelo de datos sigue siendo una propuesta, no un esquema aprobado.

El MVP de las primeras semanas de octubre es el primer corte, no toda la entrega. El [alcance final](docs/reference/final-scope.md) agrega mejoras del equipo (no requisitos académicos explícitos): rutas/calendarios y selección web de asientos por intervalo; todavía no están implementadas.

## Base Mobile: ejecución local

El [paquete existente](src/Mobile/package.json) fija Expo `57.0.27`, React Native `0.86.3`, React `19.2.3` y TypeScript `6.0.3`. El entorno previo observado fue Node `24.11.1` y npm `11.19.0`. La pantalla usa componentes nativos, StyleSheet y áreas seguras; no NativeWind ni llamadas de red.

Para ejecutar el scaffold en un entorno preparado:

```bash
cd src/Mobile
npm ci
npm run typecheck
npm start
```

`npm run android` / `npm run ios` requieren un entorno compatible; `npm run export:android` genera un bundle JS, no una compilación Android nativa. Los comandos son instrucciones, **no ejecuciones de este corte documental**.

La evidencia previa es parcial: typecheck, compatibilidad Expo y exportación JS pasaron según el registro original; dos intentos independientes de readiness Metro agotaron el tiempo. Posteriormente el usuario reportó éxito en celular mediante túnel/ngrok, no observado por el agente. La auditoría previa sigue sin remediar: 22 paquetes afectados (15 altos, 7 moderados). Ver [informe §7](docs/project-report.md#7-calidad-pruebas-y-gestión-de-configuración) y [registro Mobile](odd/tasks/mvp-001-01-mobile-foundation.md). No acredita revisión de Enzo ni finalización de MVP-001-01.

## Base API: scaffold .NET 10

La [solución API-Berruti.slnx](src/Api/API-Berruti/API-Berruti.slnx) contiene el [proyecto API-Berruti.csproj](src/Api/API-Berruti/API-Berruti/API-Berruti.csproj), con `net10.0` y `Microsoft.AspNetCore.OpenApi` `10.0.12`. [Program.cs](src/Api/API-Berruti/API-Berruti/Program.cs) registra Controllers y OpenAPI, expone OpenAPI solo en Development y conserva redirección HTTPS y middleware de autorización; esto **no implementa autenticación ni autorización por rol**. El ejemplo WeatherForecast devuelve cinco registros aleatorios, no datos del dominio.

Instrucciones para un entorno preparado, **no ejecutadas en este corte**:

```bash
dotnet build src/Api/API-Berruti/API-Berruti.slnx --nologo
dotnet run --project src/Api/API-Berruti/API-Berruti/API-Berruti.csproj --launch-profile http
```

Los [perfiles existentes](src/Api/API-Berruti/API-Berruti/Properties/launchSettings.json) usan Development: `http` publica `http://localhost:5095`; `https` publica `https://localhost:7053;http://localhost:5095`. No son el mecanismo del smoke test histórico: aquel usó `--no-build --no-launch-profile --urls http://127.0.0.1:5095` con Development explícito.

El verificador de la sesión original observó SDK `10.0.401`, build API sin errores ni advertencias, `/weatherforecast` HTTP 200 con cinco registros JSON y `/openapi/v1.json` HTTP 200 con OpenAPI `3.1.1`. También observó `Failed to determine the https port for redirect`. Son **resultados históricos, no comprobaciones del candidato actual**; TLS, integración cliente/API y auth siguen sin verificar. [Informe §7](docs/project-report.md#7-calidad-pruebas-y-gestión-de-configuración) y [registro de este corte](odd/tasks/api-foundation-delivery.md) separan procedencia, checks locales y pendientes. `.gitignore` excluye artefactos .NET/Visual Studio, no soluciones, proyectos ni fuentes.

## Base Web: scaffold Blazor .NET 10

La [solución Web-App.slnx](src/Web/Web-App/Web-App.slnx) contiene el [proyecto net10.0](src/Web/Web-App/Web-App/Web-App.csproj). [Program.cs](src/Web/Web-App/Web-App/Program.cs) registra componentes y renderizado InteractiveServer; [App.razor](src/Web/Web-App/Web-App/Components/App.razor) carga Routes y Blazor, sin imponer interactividad global. [Counter](src/Web/Web-App/Web-App/Components/Pages/Counter.razor) declara `@rendermode InteractiveServer`. Las páginas de plantilla son Home (`/`), Counter (`/counter`), Weather (`/weather`, datos aleatorios locales, no API), Error (`/Error`) y NotFound (`/not-found`). No son pantallas de negocio, auth ni integración cliente/API; se conservan los nombres originales `Web-App` y `Web_App`.

Desde la **raíz del repositorio**, en un entorno preparado:

```bash
dotnet build src/Web/Web-App/Web-App.slnx --nologo
```

En **otra terminal**, también desde la raíz, para mantener Web en ejecución:

```bash
dotnet run --project src/Web/Web-App/Web-App/Web-App.csproj --launch-profile http
```

El [perfil http](src/Web/Web-App/Web-App/Properties/launchSettings.json) usa Development y `http://localhost:5025` (`https` usa `https://localhost:7054;http://localhost:5025`). Son instrucciones de ejecución local; el verificador independiente comprobó **este corte reducido** con SDK `10.0.401`: build exit 0, sin advertencias/errores; Development con `--no-build --no-launch-profile --urls http://127.0.0.1:5025`, Home/Counter/Weather y cuatro assets fingerprinted HTTP 200. Playwright ya instalado con Chrome verificó Home visible, Counter 0→1 por clic, cinco filas Weather y Bootstrap aplicado; sin errores de consola, página ni red. Servidor/navegador propios cerrados y puerto liberado. El override no ejecutó el perfil; el warning de redirección HTTPS no fue fatal. Las pruebas del candidato completo anterior se conservan como historia separada, no como sustituto de esta verificación. TLS, auth e integración cliente/API siguen sin verificar; HTTP HTML solo no acredita interactividad. Ver [informe §7](docs/project-report.md#7-calidad-pruebas-y-gestión-de-configuración) y [registro Web](odd/tasks/web-foundation-delivery.md).

Bootstrap `5.3.3` conserva CSS y licencia MIT completa; el comentario `sourceMappingURL` original permanece, pero el mapa no se distribuye y no se promete depuración con sourcemaps en DevTools.

## Recorrido que queremos demostrar

El caso conductor va de **Ombúes a Colonia**, con transbordo en **Radial de Conchillas**: dos servicios, dos tickets QR diferentes y validación de un solo uso para cada tramo. El primer tramo no asigna asiento; el segundo lo asignaría automáticamente. También se proponen dos abonos relacionados, con igual cupo inicial y saldos independientes: cada validación descuenta una unidad del abono del tramo correspondiente.

Los roles previstos son **pasajero** (web y móvil), **cobrador** (móvil) y **administrador** (web). Un dispositivo o celular embarcado autorizado publicaría posiciones ficticias para consultar última actualización, llegada aproximada y demora básica; esto es geolocalización académica, no seguimiento operativo en tiempo real. La compra propone **Mercado Pago mediante su API en entorno TEST**, decisión revisable y aún no implementada: la API tendría que verificar la aprobación del proveedor antes de emitir tickets. No se prevén cobros reales ni almacenamiento de tarjetas. Los registros operativos de demostración son ficticios; los escenarios de aprobación y rechazo de pago deberán usar el proveedor TEST, no resultados locales inventados.

## Estructura documental actual

El árbol resume los documentos y carpetas del proyecto, incluido el backlog completo en su nueva ruta. No incluye índices locales ni carpetas ignoradas.

```text
.
├── .github/
│   └── workflows/docs-validation.yml
├── docs/
│   ├── explanation/
│   │   ├── data-model.md
│   │   ├── entities-explained.md
│   │   ├── mvp.md
│   │   └── ux-design.md
│   ├── reference/
│   │   ├── final-scope.md
│   │   ├── requirements.md
│   │   └── user-stories.md
│   ├── tasks.json           # backlog del proyecto completo
│   └── project-report.md
├── src/
│   ├── Api/API-Berruti/        # solución y scaffold net10.0, sin negocio
│   ├── Web/Web-App/            # plantilla Blazor net10.0, CSS mínimo, sin negocio
│   └── Mobile/                # base Expo/TypeScript anterior, sin negocio
├── odd/
│   └── tasks/                 # registros de trabajo y evidencia
├── skills/
│   └── ctc-project-conventions/
│       ├── SKILL.md
│       ├── references/
│       ├── assets/
│       └── scripts/
├── .gitignore
├── AGENTS.md
└── README.md
```

[AGENTS.md](AGENTS.md) establece cómo mantener el backlog y el informe; la [skill del proyecto](skills/ctc-project-conventions/SKILL.md) reúne convenciones de clase. El workflow existente valida documentación, no acredita pruebas funcionales del MVP.

## Estructura objetivo ilustrativa

**Propuesta, no carpetas existentes ni nombres definitivos.** Cuando el MVP esté implementado, una organización posible separaría responsabilidades así:

```text
.
├── src/
│   ├── Api/
│   │   ├── Controllers/       # límite HTTP y autorización
│   │   ├── Services/          # reglas de negocio y validaciones
│   │   ├── Repositories/      # acceso a datos
│   │   ├── Data/              # EF Core DbContext y persistencia MySQL
│   │   └── Contracts/         # DTO explícitos de solicitud/respuesta
│   ├── Web/                   # Blazor: pasajero y administrador
│   └── Mobile/                # React Native/TypeScript/Expo: pasajero y cobrador
├── tests/                     # pruebas por definir para API y recorridos
├── docs/                      # requisitos, diseño, backlog e informe
└── skills/                    # convenciones del proyecto
```

En la API, el flujo previsto es **Controllers → Services → Repositories → EF Core DbContext**. Web y móvil consumirían contratos DTO explícitos, sin depender de entidades de persistencia ni decidir por sí solos autorización, aprobación de pago o consumo de QR. La forma exacta de proyectos, carpetas y pruebas se acordará durante la implementación; este dibujo no es una instrucción de instalación ni una promesa de herramientas ya configuradas.

Para seguir el trabajo, revisá los [requisitos](docs/reference/requirements.md), las [historias de usuario](docs/reference/user-stories.md) y las decisiones abiertas del [modelado](docs/explanation/data-model.md) antes de iniciar código o migraciones.
