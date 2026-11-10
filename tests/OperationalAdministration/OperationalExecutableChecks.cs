using System.Reflection;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using API_Berruti.Controllers.Operational;
using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Operational;
using API_Berruti.Services.Operational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

internal static class OperationalExecutableChecks
{
    public static async Task RunAsync(Action<bool, string> check)
    {
        await CheckAuthorizationAsync(check);
        await CheckDemoPreparationAsync(check);
        CheckDecimalRangeValidation(check);
    }

    private static void CheckDecimalRangeValidation(Action<bool, string> check)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            foreach (var cultureName in new[] { "es-UY", "en-US" })
            {
                var culture = CultureInfo.GetCultureInfo(cultureName);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                var demo = ValidRequest();
                foreach (var propertyName in new[]
                {
                    nameof(SolicitudPreparacionDemoDto.ImporteOmbuesRadial),
                    nameof(SolicitudPreparacionDemoDto.ImporteRadialColonia)
                })
                    CheckAmountBoundaries(demo, propertyName, cultureName, check);

                var tariff = new TarifaDto { TramoId = "tramo-test", VigenteDesde = new DateTime(2031, 1, 1), VigenteHasta = new DateTime(2031, 12, 31) };
                CheckAmountBoundaries(tariff, nameof(TarifaDto.Importe), cultureName, check);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    private static void CheckAmountBoundaries(object instance, string propertyName, string cultureName, Action<bool, string> check)
    {
        foreach (var value in new[] { 0m, 100.25m, 9999999999999999.99m, -0.01m, 10000000000000000m })
            ValidateAmount(instance, propertyName, value, cultureName, check);
    }

    private static void ValidateAmount(object instance, string propertyName, decimal value, string cultureName, Action<bool, string> check)
    {
        foreach (var decimalProperty in instance.GetType().GetProperties()
                     .Where(property => property.PropertyType == typeof(decimal) && property.Name != propertyName))
            decimalProperty.SetValue(instance, 100.25m);
        instance.GetType().GetProperty(propertyName)!.SetValue(instance, value);
        try
        {
            var results = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
            var expectedValid = value >= 0m && value <= 9999999999999999.99m;
            check(valid == expectedValid,
                $"{instance.GetType().Name}.{propertyName} accepts {value} under {cultureName} without culture-sensitive range parsing");
        }
        catch (Exception exception)
        {
            check(false, $"{instance.GetType().Name}.{propertyName} throws {exception.GetType().Name} under {cultureName}");
        }
    }

    private static async Task CheckAuthorizationAsync(Action<bool, string> check)
    {
        var endpoints = new[]
        {
            new ControllerEndpoint("Paradas", typeof(ParadasController), typeof(IParadasService),
                async controller => await ((ParadasController)controller).Listar()),
            new ControllerEndpoint("Tramos", typeof(TramosController), typeof(ITramosService),
                async controller => await ((TramosController)controller).Listar()),
            new ControllerEndpoint("Servicios", typeof(ServiciosController), typeof(IServiciosService),
                async controller => await ((ServiciosController)controller).Listar()),
            new ControllerEndpoint("Tarifas", typeof(TarifasController), typeof(ITarifasService),
                async controller => await ((TarifasController)controller).Listar()),
            new ControllerEndpoint("PreparacionDemo", typeof(PreparacionDemoController), typeof(IPreparacionDemoService),
                async controller => await ((PreparacionDemoController)controller).Preparar(ValidRequest()))
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();
        using var provider = services.BuildServiceProvider();
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        foreach (var endpointCase in endpoints)
        {
            var actionReached = 0;
            var serviceCalls = 0;
            var attributes = endpointCase.ControllerType.GetCustomAttributes(inherit: true).OfType<IAuthorizeData>().ToArray();
            check(attributes.Length > 0, $"{endpointCase.Name} actual controller exposes authorization metadata");
            var policy = await AuthorizationPolicy.CombineAsync(policyProvider, attributes);
            check(policy is not null, $"{endpointCase.Name} actual authorization metadata produces a policy");
            if (policy is null) continue;

            var anonymous = await authorization.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, policy.Requirements);
            var passenger = await authorization.AuthorizeAsync(CreatePrincipal("Pasajero"), null, policy.Requirements);
            var cashier = await authorization.AuthorizeAsync(CreatePrincipal("Cobrador"), null, policy.Requirements);
            var administrator = await authorization.AuthorizeAsync(CreatePrincipal("Administrador"), null, policy.Requirements);
            check(!anonymous.Succeeded, $"{endpointCase.Name} actual policy rejects anonymous principal");
            check(!passenger.Succeeded, $"{endpointCase.Name} actual policy rejects Pasajero");
            check(!cashier.Succeeded, $"{endpointCase.Name} actual policy rejects Cobrador");
            check(administrator.Succeeded, $"{endpointCase.Name} actual policy authorizes Administrador");

            if (administrator.Succeeded)
            {
                actionReached++;
                var proxy = (CountingServiceProxy)DispatchProxy.Create(endpointCase.ServiceType, typeof(CountingServiceProxy));
                var controller = Activator.CreateInstance(endpointCase.ControllerType, proxy)!;
                var actionResult = await endpointCase.Invoke(controller);
                serviceCalls += proxy.Calls;
                var result = actionResult.GetType().GetProperty("Result")?.GetValue(actionResult);
                var statusCode = (result as IStatusCodeActionResult)?.StatusCode ?? StatusCodes.Status200OK;
                check(statusCode == StatusCodes.Status200OK && actionReached == 1 && serviceCalls == 1,
                    $"{endpointCase.Name} authorized principal reaches actual controller action and service");
            }
        }
    }

    private static ClaimsPrincipal CreatePrincipal(string role) => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, "operational-test-user"), new Claim(ClaimTypes.Role, role)],
        "operational-test"));

    private static async Task CheckDemoPreparationAsync(Action<bool, string> check)
    {
        var repository = new MemoryDemoRepository();
        var service = new PreparacionDemoService(repository);
        var request = ValidRequest();

        var first = await service.PrepararAsync(request);
        check(first.Exito && !first.Conflicto && first.ParadasCreadas == 3 && first.TramosCreados == 2 &&
            first.ServiciosCreados == 2 && first.TarifasCreadas == 2, "demo preparation inserts the missing scenario once");
        check(repository.Services.Select(s => s.TramoId).SequenceEqual(repository.Tramos.Select(t => t.Id)) &&
            repository.Tariffs.Select(t => t.TramoId).SequenceEqual(repository.Tramos.Select(t => t.Id)),
            "demo services and tariffs retain the generated tramo IDs");
        check(repository.Services[0].PoliticaAsiento == PoliticaAsiento.SinAsiento &&
            repository.Services[1].PoliticaAsiento == PoliticaAsiento.AsignacionAutomatica &&
            repository.Tramos[0].DestinoId == repository.Tramos[1].OrigenId,
            "demo preparation preserves the transfer and both seat policies");

        var savedIds = repository.Services.Select(s => s.Id).Concat(repository.Tariffs.Select(t => t.Id)).ToArray();
        var insertCount = repository.InsertCount;
        var saveCount = repository.SaveCount;
        var repeated = await service.PrepararAsync(request);
        check(repeated.Exito && repeated.SinCambios && repository.InsertCount == insertCount && repository.SaveCount == saveCount &&
            repository.Services.Select(s => s.Id).Concat(repository.Tariffs.Select(t => t.Id)).SequenceEqual(savedIds),
            "repeating an already prepared demo makes no changes and preserves IDs");

        var conflictRepository = CreateExistingScenario();
        conflictRepository.Services.Add(new Servicio
        {
            TramoId = "tramo-ombues-radial", Fecha = request.Fecha.Date,
            HoraSalida = request.HoraSalidaOmbuesRadial, HoraLlegada = request.HoraLlegadaOmbuesRadial,
            Capacidad = request.CapacidadOmbuesRadial + 1, PoliticaAsiento = PoliticaAsiento.SinAsiento
        });
        var conflict = await new PreparacionDemoService(conflictRepository).PrepararAsync(request);
        check(conflict.Conflicto && conflict.SinCambios && conflictRepository.InsertCount == 0 && conflictRepository.SaveCount == 0,
            "incompatible existing service returns conflict before any insertion");

        var invalidRepository = new MemoryDemoRepository();
        var invalidRequest = ValidRequest();
        invalidRequest.CapacidadOmbuesRadial = 0;
        var rejected = false;
        try { await new PreparacionDemoService(invalidRepository).PrepararAsync(invalidRequest); }
        catch (ArgumentException) { rejected = true; }
        check(rejected && invalidRepository.ReadCount == 0 && invalidRepository.InsertCount == 0,
            "invalid values are rejected before repository reads or writes");
    }

    private static SolicitudPreparacionDemoDto ValidRequest() => new()
    {
        Fecha = new DateTime(2031, 5, 12),
        HoraSalidaOmbuesRadial = new TimeSpan(8, 0, 0),
        HoraLlegadaOmbuesRadial = new TimeSpan(9, 0, 0),
        CapacidadOmbuesRadial = 28,
        ImporteOmbuesRadial = 125.50m,
        VigenteDesdeOmbuesRadial = new DateTime(2031, 1, 1),
        VigenteHastaOmbuesRadial = new DateTime(2031, 12, 31),
        HoraSalidaRadialColonia = new TimeSpan(9, 30, 0),
        HoraLlegadaRadialColonia = new TimeSpan(10, 30, 0),
        CapacidadRadialColonia = 24,
        ImporteRadialColonia = 200.00m,
        VigenteDesdeRadialColonia = new DateTime(2031, 1, 1),
        VigenteHastaRadialColonia = new DateTime(2031, 12, 31)
    };

    private static MemoryDemoRepository CreateExistingScenario()
    {
        var repository = new MemoryDemoRepository();
        repository.Stops.AddRange(new[]
        {
            new Parada { Id = "stop-ombues", Nombre = "Ombúes" },
            new Parada { Id = "stop-radial", Nombre = "Radial de Conchillas" },
            new Parada { Id = "stop-colonia", Nombre = "Colonia" }
        });
        repository.Tramos.AddRange(new[]
        {
            new Tramo { Id = "tramo-ombues-radial", OrigenId = "stop-ombues", DestinoId = "stop-radial" },
            new Tramo { Id = "tramo-radial-colonia", OrigenId = "stop-radial", DestinoId = "stop-colonia" }
        });
        return repository;
    }

    private sealed record ControllerEndpoint(string Name, Type ControllerType, Type ServiceType,
        Func<object, Task<object>> Invoke);

}

