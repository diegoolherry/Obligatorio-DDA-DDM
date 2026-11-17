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
var payment = model.FindEntityType(typeof(Pago));
var paymentSegment = model.FindEntityType(typeof(TramoPago));
var purchase = model.FindEntityType(typeof(Compra));
var purchaseSegment = model.FindEntityType(typeof(CompraServicio));
var ticket = model.FindEntityType(typeof(Ticket));
Check("attempt, selected segment and seat hold map to their declared tables and keys", () =>
    attempt?.GetTableName() == "intento_compra" && attempt.FindPrimaryKey()?.Properties.Select(p => p.Name).SequenceEqual(["Id"]) == true &&
    selection?.GetTableName() == "tramo_intento_compra" && selection.FindPrimaryKey()?.Properties.Select(p => p.Name).SequenceEqual(["IntentoId", "Orden"]) == true &&
    hold?.GetTableName() == "hold_asiento" && hold.FindPrimaryKey()?.Properties.Select(p => p.Name).SequenceEqual(["IntentoId", "Orden"]) == true);

Check("payment, accepted-price, purchase, purchased-segment and ticket tables have durable keys", () =>
    payment?.GetTableName() == "pago" && payment.FindPrimaryKey()?.Properties.Select(p => p.Name).SequenceEqual([nameof(Pago.Id)]) == true &&
    paymentSegment?.GetTableName() == "tramo_pago" && paymentSegment.FindPrimaryKey()?.Properties.Select(p => p.Name).SequenceEqual([nameof(TramoPago.PagoId), nameof(TramoPago.Orden)]) == true &&
    purchase?.GetTableName() == "compra" && purchase.FindPrimaryKey()?.Properties.Select(p => p.Name).SequenceEqual([nameof(Compra.Id)]) == true &&
    purchaseSegment?.GetTableName() == "compra_servicio" && purchaseSegment.FindPrimaryKey()?.Properties.Select(p => p.Name).SequenceEqual([nameof(CompraServicio.Id)]) == true &&
    ticket?.GetTableName() == "ticket" && ticket.FindPrimaryKey()?.Properties.Select(p => p.Name).SequenceEqual([nameof(Ticket.Id)]) == true);

Check("accepted re-quote rows and purchases cannot cross payment or attempt boundaries", () =>
    payment is not null && paymentSegment is not null && purchase is not null && purchaseSegment is not null &&
    payment.GetKeys().Any(key => key.Properties.Select(p => p.Name).SequenceEqual([nameof(Pago.Id), nameof(Pago.IntentoId)])) &&
    paymentSegment.GetForeignKeys().Any(fk => fk.PrincipalEntityType.ClrType == typeof(Pago) &&
        fk.Properties.Select(p => p.Name).SequenceEqual([nameof(TramoPago.PagoId), nameof(TramoPago.IntentoId)]) &&
        fk.PrincipalKey.Properties.Select(p => p.Name).SequenceEqual([nameof(Pago.Id), nameof(Pago.IntentoId)]) && fk.DeleteBehavior == DeleteBehavior.Restrict) &&
    paymentSegment.GetForeignKeys().Any(fk => fk.PrincipalEntityType.ClrType == typeof(TramoIntentoCompra) &&
        fk.Properties.Select(p => p.Name).SequenceEqual([nameof(TramoPago.IntentoId), nameof(TramoPago.Orden)]) &&
        fk.PrincipalKey.Properties.Select(p => p.Name).SequenceEqual([nameof(TramoIntentoCompra.IntentoId), nameof(TramoIntentoCompra.Orden)]) && fk.DeleteBehavior == DeleteBehavior.Restrict) &&
    purchase.GetIndexes().Any(index => index.IsUnique && index.Properties.Select(p => p.Name).SequenceEqual([nameof(Compra.IntentoId)])) &&
    purchase.GetForeignKeys().Any(fk => fk.PrincipalEntityType.ClrType == typeof(Pago) && fk.Properties.Select(p => p.Name).SequenceEqual([nameof(Compra.PagoId), nameof(Compra.IntentoId)]) &&
        fk.PrincipalKey.Properties.Select(p => p.Name).SequenceEqual([nameof(Pago.Id), nameof(Pago.IntentoId)])) &&
    purchaseSegment.GetForeignKeys().Any(fk => fk.PrincipalEntityType.ClrType == typeof(Compra) && fk.Properties.Select(p => p.Name).SequenceEqual([nameof(CompraServicio.CompraId), nameof(CompraServicio.IntentoId), nameof(CompraServicio.PagoId)]) &&
        fk.PrincipalKey.Properties.Select(p => p.Name).SequenceEqual([nameof(Compra.Id), nameof(Compra.IntentoId), nameof(Compra.PagoId)])) &&
    purchaseSegment.GetForeignKeys().Any(fk => fk.PrincipalEntityType.ClrType == typeof(TramoPago) && fk.Properties.Select(p => p.Name).SequenceEqual([nameof(CompraServicio.PagoId), nameof(CompraServicio.Orden)]) &&
        fk.PrincipalKey.Properties.Select(p => p.Name).SequenceEqual([nameof(TramoPago.PagoId), nameof(TramoPago.Orden)])));

