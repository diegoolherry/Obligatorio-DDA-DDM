# Proyecto académico de pasajes y abonos por tramo

Estamos definiendo un prototipo académico de transporte: permitir que un pasajero busque y compre un itinerario, presente un ticket QR por tramo y consulte sus abonos; que un cobrador valide esos comprobantes; y que un administrador prepare los datos de demostración. Es un proyecto inspirado en una operativa observada, **no un sistema oficial ni afiliado a la empresa**. La propuesta es preliminar y debe contrastarse con la consigna del curso.

## Por dónde empezar

1. Leé el [alcance y los escenarios del MVP](docs/mvp.md) para distinguir reglas observadas, decisiones académicas y dudas abiertas.
2. Consultá el [backlog](docs/mvp-tasks.json) para estados, responsables, dependencias y criterios de aceptación vigentes. Al redactar este README hay nueve tareas `pending` y una `in_progress` (MVP-010, modelado); el diseño espera revisión cruzada de Enzo. Este resumen no reemplaza el JSON.
3. Para el diseño, mirá el [borrador UML/ER](docs/design/data-model.md) y la [guía de experiencia](DESIGN.md). El [informe académico vivo](docs/project-report.md) reúne decisiones, evidencia y pendientes; su Markdown es la fuente para una eventual versión Word.

Todavía **no hay código rastreado de API, Blazor ni aplicación móvil**. No hay migraciones, integración con proveedor de pagos ni despliegue verificados. MySQL está instalado y se comprobó una conexión administrativa local, pero no está integrado con la API; el modelo de datos sigue siendo una propuesta, no un esquema aprobado.

## Recorrido que queremos demostrar

El caso conductor va de **Ombúes a Colonia**, con transbordo en **Radial de Conchillas**: dos servicios, dos tickets QR diferentes y validación de un solo uso para cada tramo. El primer tramo no asigna asiento; el segundo lo asignaría automáticamente. También se proponen dos abonos relacionados, con igual cupo inicial y saldos independientes: cada validación descuenta una unidad del abono del tramo correspondiente.

Los roles previstos son **pasajero** (web y móvil), **cobrador** (móvil) y **administrador** (web). Un dispositivo o celular embarcado autorizado publicaría posiciones ficticias para consultar última actualización, llegada aproximada y demora básica; esto es geolocalización académica, no seguimiento operativo en tiempo real. La compra propone **Mercado Pago mediante su API en entorno TEST**, decisión revisable y aún no implementada: la API tendría que verificar la aprobación del proveedor antes de emitir tickets. No se prevén cobros reales ni almacenamiento de tarjetas. Los registros operativos de demostración son ficticios; los escenarios de aprobación y rechazo de pago deberán usar el proveedor TEST, no resultados locales inventados.

## Estructura actual (archivos rastreados)

El árbol resume carpetas y documentos rastreados por Git antes de crear este README; `README.md` se agrega en la raíz con este cambio. No incluye índices locales ni carpetas ignoradas.

```text
.
├── .github/
│   └── workflows/docs-validation.yml
├── docs/
│   ├── design/data-model.md
│   ├── mvp.md
│   ├── mvp-tasks.json
│   ├── project-report.md
│   ├── requerimientos.md
│   └── user-stories.md
├── odd/
│   └── tasks/                 # registros de trabajo documental
├── skills/
│   └── ctc-project-conventions/
│       ├── SKILL.md
│       ├── references/
│       ├── assets/
│       └── scripts/
├── .gitignore
├── AGENTS.md
└── DESIGN.md
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

Para seguir el trabajo, revisá los [requisitos](docs/requerimientos.md), las [historias de usuario](docs/user-stories.md) y las decisiones abiertas del [modelado](docs/design/data-model.md) antes de iniciar código o migraciones.
