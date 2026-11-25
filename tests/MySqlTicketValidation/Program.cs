using API_Berruti.Data;
using API_Berruti.DTOs.Authentication;
using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Authentication;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Authentication;
using API_Berruti.Repositories.Operational;
using API_Berruti.Security.Authentication;
using API_Berruti.Services.Identifiers;
using API_Berruti.Services.Location;
using API_Berruti.Services.Operational;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// MVP-005 T2: comprobaciones contra MySQL real de la consumición atómica y de la consulta del pasajero.
// Usa SOLO la base de prueba dedicada de compras (berruticomprastests / berruti_purchase_test, loopback), ya migrada
// con ConsumoTicket y preparada con `tests/MySqlPurchase --prepare` (contadores, pasajero, servicios). Escribe filas
// de prueba que se conservan: no hay limpieza. Sin esas variables, no se ejecuta nada.
const string uso = "Sin argumentos o --plan: solo muestra el plan. Con --run --allow-test-db-writes ejecuta contra la base de prueba dedicada. Variables: BERUTI_MYSQL_PURCHASE_TEST_CONNECTION, BERUTI_MYSQL_PURCHASE_TEST_PASSENGER_SUBJECT, BERUTI_MYSQL_PURCHASE_TEST_SERVICE_NO_SEAT, BERUTI_MYSQL_PURCHASE_TEST_SERVICE_ASSIGNED, DemoSeed__Contrasena.";

if (args.Length == 0 || args.SequenceEqual(["--plan"]))
{
    Console.WriteLine("PLANNED ONLY: no environment secrets read, DbContext created, SQL executed, or tests run.");
    Console.WriteLine(uso);
    return 0;
}
if (!args.SequenceEqual(["--run", "--allow-test-db-writes"]))
{
    Console.Error.WriteLine("REFUSED: use --run together with --allow-test-db-writes.");
    Console.Error.WriteLine(uso);
    return 2;
}

try
{
    await RunAsync();
    Console.WriteLine("MySQL ticket validation checks passed. Test rows were intentionally retained.");
    return 0;
}
catch (HarnessFailure ex)
{
    Console.Error.WriteLine($"FAILED: {ex.Message}");
    return 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"FAILED: unexpected {ex.GetType().Name}; details omitted to protect fixture data and credentials.");
    return 1;
}

