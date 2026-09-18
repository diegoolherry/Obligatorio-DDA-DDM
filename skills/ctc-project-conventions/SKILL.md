---
name: ctc-project-conventions
description: Aplicar las convenciones de clase para Blazor, C#, API, React Native, TypeScript, Expo y UX móvil. Usar al planificar, implementar, depurar o revisar archivos .razor, .cs, .ts o .tsx, formularios, componentes, servicios, hooks, listas, estados HTTP o trabajo de Figma a nativo.
---

# Convenciones del proyecto CTC

## Contrato de activación

1. Leé `references/shared-workflow.md`.
2. Leé `references/dda-blazor.md` para trabajo de Blazor/C# o `references/dam-react-native.md` para trabajo móvil.
3. Leé `references/mobile-ux.md` antes de crear o cambiar un flujo de usuario o una pantalla.
4. Si la consigna o la configuración vigente contradice esta skill, informá el conflicto y seguí la fuente vigente. Actualizá la skill solo con aprobación del equipo.

## Reglas obligatorias

- Mantené límites claros de responsabilidad: la UI renderiza y despacha eventos; los servicios o acciones poseen las operaciones de negocio o datos; los modelos y tipos definen contratos.
- Registrá los servicios de Blazor mediante inyección de dependencias e inyectalos; nunca construyas un servicio dentro de un componente Razor.
- Usá contratos tipados. No introduzcas `any` en TypeScript para ocultar errores.
- No mutés directamente el estado de React ni edites accidentalmente un modelo compartido de Blazor; creá o copiá un valor.
- Modelá carga, vacío, error, éxito y reintento cuando intervengan datos remotos.
- Usá elementos nativos de React Native, no elementos HTML, y mantené el texto visible dentro de `Text`.
- No agregues una dependencia solamente para evitar comprender o implementar un concepto pequeño de clase.
- Mantené cada cambio pequeño, compilable y explicable por ambos estudiantes.

## Puertas de decisión

- **Blazor:** decidí página versus componente reutilizable y luego responsabilidad de UI versus servicio antes de escribir código.
- **React Native:** decidí props versus estado local; usá Context solo para estado realmente global y efectos solo para sistemas externos.
- **Colecciones:** usá LINQ en C#; usá claves estables y `FlatList` para colecciones móviles que puedan crecer.
- **Formularios:** definí el modelo tipado y las reglas de validación antes de los campos de UI.
- **UX:** definí objetivo del usuario, camino feliz, fallos, permisos y conectividad antes del pulido visual.
- **API:** mantené a los clientes dependientes de contratos explícitos de solicitud/respuesta, no de entidades del servidor. Hasta que la consigna o el material de API definan más, no inventes políticas del proyecto.

## Pasos de ejecución

1. Inspeccioná el código cercano e identificá las reglas de referencia aplicables.
2. Escribí el flujo de usuario y los estados de interfaz para comportamientos que cruzan pantallas o la red.
3. Implementá el corte vertical coherente más pequeño, con límites tipados.
4. Validá con la compilación o pruebas del proyecto y `scripts/check_project_conventions.py` cuando esté disponible.
5. Informá supuestos, verificaciones omitidas y cualquier desvío intencional de las convenciones de clase.

## Contrato de salida

En una implementación o revisión, indicá la capa afectada, los archivos modificados, las verificaciones ejecutadas y los supuestos sin resolver. Explicá las decisiones arquitectónicas con conceptos de clase en lugar de nombrar patrones sin justificación.

## Referencias

- `references/shared-workflow.md`
- `references/dda-blazor.md`
- `references/dam-react-native.md`
- `references/mobile-ux.md`
- `assets/blazor-form-pattern.razor`
- `assets/react-native-async-list-pattern.tsx`
