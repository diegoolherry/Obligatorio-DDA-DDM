using System.Reflection;

try
{
var failures = new List<string>();
var checkCount = 0;
void Check(bool ok, string name) { checkCount++; if (!ok) failures.Add(name); }
var root = Directory.GetCurrentDirectory();
string Read(string relative) => File.ReadAllText(Path.Combine(root, relative));
foreach (var (file, route) in new[] {
    ("src/Api/API-Berruti/API-Berruti/Controllers/Operational/ParadasController.cs", "api/administracion/paradas"),
    ("src/Api/API-Berruti/API-Berruti/Controllers/Operational/TramosController.cs", "api/administracion/tramos") })
{
    var source = Read(file);
    Check(source.Contains("Authorize(Roles = \"Administrador\")"), $"{file} requires administrator role");
    Check(source.Contains($"Route(\"{route}\")") && source.Contains("HttpGet") && source.Contains("HttpPost") && source.Contains("HttpPut") && source.Contains("HttpDelete"), $"{file} exposes CRUD routes");
}
var context = Read("src/Api/API-Berruti/API-Berruti/Data/BerrutiDbContext.cs");
Check(context.Contains("DbSet<Parada> Paradas") && context.Contains("DbSet<Tramo> Tramos") && context.Contains("HasForeignKey(t => t.OrigenId)") && context.Contains("HasForeignKey(t => t.DestinoId)"), "EF maps stop entities and both tramo endpoints");
var page = Read("src/Web/Web-App/Web-App/Components/Pages/Admin/AdministracionOperativa.razor");
Check(page.Contains("@page \"/admin/operativa\"") && page.Contains("GuardarParadaAsync") && page.Contains("ProbarAdministradorAsync"), "Blazor page consumes administrator session and CRUD operations");
var servicioControllerPath = Path.Combine(root, "src/Api/API-Berruti/API-Berruti/Controllers/Operational/ServiciosController.cs");
Check(File.Exists(servicioControllerPath), "Services CRUD controller exists");
if (File.Exists(servicioControllerPath))
{
    var source = File.ReadAllText(servicioControllerPath);
    Check(source.Contains("Authorize(Roles = \"Administrador\")") && source.Contains("Route(\"api/administracion/servicios\")") && source.Contains("HttpGet") && source.Contains("HttpPost") && source.Contains("HttpPut") && source.Contains("HttpDelete"), "Services CRUD requires administrator role");
}
var servicioModelPath = Path.Combine(root, "src/Api/API-Berruti/API-Berruti/Models/Operational/Servicio.cs");
if (!File.Exists(servicioModelPath)) Check(false, "typed Service persistence model contains schedule, capacity, and seat policy");
else
{
    var serviceModel = File.ReadAllText(servicioModelPath);
    Check(serviceModel.Contains("TramoId") && serviceModel.Contains("Fecha") && serviceModel.Contains("HoraSalida") && serviceModel.Contains("HoraLlegada") && serviceModel.Contains("Capacidad") && serviceModel.Contains("PoliticaAsiento"), "typed Service persistence model contains schedule, capacity, and seat policy");
}
var serviceContext = Read("src/Api/API-Berruti/API-Berruti/Data/BerrutiDbContext.cs");
Check(serviceContext.Contains("DbSet<Servicio> Servicios") && serviceContext.Contains("ToTable(\"servicio\")") &&
    serviceContext.Contains("HasColumnName(\"fecha\").HasColumnType(\"date\")") &&
    serviceContext.Contains("HasColumnName(\"hora_salida\").HasColumnType(\"time\")") &&
    serviceContext.Contains("HasColumnName(\"hora_llegada\").HasColumnType(\"time\")") &&
    serviceContext.Contains("HasColumnName(\"capacidad\")") && serviceContext.Contains("HasColumnName(\"politica_asiento\").HasConversion<string>()") &&
    serviceContext.Contains("HasForeignKey(s => s.TramoId)"), "EF maps service columns, string policy, and tramo relationship");
var serviceLogicPath = Path.Combine(root, "src/Api/API-Berruti/API-Berruti/Services/Operational/ServiciosService.cs");
var serviceLogic = File.ReadAllText(serviceLogicPath);
Check(serviceLogic.Contains("ExisteTramoAsync(dto.TramoId)") && serviceLogic.Contains("dto.Capacidad < 1") && serviceLogic.Contains("Enum.IsDefined(dto.PoliticaAsiento)"), "service validates existing tramo, positive capacity, and supported policy");
var operationalPage = Read("src/Web/Web-App/Web-App/Components/Pages/Admin/AdministracionOperativa.razor");
Check(operationalPage.Contains("ServicioForm") && operationalPage.Contains("SinAsiento") && operationalPage.Contains("AsignacionAutomatica") && operationalPage.Contains("Capacidad"), "Blazor page exposes editable service schedule and seat policy");
var tarifaControllerPath = Path.Combine(root, "src/Api/API-Berruti/API-Berruti/Controllers/Operational/TarifasController.cs");
Check(File.Exists(tarifaControllerPath), "Tariff CRUD controller exists");
if (File.Exists(tarifaControllerPath))
{
    var source = File.ReadAllText(tarifaControllerPath);
    Check(source.Contains("Authorize(Roles = \"Administrador\")") && source.Contains("Route(\"api/administracion/tarifas\")") && source.Contains("HttpGet") && source.Contains("HttpPost") && source.Contains("HttpPut") && source.Contains("HttpDelete"), "Tariff CRUD requires administrator role");
}
var tarifaLogicPath = Path.Combine(root, "src/Api/API-Berruti/API-Berruti/Services/Operational/TarifasService.cs");
Check(File.Exists(tarifaLogicPath), "Tariff service exists");
if (File.Exists(tarifaLogicPath))
{
    var source = File.ReadAllText(tarifaLogicPath);
    Check(source.Contains("ExisteTramoAsync(dto.TramoId)") && source.Contains("dto.Importe < 0") && source.Contains("dto.VigenteHasta.Date < dto.VigenteDesde.Date"), "Tariff service validates tramo, non-negative amount, and ordered validity dates");
}
var tarifaContext = Read("src/Api/API-Berruti/API-Berruti/Data/BerrutiDbContext.cs");
Check(tarifaContext.Contains("DbSet<Tarifa> Tarifas") && tarifaContext.Contains("ToTable(\"tarifa\")") &&
    tarifaContext.Contains("HasColumnName(\"importe\").HasPrecision(18, 2)") &&
    tarifaContext.Contains("HasColumnName(\"vigente_desde\").HasColumnType(\"date\")") &&
    tarifaContext.Contains("HasColumnName(\"vigente_hasta\").HasColumnType(\"date\")") &&
    tarifaContext.Contains("HasForeignKey(t => t.TramoId)"), "EF maps tariff amount, validity dates, and tramo relationship");
Check(operationalPage.Contains("TarifaForm") && operationalPage.Contains("GuardarTarifaAsync"), "Blazor page exposes tariff form and save operation");
Check(operationalPage.Contains("SolicitarEliminarTramo(TramoDto t) => SolicitarEliminar(\"tramos\", t.Id, \"tramo\")") &&
    operationalPage.Contains("SolicitarEliminarTarifa(TarifaDto t) => SolicitarEliminar(\"tarifas\", t.Id, \"tarifa\")") &&
    operationalPage.Contains("@onclick=\"ConfirmarEliminarAsync\"") &&
    operationalPage.Contains("Cliente.EliminarAsync(pending.Tipo, pending.Id)"),
    "Blazor delete actions request confirmation before the shared CRUD delete operation");
var demoControllerPath = Path.Combine(root, "src/Api/API-Berruti/API-Berruti/Controllers/Operational/PreparacionDemoController.cs");
Check(File.Exists(demoControllerPath), "demo preparation endpoint exists");
if (File.Exists(demoControllerPath))
{
    var source = File.ReadAllText(demoControllerPath);
    Check(source.Contains("Authorize(Roles = \"Administrador\")") && source.Contains("HttpPost(\"preparar-demo\")") && source.Contains("Conflict("), "demo preparation is administrator-only with conflict response");
}
var demoServicePath = Path.Combine(root, "src/Api/API-Berruti/API-Berruti/Services/Operational/PreparacionDemoService.cs");
Check(File.Exists(demoServicePath), "demo preparation service exists");
if (File.Exists(demoServicePath))
{
    var source = File.ReadAllText(demoServicePath);
    var conflictCheck = source.IndexOf("if (conflictos.Count > 0)", StringComparison.Ordinal);
    var firstInsert = source.IndexOf("AgregarParadaAsync(", StringComparison.Ordinal);
    Check(source.Contains("Ombúes") && source.Contains("Radial de Conchillas") && source.Contains("Colonia") &&
        source.Contains("PoliticaAsiento.SinAsiento") && source.Contains("PoliticaAsiento.AsignacionAutomatica"), "demo builder uses the approved locations and policies");
    Check(conflictCheck >= 0 && firstInsert > conflictCheck && !source.Contains("ActualizarAsync("), "demo builder detects conflicts before insert and never updates existing records");
}
Check(operationalPage.Contains("PreparacionDemoForm") && operationalPage.Contains("PrepararDemoAsync") && operationalPage.Contains("CapacidadOmbuesRadial") && operationalPage.Contains("ImporteOmbuesRadial"), "Blazor demo form accepts administrator-provided values");
var migrationDirectory = Path.Combine(root, "src/Api/API-Berruti/API-Berruti/Data/Migrations");
var operationalMigrations = Directory.Exists(migrationDirectory)
    ? Directory.GetFiles(migrationDirectory, "*_EsquemaOperativa.cs")
    : Array.Empty<string>();
Check(operationalMigrations.Length == 1, "operational EF migration exists");
var operationalScripts = Directory.GetFiles(Path.Combine(root, "scripts/database"), "*_EsquemaOperativa.sql");
Check(operationalScripts.Length == 1, "versioned generated operational migration SQL exists");
Check(!File.Exists(Path.Combine(root, "scripts/database/create-operational-schema.sql")), "hand-authored operational schema SQL was retired");
Check(File.Exists(Path.Combine(root, "scripts/database/migrations.sql")) &&
    !File.Exists(Path.Combine(root, "scripts/database/20261109202055_EsquemaAutenticacion.sql")), "existing auth migration SQL remains at its version-controlled path");
if (operationalMigrations.Length == 1 && operationalScripts.Length == 1)
{
    var migration = File.ReadAllText(operationalMigrations[0]);
    var migrationSql = File.ReadAllText(operationalScripts[0]);
    Check(new[] { "parada", "tramo", "servicio", "tarifa" }.All(name => migration.Contains($"name: \"{name}\"")), "operational migration creates all four mapped tables");
    Check(migration.Contains("table.PrimaryKey") && migration.Contains("IX_tramo_origen_id") && migration.Contains("IX_tramo_destino_id") &&
        migration.Contains("IX_servicio_tramo_id") && migration.Contains("IX_tarifa_tramo_id") && migration.Contains("ReferentialAction.Restrict"),
        "operational migration defines primary keys, indexes, and restrictive foreign keys");
    Check(migrationSql.Contains("CREATE TABLE `servicio`") && migrationSql.Contains("`fecha` date") &&
        migrationSql.Contains("`hora_salida` time") && migrationSql.Contains("`hora_llegada` time") && migrationSql.Contains("`capacidad` int") &&
        migrationSql.Contains("`politica_asiento` varchar(24)"), "generated SQL includes mapped Servicio columns");
    Check(new[] { "CREATE TABLE `parada`", "CREATE TABLE `tramo`", "CREATE TABLE `servicio`", "CREATE TABLE `tarifa`" }
        .All(statement => migrationSql.Contains(statement)), "generated SQL creates all operational tables");
    Check(migrationSql.Contains("PRIMARY KEY (`parada_id`)") && migrationSql.Contains("PRIMARY KEY (`tramo_id`)") &&
        migrationSql.Contains("PRIMARY KEY (`servicio_id`)") && migrationSql.Contains("PRIMARY KEY (`tarifa_id`)"),
        "generated SQL defines each operational primary key");
    Check(migrationSql.Contains("IX_tramo_origen_id") && migrationSql.Contains("IX_tramo_destino_id") &&
        migrationSql.Contains("IX_servicio_tramo_id") && migrationSql.Contains("IX_tarifa_tramo_id"),
        "generated SQL defines expected foreign-key indexes");
    Check(migrationSql.Split("ON DELETE RESTRICT", StringSplitOptions.None).Length - 1 == 4 &&
        migrationSql.Contains("FOREIGN KEY (`origen_id`)") && migrationSql.Contains("FOREIGN KEY (`destino_id`)"),
        "generated SQL includes both stop foreign keys and restrictive deletes");
}
await OperationalExecutableChecks.RunAsync(Check);
Console.WriteLine($"{(failures.Count == 0 ? "PASS" : "FAIL")}: {checkCount} structural checks; {string.Join(", ", failures)}");
return failures.Count == 0 ? 0 : 1;
}
catch (Exception exception) when (exception is not AccessViolationException and not System.Runtime.InteropServices.SEHException)
{
    Console.Error.WriteLine($"FAIL: Managed test harness exception: {exception}");
    if (exception is ReflectionTypeLoadException reflection)
        foreach (var loaderException in reflection.LoaderExceptions.Where(item => item is not null))
            Console.Error.WriteLine($"LOADER: {loaderException}");
    return 1;
}