static async Task RunAsync()
{
    var raw = Environment.GetEnvironmentVariable("BERUTI_MYSQL_PURCHASE_TEST_CONNECTION");
    var pasajeroPublico = Environment.GetEnvironmentVariable("BERUTI_MYSQL_PURCHASE_TEST_PASSENGER_SUBJECT");
    var servicioA = Environment.GetEnvironmentVariable("BERUTI_MYSQL_PURCHASE_TEST_SERVICE_NO_SEAT");
    var servicioB = Environment.GetEnvironmentVariable("BERUTI_MYSQL_PURCHASE_TEST_SERVICE_ASSIGNED");
    var password = Environment.GetEnvironmentVariable("DemoSeed__Contrasena");
    if (string.IsNullOrWhiteSpace(raw) || string.IsNullOrWhiteSpace(pasajeroPublico) || string.IsNullOrWhiteSpace(servicioA)
        || string.IsNullOrWhiteSpace(servicioB) || servicioA == servicioB || string.IsNullOrWhiteSpace(password))
        throw new HarnessFailure("missing or invalid environment for the dedicated purchase-test database");
    string conexion;
    try { conexion = ConfiguracionAutenticacion.ValidarConexion(raw, "PurchaseTests").ConnectionString; }
    catch { throw new HarnessFailure("connection must match the dedicated loopback purchase-test tuple"); }

    await using (var contexto = Crear(conexion))
    {
        await contexto.Database.OpenConnectionAsync();
        await using var comando = contexto.Database.GetDbConnection().CreateCommand();
        comando.CommandText = "SELECT DATABASE(), CURRENT_USER(), (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ticket' AND COLUMN_NAME = 'usado_utc')";
        await using var lector = await comando.ExecuteReaderAsync();
        Verificar(await lector.ReadAsync(), "identity query returns a row");
        Verificar(lector.GetString(0) == "berruticomprastests" && lector.GetString(1).Split('@')[0].Equals("berruti_purchase_test", StringComparison.OrdinalIgnoreCase),
            "connected to the dedicated non-root test database");
        Verificar(lector.GetInt64(2) == 1, "ConsumoTicket migration is applied (ticket.usado_utc exists)");
    }

    var zona = TimeZoneInfo.FindSystemTimeZoneById("America/Montevideo");
    var hoy = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona).Date;
    var ayer = hoy.AddDays(-1);

    var (cobradorPublico, otroPasajeroPublico, cobradorSinAsignarPublico) = await PrepararCuentasAsync(conexion, password, servicioA, servicioB);
    var a1 = await SembrarCompraAsync(conexion, pasajeroPublico, servicioA, hoy.AddHours(14), servicioB, hoy.AddHours(16));
    var vencida = await SembrarCompraAsync(conexion, pasajeroPublico, servicioA, ayer.AddHours(9), servicioB, ayer.AddHours(11));
    var ajena = await SembrarCompraAsync(conexion, otroPasajeroPublico, servicioA, hoy.AddHours(14), servicioB, hoy.AddHours(16));

    ITicketsService Servicio(BerrutiDbContext contexto) =>
        new TicketsService(new TicketsRepository(contexto), TimeProvider.System, new ParametrosUbicacion { ZonaHoraria = zona });
    async Task<ResultadoValidacionTicketDto> Escanear(string qr, string servicioId)
    {
        await using var contexto = Crear(conexion);
        return await Servicio(contexto).ValidarAsync(cobradorPublico, new ValidarTicketRequestDto { QrOpaco = qr, ServicioId = servicioId });
    }

    // L9: un cobrador activo sin fila en cobrador_servicio no consume nada ni recibe un resultado de negocio.
    var sinAsignar = await Servicio(Crear(conexion)).ValidarAsync(cobradorSinAsignarPublico, new ValidarTicketRequestDto { QrOpaco = a1.Qr1, ServicioId = servicioA })
        .ContinueWith(t => t.Exception?.GetBaseException() is CobradorNoAsignadoException);
    Verificar(sinAsignar && (await LeerUsoAsync(conexion, a1.Qr1)).UsadoUtc is null,
        "unassigned collector is rejected (CobradorNoAsignadoException) and the ticket stays unused");

    var primero = await Escanear(a1.Qr1, servicioA);
    Verificar(primero.Resultado == ResultadoValidacion.Valido && primero.UsadoUtc is not null, "first valid scan consumes the ticket");
    var filaPrimera = await LeerUsoAsync(conexion, a1.Qr1);
    Verificar(filaPrimera.UsadoUtc == primero.UsadoUtc && filaPrimera.ValidadoPor is not null, "row stores the use timestamp and the validating collector");
    var segundo = await Escanear(a1.Qr1, servicioA);
    Verificar(segundo.Resultado == ResultadoValidacion.YaUtilizado && segundo.UsadoUtc == primero.UsadoUtc, "second scan is rejected with the first-use time");
    Verificar((await LeerUsoAsync(conexion, a1.Qr1)).UsadoUtc == primero.UsadoUtc, "second scan leaves the timestamp unchanged");

    var otroTramo = await Escanear(a1.Qr2, servicioA);
    Verificar(otroTramo.Resultado == ResultadoValidacion.TramoIncorrecto && (await LeerUsoAsync(conexion, a1.Qr2)).UsadoUtc is null,
        "wrong segment is reported and the ticket stays unused");
    var vencido = await Escanear(vencida.Qr1, servicioA);
    Verificar(vencido.Resultado == ResultadoValidacion.Vencido && (await LeerUsoAsync(conexion, vencida.Qr1)).UsadoUtc is null,
        "expired ticket is reported and not consumed");
    var inexistente = await Escanear(new string('Z', 64), servicioA);
    Verificar(inexistente.Resultado == ResultadoValidacion.CodigoInexistente, "unknown code is reported");
    var minusculas = await Escanear(a1.Qr2.ToLowerInvariant(), servicioB);
    Verificar(minusculas.Resultado == ResultadoValidacion.CodigoInexistente && (await LeerUsoAsync(conexion, a1.Qr2)).UsadoUtc is null,
        "QR comparison is case-sensitive (ascii_bin): a different-case code does not consume");

    // Concurrencia real: N escaneos simultáneos del mismo ticket, cada uno con su propia conexión, en varias rondas.
    for (var ronda = 1; ronda <= 5; ronda++)
    {
        var compra = await SembrarCompraAsync(conexion, pasajeroPublico, servicioA, hoy.AddHours(14), servicioB, hoy.AddHours(16));
        var inicio = new TaskCompletionSource();
        var tareas = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            await inicio.Task;
            return await Escanear(compra.Qr1, servicioA);
        })).ToArray();
        inicio.SetResult();
        var resultados = await Task.WhenAll(tareas);
        var validos = resultados.Where(r => r.Resultado == ResultadoValidacion.Valido).ToList();
        Verificar(validos.Count == 1 && resultados.Count(r => r.Resultado == ResultadoValidacion.YaUtilizado) == 7,
            $"round {ronda}: eight concurrent scans -> exactly one válido and seven ya utilizado");
        var fila = await LeerUsoAsync(conexion, compra.Qr1);
        Verificar(fila.UsadoUtc == validos[0].UsadoUtc && resultados.Where(r => r.Resultado == ResultadoValidacion.YaUtilizado).All(r => r.UsadoUtc == fila.UsadoUtc),
            $"round {ronda}: a single timestamp is stored and every loser reports it");
    }

    // Consulta del pasajero contra SQL real: solo sus tickets.
    await using (var contexto = Crear(conexion))
    {
        var propios = await new TicketsRepository(contexto).ListarDePasajeroAsync(pasajeroPublico);
        var qrPropios = propios.Select(t => t.QrOpaco).ToHashSet(StringComparer.Ordinal);
        Verificar(qrPropios.IsSupersetOf([a1.Qr1, a1.Qr2, vencida.Qr1, vencida.Qr2]) && !qrPropios.Contains(ajena.Qr1) && !qrPropios.Contains(ajena.Qr2),
            "passenger query returns the passenger's own tickets and never another passenger's");
        var delOtro = await new TicketsRepository(contexto).ListarDePasajeroAsync(otroPasajeroPublico);
        Verificar(delOtro.Select(t => t.QrOpaco).ToHashSet(StringComparer.Ordinal).SetEquals([ajena.Qr1, ajena.Qr2]),
            "the other passenger sees exactly their own tickets");
        var lista = await Servicio(contexto).ListarDelPasajeroAsync(pasajeroPublico);
        Verificar(lista.Anteriores.Any(t => t.QrOpaco == a1.Qr1 && t.Estado == EstadoTicket.Utilizado)
            && lista.Anteriores.Any(t => t.QrOpaco == vencida.Qr1 && t.Estado == EstadoTicket.Vencido)
            && lista.Vigentes.Any(t => t.QrOpaco == a1.Qr2 && t.Estado == EstadoTicket.SinUsar),
            "grouping: utilizado and vencido are anteriores, sin usar is vigente");
    }

    // La restricción de la base: el uso y el cobrador se escriben juntos.
    await using (var contexto = Crear(conexion))
    {
        var rechazada = false;
        try
        {
            await contexto.Database.ExecuteSqlInterpolatedAsync($"UPDATE ticket SET usado_utc = {DateTime.UtcNow} WHERE qr_opaco = {ajena.Qr1}");
        }
        catch (Exception) { rechazada = true; }
        Verificar(rechazada && (await LeerUsoAsync(conexion, ajena.Qr1)).UsadoUtc is null, "ck_ticket_uso rejects a use timestamp without a collector");
    }
}

