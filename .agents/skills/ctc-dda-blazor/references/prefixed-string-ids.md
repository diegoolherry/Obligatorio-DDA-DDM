# IDs string prefijados: ejemplo docente y límites

## Fuente y alcance

El usuario aportó `GuyaSuna/Maquinaria-Mobile-API` como ejemplo construido a mano por el profesor en clase. Esa atribución es un reporte del usuario; el código siguiente sí fue inspeccionado directamente, no mediante NotebookLM. No se ejecutó el proyecto ni se probaron sus garantías de persistencia o concurrencia.

Versión consultada: commit `823b6b568fdf1ced79ffc3e8585a32509e24372a`.

- [EquipmentController.cs, método Post](https://github.com/GuyaSuna/Maquinaria-Mobile-API/blob/823b6b568fdf1ced79ffc3e8585a32509e24372a/Api-Maquinaria/Api-Maquinaria/Controllers/EquipmentController.cs).
- [Equipment.cs, propiedad Id](https://github.com/GuyaSuna/Maquinaria-Mobile-API/blob/823b6b568fdf1ced79ffc3e8585a32509e24372a/Api-Maquinaria/Api-Maquinaria/Models/Equipment.cs).
- [EquipmentInput.cs, DTO de entrada](https://github.com/GuyaSuna/Maquinaria-Mobile-API/blob/823b6b568fdf1ced79ffc3e8585a32509e24372a/Api-Maquinaria/Api-Maquinaria/DTOs/EquipmentInput.cs).

## Fragmento relevante

Fragmento literal de `Post`, previo a construir y guardar `Equipment`:

```csharp
var ids = await _context.Equipments
    .Select(e => e.Id)
    .ToListAsync();

var numeros = ids.Select(id => int.Parse(id.Substring(3)));

var siguiente = numeros.DefaultIfEmpty(0).Max() + 1;
```

En el inicializador de la entidad, el mismo método asigna:

```csharp
Id = "eq-" + siguiente,
```

Esta última línea pertenece a un inicializador, no es una sentencia independiente. El modelo declara `public string Id { get; set; } = string.Empty;`. El DTO `EquipmentInput` no recibe `Id`: la API lo genera y luego llama a `SaveChangesAsync`. Los métodos de consulta, actualización y eliminación reciben `string id`.

## Cómo funciona

1. Consulta y carga los IDs existentes de equipos.
2. `Substring(3)` salta los tres caracteres de `eq-`: de `eq-12` obtiene `12`. No genera el ID ni valida el prefijo.
3. `int.Parse` convierte ese sufijo a entero.
4. `DefaultIfEmpty(0).Max() + 1` obtiene el siguiente número; con una colección vacía calcula 1.
5. Concatena el prefijo: `eq-1`, `eq-2`, etc. Es el ID real del modelo, no el campo comercial `Code`, que es independiente.

Se calcula el máximo numérico, no el máximo alfabético de strings: entre `eq-9` y `eq-10`, el siguiente es `eq-11`. Estas salidas se deducen del código; no son resultados de pruebas ejecutadas.

## Límites que deben conservarse al adaptar

- **Formato asumido:** el desplazamiento 3 presupone un prefijo de tres caracteres. No comprueba que sea `eq-`; IDs cortos, sufijos no numéricos o valores fuera de rango pueden provocar excepciones. No trasladar el número 3 a prefijos de otra longitud sin revisarlo.
- **Concurrencia:** dos solicitudes pueden leer el mismo máximo y generar el mismo ID. El fragmento no demuestra un mecanismo de asignación concurrente segura; una restricción de unicidad por sí sola no hace que ambas altas terminen correctamente. Acordar y probar el mecanismo antes de implementar un flujo concurrente.
- **Borrados:** al eliminar el registro con el mayor número, ese número puede reutilizarse; si la tabla queda vacía, el cálculo vuelve a 1. No acredita numeración histórica irreutilizable ni sin huecos.
- **Costo y rango:** carga todos los IDs en memoria; usa `int`, con rango finito. No asumir escalabilidad ni crecimiento ilimitado.
- **Responsabilidades:** el ejemplo accede a DbContext desde el controller y devuelve entidades. Es evidencia del ejemplo, no autorización para abandonar la separación Controller → Service → Repository → DbContext ni los contratos DTO exigidos por este proyecto.
- **Seguridad:** un ID correlativo es predecible, no una credencial. No reemplaza autorización, QR opaco, tokens de sesión ni referencias externas de pagos.

## Uso en este proyecto

El usuario confirmó el requisito de IDs reales string con letras de referencia a la entidad, por ejemplo `EV-1`. El prefijo `eq-` pertenece al ejemplo; no define los prefijos, casing, correlativos o tratamiento de Identity de Berruti. Esas reglas siguen pendientes de acuerdo.

Clasificar la generación prefijada con `Substring`, LINQ y `Max + 1`, y el DTO de entrada con DataAnnotations, como **aplicados en el código del ejemplo aportado como docente**. No extender esa evidencia a DTO de respuesta, autenticación, relaciones complejas o control concurrente. Esta referencia documenta el patrón y sus límites; no implementa IDs, no cambia contratos existentes ni acredita migraciones o pruebas del proyecto.
