namespace API_Berruti.Repositories.Operational;

public interface IIntentosPendientesRepository
{
    /// <summary>Id del intento más reciente del pasajero que requiere atención, o null si no hay.</summary>
    Task<string?> BuscarIdPendienteAsync(
        string identificadorPublico,
        DateTime ahoraUtc,
        CancellationToken cancellationToken = default);
}
