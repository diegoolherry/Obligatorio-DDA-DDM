# DDA: Blazor y C#

## Estructura y estado

- Seguí el modo configurado de Blazor Web App. El material de clase usa .NET 10, Interactive Server e interactividad global; verificá el repositorio antes de aplicar exactamente esa configuración.
- Ubicá las vistas con ruta en `Components/Pages`, el layout en `Components/Layout` y la UI reutilizable en `Components` o en una carpeta local de la funcionalidad.
- Una página recibe entrada, renderiza estado, maneja eventos de UI y llama a un servicio. Un servicio posee la lógica CRUD/de datos y su colección.
- Registrá servicios en `Program.cs` e inyectalos con `@inject` o inyección por constructor. Preferí duración scoped para servicios de aplicación estilo clase; justificá cualquier singleton porque comparte estado entre usuarios.
- Dejá que el servicio asigne identificadores. Devolvé un resultado explícito y pequeño, como `bool`, cuando el llamador solo necesite éxito o fallo.

## Formularios y validación

- Colocá `DataAnnotations` en el modelo de entrada.
- Usá `EditForm`, `DataAnnotationsValidator`, componentes de entrada de Blazor, `@bind-Value` y un `ValidationMessage` para cada campo importante.
- Manejá el envío válido con `OnValidSubmit`.
- No combines `@bind` y `@oninput` para el mismo evento.
- Pasá un método de evento sin paréntesis. Usá una lambda solo cuando el evento deba llevar un valor u otro contexto.

Consultá `../assets/blazor-form-pattern.razor` para un patrón mínimo.

## Colecciones y edición

- Preferí LINQ (`Where`, `Select`, `OrderBy`, `FirstOrDefault`, `Any`) y materializá con `ToList()` cuando se requiera una instantánea.
- Manejá el posible `null` de `FirstOrDefault`.
- Copiá un objeto antes de agregarlo a una colección o editarlo en un formulario. No vincules un formulario de edición directamente al objeto almacenado si cancelar debe preservar el original.

## Verificaciones de revisión

- No debe haber `new SomeService()` dentro de un componente Razor.
- El código de UI no posee reglas de almacenamiento CRUD.
- La validación es visible junto al campo relevante.
- La página no genera IDs ni toma decisiones de persistencia.
- Los eventos y el binding usan un mecanismo claro.

## Base de las fuentes

Derivada de las presentaciones suministradas de introducción a Blazor, CRUD local/servicios, binding/eventos/formularios, DataAnnotations, EditForm, lambda y LINQ. Estas fuentes enseñan primero estado local; las reglas de API y persistencia quedan sujetas a la futura consigna.
