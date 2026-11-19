using API_Berruti.DTOs.Operational;
using API_Berruti.Repositories.Operational;

namespace API_Berruti.Services.Operational;

public sealed class IntentosPendientesService : IIntentosPendientesService
{
    private readonly IIntentosPendientesRepository _pendientes;
    private readonly IIntentosCompraService _intentos;
    private readonly IPagosService _pagos;
    private readonly TimeProvider _timeProvider;

    public IntentosPendientesService(
        IIntentosPendientesRepository pendientes,
        IIntentosCompraService intentos,
        IPagosService pagos,
        TimeProvider timeProvider)
    {
        _pendientes = pendientes;
        _intentos = intentos;
        _pagos = pagos;
        _timeProvider = timeProvider;
    }

    public async Task<IntentoPendienteResponseDto?> ConsultarAsync(
        string identificadorPublico,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identificadorPublico))
            throw new ArgumentException("La identidad pública del pasajero es obligatoria.", nameof(identificadorPublico));

        cancellationToken.ThrowIfCancellationRequested();
        var intentoId = await _pendientes.BuscarIdPendienteAsync(
            identificadorPublico, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        if (intentoId is null) return null;

        return new IntentoPendienteResponseDto
        {
            Intento = await _intentos.ConsultarAsync(identificadorPublico, intentoId, cancellationToken),
            Pago = await _pagos.ConsultarActualAsync(identificadorPublico, intentoId, cancellationToken)
        };
    }
}
