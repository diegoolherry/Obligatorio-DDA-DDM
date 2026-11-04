# Preparar dos bases MySQL locales para revisión

El [script de aprovisionamiento](../../scripts/database/prepare-local-auth.sql) crea bases y cuentas, pero **no crea tablas ni implementa autenticación, EF Core o migraciones**. El usuario ya reportó su ejecución local: las capturas y resultados aportados confirman las dos bases, los permisos previstos y ambas cuentas desbloqueadas; también reportó conexiones exitosas en Workbench. El agente no ejecutó SQL. Para ese entorno, **no vuelvas a ejecutar el aprovisionamiento**. Los pasos siguientes se conservan como procedimiento para una instalación nueva, no como acciones pendientes sobre las cuentas existentes. No completa MVP-001-03 ni cambia el backlog. Solo sirve para una instalación nueva, local y aislada con datos ficticios; no autoriza despliegue público.

## Antes de ejecutar por tu cuenta

- Requisito declarado para este script: **MySQL 8+**; las fuentes de sintaxis son los manuales oficiales enlazados al final. El [proyecto](../explanation/mvp.md) decide el motor MySQL, sin fijar allí una versión. La gramática se revisó contra esa documentación, sin parser ni servidor. La versión y compatibilidad reales siguen sin verificar.
- El [informe](../project-report.md#4-solución-y-decisiones-tecnológicas) conserva la comprobación administrativa local histórica y MySQL instalado. Que MySQL estuviera funcionando entonces **no demuestra que este SQL se haya ejecutado**. Esta preparación no abrió conexiones ni verificó su estado actual.
- Revisá el script en Workbench con una cuenta administradora local autorizada para inventariar, crear bases/cuentas y otorgar estos permisos. La aplicación nunca debe usar `root`.
- No ejecutes el archivo completo ni uses ejecución por lotes: algunos clientes continúan tras errores. Seleccioná únicamente las consultas de la sección indicada; en creación, ejecutá **una sentencia por vez y detenete al primer error**.

| Uso futuro | Base | Cuenta MySQL | Permisos sobre su propia base |
| --- | --- | --- | --- |
| Demo local | `berrutilocal` | `'berruti_app'@'localhost'` | `SELECT`, `INSERT`, `UPDATE`, `DELETE` |
| Pruebas aisladas | `berrutiauthtests` | `'berruti_test'@'localhost'` | `SELECT`, `INSERT`, `UPDATE`, `DELETE` |

Las bases no contienen `_` ni `%`: en permisos de base, esos caracteres pueden interpretarse como comodines según la configuración del servidor. Estos nombres evitan depender de `partial_revokes`; no hace falta consultar ni cambiar variables globales. Las cuentas sí pueden conservar `_` en sus nombres.

## 1. Inventariá antes de crear

Seleccioná **solo la sección 1** del script y ejecutá sus dos consultas de lectura. Ambas deben finalizar correctamente y devolver **cero filas**: ninguna base objetivo ni usuario objetivo, incluso con otro host, debe existir. Un error de permisos no equivale a ausencia.

Confirmá manualmente esos resultados antes de pasar a la creación. Si aparece cualquier nombre, **detenete**: este script no reutiliza ni modifica objetos existentes. No contiene `IF NOT EXISTS` y no verifica automáticamente esta puerta; evitá creación concurrente por otros administradores durante el procedimiento.

## 2. Creá de a una sentencia

Solo tras esa confirmación, seleccioná y ejecutá individualmente cada sentencia de la **sección 2**, en orden. Se crean dos bases vacías y dos cuentas distintas, sin contraseña configurada y con `ACCOUNT LOCK`; **no pueden iniciar sesión mientras estén bloqueadas**. Cada cuenta recibe únicamente DML en su propia base: la de pruebas no recibe acceso a la demo ni viceversa. No se otorgan permisos globales, `GRANT OPTION` ni privilegios para crear o migrar tablas.

**No es una transacción ni es idempotente.** DDL y gestión de cuentas/permisos producen commits implícitos. Si falla una sentencia, puede haber éxito parcial durable: detenete, conservá el resultado sin secretos y pedí revisión administrativa del inventario. No confíes en rollback, no repitas a ciegas y no hagas limpieza automática; este script no elimina ni repara nada.

## 3. Revisá permisos y habilitá localmente

Tras completar todas las sentencias sin error, ejecutá **solo la sección 3**, de lectura. Esperá las dos bases y las dos cuentas `localhost` con `account_locked = 'Y'`. Revisá los dos `SHOW GRANTS`: cada cuenta debe tener solo los cuatro permisos de la tabla en su base y ningún rol o privilegio adicional. MySQL puede mostrar `USAGE ON *.*`: significa ausencia de privilegios globales, no acceso a todas las bases. Ante cualquier diferencia, detenete y revisá con el administrador.

Cuando estés listo, en la administración de cuentas de **MySQL Workbench**, una persona debe fijar una contraseña distinta para cada cuenta y desbloquearla; verificá que la interfaz de tu versión permita ambas acciones. Si no lo permite, dejá las cuentas bloqueadas y solicitá revisión, sin improvisar SQL con secretos. No compartas contraseñas por chat, capturas, logs ni archivos versionados; no hay contraseñas de ejemplo ejecutables en el script. Repetí las consultas de lectura para revisar el desbloqueo y que los permisos no hayan cambiado.

Elegí después la pareja demo/cuenta de aplicación o pruebas/cuenta de pruebas según el proceso; nunca reutilices la cuenta demo para pruebas. El esquema deberá revisarlo y aplicarlo un administrador por separado: los usuarios runtime no pueden migrarlo. Todavía no hay configuración de conexión consumida por la API documentada aquí, proveedor EF instalado por este cambio ni pruebas de acceso o aislamiento ejecutadas. `SHOW GRANTS` es inspección, no una prueba funcional de aislamiento.

`'localhost'` es parte de la identidad MySQL: verificá posteriormente qué cuenta coincide con el transporte y host reales de la conexión local (por ejemplo, mediante `CURRENT_USER()` en una comprobación humana). No amplíes hosts ni permisos para sortear un fallo. El iPhone llama a la **API**, no a MySQL: no publiques el puerto 3306 ni cambies firewall, certificados o exposición de red para este procedimiento. Para las direcciones de clientes, consultá [cliente/API](client-api-configuration.md); los [scaffolds](../tutorials/first-local-run.md) todavía no requieren esta base para arrancar.

## Referencias de sintaxis y límites

Documentación oficial MySQL 8.4, como referencia dentro del requisito MySQL 8+ (no prueba de la instalación local):

- [CREATE USER](https://dev.mysql.com/doc/refman/8.4/en/create-user.html): creación y bloqueo inicial de cuentas.
- [GRANT](https://dev.mysql.com/doc/refman/8.4/en/grant.html): alcance de permisos y comodines en nombres de bases.
- [Sentencias con commit implícito](https://dev.mysql.com/doc/refman/8.4/en/implicit-commit.html): límites transaccionales del aprovisionamiento.
