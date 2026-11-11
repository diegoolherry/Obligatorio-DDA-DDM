# Migrar o recrear la base local

El esquema se define con migraciones de EF Core. Si necesitás conservar los datos, usa la actualización no destructiva de abajo; el reinicio de esta guía es una operación distinta que borra las tablas y datos de la base elegida.

## Actualizar sin borrar datos

Desde la raíz del repositorio, ejecuta el lanzador local:

```powershell
.\scripts\update-local-db.ps1 -Diagnose
```

Sin argumentos el lanzador también ejecuta solo diagnóstico. Requiere el SDK .NET 10, restaura el manifiesto local `dotnet-tools.json`, compila el proyecto API y luego ejecuta `dotnet ef migrations list --no-build` para `BerrutiDbContext`. Solicita la contraseña solo después de esos pasos previos. Un código de salida cero o una lista mostrada no demuestra que EF haya podido leer el historial de la base ni que haya permisos DDL.

El lanzador usa únicamente `localhost:3306`, `berrutilocal` y `berruti_app`, con `Development`. Pide la contraseña local de MySQL mediante `Read-Host -AsSecureString`; la mantiene solo en memoria y la pasa al proceso `dotnet` mediante `ConnectionStrings__Berruti`. No requiere `.env` ni guarda credenciales. La configuración de conexión para arrancar la API después es separada. Necesitas una base existente y la cuenta local configurada. La salida nativa de EF se captura en memoria y se sanitiza antes de mostrarla; no se escribe un log. El lanzador no puede recuperar el mensaje de un intento anterior que fue descartado por su versión previa.

El lanzador no aplica migraciones en modo diagnóstico. Si decidís aplicar cambios, primero revisá el diagnóstico, el historial y el esquema existente, prepará un respaldo y obtené autorización explícita. Solo entonces ejecutá `.\scripts\update-local-db.ps1 -Apply`; ese modo usa `dotnet ef database update` sin migración objetivo, y EF aplica migraciones pendientes según el historial. Una falla durante la aplicación puede dejar cambios parciales, por lo que no se debe reintentar ni reparar el historial a ciegas. Consulta la [documentación oficial de aplicación de migraciones](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying).

Antes de aplicar, confirma que el historial EF corresponde al esquema de `berrutilocal`. Si hay tablas creadas manualmente, falta historial o EF informa un conflicto, detente y solicita revisión del esquema/historial; no borres tablas, no repares el historial a ciegas y no reintentes hasta entender el estado. El lanzador no crea cuentas demo ni datos operativos. La migración se prepara aquí, pero no se aplicó ni se probó contra MySQL.

La restauración de herramientas y la compilación pueden requerir acceso a los feeds de NuGet o una caché local con las versiones necesarias.

