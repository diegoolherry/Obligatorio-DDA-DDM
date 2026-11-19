using System.Globalization;

namespace API_Berruti.Services.Identifiers;

public class Correlativo : ICorrelativo
{
    public string Siguiente(string prefijo, int ultimo)
    {
        if (prefijo != "US-" && prefijo != "RO-" && prefijo != "SA-" && prefijo != "IC-" &&
            prefijo != "PG-" && prefijo != "CO-" && prefijo != "CS-" && prefijo != "TI-")
            throw new ArgumentException("Prefijo de entidad desconocido.", nameof(prefijo));
        if (ultimo < 0 || ultimo > 50000)
            throw new ArgumentOutOfRangeException(nameof(ultimo), "Contador fuera de rango.");
        if (ultimo == 50000)
            throw new InvalidOperationException("Se agotó el rango de identificadores de la entidad.");
        return prefijo + (ultimo + 1).ToString(CultureInfo.InvariantCulture);
    }
}
