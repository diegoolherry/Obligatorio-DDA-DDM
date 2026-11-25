using API_Berruti.DTOs.Operational;

namespace API_Berruti.Services.Operational;

public interface ITicketsService
{
    Task<ListaTicketsPasajeroDto> ListarDelPasajeroAsync(string identificadorPublico, CancellationToken cancellationToken = default);

    Task<ResultadoValidacionTicketDto> ValidarAsync(
        string identificadorPublicoCobrador,
        ValidarTicketRequestDto solicitud,
        CancellationToken cancellationToken = default);
}
