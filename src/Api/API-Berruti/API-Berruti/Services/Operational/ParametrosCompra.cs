using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace API_Berruti.Services.Operational;

public class ParametrosCompra
{
    public int HoldMinutos { get; init; }

    public static ParametrosCompra Desde(IConfiguration configuracion)
    {
        var valor = configuracion["Compra:HoldMinutos"];
        if (!int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutos) || minutos <= 0)
            throw new InvalidOperationException("Compra:HoldMinutos debe ser un entero positivo.");
        return new ParametrosCompra { HoldMinutos = minutos };
    }
}
