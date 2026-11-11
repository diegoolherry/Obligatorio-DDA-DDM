using API_Berruti.DTOs.Location;
using API_Berruti.Models.Location;
using API_Berruti.Repositories.Location;
using API_Berruti.Security.Authentication;

namespace API_Berruti.Services.Location;

public class UbicacionService(IUbicacionRepository repository, ParametrosUbicacion parametros, TimeProvider reloj) : IUbicacionService
{
    private const double RadioTierraKm = 6371.0;

    public async Task<IReadOnlyList<ServicioUbicacionDto>> ListarServiciosAsignadosAsync(string identificadorPublico)
    {
        var usuarioId = await BuscarCobradorAsync(identificadorPublico);
        return await repository.ListarServiciosAsignadosAsync(usuarioId);
    }

    public async Task<PublicacionPosicionResponse> PublicarAsync(string identificadorPublico, string servicioId, PublicarPosicionRequest solicitud)
    {
        var usuarioId = await BuscarCobradorAsync(identificadorPublico);
        _ = await repository.BuscarServicioAsync(servicioId) ?? throw new KeyNotFoundException();
        if (!await repository.EstaAsignadoAsync(usuarioId, servicioId))
            throw new UnauthorizedAccessException("El cobrador no está asignado a este servicio.");
        var latitud = Coordenada(solicitud.Latitud, "latitud", 90);
        var longitud = Coordenada(solicitud.Longitud, "longitud", 180);

        var anterior = await repository.BuscarUltimaPosicionAsync(servicioId);
        var posicion = new PosicionRecibida
        {
            UsuarioId = usuarioId,
            ServicioId = servicioId,
            Latitud = latitud,
            Longitud = longitud,
            RecibidaUtc = PoliticaSesion.AhoraUtc(reloj)
        };
        await repository.AgregarPosicionAsync(posicion);
        await repository.GuardarAsync();

        double? intervalo = anterior == null ? null : (posicion.RecibidaUtc - anterior.RecibidaUtc).TotalSeconds;
        return new PublicacionPosicionResponse
        {
            Id = posicion.Id,
            ServicioId = servicioId,
            Latitud = latitud,
            Longitud = longitud,
            RecibidaUtc = posicion.RecibidaUtc,
            IntervaloSegundos = intervalo,
            PeriodicidadSegundos = parametros.PeriodicidadSegundos,
            CumplePeriodicidad = intervalo == null ? null : intervalo <= parametros.PeriodicidadSegundos + parametros.MargenPeriodicidadSegundos
        };
    }

    public Task<IReadOnlyList<ServicioUbicacionDto>> ListarServiciosAsync() => repository.ListarServiciosAsync();

    public async Task<ConsultaUbicacionResponse> ConsultarAsync(string servicioId)
    {
        var servicio = await repository.BuscarServicioAsync(servicioId) ?? throw new KeyNotFoundException();
        var respuesta = new ConsultaUbicacionResponse
        {
            Servicio = servicio,
            EstadoDemora = EstadoDemora.SinDatos,
            Parametros = new ParametrosUbicacionDto
            {
                PeriodicidadSegundos = parametros.PeriodicidadSegundos,
                UmbralDesactualizacionSegundos = parametros.UmbralDesactualizacionSegundos,
                VelocidadPromedioKmH = parametros.VelocidadPromedioKmH,
                ToleranciaDemoraMinutos = parametros.ToleranciaDemoraMinutos
            }
        };

        var ultima = await repository.BuscarUltimaPosicionAsync(servicioId);
        if (ultima == null)
        {
            respuesta.Motivo = "Todavía no hay posiciones publicadas para este servicio.";
            return respuesta;
        }

        var ahora = PoliticaSesion.AhoraUtc(reloj);
        var antiguedad = Math.Max(0, (ahora - ultima.RecibidaUtc).TotalSeconds);
        respuesta.UltimaPosicion = new UltimaPosicionDto
        {
            Latitud = ultima.Latitud,
            Longitud = ultima.Longitud,
            RecibidaUtc = ultima.RecibidaUtc,
            AntiguedadSegundos = antiguedad
        };
        if (antiguedad > parametros.UmbralDesactualizacionSegundos)
        {
            respuesta.Desactualizada = true;
            respuesta.Motivo = "La última posición está desactualizada; no se estima la llegada.";
            return respuesta;
        }
        if (!parametros.Destinos.TryGetValue(servicio.Destino, out var destino))
        {
            respuesta.Motivo = "No hay una referencia de destino configurada para estimar la llegada.";
            return respuesta;
        }

        // Estimación académica: distancia en línea recta al destino dividida por la velocidad promedio configurada.
        var distanciaKm = DistanciaKm((double)ultima.Latitud, (double)ultima.Longitud, destino.Latitud, destino.Longitud);
        var llegadaUtc = DateTime.SpecifyKind(ultima.RecibidaUtc, DateTimeKind.Utc).AddHours(distanciaKm / parametros.VelocidadPromedioKmH);
        var llegadaLocal = TimeZoneInfo.ConvertTimeFromUtc(llegadaUtc, parametros.ZonaHoraria);
        respuesta.LlegadaAproximada = DateTime.SpecifyKind(llegadaLocal, DateTimeKind.Unspecified);
        respuesta.EstadoDemora = llegadaLocal > servicio.Llegada.AddMinutes(parametros.ToleranciaDemoraMinutos)
            ? EstadoDemora.ConDemora
            : EstadoDemora.EnHorario;
        return respuesta;
    }

    private async Task<string> BuscarCobradorAsync(string identificadorPublico) =>
        await repository.BuscarUsuarioIdPorPublicoAsync(identificadorPublico)
        ?? throw new UnauthorizedAccessException("La identidad no corresponde a un usuario activo.");

    private static decimal Coordenada(decimal? valor, string nombre, decimal limite)
    {
        if (valor == null) throw new ArgumentException($"La {nombre} es obligatoria.");
        if (valor < -limite || valor > limite) throw new ArgumentException($"La {nombre} debe estar entre {-limite} y {limite}.");
        if (decimal.Round(valor.Value, 6) != valor.Value) throw new ArgumentException($"La {nombre} admite hasta 6 decimales.");
        return valor.Value;
    }

    // Fórmula de haversine.
    private static double DistanciaKm(double lat1, double lon1, double lat2, double lon2)
    {
        static double Radianes(double grados) => grados * Math.PI / 180;
        var dLat = Radianes(lat2 - lat1);
        var dLon = Radianes(lon2 - lon1);
        var a = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(Radianes(lat1)) * Math.Cos(Radianes(lat2)) * Math.Pow(Math.Sin(dLon / 2), 2);
        return 2 * RadioTierraKm * Math.Asin(Math.Sqrt(a));
    }
}
