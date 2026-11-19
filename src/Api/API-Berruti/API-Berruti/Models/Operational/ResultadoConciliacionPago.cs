using API_Berruti.DTOs.Operational;

namespace API_Berruti.Models.Operational;

public sealed record ResultadoConciliacionPago(
    RegistroPago Registro,
    CompraConfirmadaResponseDto? Compra,
    bool RequiereDevolucion);
