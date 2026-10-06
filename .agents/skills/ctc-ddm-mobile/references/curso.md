# DDM: React Native, Expo y NativeWind con evidencia limitada

## Cómo interpretar el material

Las fuentes y localizaciones son extractos NotebookLM suministrados por el usuario; los PDF originales no fueron inspeccionados independientemente. «Enseñado/aplicado» describe lo reportado en esos extractos. Separa **enseñado/aplicado**, **solo mencionado**, **exigido por proyecto sin evidencia docente** y **no verificado**. Una advertencia o plantilla sugerida no es una prohibición universal.

Si una implementación depende de conceptos no demostrados, incluso solo mencionados, presenta la solución más simple, su límite, riesgos y alternativa y pide aprobación del equipo antes de avanzar. No suprimas DTO, auth, QR o pagos exigidos. El flujo y la UX permanecen en la skill compartida; no se duplican aquí.

## Mapa de fuentes y decisiones

| Fuente aportada / localización | Evidencia reportada y límite operativo |
| --- | --- |
| `03-NativeWind_Expo_57_Blueprint.pdf`, diap. 1/4/5 | Menciona Expo 57, RN 0.86.3, `nativewind` 5.0.0-rc.0 y `react-native-css` 3.1.0-rc.0 con instalación exacta. Son **versiones del curso**, no compatibilidad actual independientemente verificada ni mandato de instalar/actualizar. |
| Mismo archivo, diap. 1/4/5 | NativeWind aplica utilidades estilo Tailwind mediante `className` en React Native, no CSS DOM del navegador. Configuración `global.css`, wrapper Metro de NativeWind y tipos depende de la versión; verifica paquetes reales antes de aplicarla. Los paquetes no tienen por qué compartir número de versión. |
| `Guia_Instalacion_React_Native_Expo_Windows_macOS_Linux 3.pdf`, secc. 1/2, pág. 1 | `npx` y Node LTS compatible; no se necesita CLI global. No conviertas esa recomendación en permiso de cambiar el entorno. |
| Mismo archivo, secc. 7, pág. 4 | `blank-typescript` es starter sugerido, no la única plantilla posible. |
| `Manual_de_Construcción_React_y_TypeScript.pdf`, diap. 8 | Estado inmutable y setters; no `any` para esconder errores; evita App monolítico y abuso de Context. Claves estables, no índice si cambia orden o pertenencia de lista. |
| Mismo archivo, diap. 5 | Deriva datos calculables durante render, no con un efecto que duplique estado. |
| `React_Native_Expo_Essentials.pdf`, diap. 5/2 | Texto visible dentro de `Text`; evita `npm` con `sudo`. |
| `React_Native_Mobile_Blueprint.pdf`, diap. 6 | Ejemplo de dimensiones positivas explícitas para imágenes mediante clases. Asegura tamaño visible/resuelto; no concluyas que ningún otro mecanismo de dimensionado funciona. |
| `React_Native_Essentials.pdf`, diap. 3/5 | Evita carpetas de proyecto sincronizadas con nube; `Pressable` como base habitual. |
| `01-Navegación_y_Formularios_SIGMA.pdf`, diap. 2/1 | Mantén un enfoque de router coherente. El diagrama `Stack.Navigator` difiere del código Expo Router; Expo Router usa React Navigation internamente, no prohíbas esa dependencia transitiva. |
| `02-SIGMA_Navigation_Roadmap.pdf`, diap. 3/5, frente a `React_Native_Essentials.pdf`, diap. 4 | Ejemplos `app/` con Stack Expo Router y alternativa `src/app/`; inspecciona el árbol real y conserva una ubicación, sin duplicar ambas. |
| `Manual Completo de Figma y React Native.pdf`, secc. 10, pág. 10 | Ejemplo aislado de `TouchableOpacity`; no está prohibido por preferir `Pressable`. |
| `Construcción_de_Interfaces_React_Native.pdf`, diap. 2 | APIs/DB fuera de la fase **inicial**, no prohibidas para siempre. HTTP en `useEffect` aparece como teoría en los extractos, no implementación práctica demostrada. |

## Elección de estilos y ejemplo mínimo adaptado

StyleSheet también está reportado como enseñado, sin ubicación precisa suministrada para ese punto. Conserva StyleSheet si es el sistema elegido; si NativeWind ya está configurado y es compatible, usa utilidades literales. No instales ni migres por el solo hecho de cargar esta skill. Flexbox nativo y estilos camelCase de StyleSheet no son CSS de navegador.

Ejemplo adaptado de la regla, **no transcripción del curso ni código probado en un host**; presupone NativeWind ya configurado:

```tsx
import { Text } from 'react-native';

export function Estado({ activo }: { activo: boolean }) {
  return (
    <Text className={activo ? 'text-green-700 font-bold' : 'text-gray-600 font-normal'}>
      {activo ? 'Activo' : 'Inactivo'}
    </Text>
  );
}
```

Usa conjuntos completos como arriba; no construyas `text-${color}-700` suponiendo que el extractor generará esa clase. Comprueba utilidades soportadas por la versión instalada. Para una imagen, `w-24 h-24` ilustra dimensiones explícitas positivas en una configuración compatible, no una obligación universal de usar esas clases.

## Límites y consulta previa

- Prefiere props y estado local; Context no es store por defecto. Actualiza objetos/arrays con nuevos valores y setters; usa claves de dominio estables.
- Mantén componentes tipados pequeños. `useEffect` sincroniza sistemas externos; no convierte teoría HTTP en evidencia de una integración enseñada.
- HTTP práctico, autenticación, almacenamiento persistente, librerías de pruebas y código Reanimated personalizado requieren explicación y aprobación cuando no exista evidencia adicional. Un paquete instalado transitivamente no acredita cobertura de clase ni autoriza usarlo por defecto.
- Las versiones/configuraciones reales y requisitos prevalecen. Si difieren del material, informa el conflicto antes de cambiar nada; no impongas dependencias ni retires requisitos para ajustarlos al ejemplo.
