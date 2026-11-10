using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Operational;

namespace API_Berruti.Services.Operational;

public class TramosService(ITramosRepository repository) : ITramosService
{
    public async Task<IReadOnlyList<TramoDto>> ListarAsync()
    {
        var tramos = await repository.ListarAsync();
        var paradas = new Dictionary<string, string>();
        foreach (var tramo in tramos)
        {
            if (!paradas.ContainsKey(tramo.OrigenId)) paradas[tramo.OrigenId] = (await repository.BuscarParadaAsync(tramo.OrigenId))?.Nombre ?? "";
            if (!paradas.ContainsKey(tramo.DestinoId)) paradas[tramo.DestinoId] = (await repository.BuscarParadaAsync(tramo.DestinoId))?.Nombre ?? "";
        }
        return tramos.Select(t => ToDto(t, paradas[t.OrigenId], paradas[t.DestinoId])).ToArray();
    }
    public async Task<TramoDto> CrearAsync(TramoDto dto)
    {
        var tramo = await CrearEntidadAsync(dto);
        await repository.AgregarAsync(tramo);
        await repository.GuardarAsync();
        return await MapearAsync(tramo);
    }
    public async Task<TramoDto> ActualizarAsync(string id, TramoDto dto)
    {
        var tramo = await repository.BuscarAsync(id) ?? throw new KeyNotFoundException();
        ValidarParadas(dto);
        if (!await repository.ExisteParadaAsync(dto.OrigenId) || !await repository.ExisteParadaAsync(dto.DestinoId))
            throw new ArgumentException("El origen y destino deben ser paradas existentes.");
        tramo.OrigenId = dto.OrigenId;
        tramo.DestinoId = dto.DestinoId;
        await repository.GuardarAsync();
        return await MapearAsync(tramo);
    }
    public async Task EliminarAsync(string id)
    {
        var tramo = await repository.BuscarAsync(id) ?? throw new KeyNotFoundException();
        await repository.EliminarAsync(tramo);
        await repository.GuardarAsync();
    }
    private async Task<Tramo> CrearEntidadAsync(TramoDto dto)
    {
        ValidarParadas(dto);
        if (dto.OrigenId == dto.DestinoId) throw new ArgumentException("El origen y destino deben ser distintos.");
        if (!await repository.ExisteParadaAsync(dto.OrigenId) || !await repository.ExisteParadaAsync(dto.DestinoId))
            throw new ArgumentException("El origen y destino deben ser paradas existentes.");
        return new Tramo { OrigenId = dto.OrigenId, DestinoId = dto.DestinoId };
    }
    private static void ValidarParadas(TramoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.OrigenId) || string.IsNullOrWhiteSpace(dto.DestinoId))
            throw new ArgumentException("El origen y destino son obligatorios.");
        if (dto.OrigenId == dto.DestinoId) throw new ArgumentException("El origen y destino deben ser distintos.");
    }
    private async Task<TramoDto> MapearAsync(Tramo t) => ToDto(t,
        (await repository.BuscarParadaAsync(t.OrigenId))?.Nombre ?? "", (await repository.BuscarParadaAsync(t.DestinoId))?.Nombre ?? "");
    private static TramoDto ToDto(Tramo t, string origen, string destino) => new() { Id = t.Id, OrigenId = t.OrigenId, DestinoId = t.DestinoId, OrigenNombre = origen, DestinoNombre = destino };
}
