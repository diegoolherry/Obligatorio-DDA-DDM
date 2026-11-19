using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace API_Berruti.Services.Operational;

public sealed class ParametrosMercadoPago
{
    private ParametrosMercadoPago(string accessToken, long expectedSellerId, string expectedSiteId)
    {
        AccessToken = accessToken;
        ExpectedSellerId = expectedSellerId;
        ExpectedSiteId = expectedSiteId;
    }

    public string AccessToken { get; }
    public long ExpectedSellerId { get; }
    public string ExpectedSiteId { get; }

    public static ParametrosMercadoPago Desde(IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(configuracion);

        var accessToken = configuracion["MercadoPago:AccessToken"];
        var testOriginConfirmed = bool.TryParse(configuracion["MercadoPago:TestCredentialOriginConfirmed"], out var confirmed)
            && confirmed;
        var expectedSellerId = configuracion["MercadoPago:ExpectedSellerId"];
        var expectedSiteId = configuracion["MercadoPago:ExpectedSiteId"]?.Trim();
        if (string.IsNullOrWhiteSpace(accessToken)
            || !testOriginConfirmed
            || !long.TryParse(expectedSellerId, NumberStyles.None, CultureInfo.InvariantCulture, out var sellerId)
            || sellerId <= 0
            || string.IsNullOrWhiteSpace(expectedSiteId))
            throw new InvalidOperationException("Se requiere configurar una credencial privada y una identidad de cuenta TEST confirmada.");

        return new ParametrosMercadoPago(accessToken, sellerId, expectedSiteId);
    }
}