Check("one ticket is linked to each purchase segment and QR values are unique", () =>
    ticket is not null && ticket.GetIndexes().Any(index => index.IsUnique && index.Properties.Select(p => p.Name).SequenceEqual([nameof(Ticket.CompraServicioId)])) &&
    ticket.GetIndexes().Any(index => index.IsUnique && index.Properties.Select(p => p.Name).SequenceEqual([nameof(Ticket.QrOpaco)])) &&
    CheckString(ticket, nameof(Ticket.QrOpaco), "qr_opaco", 128, "ascii", "ascii_bin") &&
    HasRestrictedFk(ticket, typeof(CompraServicio), [nameof(Ticket.CompraServicioId)], "fk_ticket_compra_servicio"));

Check("payment and purchase amounts use bounded decimal(10,2) with nonnegative checks", () =>
    payment?.FindProperty(nameof(Pago.ImporteEsperado))?.GetPrecision() == 10 && payment.FindProperty(nameof(Pago.ImporteEsperado))?.GetScale() == 2 &&
    payment.FindProperty(nameof(Pago.ImportePagado))?.GetPrecision() == 10 && payment.FindProperty(nameof(Pago.ImportePagado))?.GetScale() == 2 &&
    payment.GetCheckConstraints().Any(check => check.Name == "ck_pago_importes") &&
    purchase?.FindProperty(nameof(Compra.ImporteTotal))?.GetPrecision() == 10 && purchase.FindProperty(nameof(Compra.ImporteTotal))?.GetScale() == 2);

Check("payment state, idempotency keys, provider fields and status timestamps are constrained", () =>
    payment is not null && payment.FindProperty(nameof(Pago.Estado))?.GetMaxLength() == 24 &&
    payment.GetIndexes().Any(index => index.IsUnique && index.Properties.Select(p => p.Name).SequenceEqual([nameof(Pago.ClaveCreacion)])) &&
    payment.GetIndexes().Any(index => index.IsUnique && index.Properties.Select(p => p.Name).SequenceEqual([nameof(Pago.ClaveDevolucion)])) &&
    payment.FindProperty(nameof(Pago.OrdenProveedorId))?.GetMaxLength() == 256 && payment.FindProperty(nameof(Pago.CheckoutUrl))?.GetMaxLength() == 2048 &&
    new[] { nameof(Pago.CreadoUtc), nameof(Pago.ActualizadoUtc), nameof(Pago.UltimaVerificacionUtc), nameof(Pago.DevolucionSolicitadaUtc), nameof(Pago.DevolucionConfirmadaUtc) }
        .All(name => payment.FindProperty(name)?.GetColumnType() == "datetime(6)"));
Check("a new payment defaults to unknown until the API obtains an authoritative provider result", () => new Pago().Estado == EstadoPago.Desconocido);

Check("new payment, purchase and ticket timestamps restore UTC kind, including nullable payment status times", () =>
{
    if (payment is null || purchase is null || ticket is null) return false;
    var required = new[] { (payment, nameof(Pago.CreadoUtc)), (payment, nameof(Pago.ActualizadoUtc)),
        (purchase, nameof(Compra.ConfirmadoUtc)), (ticket, nameof(Ticket.EmitidoUtc)) };
    if (required.Any(item => item.Item1.FindProperty(item.Item2)?.GetValueConverter()?.ConvertFromProvider(new DateTime(2026, 1, 2)) is not DateTime restored || restored.Kind != DateTimeKind.Utc)) return false;
    var nullable = new[] { nameof(Pago.UltimaVerificacionUtc), nameof(Pago.DevolucionSolicitadaUtc), nameof(Pago.DevolucionConfirmadaUtc) };
    return nullable.All(name => payment.FindProperty(name)?.GetValueConverter()?.ConvertFromProvider(new DateTime(2026, 1, 2)) is DateTime restored && restored.Kind == DateTimeKind.Utc) &&
        nullable.All(name => payment.FindProperty(name)?.GetValueConverter()?.ConvertFromProvider(null) is null);
});

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
    if (new[] { "IC-", "PG-", "CO-", "CS-", "TI-" }.Any(prefix => ids.Siguiente(prefix, 0) != prefix + "1" || ids.Siguiente(prefix, 49999) != prefix + "50000")) return false;
    try { ids.Siguiente("IC-", 50000); return false; } catch (InvalidOperationException) { }
    try { ids.Siguiente("IC-", -1); return false; } catch (ArgumentOutOfRangeException) { }
    try { ids.Siguiente("IC-", 50001); return false; } catch (ArgumentOutOfRangeException) { }
    try { ids.Siguiente("XX-", 0); return false; } catch (ArgumentException) { }
    return true;
});

