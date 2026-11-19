namespace API_Berruti.Services.Operational;

/// <summary>
/// Indica un fallo definitivo anterior al envío de la orden a Mercado Pago (validación de datos,
/// clave de idempotencia o verificación de identidad TEST). No existe una orden del proveedor que
/// conciliar, por lo que el pago puede rechazarse de forma segura. Un fallo posterior al envío
/// (timeout, error de red, respuesta no exitosa o ilegible) es ambiguo y no usa este tipo.
/// </summary>
public sealed class FalloPreEnvioProveedorException : InvalidOperationException
{
    public FalloPreEnvioProveedorException()
        : base("No se pudo iniciar el pago porque la verificación previa del proveedor falló.")
    {
    }

    public FalloPreEnvioProveedorException(Exception innerException)
        : base("No se pudo iniciar el pago porque la verificación previa del proveedor falló.", innerException)
    {
    }
}
