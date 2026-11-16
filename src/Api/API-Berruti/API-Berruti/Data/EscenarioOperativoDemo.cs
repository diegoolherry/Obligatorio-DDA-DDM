using API_Berruti.Models.Operational;

namespace API_Berruti.Data;

// Escenario operativo ficticio que ReinicioLocal siembra en Development. Es puro: no toca la base.
public sealed record EscenarioOperativo(
    IReadOnlyList<Parada> Paradas,
    IReadOnlyList<Tramo> Tramos,
    IReadOnlyList<Servicio> Servicios,
    IReadOnlyList<Tarifa> Tarifas)
{
    public IReadOnlyList<Salida> Salidas { get; init; } = [];
    public IReadOnlyList<AsientoSalida> AsientosSalida { get; init; } = [];
}

public static class EscenarioOperativoDemo
{
    public static EscenarioOperativo Construir(DateTime fecha)
    {
        var dia = fecha.Date;
        var ombues = new Parada { Nombre = "Ombúes" };
        var radial = new Parada { Nombre = "Radial de Conchillas" };
        var colonia = new Parada { Nombre = "Colonia" };

        var ombuesRadial = new Tramo { OrigenId = ombues.Id, DestinoId = radial.Id };
        var radialColonia = new Tramo { OrigenId = radial.Id, DestinoId = colonia.Id };
        var salida = new Salida { Id = "SA-1", Fecha = DateOnly.FromDateTime(dia) };

        var servicios = new[]
        {
            new Servicio
            {
                TramoId = ombuesRadial.Id, Salida = dia.AddHours(8), Llegada = dia.AddHours(8).AddMinutes(30),
                Capacidad = 30, PoliticaAsiento = PoliticaAsiento.SinAsiento
            },
            new Servicio
            {
                TramoId = radialColonia.Id, Salida = dia.AddHours(8).AddMinutes(45), Llegada = dia.AddHours(10),
                Capacidad = 46, PoliticaAsiento = PoliticaAsiento.AsignacionAutomatica, SalidaId = salida.Id
            },
        };

        var tarifas = new[]
        {
            new Tarifa { TramoId = ombuesRadial.Id, Importe = 100.00m, VigenteDesde = dia, VigenteHasta = dia.AddYears(1) },
            new Tarifa { TramoId = radialColonia.Id, Importe = 250.00m, VigenteDesde = dia, VigenteHasta = dia.AddYears(1) },
        };

        return new EscenarioOperativo([ombues, radial, colonia], [ombuesRadial, radialColonia], servicios, tarifas)
        {
            Salidas = [salida],
            AsientosSalida = Enumerable.Range(1, 46)
                .Select(numeroAsiento => new AsientoSalida { SalidaId = salida.Id, NumeroAsiento = numeroAsiento })
                .ToArray()
        };
    }
}
