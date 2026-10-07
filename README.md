# Obligatorio Berruti

MVP académico de pasajes y abonos por tramo.

Estamos definiendo un prototipo académico de transporte: permitir que un pasajero busque y compre un itinerario, presente un ticket QR por tramo y consulte sus abonos; que un cobrador valide esos comprobantes; y que un administrador prepare los datos de demostración. Es un proyecto inspirado en una operativa observada, **no un sistema oficial ni afiliado a la empresa**. La propuesta es preliminar y debe contrastarse con la consigna del curso.

## Por dónde empezar

1. Leé el [alcance y los escenarios del MVP](docs/mvp.md) para distinguir reglas observadas, decisiones académicas y dudas abiertas.
2. Consultá el [backlog del proyecto completo](docs/tasks.json) para estados, responsables, dependencias y criterios de aceptación vigentes. Un único array `tasks` reúne 19 registros: 14 originales (diez MVP y cuatro FIN) y cinco hijos ejecutables de MVP-001 (`MVP-001-01` a `MVP-001-05`, vinculados por `parentId`). En total, 17 están `pending`, MVP-010 está `done` (modelado conceptual) y MVP-001-01 está `in_progress` (fundación parcial Mobile). Los cinco hijos tienen a Diego como responsable confirmado y a Enzo como revisor; todos dependen explícitamente de MVP-010, la estructura precede a los contratos y estos a los tres flujos de sesión independientes. MVP-001 es un hito con criterios originales autoritativos: no se completa automáticamente al completar hijos. Estos conteos no representan funcionalidades independientes ni progreso de implementación. Las áreas `API`, `Web`, `Mobile` y `Docs` permiten varias etiquetas por tarea transversal; los IDs `MVP-*` y `FIN-*` se conservan. Los responsables FIN son propuestos, sin asignación humana confirmada. La aprobación conceptual de Diego y Enzo fue reportada por el usuario; la revisión cruzada de la fundación sigue pendiente. Este resumen no reemplaza el JSON.
3. Para el diseño, mirá el [borrador UML/ER](docs/design/data-model.md) y la [guía de experiencia](DESIGN.md). El [informe académico vivo](docs/project-report.md) reúne decisiones, evidencia y pendientes; su Markdown es la fuente para una eventual versión Word.

Este corte incorpora únicamente la **base Mobile existente** en `src/Mobile`; API y Blazor siguen planificados en esta rama. No hay autenticación, integración cliente/API ni funcionalidades de producto. No hay migraciones, integración con proveedor de pagos ni despliegue verificados. MySQL está instalado y se comprobó una conexión administrativa local, pero no está integrado con la API; el modelo de datos sigue siendo una propuesta, no un esquema aprobado.

El MVP de las primeras semanas de octubre es el primer corte, no toda la entrega. El [alcance final](docs/final-scope.md) agrega mejoras del equipo (no requisitos académicos explícitos): rutas/calendarios y selección web de asientos por intervalo; todavía no están implementadas.

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
│   ├── design/data-model.md
│   ├── final-scope.md
│   ├── mvp.md
│   ├── tasks.json           # backlog del proyecto completo
│   ├── project-report.md
│   ├── requerimientos.md
│   └── user-stories.md
├── src/
│   └── Mobile/                # base Expo/TypeScript existente, sin negocio
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
├── README.md
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
