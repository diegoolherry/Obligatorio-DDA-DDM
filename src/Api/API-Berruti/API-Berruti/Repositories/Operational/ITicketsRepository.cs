using API_Berruti.Models.Operational;

namespace API_Berruti.Repositories.Operational;

public interface ITicketsRepository
{
    /// <summary>Tickets del pasajero activo con ese identificador público. Lanza UnauthorizedAccessException si no lo es.</summary>
    Task<IReadOnlyList<TicketRegistro>> ListarDePasajeroAsync(string identificadorPublico, CancellationToken cancellationToken = default);

    /// <summary>Id interno del cobrador activo con ese identificador público. Lanza UnauthorizedAccessException si no lo es.</summary>
    Task<string> BuscarCobradorIdAsync(string identificadorPublico, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consumo atómico: una sola sentencia UPDATE que marca el uso solo si el código existe, no se usó, pertenece al
    /// servicio indicado y su salida (hora local) no es anterior a <paramref name="inicioDiaLocal"/>. Devuelve si lo consumió.
    /// </summary>
    Task<bool> ConsumirAsync(
        string qrOpaco,
        string servicioId,
        string cobradorId,
        DateTime ahoraUtc,
        DateTime inicioDiaLocal,
        CancellationToken cancellationToken = default);

    Task<TicketRegistro?> BuscarPorQrAsync(string qrOpaco, CancellationToken cancellationToken = default);
}
