# Preparar el pasajero ficticio de demo para revisión manual

## Evidencia posterior de la demo existente

El esquema y bootstrap de esta demo ya fueron ejecutados según las autorizaciones y evidencia [L39–L45](../../odd/tasks/mvp-001-03-passenger-auth.md): cinco tablas, un pasajero ficticio activo, un rol/membresía, contadores US-/RO- en 1 y cero sesiones. **No repetir CREATE ni seed sobre este estado.** Las instrucciones de revisión manual de abajo se conservan como procedimiento original, no como autorización para reejecutar.

En la alternativa delegada autorizada, la contraseña del pasajero se generó aleatoriamente dentro de MySQL y no se imprimió en herramientas, chat ni logs. La persona puede consultarla únicamente en su Workbench local, sin compartir resultado ni capturas:

```sql
SELECT correo, contrasena
FROM berrutilocal.usuario
WHERE usuario_id = 'US-1';
```

Esta consulta muestra una credencial ficticia en texto plano bajo la excepción demo; no usarla en logs ni pruebas automatizadas del agente. Es distinta de la contraseña de la cuenta MySQL. El guard de la API exige una contraseña MySQL no vacía; `berruti_app` actualmente no tiene. Configurar esa cuenta y la conexión privada requiere decisión explícita; no desactivar el guard ni usar `root` para resolverlo. El bootstrap observado no prueba login ni completa MVP-001-03.

## Alcance y puertas humanas

**Propuesta T6, no ejecutada por el agente.** El usuario respondió «Está vacía» sobre `berrutilocal`; es un reporte humano, no inventario SQL observado ni permiso para ejecutar. Esta guía prepara revisión y eventual ejecución manual separada, no acredita ingreso ni cierre de sesión. No ejecutar automáticamente ni seleccionar archivos completos.

Las bases y cuentas existentes **no se reprovisionan**: no repetir [prepare-local-auth.sql](../../scripts/database/prepare-local-auth.sql). La cuenta runtime `berruti_app` conserva solamente DML sobre `berrutilocal`; el DDL corresponde a una persona administradora autorizada, no a la API. No crear otra base/cuenta ni cambiar grants, perfiles, certificados, firewall o settings globales. Nunca correr los runners MySQL de pruebas contra la demo.

| Artefacto | Finalidad | Condición previa |
| --- | --- | --- |
| [create-auth-demo.sql](../../scripts/database/create-auth-demo.sql) | Cinco definiciones InnoDB, solo `berrutilocal.*` | Versión compatible y esquema sin objetos, confirmados por la persona |
| [seed-auth-demo-passenger.sql](../../scripts/database/seed-auth-demo-passenger.sql) | Un pasajero y rol, sin sesiones | Cinco tablas recién creadas, definiciones exactas y completamente vacías, sin uso anterior |

Ambos son propuestas nuevas; los scripts [foundation](../../scripts/database/create-auth-foundation.sql), [sessions](../../scripts/database/create-auth-sessions.sql) y fixtures existentes siguen siendo **solo de pruebas**, no instrucciones para demo. No modificarlos, ejecutarlos en demo ni reemplazar nombres a mano.

## 1. Revisar metadatos, sin escribir

Con API y otros escritores/administradores detenidos, una persona debe revisar una conexión administrativa local autorizada. Confirmar identidad/servidor y que los permisos de `berruti_app` siguen siendo únicamente SELECT/INSERT/UPDATE/DELETE sobre su propia base; `USAGE` no implica privilegios globales. No ampliar permisos si falla una consulta. La inspección de grants no demuestra aislamiento funcional; ver [aprovisionamiento histórico y límites](local-auth-database.md).

Seleccionar **solo `SELECT VERSION();`** de create-auth-demo y ejecutar esa sentencia. Requerir MySQL auténtico **8.0.16 o posterior**, con CHECK efectivos. Otra variante/producto o versión no revisada: detenerse, no inferir compatibilidad. Después seleccionar solo el inventario completo de `information_schema.TABLES` del mismo archivo. Debe devolver **cero filas**, incluidas vistas; un error de permisos no significa vacío. Ante cualquier objeto existente/inesperado, detenerse y pedir revisión: no reutilizar, reparar ni resetear.

## 2. Revisar y crear una sentencia DDL por vez

Solo después de confirmar metadatos, revisar y seleccionar individualmente cada uno de los cinco CREATE, en orden. **Detenerse al primer error.** DDL produce commits implícitos: no es una transacción atómica y rollback no deshace una creación parcial. Nunca ejecutar todo el archivo, reintentar a ciegas, borrar/recrear objetos ni reparar contadores. Cualquier estado parcial requiere una nueva revisión humana del inventario, no continuar automáticamente.

Después del DDL, volver a revisar el inventario: exactamente `contador_entidad`, `usuario`, `rol`, `usuario_rol`, `sesion`, todas InnoDB, sin otros objetos. Seleccionar estas consultas de lectura **una por vez** y comparar todas las columnas, longitudes, collations, nulabilidad, PK/FK, UNIQUE y CHECK con el archivo propuesto (no basta el nombre de las tablas):

```sql
SHOW CREATE TABLE berrutilocal.contador_entidad;
SHOW CREATE TABLE berrutilocal.usuario;
SHOW CREATE TABLE berrutilocal.rol;
SHOW CREATE TABLE berrutilocal.usuario_rol;
SHOW CREATE TABLE berrutilocal.sesion;
```

