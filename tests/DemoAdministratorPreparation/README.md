# Preparación sintética de administrador — T4

**Prototipo aislado, NO apto para producción y SIN binding real.** Cubre S6–S7 de
`odd/tasks/mvp-001-05-admin-auth.md`; no modifica ni revalida S2–S5.
No crea cuentas reales, no carga configuración y no contiene adaptador SQL/EF,
semilla ni modo de ejecución real. API/Web no se inician.

## Ejecutar desde la raíz

Requiere SDK .NET 10 y Node disponibles; no agrega paquetes. La restauración usa
únicamente este directorio local como fuente, sin descargas:

```sh
dotnet build tests/DemoAdministratorPreparation/DemoAdministratorPreparation.csproj --source tests/DemoAdministratorPreparation --nologo && node --test tests/DemoAdministratorPreparation/cli.test.mjs
```

CLI pública (después de compilar):

```sh
dotnet tests/DemoAdministratorPreparation/bin/Debug/net10.0/DemoAdministratorPreparation.dll --help
dotnet tests/DemoAdministratorPreparation/bin/Debug/net10.0/DemoAdministratorPreparation.dll --unit
```

- `--help`: exit 0, stdout exacto `Uso: DemoAdministratorPreparation --unit | --help\n`, stderr vacío.
- `--unit`: exit 0, stdout exacto `Sintetico: 55/55 PASS\n`, stderr vacío.
- Sin argumentos, argumentos desconocidos o combinados: exit 1, stdout vacío,
  stderr igual al texto de uso; rechazo anterior a construir/ejecutar los casos.
- No hay argumentos de contraseña, SQL, `--execute` ni modo seed.

## Contrato y límites

El núcleo usa registros inmutables y un puerto local reducido, tipado, equivalente
a `ICuentasRepository.CrearRolAsync` y `CrearUsuarioAsync`. El string del rol está
limitado a `Administrador`; no es una nueva política de roles ni repositorio genérico.
La implementación del puerto en el runner es exclusivamente sintética. Los snapshots
públicos contienen registros de cuentas, roles, membresías, máximos y contadores;
no contienen contraseñas, tokens ni identificadores públicos de autenticación.
La contraseña ficticia es entrada del llamador, no se persiste ni imprime y solo se
valida como en el contrato existente: no blanca, máximo 255 caracteres.

Los rótulos `berrutilocal`, `berruti_app`, loopback y 3306 **NO prueban la identidad
efectiva ni el destino MySQL**. Solo se comprueban valores del inventario falso.
Se exige inventario completo/coherente para los máximos y orden estable de snapshots;
las comparaciones conservadoras rechazan diferencias incluso de orden.

El núcleo rechaza precondiciones antes de solicitar escrituras, reutiliza solamente
un rol Administrador válido y sin membresías, o solicita un rol nuevo una sola vez.
Comprueba retornos y snapshots completos, incluidos pasajero y membresías anteriores.
US aumenta uno; RO solo aumenta si se crea rol. RO=50000 permite reutilización.
Un segundo intento después del éxito se rechaza por correo duplicado.

Las operaciones reales inspeccionadas de `CuentasRepository` tienen transacciones
separadas: **no hay atomicidad global**. El falso modela rol confirmado seguido de
fallo de usuario, y confirmación seguida de excepción. El resultado `revision` es
igual para fallo conocido, resultado parcial, excepción inesperada o postcondición
incorrecta; no promete ausencia de efectos. No hay compensaciones, borrados, ajuste
de contadores, recuperación ni reintentos automáticos. Los detalles de excepciones
no se muestran. `preparado` significa exclusivamente postcondiciones sintéticas.

## Evidencia observada

Con el comando único anterior (cada ejecución en primer plano, límite 30 s):

1. RED compilable, núcleo que rechazaba todo: build 0 errores/advertencias;
   CLI 1/3 PASS, exit 1. `--unit`: exit 1, stderr vacío, 24 líneas `FAIL: ...`
   y `Sintetico: 31/55 PASS\n`. `--help`: exit 1, stdout vacío, uso en stderr.