var migrationType = typeof(BerrutiDbContext).Assembly.GetType("API_Berruti.Data.Migrations.PagosComprasTickets");
var migration = migrationType is null ? null : Activator.CreateInstance(migrationType) as Migration;
var up = new MigrationBuilder("MySql.EntityFrameworkCore");
var upMethod = migrationType?.GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic);
upMethod?.Invoke(migration, [up]);
var attemptMigrationType = typeof(BerrutiDbContext).Assembly.GetType("API_Berruti.Data.Migrations.IntentosCompraHold");
var attemptMigration = attemptMigrationType is null ? null : Activator.CreateInstance(attemptMigrationType) as Migration;
var attemptUp = new MigrationBuilder("MySql.EntityFrameworkCore");
var attemptUpMethod = attemptMigrationType?.GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic);
attemptUpMethod?.Invoke(attemptMigration, [attemptUp]);
Check("original attempt migration still creates exactly its three tables and seeds only IC at zero", () =>
{
    var seeds = attemptUp.Operations.OfType<InsertDataOperation>().ToArray();
    return attemptMigration is not null && attemptUpMethod is not null &&
        attemptUp.Operations.OfType<CreateTableOperation>().Select(operation => operation.Name).Order().SequenceEqual(
            new[] { "hold_asiento", "intento_compra", "tramo_intento_compra" }.Order()) && seeds.Length == 1 &&
        seeds[0].Table == "contador_entidad" && seeds[0].Values.GetLength(0) == 1 && seeds[0].Values.GetLength(1) == 2 &&
        (string)seeds[0].Values[0, 0]! == "IC-" && (int)seeds[0].Values[0, 1]! == 0 &&
        !attemptUp.Operations.Any(operation => operation is DeleteDataOperation or UpdateDataOperation);
});
var attemptDown = new MigrationBuilder("MySql.EntityFrameworkCore");
var attemptDownMethod = attemptMigrationType?.GetMethod("Down", BindingFlags.Instance | BindingFlags.NonPublic);
var attemptDownRefused = false;
try { attemptDownMethod?.Invoke(attemptMigration, [attemptDown]); }
catch (TargetInvocationException exception) when (exception.InnerException is NotSupportedException) { attemptDownRefused = true; }
Check("original attempt migration Down still refuses before producing any SQL operations", () => attemptDownRefused && attemptDown.Operations.Count == 0);

Check("payment migration Up creates its five tables and seeds only its four counters", () =>
{
    var seeds = up.Operations.OfType<InsertDataOperation>().ToArray();
    return migration is not null && upMethod is not null &&
        up.Operations.OfType<CreateTableOperation>().Select(operation => operation.Name).Order().SequenceEqual(new[] { "compra", "compra_servicio", "pago", "ticket", "tramo_pago" }.Order()) && seeds.Length == 1 &&
        seeds[0].Table == "contador_entidad" && seeds[0].Values.GetLength(0) == 4 && seeds[0].Values.GetLength(1) == 2 &&
        new[] { "CO-", "PG-", "CS-", "TI-" }.All(prefix => Enumerable.Range(0, seeds[0].Values.GetLength(0)).Any(index => (string)seeds[0].Values[index, 0]! == prefix && (int)seeds[0].Values[index, 1]! == 0)) &&
        up.Operations.OfType<DropCheckConstraintOperation>().Any(operation => operation.Name == "ck_contador_prefijo") &&
        up.Operations.OfType<AddCheckConstraintOperation>().Any(operation => operation.Name == "ck_contador_prefijo" && operation.Sql.Contains("'PG-'", StringComparison.Ordinal) && operation.Sql.Contains("'TI-'", StringComparison.Ordinal)) &&
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