Solo continuar con definiciones exactas y las cinco tablas enteramente vacías, recién creadas y sin historia previa. El seed vuelve a comprobar su vaciedad antes de DML, pero no sustituye la revisión humana de definiciones ni el acceso exclusivo.

## 3. Preparar bootstrap privado, sin confirmar todavía

Abrir una **conexión dedicada nueva** de Workbench como `berruti_app`, a la base existente `berrutilocal`, sin transacciones pendientes. No reutilizar una conexión de trabajo: START TRANSACTION puede confirmar implícitamente una transacción anterior. Mantener detenidos la API y cualquier otro escritor hasta resolver el bootstrap; no es un algoritmo de bootstrap concurrente.

1. Revisar todo el seed sin ejecutarlo por lotes. Seleccionar únicamente `SET @contrasena_demo = NULL;` y ejecutar.
2. **Pausa obligatoria:** en una sentencia privada de Workbench, asignar a `@contrasena_demo` una contraseña **ficticia**, elegida localmente, distinta de cualquier contraseña real o de MySQL. No hay literal en el repositorio ni debe agregarse. No compartir esa sentencia, guardarla en archivos versionados, capturas o logs ni enviarla al chat.
3. Entre esa asignación privada y cualquier INSERT, ejecutar START TRANSACTION y luego las sentencias de preparación del seed, **una por vez**, en orden. La validación requiere presencia, al menos un carácter no blanco y hasta 255 caracteres; no agrega política de fuerza ni normaliza credenciales. Sin asignación privada, NULL por defecto impide filas en los cuatro INSERT.
4. Ejecutar cada INSERT y sus capturas inmediatas ROW_COUNT/transición, en orden, deteniéndose al primer error o cantidad inesperada. No intercalar otras sentencias entre INSERT y la captura de ROW_COUNT. Se esperan **2, 1, 1, 1** filas afectadas y pasos **1, 2, 3, 4**. Un paso inválido inhabilita los siguientes INSERT, pero no es permiso para continuar después de errores.

El resultado propuesto es `US-1`, activo, correo reservado `pasajero.demo@example.test`, rol `RO-1`/`Pasajero` y su membresía; ambos contadores quedan en **1** porque esos IDs ya se emitieron. No se actualizan contadores históricos ni se introduce administrador. `RANDOM_BYTES(32)` genera el identificador público de 43 caracteres Base64url canónicos: **no es Bearer ni contraseña**. La tabla de sesiones queda vacía; las sesiones y huellas solo las debe crear la API al ingresar.

La contraseña ficticia se guarda temporalmente en texto plano según la excepción ya aprobada: **no es seguro ni apto para producción**, usuarios reales o despliegue. El seed limpia su variable después de usarla; ante error anterior, la persona también debe limpiarla sin imprimirla.

## 4. Postchecks y decisión explícita

En la misma conexión/transacción, ejecutar los postchecks del seed sin intercalar DML. Exigir paso final **4**, cantidades afectadas **2/1/1/1**, `bootstrap_correcto = 1`, exactamente los dos contadores en 1, un usuario activo, identificador público de longitud 43/formato canónico, un rol Pasajero, una membresía exacta y cero sesiones. Las consultas muestran solo cantidades/IDs/rol/estado y forma del identificador; nunca contraseña, hash de contraseña, token ni cadena de conexión.

**El archivo deja la transacción abierta y no contiene COMMIT ejecutable.** Solo si todas las sentencias y postchecks coinciden, la persona puede seleccionar y ejecutar deliberadamente `COMMIT;` como sentencia separada privada. Si no coincide algo, hubo error, se perdió la conexión o existe incertidumbre: no confirmar ni reintentar; si la conexión sigue activa, resolver con `ROLLBACK;`, limpiar `@contrasena_demo` asignándole NULL sin mostrarla y cerrar la conexión. Si se perdió, revisar el estado con la persona administradora antes de cualquier escritura. No suponer rollback observado ni restauración de historia.

Una ejecución accidental del archivo entero **no está aprobada ni se declara segura**: aunque NULL por defecto no inserte filas, START TRANSACTION tiene riesgos y la conexión puede quedar con una transacción abierta. Detenerse, resolverla explícitamente y limpiar la variable. Los clientes por lotes pueden continuar después de errores; los gates no reemplazan seleccionar una sentencia y detenerse.

## 5. Configuración e integración posteriores, aún pendientes

La API consume privadamente `ConnectionStrings:Berruti` / `ConnectionStrings__Berruti`. En Development el par permitido es `berruti_app` / `berrutilocal`, servidor local (`localhost`, `127.0.0.1` o `::1`), puerto 3306; no usar root. Ver [guard/configuración y matriz pendiente](api-session-checks.md). No escribir `.env`, archivos de configuración ni secretos en este procedimiento; la persona conserva los valores fuera del chat/repositorio/logs. La contraseña MySQL es distinta de la contraseña ficticia del pasajero.

Para clientes, distinguir [Web HTTPS localhost:7054](web-passenger-login.md), API HTTPS localhost:7053 y [Mobile/iPhone](mobile-passenger-login.md), según [direcciones y opciones consumidas](client-api-configuration.md). Las URLs no prueban confianza de certificados. No omitir validación TLS ni cambiar confianza/red como parte de esta preparación.

**Pendiente:** DDL y sintaxis MySQL reales, proveedor/mapeos, rollback, API/SQL, permisos públicos, navegador/circuito, TLS y dispositivo, reinicio/durabilidad/logout y revisión de Enzo. La comparación estática de definiciones y gates no ejecuta SQL ni acredita estos resultados. No marca MVP-001-03 ni sus criterios completos.
