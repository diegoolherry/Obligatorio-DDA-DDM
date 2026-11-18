using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace API_Berruti.Data;

// MySQL espera 50 s por un bloqueo de fila (innodb_lock_wait_timeout), pero el comando del cliente vence a los 5 s
// (DefaultCommandTimeout) y, cuando vence primero, MySql.Data nunca completa la tarea async: la request queda colgada.
// Se baja la espera de bloqueo a 3 s en cada conexión que EF abre: así MySQL responde el error 1205 antes que el cliente.
// Es un valor de sesión, y como TODA conexión del DbContext (incluidas las del pool) pasa por ConnectionOpened,
// nunca se reutiliza una conexión sin el valor ni se altera el valor global del servidor ni el de otras aplicaciones.
public class EsperaBloqueoInterceptor : DbConnectionInterceptor
{
    public const string Sentencia = "SET SESSION innodb_lock_wait_timeout = 3";

    public override void ConnectionOpened(DbConnection conexion, ConnectionEndEventData datos)
    {
        using var comando = conexion.CreateCommand();
        comando.CommandText = Sentencia;
        comando.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(DbConnection conexion, ConnectionEndEventData datos,
        CancellationToken cancelacion = default)
    {
        await using var comando = conexion.CreateCommand();
        comando.CommandText = Sentencia;
        await comando.ExecuteNonQueryAsync(cancelacion);
    }
}
