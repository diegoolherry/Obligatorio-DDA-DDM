namespace API_Berruti.Models.Operational;

// Lectura interna de un ticket con el tramo comprado (congelado en tramo_intento_compra); no es un contrato HTTP.
public sealed record TicketRegistro(
    string TicketId,
    string QrOpaco,
    DateTime? UsadoUtc,
    int Orden,
    string ServicioId,
    string TramoId,
    string OrigenNombre,
    string DestinoNombre,
    // Hora local de Uruguay, sin conversión a UTC (igual que Servicio.Salida).
    DateTime Salida,
    DateTime Llegada,
    PoliticaAsiento PoliticaAsiento,
    int? NumeroAsiento);
