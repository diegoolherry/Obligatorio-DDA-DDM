# Recrear la base local con migraciones

Las bases locales `berrutilocal` y `berrutiauthtests` son descartables y solo tienen datos ficticios. El esquema se define con migraciones de EF Core, y un solo comando lo recrea y siembra las cuentas demo. Reemplaza los pasos manuales de Workbench de `create-auth-*.sql` y `seed-*.sql`, que quedan como referencia histórica.

## Una sola vez: permiso de esquema

Requiere las bases y cuentas de [Bases MySQL locales](local-auth-database.md) y el `.env` privado de la API con `ConnectionStrings__Berruti`.

En Workbench, con la cuenta administradora local, ejecutá [`scripts/database/grant-local-ddl.sql`](../../scripts/database/grant-local-ddl.sql). Esto da a `berruti_app` y `berruti_test` permiso para crear y borrar tablas en sus bases.

## Recrear la base

```powershell
.\scripts\reset-local-db.ps1
```

- Borra **todas** las tablas de `berrutilocal`, aplica las migraciones y crea las cuentas `pasajero.demo@example.test`, `cobrador.demo@example.test` y `admin.demo@example.test`.
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
dotnet ef migrations script --project src/Api/API-Berruti/API-Berruti/API-Berruti.csproj -o scripts/database/migrations.sql
```

3. EF deja una línea en blanco al final del script y el CI (`git diff --check`) la rechaza: borrala. Es el único retoque permitido; no edites el resto de `scripts/database/migrations.sql`.
4. Corré `reset-local-db.ps1`.

## Probar el ingreso de las cuentas demo

Con la API corriendo (`.\scripts\start-api-local.ps1`):

```powershell
.\scripts\check-demo-login.ps1 -Save   # una vez: guarda la contraseña demo cifrada con DPAPI en .local/
.\scripts\check-demo-login.ps1         # pasajero, cobrador y admin: ingreso, rol permitido/denegado, sin sesión, salida y token revocado
```

Solo imprime códigos HTTP; nunca la contraseña ni el token. El archivo cifrado está fuera de git y solo lo descifra tu usuario de Windows.