2. RED compilable, núcleo que devolvía éxito sin efectos: build limpio;
   CLI 1/3 PASS, exit 1. `--unit`: exit 1, stderr vacío, 55 líneas `FAIL: ...`
   (una por caso público) y `Sintetico: 0/55 PASS\n`. Protege rechazos, contadores,
   rutas válidas, fallos parciales, postcondiciones y redacción de excepciones.
   Los transcripts exactos de ambas salidas quedaron en los resultados de herramientas.
3. Implementación inicial: CLI 2/3 PASS, `--unit` 48/55, exit 1. Se corrigieron
   fixtures de rol ausente con máximo RO inconsistente y la expectativa de llamada
   de usuario para corrupción del rol posterior al alta. No se relajó el núcleo.
4. GREEN: build exit 0, cero errores/advertencias; CLI 3/3 PASS, cero fallos,
   ocho procesos CLI (help, seis rechazos, unit). `--unit` exactamente 55/55,
   sin stderr ni marcadores privados. Se comparan código/mensaje, snapshots públicos,
   contadores y cantidades de llamadas; nunca diccionarios privados ni reflexión.

Esto acredita efectos de interfaz del contexto de prueba, no SQL, atomicidad real,
seguridad concurrente, identidad efectiva ni conservación comprobada de datos reales.
No se accedió a ningún almacén externo para estas pruebas.

## Estado de revisión y requisitos antes de un alta real

El usuario informó: «Reviso enzo. Me dijo que todo perfecto.». Se registra el visto
bueno de Enzo al núcleo aislado según ese reporte; no se recibieron comandos ni
resultados de reejecución independiente. No es una aprobación del nuevo SQL manual ni una prueba de escritura efectiva en MySQL.
La propuesta anterior de consola/adaptador real quedó apartada: la ruta vigente
es el [borrador SQL inerte](../../scripts/database/review-demo-admin-creation.sql),
con operación manual del usuario en Workbench. Aclaración humana sobre ESTE SQL:
«osea era eso, ya hicimos la verificacion tecnica con el profe». Revisión técnica
reportada con el profesor, no atribuida a Enzo ni equivalente a ejecución/alta;
no pedir repetirla o reemplazarla por el verificador no disponible.
Los 55 casos y su visto bueno reportado no se extienden al SQL nuevo: este propone
una transacción manual, distinta de las dos operaciones del modelo del prototipo.

1. Revisión del núcleo aislado: visto bueno reportado por el usuario. Riesgo ítem 2
   de la futura asignación de privilegios; revisión SQL con profesor reportada aparte.
2. T5 completada según reporte del usuario ([L30](../../odd/tasks/mvp-001-05-admin-auth.md)):
   transacción manual única con controles de estado por mutación (ROW_COUNT() no fue
   confiable en Workbench), postcontroles en 1 y COMMIT OK. Lectura desde conexión
   nueva: US-2/RO-3 Administrador y pasajero preservado; hueco RO-2 aceptado.
   Campos privados no validados byte a byte; el agente no se conectó a la base.
   Revisión con profesor aceptada; revisión cruzada académica (Enzo) pendiente en T3.
3. Verificar identidad/destino efectivos, inventario vivo completo y pre/postcontroles
   públicos de cuentas, roles, membresías y contadores, sin divulgar secretos.
4. Resolver inventario obsoleto, concurrencia/duplicados y evidencia de transacciones
   parciales o confirmación incierta; acordar respuesta humana, sin reintentos ciegos.
5. Solo tras esas puertas validar la ejecución real única y el recorrido Web/API de
   ingreso, autorización administrativa y cierre, con revisión cruzada.

Estado posterior: los puntos 3–5 se ejecutaron según [L30](../../odd/tasks/mvp-001-05-admin-auth.md)
(alta reportada por el usuario) y [L31](../../odd/tasks/mvp-001-05-admin-auth.md) (recorrido en navegador
observado por el agente); solo falta la revisión cruzada de Enzo.

**Un PASS sintético NO autoriza el alta real ni sustituye la revisión independiente.**
T5 completada según L30; no declara T3 ni MVP-001 terminados. La preparación y las extensiones de autenticación
son exigidas/autorizadas por S6–S7, no se atribuyen como enseñanza docente de autenticación.
