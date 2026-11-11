namespace API_Berruti.Models.Catalogo;

// Hora publicada de una salida en la parada de orden Orden de su ruta. Las paradas sin hora publicada no tienen fila.
// La hora del primer orden con fila es la salida publicada; las siguientes son aproximadas.
public class HoraSalida
{
    public string SalidaId { get; set; } = string.Empty;
    public int Orden { get; set; }
    public TimeSpan Hora { get; set; }
}
