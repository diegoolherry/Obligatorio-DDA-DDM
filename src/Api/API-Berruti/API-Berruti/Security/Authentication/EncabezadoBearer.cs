namespace API_Berruti.Security.Authentication;

public static class EncabezadoBearer
{
    public static bool EsBearer(string encabezado) =>
        encabezado.Equals("Bearer", StringComparison.OrdinalIgnoreCase) ||
        encabezado.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ||
        encabezado.StartsWith("Bearer\t", StringComparison.OrdinalIgnoreCase);

    public static string? Token(string encabezado)
    {
        if (!encabezado.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;
        var token = encabezado[7..];
        return PoliticaSesion.EsToken(token) ? token : null;
    }
}
