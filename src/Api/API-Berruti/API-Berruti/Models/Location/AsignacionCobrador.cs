namespace API_Berruti.Models.Location;

// Asociación sembrada entre un usuario Cobrador y el servicio de demostración en el que viaja.
public class AsignacionCobrador
{
    public string UsuarioId { get; set; } = string.Empty;
    public string ServicioId { get; set; } = string.Empty;
}
