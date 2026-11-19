namespace API_Berruti.Services.Operational;

public sealed class AsientoNoDisponibleException(string mensaje) : InvalidOperationException(mensaje) { }
