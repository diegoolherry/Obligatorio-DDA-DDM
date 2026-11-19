using Microsoft.Extensions.Configuration;

namespace API_Berruti.Services.Operational;

public sealed class ParametrosMercadoPago
{
    private ParametrosMercadoPago(string accessToken) => AccessToken = accessToken;

    public string AccessToken { get; }

    public static ParametrosMercadoPago Desde(IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(configuracion);

        var accessToken = configuracion["MercadoPago:AccessToken"];
        if (string.IsNullOrWhiteSpace(accessToken) || !accessToken.StartsWith("TEST-", StringComparison.Ordinal))
            throw new InvalidOperationException("Se requiere una credencial privada TEST de Mercado Pago.");

        return new ParametrosMercadoPago(accessToken);
    }
}
