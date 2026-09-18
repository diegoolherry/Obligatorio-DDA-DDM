# Flujo compartido de implementación

## Antes de programar

- Partí del objetivo del usuario y del resultado observable, no de componentes del framework.
- Trazá el camino feliz y los fallos relevantes. Para flujos de red incluí latencia, falta de conexión, fallo del servidor, datos vacíos, éxito y reintento.
- Definí primero los contratos: los modelos/DTO de C# y los tipos de TypeScript deben describir el mismo significado de API sin exponer entidades de persistencia.
- Preferí un corte vertical pequeño y demostrable antes que varias capas desconectadas.

## Límites de responsabilidad

| Responsabilidad | Encargado |
| --- | --- |
| Renderizado y eventos de usuario | Componente Razor o React Native |
| Estado/comportamiento de UI reutilizable | Componente hijo o custom hook |
| Operación de negocio/datos | Servicio Blazor o acción/servicio móvil |
| Límite remoto | Cliente API y DTO explícitos |
| Validación | Modelo tipado más feedback de UI |

Usá nombres que expresen intención. Usá PascalCase para tipos, propiedades y métodos públicos de C#; camelCase para valores locales de TypeScript y estado privado de componentes; prefijá campos privados de C# con `_` cuando el proyecto siga los ejemplos de clase.

## Ritmo de trabajo

1. Compilá o ejecutá desde un estado conocido que funciona.
2. Cambiá un concepto por vez.
3. Leé el primer error relevante, no solo la última línea.
4. Verificá la capa responsable: compilación, servicio/acción, inyección de dependencias o imports, evento y luego estado/renderizado.
5. Volvé al último estado funcional en lugar de acumular correcciones especulativas.

No agregues abstracciones, paquetes ni estado global hasta que una repetición o requisito concreto los justifique.
