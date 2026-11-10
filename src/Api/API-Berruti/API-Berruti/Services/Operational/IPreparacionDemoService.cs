using API_Berruti.DTOs.Operational;

namespace API_Berruti.Services.Operational;

public interface IPreparacionDemoService
{
    Task<ResultadoPreparacionDemoDto> PrepararAsync(SolicitudPreparacionDemoDto solicitud);
}
