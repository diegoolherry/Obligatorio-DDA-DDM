# UX móvil y handoff desde Figma

## Secuencia de diseño

Trabajá en este orden: problema, flujo de usuario, wireframe, mockup y prototipo. Una pantalla no está completa porque se vea bien; debe avanzar un objetivo del usuario o apoyar una decisión clara.

- Definí contexto, objetivo, restricciones y una condición de éxito verificable.
- Dibujá el camino feliz y los caminos importantes de error/sin conexión antes de las pantallas.
- Asigná a cada pantalla un propósito claro y una acción dominante.
- Prototipá tareas en lugar de toda la aplicación y luego probalas sin decirle a la persona dónde tocar.
- Organizá Figma con frames, páginas, componentes reutilizables, variantes y Auto Layout. Traducí frames a `View`, texto a `Text`, dirección de Auto Layout a Flexbox y variantes a props/estado.

## Reglas de interfaz móvil

- Respetá las áreas seguras y los controles del sistema.
- Usá una escala de espaciado consistente basada en 4 u 8.
- Hacé que los objetivos interactivos tengan aproximadamente 48 por 48 unidades independientes de densidad cuando sea práctico.
- Mantené feedback visible de presionado, carga, éxito, deshabilitado y error.
- No comuniques estado solamente con color. Buscá al menos contraste 4.5:1 para texto normal y 3:1 para texto grande.
- Pedí solo datos necesarios en formularios; usá el teclado correspondiente; conservá valores tras un error; validá cerca del campo sin interrumpir cada pulsación.
- Hacé explícitos los permisos y diseñá para conectividad demorada, interrumpida o ausente.

## Lista de verificación de handoff

- La tarea principal se entiende sin explicación.
- Existen estados de carga, vacío, error, sin conexión y éxito cuando aplican.
- Los componentes y espaciados son reutilizables y consistentes.
- Las acciones destructivas requieren confirmación o una vía práctica para deshacer.
- Las decisiones de diseño se pueden explicar a partir de evidencia de usuario, tarea y contexto.

## Base de las fuentes

Derivada de los archivos suministrados `Figma_to_React_Native.pdf` y `Mobile_UX_UI_Blueprint.pdf`.
