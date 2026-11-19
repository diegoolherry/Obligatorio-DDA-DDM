using API_Berruti.DTOs.Operational;

namespace API_Berruti.Services.Operational;

public interface IIntentosPendientesService
{
    /// <summary>Devuelve el intento más reciente que requiere atención o null si no hay.</summary>
    Task<IntentoPendienteResponseDto?> ConsultarAsync(string identificadorPublico, CancellationToken cancellationToken = default);
}
