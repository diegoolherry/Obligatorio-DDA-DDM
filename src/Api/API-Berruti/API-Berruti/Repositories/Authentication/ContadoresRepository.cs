using API_Berruti.Data;
using API_Berruti.Services.Identifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace API_Berruti.Repositories.Authentication;

public class ContadoresRepository : IContadoresRepository
{
    private readonly BerrutiDbContext _contexto;
    private readonly ICorrelativo _correlativo;
    public ContadoresRepository(BerrutiDbContext contexto, ICorrelativo correlativo)
    {
        _contexto = contexto;
        _correlativo = correlativo;
    }

    // Solo dentro de la transacción del alta: no confirma números sin registro asociado.
    public async Task<string> IncrementarAsync(string prefijo)
    {
        var transaccion = _contexto.Database.CurrentTransaction
            ?? throw new InvalidOperationException("El alta requiere una transacción.");
        await using var consulta = _contexto.Database.GetDbConnection().CreateCommand();
        consulta.Transaction = transaccion.GetDbTransaction();
        consulta.CommandText = "SELECT ultimo FROM contador_entidad WHERE prefijo = @prefijo FOR UPDATE";
        var parametro = consulta.CreateParameter();
        parametro.ParameterName = "@prefijo";
        parametro.Value = prefijo;
        consulta.Parameters.Add(parametro);
        var valor = await consulta.ExecuteScalarAsync();
        if (valor == null || valor == DBNull.Value)
            throw new InvalidOperationException("Falta el contador preparado por el administrador.");
        var ultimo = Convert.ToInt32(valor);
        var identificador = _correlativo.Siguiente(prefijo, ultimo);
        await _contexto.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE contador_entidad SET ultimo = {ultimo + 1} WHERE prefijo = {prefijo}");
        return identificador;
    }
}
