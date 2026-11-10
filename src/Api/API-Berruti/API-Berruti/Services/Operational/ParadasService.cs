using API_Berruti.DTOs.Operational;
using API_Berruti.Models.Operational;
using API_Berruti.Repositories.Operational;

namespace API_Berruti.Services.Operational;

public class ParadasService(IParadasRepository repository) : IParadasService
{
    public async Task<IReadOnlyList<ParadaDto>> ListarAsync() => (await repository.ListarAsync()).Select(ToDto).ToArray();
    public async Task<ParadaDto> CrearAsync(ParadaDto dto)
    {
        var parada = new Parada { Nombre = Normalizar(dto.Nombre) };
        await repository.AgregarAsync(parada);
        await repository.GuardarAsync();
        return ToDto(parada);
    }
    public async Task<ParadaDto> ActualizarAsync(string id, ParadaDto dto)
    {
        var parada = await repository.BuscarAsync(id) ?? throw new KeyNotFoundException();
        parada.Nombre = Normalizar(dto.Nombre);
        await repository.GuardarAsync();
        return ToDto(parada);
    }
    public async Task EliminarAsync(string id)
    {
        var parada = await repository.BuscarAsync(id) ?? throw new KeyNotFoundException();
        await repository.EliminarAsync(parada);
        await repository.GuardarAsync();
    }
    private static string Normalizar(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("El nombre es obligatorio.");
        nombre = nombre.Trim();
        if (nombre.Length > 100) throw new ArgumentException("El nombre supera los 100 caracteres.");
        return nombre;
    }
    private static ParadaDto ToDto(Parada p) => new() { Id = p.Id, Nombre = p.Nombre };
}
