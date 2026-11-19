namespace API_Berruti.Models.Operational;

public sealed record RegistroPago(Pago Pago, DateTime HoldVenceEn, bool EsNuevo);
