namespace API_Berruti.Security.Authentication;

public static class EncabezadoBearer
{
    public static bool EsBearer(string encabezado) =>
        encabezado.Equals("Bearer", StringComparison.OrdinalIgnoreCase) ||
        encabezado.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ||
        encabezado.StartsWith("Bearer\t", StringComparison.OrdinalIgnoreCase);
}