Las referencias Microsoft `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Relational` y `Microsoft.EntityFrameworkCore.Design` de la API deben quedar alineadas en `10.0.11`; el proveedor `MySql.EntityFrameworkCore` `10.0.9` se conserva y el restore/build offline confirmó el grafo compatible disponible. La guía oficial recomienda la misma versión para todos los paquetes EF Core de Microsoft y compatibilidad para el proveedor externo ([versiones de paquetes EF Core](https://learn.microsoft.com/ef/core/what-is-new/nuget-packages)). El manifiesto local conserva `dotnet-ef` `10.0.9`, que comparte el major 10 con el runtime; la generación de SQL observó una advertencia de que esa herramienta es anterior al runtime `10.0.11`. No se ocultó ni se actualizó la herramienta, y no se descargó ninguna versión.

Verificación de esta preparación: restauración solo desde la caché local, API/Web compiladas sin advertencias ni errores y generación efímera de SQL de migraciones inspeccionada, sin conectarse a una base. No es evidencia de que el esquema/historial de `berrutilocal` sea compatible ni de que la actualización se haya aplicado.

**Precaución:** MySQL puede conservar cambios de esquema parciales si una migración falla. No se garantiza que toda la operación sea atómica ni que se revierta automáticamente. Ante un error, conserva el respaldo y revisa el estado antes de volver a ejecutar el comando; el lanzador no intenta limpiar ni reparar la base.

## Una sola vez: permiso de esquema

Requiere las bases y cuentas de [Bases MySQL locales](local-auth-database.md) y el `.env` privado de la API con `ConnectionStrings__Berruti`.

En Workbench, con la cuenta administradora local, ejecutá [`scripts/database/grant-local-ddl.sql`](../../scripts/database/grant-local-ddl.sql). Esto da a `berruti_app` y `berruti_test` permiso para crear y borrar tablas en sus bases.

## Operación destructiva: recrear la base

**Advertencia:** el siguiente procedimiento borra todas las tablas y los datos de la base seleccionada. No lo uses para aplicar una migración conservando datos; el flujo correcto es `scripts/update-local-db.ps1`.

```powershell
.\scripts\reset-local-db.ps1
```

- Borra **todas** las tablas de `berrutilocal`, aplica las migraciones y crea las cuentas `pasajero.demo@example.test`, `cobrador.demo@example.test` y `admin.demo@example.test`.
- En Development también siembra el escenario operativo ficticio (paradas Ombúes, Radial de Conchillas y Colonia; dos tramos encadenados; dos servicios de 08:00–08:30 y 08:45–10:00; tarifas de 100.00 y 250.00). Los servicios quedan en la fecha de **mañana** respecto del día del reinicio (`DateTime.Today.AddDays(1)`, hora local) y las tarifas rigen desde ese día por un año; hay que volver a reiniciar para que la fecha siga siendo futura. `-Environment AuthenticationTests` no lo siembra.
- También siembra el catálogo de horarios de FIN-001 desde `Data/Catalogo/horarios-invierno-2026.json`: 4 bloques, rutas dirigidas con sus paradas ordenadas, salidas, feriados y excepciones. Reutiliza por nombre las paradas del escenario y agrega las que faltan. No genera servicios ni tarifas: para eso está `POST /api/administracion/catalogo/servicios` ([API](../reference/api.md#catálogo-de-horarios-fin-001)).
- Pide una contraseña ficticia para las tres cuentas: no se muestra ni se guarda. No uses una contraseña real.
- `.\scripts\reset-local-db.ps1 -Environment AuthenticationTests` recrea solo el esquema de `berrutiauthtests`, sin cuentas. Necesita un `.env` con la cadena de `berruti_test`.
- El guard de la API rechaza cualquier servidor que no sea `localhost:3306` o una base distinta de esas dos.

Después arrancá la API con `.\scripts\start-api-local.ps1`.

## Cambiar el esquema

1. Modificá las entidades o `Data/BerrutiDbContext.cs`.
2. Creá la migración y regenerá el script SQL versionado para que el equipo lo revise:

```powershell
dotnet tool restore
dotnet ef migrations add <Nombre> --project src/Api/API-Berruti/API-Berruti/API-Berruti.csproj --output-dir Data/Migrations
dotnet ef migrations script <anterior> <nueva> --project src/Api/API-Berruti/API-Berruti/API-Berruti.csproj -o scripts/database/<id>_<Nombre>.sql
```

3. Si la migración cambia columnas con datos, ajustá el `.cs` de la migración para convertirlos (por ejemplo `HorarioServicio` convierte `fecha`/`hora_salida`/`hora_llegada` en `salida`/`llegada`) antes de generar el SQL. Se genera un script por migración; `scripts/database/migrations.sql` es histórico y solo contiene `EsquemaAutenticacion`.
4. Conserva el SQL generado por EF sin retoques manuales y versionado según la convención de scripts por migración del repositorio. Única normalización permitida: quitar la línea en blanco final que agrega EF, porque `git diff --check` del CI la rechaza; el SQL no cambia.
5. Para revisar el estado sin modificar datos, ejecuta `scripts/update-local-db.ps1 -Diagnose` (o sin argumentos). Solo con autorización explícita y después de revisar historial/esquema, usa `scripts/update-local-db.ps1 -Apply`; reserva el procedimiento destructivo de arriba para recrear expresamente una base descartable.

## Probar el ingreso de las cuentas demo

Con la API corriendo (`.\scripts\start-api-local.ps1`):

```powershell
.\scripts\check-demo-login.ps1 -Save   # una vez: guarda la contraseña demo cifrada con DPAPI en .local/
.\scripts\check-demo-login.ps1         # pasajero, cobrador y admin: ingreso, rol permitido/denegado, sin sesión, salida y token revocado
```

Solo imprime códigos HTTP; nunca la contraseña ni el token. El archivo cifrado está fuera de git y solo lo descifra tu usuario de Windows.
