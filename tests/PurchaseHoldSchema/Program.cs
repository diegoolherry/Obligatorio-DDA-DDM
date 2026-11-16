using System.Data;
using System.Reflection;
using API_Berruti.Data;
using API_Berruti.Models.Operational;
using API_Berruti.Services.Identifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

var failures = 0;
var checks = 0;
void Check(string name, Func<bool> assertion)
{
    checks++;
    try
    {
        if (!assertion()) throw new InvalidOperationException("Assertion returned false.");
        Console.WriteLine($"PASS: {name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.Error.WriteLine($"FAIL: {name} ({exception.GetType().Name}: {exception.Message})");
    }
}

var options = new DbContextOptionsBuilder<BerrutiDbContext>()
    .UseMySQL("Server=localhost;Database=berruti_app;User ID=berruti_test;Password=sin-conexion")
    .Options;
using var context = new BerrutiDbContext(options);
var connection = context.Database.GetDbConnection();
Check("model inspection never opens the configured placeholder connection", () => connection.State == ConnectionState.Closed);
var model = context.GetService<IDesignTimeModel>().Model;
Check("design-time model inspection leaves the connection closed", () => connection.State == ConnectionState.Closed);

var attempt = model.FindEntityType(typeof(IntentoCompra));
var selection = model.FindEntityType(typeof(TramoIntentoCompra));
var hold = model.FindEntityType(typeof(HoldAsiento));
Check("attempt, selected segment and seat hold map to their declared tables and keys", () =>
    attempt?.GetTableName() == "intento_compra" && attempt.FindPrimaryKey()?.Properties.Select(p => p.Name).SequenceEqual(["Id"]) == true &&
    selection?.GetTableName() == "tramo_intento_compra" && selection.FindPrimaryKey()?.Properties.Select(p => p.Name).SequenceEqual(["IntentoId", "Orden"]) == true &&
    hold?.GetTableName() == "hold_asiento" && hold.FindPrimaryKey()?.Properties.Select(p => p.Name).SequenceEqual(["IntentoId", "Orden"]) == true);

bool CheckString(IEntityType entity, string propertyName, string columnName, int length, string charset, string collation)
{
    var property = entity.FindProperty(propertyName);
    return property?.GetColumnName() == columnName && property.GetMaxLength() == length &&
        property.FindAnnotation("MySQL:Charset")?.Value as string == charset &&
        property.FindAnnotation("MySQL:Collation")?.Value as string == collation;
}

Check("string identifiers and selection references retain their column names, lengths and binary collations", () =>
    attempt is not null && selection is not null && hold is not null &&
    CheckString(attempt, nameof(IntentoCompra.Id), "intento_id", 8, "ascii", "ascii_bin") &&
    CheckString(attempt, nameof(IntentoCompra.UsuarioId), "usuario_id", 8, "ascii", "ascii_bin") &&
    CheckString(selection, nameof(TramoIntentoCompra.IntentoId), "intento_id", 8, "ascii", "ascii_bin") &&
    CheckString(hold, nameof(HoldAsiento.SalidaId), "salida_id", 8, "ascii", "ascii_bin"));

Check("attempt UTC properties preserve datetime precision and restore UTC kind", () =>
{
    if (attempt is null) return false;
    var properties = new[] { nameof(IntentoCompra.CreadoUtc), nameof(IntentoCompra.HoldVenceUtc) }
        .Select(name => attempt.FindProperty(name)).ToArray();
    return properties.All(property => property?.GetColumnType() == "datetime(6)" &&
        property.GetValueConverter()?.ConvertFromProvider(new DateTime(2026, 1, 2)) is DateTime restored &&
        restored.Kind == DateTimeKind.Utc);
});

Check("attempt deadline check is declared in EF metadata", () =>
    attempt?.GetCheckConstraints().Any(c => c.Name == "ck_intento_compra_plazo" && c.Sql == "hold_vence_utc > creado_utc") == true);

bool HasRestrictedFk(IEntityType entity, Type principalType, string[] properties, string name) =>
    entity.GetForeignKeys().Any(fk => fk.PrincipalEntityType.ClrType == principalType &&
        fk.Properties.Select(p => p.Name).SequenceEqual(properties) && fk.DeleteBehavior == DeleteBehavior.Restrict &&
        fk.GetConstraintName() == name);

Check("required attempt and selection foreign keys use Restrict", () =>
    attempt is not null && selection is not null &&
    HasRestrictedFk(attempt, typeof(API_Berruti.Models.Authentication.Usuario), [nameof(IntentoCompra.UsuarioId)], "fk_intento_compra_usuario") &&
    HasRestrictedFk(selection, typeof(IntentoCompra), [nameof(TramoIntentoCompra.IntentoId)], "fk_tramo_intento_intento") &&
    HasRestrictedFk(selection, typeof(API_Berruti.Models.Operational.Servicio), [nameof(TramoIntentoCompra.ServicioId)], "fk_tramo_intento_servicio"));

Check("hold composite foreign keys bind selection, inventory and both stop boundaries with Restrict", () =>
    hold is not null &&
    HasRestrictedFk(hold, typeof(TramoIntentoCompra), [nameof(HoldAsiento.IntentoId), nameof(HoldAsiento.Orden)], "fk_hold_asiento_seleccion") &&
    HasRestrictedFk(hold, typeof(AsientoSalida), [nameof(HoldAsiento.SalidaId), nameof(HoldAsiento.NumeroAsiento)], "fk_hold_asiento_inventario") &&
    hold.GetForeignKeys().Count(fk => fk.PrincipalEntityType.ClrType == typeof(ParadaSalida) &&
        fk.DeleteBehavior == DeleteBehavior.Restrict && fk.Properties.Select(p => p.Name).SequenceEqual([nameof(HoldAsiento.SalidaId), nameof(HoldAsiento.OrdenEmbarque)])) == 1 &&
    hold.GetForeignKeys().Count(fk => fk.PrincipalEntityType.ClrType == typeof(ParadaSalida) &&
        fk.DeleteBehavior == DeleteBehavior.Restrict && fk.Properties.Select(p => p.Name).SequenceEqual([nameof(HoldAsiento.SalidaId), nameof(HoldAsiento.OrdenDesembarque)])) == 1);

Check("IC correlativo is unpadded, prefix-limited, and bounded", () =>
{
    var ids = new Correlativo();
    if (ids.Siguiente("IC-", 0) != "IC-1" || ids.Siguiente("IC-", 49999) != "IC-50000") return false;
    try { ids.Siguiente("IC-", 50000); return false; } catch (InvalidOperationException) { }
    try { ids.Siguiente("IC-", -1); return false; } catch (ArgumentOutOfRangeException) { }
    try { ids.Siguiente("IC-", 50001); return false; } catch (ArgumentOutOfRangeException) { }
    try { ids.Siguiente("XX-", 0); return false; } catch (ArgumentException) { }
    return true;
});

var migrationType = typeof(BerrutiDbContext).Assembly.GetType("API_Berruti.Data.Migrations.IntentosCompraHold");
var migration = migrationType is null ? null : Activator.CreateInstance(migrationType) as Migration;
var up = new MigrationBuilder("MySql.EntityFrameworkCore");
var upMethod = migrationType?.GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic);
upMethod?.Invoke(migration, [up]);
Check("migration Up creates exactly the three hold tables and seeds one IC counter only", () =>
{
    var seeds = up.Operations.OfType<InsertDataOperation>().ToArray();
    return migration is not null && upMethod is not null &&
        up.Operations.OfType<CreateTableOperation>().Select(operation => operation.Name).Order().SequenceEqual(
            new[] { "hold_asiento", "intento_compra", "tramo_intento_compra" }.Order()) && seeds.Length == 1 &&
        seeds[0].Table == "contador_entidad" && seeds[0].Values.GetLength(0) == 1 && seeds[0].Values.GetLength(1) == 2 &&
        (string)seeds[0].Values[0, 0]! == "IC-" && (int)seeds[0].Values[0, 1]! == 0 &&
        !up.Operations.Any(operation => operation is DeleteDataOperation or UpdateDataOperation);
});

var down = new MigrationBuilder("MySql.EntityFrameworkCore");
var downMethod = migrationType?.GetMethod("Down", BindingFlags.Instance | BindingFlags.NonPublic);
var downRefused = false;
try { downMethod?.Invoke(migration, [down]); }
catch (TargetInvocationException exception) when (exception.InnerException is NotSupportedException) { downRefused = true; }
Check("migration Down refuses before producing any SQL operations", () => downRefused && down.Operations.Count == 0);

Check("all metadata and migration inspection leaves the connection closed", () => connection.State == ConnectionState.Closed);
Console.WriteLine($"{checks - failures}/{checks} schema checks PASS. These checks inspect metadata and migration operations only; they do not prove MySQL enforcement or runtime behavior.");
return failures == 0 ? 0 : 1;