static BerrutiDbContext Crear(string conexion) =>
    new(new DbContextOptionsBuilder<BerrutiDbContext>().UseMySQL(conexion).Options);

static async Task<(string CobradorPublico, string OtroPasajeroPublico, string CobradorSinAsignarPublico)> PrepararCuentasAsync(
    string conexion, string password, string servicioA, string servicioB)
{
    await using var contexto = Crear(conexion);
    var contadores = new ContadoresRepository(contexto, new Correlativo());
    var cuentas = new CuentasRepository(contexto, contadores, new PasswordHasher<Usuario>());
    var rolCobrador = await contexto.Roles.FirstOrDefaultAsync(r => r.Nombre == nameof(RolIngreso.Cobrador))
        ?? await cuentas.CrearRolAsync(RolIngreso.Cobrador);
    var rolPasajero = await contexto.Roles.FirstOrDefaultAsync(r => r.Nombre == nameof(RolIngreso.Pasajero))
        ?? await cuentas.CrearRolAsync(RolIngreso.Pasajero);
    var sufijo = Guid.NewGuid().ToString("N")[..12];
    var cobrador = await cuentas.CrearUsuarioAsync($"cobrador.{sufijo}@example.test", password, rolCobrador.Identificador);
    var otro = await cuentas.CrearUsuarioAsync($"pasajero.{sufijo}@example.test", password, rolPasajero.Identificador);
    var sinAsignar = await cuentas.CrearUsuarioAsync($"cobrador.sin.{sufijo}@example.test", password, rolCobrador.Identificador);
    // L9: el cobrador de las pruebas viaja en los dos servicios (cobrador_servicio); el segundo no tiene asignaciones.
    contexto.AsignacionesCobrador.AddRange(
        new API_Berruti.Models.Location.AsignacionCobrador { UsuarioId = cobrador.Identificador, ServicioId = servicioA },
        new API_Berruti.Models.Location.AsignacionCobrador { UsuarioId = cobrador.Identificador, ServicioId = servicioB });
    await contexto.SaveChangesAsync();
    return (cobrador.IdentificadorPublico, otro.IdentificadorPublico, sinAsignar.IdentificadorPublico);
}

