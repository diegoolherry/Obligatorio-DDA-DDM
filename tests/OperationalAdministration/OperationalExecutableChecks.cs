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
                async controller => await ((TarifasController)controller).Listar())
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
