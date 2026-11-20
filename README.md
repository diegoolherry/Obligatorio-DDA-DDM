# Obligatorio Berruti

Proyecto académico de pasajes y abonos por tramo, inspirado en una operativa observada: **no es un sistema oficial ni afiliado a la empresa**. API .NET, Web Blazor y Mobile Expo tienen autenticación JWT, búsqueda de itinerarios y compra de pasajes con Mercado Pago (modo TEST, sin cobros reales). La compra se ejecutó de punta a punta desde la Web el 2026-11-19 ([PR #130](https://github.com/diegoolherry/Obligatorio-DDA-DDM/pull/130)); el código de compra Mobile no se ejecutó en ese E2E. Faltan QR, validación y abonos (ver el [backlog](docs/tasks.json)).

## Elegí tu recorrido

| Quiero… | Documento |
| --- | --- |
| Aprender con una primera ejecución | [Tutorial: primer arranque local](docs/tutorials/first-local-run.md) |
| Resolver una tarea concreta | [Direcciones cliente/API](docs/how-to/client-api-configuration.md) · [Bases MySQL locales](docs/how-to/local-auth-database.md) · [Recrear la base local](docs/how-to/local-database-reset.md) · [Mercado Pago TEST de punta a punta](docs/how-to/purchase-test-checks.md) · [Resolución de problemas](docs/how-to/troubleshooting.md) |
| Configurar el ingreso local | [API y autenticación JWT](docs/how-to/api-session-checks.md) · [Cliente Web](docs/how-to/web-passenger-login.md) · [Cliente Mobile](docs/how-to/mobile-passenger-login.md) |
| Consultar especificaciones | [Requisitos](docs/reference/requirements.md) · [Historias de usuario](docs/reference/user-stories.md) · [Alcance final](docs/reference/final-scope.md) · [API implementada](docs/reference/api.md) · [Contratos iniciales de autenticación](docs/reference/auth-contracts.md) |
| Entender el contexto y las decisiones | [MVP](docs/explanation/mvp.md) · [Modelo de datos](docs/explanation/data-model.md) · [Entidades explicadas](docs/explanation/entities-explained.md) · [Diseño UX](docs/explanation/ux-design.md) · [Defensa de conceptos de MVP-004](docs/explanation/mvp-004-defensa-conceptos.md) |

## Fuentes canónicas

- [Backlog del proyecto completo](docs/tasks.json): estados, responsables, dependencias y criterios de aceptación. No se replica su progreso aquí.
- [Informe académico vivo](docs/project-report.md): decisiones, evidencia histórica y pendientes; fuente Markdown para la futura entrega Word.
- [AGENTS.md](AGENTS.md): salvaguardas de colaboración y mantenimiento. [Skill compartida](.agents/skills/ctc-project-conventions/SKILL.md): flujo y convenciones de clase.

<a id="base-web-scaffold-blazor-net-10"></a>

Los enlaces históricos a la base Web continúan aquí: las [instrucciones actuales](docs/tutorials/first-local-run.md#2-iniciá-web) están en el tutorial y la evidencia permanece en el informe §7.

Para preparar el entorno, seguí el tutorial: contiene las rutas de las soluciones existentes y los comandos del scaffold, sin recrear proyectos. Para reglas de producto, consultá las referencias y explicaciones antes de implementar.
