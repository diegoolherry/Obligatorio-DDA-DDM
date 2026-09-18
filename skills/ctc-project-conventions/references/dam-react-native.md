# DAM: React Native, TypeScript y Expo

## Reglas de plataforma

- Usá la configuración Expo del repositorio y verificá el SDK configurado. La clase usa `create-expo-app`, `npx expo start` y Expo Go en un dispositivo que comparte la red de desarrollo.
- Usá `View`, `Text`, `Image`, `TextInput`, `Pressable`, `ScrollView` y `FlatList`; no uses etiquetas HTML.
- Mantené las cadenas visibles dentro de `Text`. Asigná dimensiones mayores que cero a las imágenes.
- Usá `StyleSheet.create`, propiedades de estilo en camelCase, valores numéricos independientes de densidad y Flexbox. React Native usa dirección de columna por defecto.
- Usá `ScrollView` para contenido corto y estático, y `FlatList` para colecciones que puedan crecer, con una clave estable de dominio.

## React y TypeScript

- Los componentes renderizan UI; las props son entradas de solo lectura; el estado pertenece al componente que lo cambia.
- Definí interfaces/tipos para props, datos de API y valores de dominio. Usá `?` solo cuando la ausencia sea válida. Nunca reemplaces incertidumbre por `any`.
- Actualizá estado mediante su setter y creá un array/objeto nuevo; nunca mutés el estado existente.
- Preferí props para relaciones poco profundas. Usá Context para estado transversal real como autenticación o tema, no como store por defecto.
- Extraé lógica de estado reutilizable a un hook `use...`. Los hooks devuelven datos/acciones y no renderizan UI.
- Usá `useEffect` solo para sincronizar con HTTP, temporizadores, suscripciones o API del dispositivo. Derivá valores calculables durante el renderizado.

## Datos y formularios

- Representá trabajo remoto con estados de carga, vacío, error, éxito y reintento.
- Mantené operaciones HTTP/de negocio en una acción o servicio, no en un componente de pantalla grande.
- Las entradas controladas usan `value` y `onChangeText`. Para un formulario complejo, una librería de formularios tipada es aceptable solo si el proyecto ya la usa o el equipo aprueba la dependencia.
- Conservá los valores ingresados después de errores recuperables de validación o red.

Consultá `../assets/react-native-async-list-pattern.tsx` para la forma del estado y el límite de la lista.

## Verificaciones de revisión

- No debe haber mutación directa de estado, claves de índice inestables, `any` sin explicación ni un `App.tsx` monolítico.
- Un ítem de lista es un componente tipado pequeño cuando tiene presentación o interacción significativa.
- Los efectos tienen dependencias correctas y realizan sincronización externa.
- Investigá el primer error relevante de terminal o dispositivo antes de limpiar caché; usá una caché limpia de Expo solo cuando la caché sea una causa plausible.

## Base de las fuentes

Derivada de los materiales suministrados React Native Essentials, Expo Essentials, Mobile Blueprint, Universal React Blueprint y el manual de construcción React + TypeScript.
