using System.ComponentModel.DataAnnotations;
using API_Berruti.DTOs.Operational;
using API_Berruti.Repositories.Operational;

namespace API_Berruti.Services.Operational;

public class IntentosCompraService : IIntentosCompraService
{
    private readonly IIntentosCompraRepository _repository;
    private readonly IItinerariosService _itinerarios;

    public IntentosCompraService(IIntentosCompraRepository repository, IItinerariosService itinerarios)
    {
        _repository = repository;
        _itinerarios = itinerarios;
    }

    public async Task<IniciarIntentoCompraResponseDto> CrearAsync(
        string identificadorPublico,
        IniciarIntentoCompraRequestDto solicitud,
        CancellationToken cancellationToken = default)
    {
        ValidarSolicitud(identificadorPublico, solicitud);
        var selecciones = solicitud.Tramos.OrderBy(tramo => tramo.Orden).ToList();
        if (selecciones.Select(tramo => tramo.Orden).Distinct().Count() != selecciones.Count ||
            selecciones.Select(tramo => tramo.ServicioId).Distinct(StringComparer.Ordinal).Count() != selecciones.Count)
            throw new ArgumentException("Los órdenes y servicios seleccionados deben ser únicos.", nameof(solicitud));

        var servicios = await _repository.BuscarServiciosAsync(selecciones.Select(tramo => tramo.ServicioId).ToArray());
        if (servicios.Count != selecciones.Count)
            throw new KeyNotFoundException("Uno o más servicios seleccionados no existen.");

        var porId = servicios.ToDictionary(item => item.Servicio.Id, StringComparer.Ordinal);
        var seleccionados = selecciones.Select(seleccion => porId[seleccion.ServicioId]).ToList();
        var origen = seleccionados[0].OrigenId;
        var destino = seleccionados[^1].DestinoId;
        var fecha = DateOnly.FromDateTime(seleccionados[0].Servicio.Salida);
        var ofrecidos = await _itinerarios.BuscarAsync(origen, destino, fecha);
        var idsSolicitados = selecciones.Select(seleccion => seleccion.ServicioId).ToArray();
        var cotizacion = ofrecidos.FirstOrDefault(ofrecido =>
            ofrecido.Tramos.Select(tramo => tramo.ServicioId).SequenceEqual(idsSolicitados, StringComparer.Ordinal));
        if (cotizacion is null)
            throw new ArgumentException("La secuencia seleccionada no coincide con un itinerario ofrecido actualmente.", nameof(solicitud));

        return await _repository.CrearAsync(identificadorPublico, cotizacion, cancellationToken);
    }

    public Task<IniciarIntentoCompraResponseDto> ConsultarAsync(
        string identificadorPublico,
        string intentoId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identificadorPublico))
            throw new ArgumentException("La identidad pública del pasajero es obligatoria.", nameof(identificadorPublico));
        if (string.IsNullOrWhiteSpace(intentoId))
            throw new ArgumentException("El identificador del intento es obligatorio.", nameof(intentoId));
        return _repository.ConsultarAsync(identificadorPublico, intentoId, cancellationToken);
    }

    private static void ValidarSolicitud(string identificadorPublico, IniciarIntentoCompraRequestDto solicitud)
    {
        if (string.IsNullOrWhiteSpace(identificadorPublico))
            throw new ArgumentException("La identidad pública del pasajero es obligatoria.", nameof(identificadorPublico));
        ArgumentNullException.ThrowIfNull(solicitud);
        var resultados = new List<ValidationResult>();
        if (!Validator.TryValidateObject(solicitud, new ValidationContext(solicitud), resultados, validateAllProperties: true))
            throw new ArgumentException(resultados[0].ErrorMessage ?? "La solicitud no es válida.", nameof(solicitud));
    }
}
