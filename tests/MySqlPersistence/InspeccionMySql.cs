using System.Data;
using System.Data.Common;
using System.Text.Json;

public static class InspeccionMySql
{
    // El helper toma posesión de una conexión dedicada (abierta o cerrada).
    // No comparte una conexión con repositorios ni continúa hacia sus escrituras.
    public static async Task<int> EjecutarAsync(DbConnection conexion, TextWriter salida, TextWriter errores)
    {
        try
        {
            var informe = new StringWriter();
            await using (conexion)
            {
                if (conexion.State == ConnectionState.Closed) await conexion.OpenAsync();
                var identidad = await LeerAsync(conexion, "SELECT CURRENT_USER(), DATABASE()", 1);
                if (identidad.Count != 1 || !Equals(identidad[0][0], "berruti_test@localhost") ||
                    !Equals(identidad[0][1], "berrutiauthtests")) throw new InvalidOperationException();
                try
                {
                    await EjecutarAsync(conexion, "START TRANSACTION READ ONLY");
                    var tablas = await LeerAsync(conexion,
                        "SELECT TABLE_NAME, TABLE_TYPE, ENGINE FROM information_schema.TABLES WHERE TABLE_SCHEMA = 'berrutiauthtests' ORDER BY TABLE_NAME LIMIT 129", 128);
                    var columnas = await LeerAsync(conexion,
                        "SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLLATION_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = 'berrutiauthtests' ORDER BY TABLE_NAME, ORDINAL_POSITION LIMIT 1025", 1024);
                    var restricciones = await LeerAsync(conexion,
                        "SELECT TABLE_NAME, CONSTRAINT_NAME, CONSTRAINT_TYPE FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = 'berrutiauthtests' ORDER BY TABLE_NAME, CONSTRAINT_NAME LIMIT 513", 512);
                    var claves = await LeerAsync(conexion,
                        "SELECT TABLE_NAME, CONSTRAINT_NAME, COLUMN_NAME, ORDINAL_POSITION, REFERENCED_TABLE_SCHEMA, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME FROM information_schema.KEY_COLUMN_USAGE WHERE CONSTRAINT_SCHEMA = 'berrutiauthtests' ORDER BY TABLE_NAME, CONSTRAINT_NAME, ORDINAL_POSITION LIMIT 1025", 1024);
                    var triggers = await LeerAsync(conexion,
                        "SELECT TRIGGER_NAME, EVENT_OBJECT_TABLE, ACTION_TIMING, EVENT_MANIPULATION FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA = 'berrutiauthtests' ORDER BY TRIGGER_NAME LIMIT 129", 128);
                    void Mostrar(string etiqueta, List<object?[]> filas)
                    {
                        foreach (var fila in filas) informe.WriteLine($"{etiqueta}: {JsonSerializer.Serialize(fila)}");
                    }
                    Mostrar("Tabla [nombre,tipo,motor]", tablas);
                    Mostrar("Columna [tabla,nombre,tipo,nulable,collation]", columnas);
                    Mostrar("Restricción [tabla,nombre,tipo]", restricciones);
                    Mostrar("Clave [tabla,restricción,columna,posición,baseFK,tablaFK,columnaFK]", claves);
                    Mostrar("Trigger [nombre,tabla,momento,evento]", triggers);
                    // Solo estas constantes pueden formar un identificador SQL; nunca nombres del inventario.
                    foreach (var tabla in new[] { "usuario", "rol", "usuario_rol", "contador_entidad", "sesion" })
                    {
                        if (!tablas.Any(f => Equals(f[0], tabla) && Equals(f[1], "BASE TABLE")))
                        {
                            informe.WriteLine($"Ausente: {tabla} (BASE TABLE)");
                            continue;
                        }
                        var conteo = await LeerAsync(conexion, $"SELECT COUNT(*) FROM `berrutiauthtests`.`{tabla}`", 1, numerico: true);
                        informe.WriteLine($"Conteo {tabla}: {conteo.Single().Single()}");
                        if (tabla != "contador_entidad") continue;
                        if (!columnas.Any(f => Equals(f[0], tabla) && Equals(f[1], "prefijo") &&
                                (Equals(f[2], "varchar") || Equals(f[2], "char"))) ||
                            !columnas.Any(f => Equals(f[0], tabla) && Equals(f[1], "ultimo") &&
                                new[] { "tinyint", "smallint", "mediumint", "int", "bigint" }.Contains(f[2])))
                            throw new InvalidOperationException();
                        // Comparación binaria: prefijos similares por collation no son prefijos conocidos.
                        // Nunca devuelve prefijos arbitrarios ni valores fuera de 0..50000, solo su cantidad.
                        var estado = await LeerAsync(conexion,
                            "SELECT COUNT(CASE WHEN prefijo IS NULL OR BINARY prefijo NOT IN ('US-','RO-') OR ultimo IS NULL OR ultimo < 0 OR ultimo > 50000 THEN 1 END), " +
                            "COUNT(CASE WHEN BINARY prefijo = 'US-' THEN 1 END), MIN(CASE WHEN BINARY prefijo = 'US-' AND ultimo BETWEEN 0 AND 50000 THEN ultimo END), MAX(CASE WHEN BINARY prefijo = 'US-' AND ultimo BETWEEN 0 AND 50000 THEN ultimo END), " +
                            "COUNT(CASE WHEN BINARY prefijo = 'RO-' THEN 1 END), MIN(CASE WHEN BINARY prefijo = 'RO-' AND ultimo BETWEEN 0 AND 50000 THEN ultimo END), MAX(CASE WHEN BINARY prefijo = 'RO-' AND ultimo BETWEEN 0 AND 50000 THEN ultimo END) FROM `berrutiauthtests`.`contador_entidad`", 1, numerico: true);
                        informe.WriteLine($"Contadores: {JsonSerializer.Serialize(estado.Single())}");
                    }
                }
                finally
                {
                    // También se intenta si START falla; Dispose cierra aun si ROLLBACK falla.
                    await EjecutarAsync(conexion, "ROLLBACK");
                }
            }
            // Sin salida parcial ni éxito hasta terminar rollback y liberar la conexión.
            salida.Write(informe.ToString());
            salida.WriteLine("Inspección ejecutada (solo lectura); no acredita pruebas normal/frontera ni compatibilidad completa del DDL. Revisar metadatos antes de DML.");
            return 0;
        }
        catch
        {
            errores.WriteLine("Inspección no completada. Revisar localmente configuración, identidad, esquema, permisos y conectividad. Detalles privados omitidos; no acredita pruebas ni repara datos.");
            return 1;
        }
    }

    private static async Task EjecutarAsync(DbConnection conexion, string sql)
    {
        await using var comando = conexion.CreateCommand();
        comando.CommandTimeout = 5;
        comando.CommandText = sql;
        await comando.ExecuteNonQueryAsync();
    }

    private static async Task<List<object?[]>> LeerAsync(DbConnection conexion, string sql, int limite, bool numerico = false)
    {
        await using var comando = conexion.CreateCommand();
        comando.CommandTimeout = 5;
        comando.CommandText = sql;
        await using var lector = await comando.ExecuteReaderAsync();
        var filas = new List<object?[]>();
        while (await lector.ReadAsync())
        {
            if (filas.Count == limite) throw new InvalidOperationException();
            var fila = new object?[lector.FieldCount];
            for (var i = 0; i < fila.Length; i++)
            {
                var valor = lector.GetValue(i);
                if (valor is DBNull) fila[i] = null;
                else if (valor is byte or sbyte or short or ushort or int or uint or long or ulong or decimal)
                    fila[i] = valor;
                else if (!numerico && valor is string texto && texto.Length <= 256) fila[i] = texto;
                else throw new InvalidOperationException();
            }
            filas.Add(fila);
        }
        return filas;
    }
}