// Siembra intento → tramos → pago aprobado → compra → dos compra_servicio → dos tickets, con IDs de los contadores.
static async Task<(string Qr1, string Qr2)> SembrarCompraAsync(
    string conexion, string pasajeroPublico, string servicio1, DateTime salida1, string servicio2, DateTime salida2)
{
    await using var contexto = Crear(conexion);
    var usuarioId = await contexto.Usuarios.Where(u => u.IdentificadorPublico == pasajeroPublico).Select(u => u.Identificador).SingleAsync();
    var contadores = new ContadoresRepository(contexto, new Correlativo());
    var ahora = DateTime.UtcNow;
    ahora = new DateTime(ahora.Ticks - ahora.Ticks % 10, DateTimeKind.Utc);
    await using var transaccion = await contexto.Database.BeginTransactionAsync();
    var intentoId = await contadores.IncrementarAsync("IC-");
    var pagoId = await contadores.IncrementarAsync("PG-");
    var compraId = await contadores.IncrementarAsync("CO-");
    var cs1 = await contadores.IncrementarAsync("CS-");
    var cs2 = await contadores.IncrementarAsync("CS-");
    var ti1 = await contadores.IncrementarAsync("TI-");
    var ti2 = await contadores.IncrementarAsync("TI-");

    contexto.IntentosCompra.Add(new IntentoCompra { Id = intentoId, UsuarioId = usuarioId, CreadoUtc = ahora.AddMinutes(-5), HoldVenceUtc = ahora.AddMinutes(10) });
    TramoIntentoCompra Tramo(int orden, string servicioId, DateTime salida) => new()
    {
        IntentoId = intentoId, Orden = orden, ServicioId = servicioId, TramoId = $"tramo-{orden}",
        OrigenId = $"origen-{orden}", OrigenNombre = orden == 1 ? "Ombúes" : "Radial de Conchillas",
        DestinoId = $"destino-{orden}", DestinoNombre = orden == 1 ? "Radial de Conchillas" : "Colonia",
        Salida = salida, Llegada = salida.AddHours(1), Importe = 100m, PoliticaAsiento = PoliticaAsiento.SinAsiento
    };
    contexto.TramosIntentoCompra.AddRange(Tramo(1, servicio1, salida1), Tramo(2, servicio2, salida2));
    contexto.Set<Pago>().Add(new Pago
    {
        Id = pagoId, IntentoId = intentoId, ImporteEsperado = 200m, ImportePagado = 200m, Moneda = "UYU", Estado = EstadoPago.Aprobado,
        ClaveCreacion = Guid.NewGuid(), OrdenProveedorId = $"TEST-{Guid.NewGuid():N}", CreadoUtc = ahora.AddMinutes(-4), ActualizadoUtc = ahora.AddMinutes(-3)
    });
    contexto.Set<TramoPago>().AddRange(
        new TramoPago { PagoId = pagoId, IntentoId = intentoId, Orden = 1, Importe = 100m },
        new TramoPago { PagoId = pagoId, IntentoId = intentoId, Orden = 2, Importe = 100m });
    contexto.Set<Compra>().Add(new Compra { Id = compraId, IntentoId = intentoId, PagoId = pagoId, ImporteTotal = 200m, ConfirmadoUtc = ahora.AddMinutes(-2) });
    contexto.Set<CompraServicio>().AddRange(
        new CompraServicio { Id = cs1, CompraId = compraId, IntentoId = intentoId, PagoId = pagoId, Orden = 1, Importe = 100m },
        new CompraServicio { Id = cs2, CompraId = compraId, IntentoId = intentoId, PagoId = pagoId, Orden = 2, Importe = 100m });
    var qr1 = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    var qr2 = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    contexto.Set<Ticket>().AddRange(
        new Ticket { Id = ti1, CompraServicioId = cs1, QrOpaco = qr1, EmitidoUtc = ahora.AddMinutes(-2) },
        new Ticket { Id = ti2, CompraServicioId = cs2, QrOpaco = qr2, EmitidoUtc = ahora.AddMinutes(-2) });
    await contexto.SaveChangesAsync();
    await transaccion.CommitAsync();
    return (qr1, qr2);
}

static async Task<(DateTime? UsadoUtc, string? ValidadoPor)> LeerUsoAsync(string conexion, string qr)
{
    await using var contexto = Crear(conexion);
    var fila = await contexto.Set<Ticket>().AsNoTracking().Where(t => t.QrOpaco == qr).Select(t => new { t.UsadoUtc, t.ValidadoPorUsuarioId }).SingleAsync();
    return (fila.UsadoUtc, fila.ValidadoPorUsuarioId);
}

static void Verificar(bool condicion, string afirmacion)
{
    if (!condicion) throw new HarnessFailure(afirmacion);
    Console.WriteLine($"PASS: {afirmacion}");
}

sealed class HarnessFailure(string message) : Exception(message);