public class CountingServiceProxy : DispatchProxy
{
    public int Calls { get; private set; }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        Calls++;
        var returnType = targetMethod?.ReturnType ?? throw new InvalidOperationException("Controller service method was not resolved.");
        if (returnType == typeof(Task)) return Task.CompletedTask;
        if (!returnType.IsGenericType || returnType.GetGenericTypeDefinition() != typeof(Task<>))
            throw new InvalidOperationException($"Unexpected fake service return type {returnType}.");

        var resultType = returnType.GetGenericArguments()[0];
        object? result = null;
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
            result = Array.CreateInstance(resultType.GetGenericArguments()[0], 0);
        else if (!resultType.IsAbstract && !resultType.IsInterface)
            result = Activator.CreateInstance(resultType);
        return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType).Invoke(null, [result]);
    }
}

internal sealed class MemoryDemoRepository : IPreparacionDemoRepository
{
    public List<Parada> Stops { get; } = [];
    public List<Tramo> Tramos { get; } = [];
    public List<Servicio> Services { get; } = [];
    public List<Tarifa> Tariffs { get; } = [];
    public int ReadCount { get; private set; }
    public int InsertCount { get; private set; }
    public int SaveCount { get; private set; }

    public Task<List<Parada>> ListarParadasAsync() { ReadCount++; return Task.FromResult(Stops.ToList()); }
    public Task<List<Tramo>> ListarTramosAsync() { ReadCount++; return Task.FromResult(Tramos.ToList()); }
    public Task<List<Servicio>> ListarServiciosAsync() { ReadCount++; return Task.FromResult(Services.ToList()); }
    public Task<List<Tarifa>> ListarTarifasAsync() { ReadCount++; return Task.FromResult(Tariffs.ToList()); }
    public Task AgregarParadaAsync(Parada item) { Stops.Add(item); InsertCount++; return Task.CompletedTask; }
    public Task AgregarTramoAsync(Tramo item) { Tramos.Add(item); InsertCount++; return Task.CompletedTask; }
    public Task AgregarServicioAsync(Servicio item) { Services.Add(item); InsertCount++; return Task.CompletedTask; }
    public Task AgregarTarifaAsync(Tarifa item) { Tariffs.Add(item); InsertCount++; return Task.CompletedTask; }
    public Task GuardarAsync() { SaveCount++; return Task.CompletedTask; }
}
