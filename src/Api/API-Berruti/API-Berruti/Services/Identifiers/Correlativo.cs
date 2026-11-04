using System.Globalization;

namespace API_Berruti.Services.Identifiers;

public class Correlativo : ICorrelativo
{
    public string Siguiente(string prefijo, int ultimo)
    {
        if (prefijo != "US-" && prefijo != "RO-")
            throw new ArgumentException("Prefijo de cuenta desconocido.", nameof(prefijo));
        if (ultimo < 0 || ultimo > 50000)
            throw new ArgumentOutOfRangeException(nameof(ultimo), "Contador fuera de rango.");
        if (ultimo == 50000)
            throw new InvalidOperationException("Se agotó el rango de identificadores de la entidad.");
        return prefijo + (ultimo + 1).ToString(CultureInfo.InvariantCulture);
    }
}
