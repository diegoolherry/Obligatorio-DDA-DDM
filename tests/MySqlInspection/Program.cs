using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

var fallos = 0;
async Task Probar(string nombre, Action<ConexionFalsa> preparar, Action<ConexionFalsa, int, string, string> verificar)
{
    var conexion = new ConexionFalsa();
    preparar(conexion);
    var salida = new StringWriter();
    var errores = new StringWriter();
    var codigo = await InspeccionMySql.EjecutarAsync(conexion, salida, errores);
    try
    {
        verificar(conexion, codigo, salida.ToString(), errores.ToString());
        Exigir(conexion.Cerrada && conexion.State == ConnectionState.Closed, "conexión liberada");
        Exigir(conexion.Creados == conexion.Liberados, "todos los comandos liberados");
        Exigir(!salida.ToString().Contains("SECRETO") && !errores.ToString().Contains("SECRETO"), "sin secretos");
        Exigir(conexion.Consultas.All(ConexionFalsa.EsPermitida), "log completo dentro de allowlist fija");
        Console.WriteLine($"PASS: {nombre}");
    }
    catch (Exception e) { fallos++; Console.Error.WriteLine($"FAIL: {nombre}: {e.Message}"); }
}
void Exigir(bool valor, string nombre) { if (!valor) throw new Exception(nombre); }
void Correcta(ConexionFalsa c, int codigo, string salida, string errores)
{
    Exigir(codigo == 0 && errores == "", "exit 0 sin stderr");
    Exigir(salida.Contains("Inspección ejecutada") && !salida.Contains("PASS"), "no acredita pruebas");
    Exigir(c.Consultas[0] == "SELECT CURRENT_USER(), DATABASE()", "identidad primero");
    Exigir(c.Consultas[1] == "START TRANSACTION READ ONLY" && c.Consultas.Last() == "ROLLBACK", "transacción solo lectura y rollback");
    Exigir(c.Tiempos.All(t => t == 5), "timeout acotado");
}
await Probar("esquema ausente informa faltantes sin contar tablas", c => c.Vacio = true, (c, n, s, e) =>
{
    Correcta(c, n, s, e);
    Exigir(s.Contains("Ausente: usuario") && !c.Consultas.Any(q => q.Contains("COUNT(")), "ausencia no es fixture inválida");
});
await Probar("tablas vacías sin semilla, metadatos y objetos extra", c => { }, (c, n, s, e) =>
{
    Correcta(c, n, s, e);
    Exigir(s.Contains("Conteo usuario: 0") && s.Contains("Contadores:") && s.Contains("vista_extra"), "metadatos y ceros");
    Exigir(c.Consultas.Count(q => q.StartsWith("SELECT COUNT(*) FROM `berrutiauthtests`.")) == 5, "solo cinco tablas conocidas");
    Exigir(!c.Consultas.Any(q => q.Contains("FROM `berrutiauthtests`.`vista_extra`") || q.Contains("FROM `berrutiauthtests`.`extra`")), "no contar extras/vistas");
    Exigir(!c.Consultas.Any(q => q.Contains("COLUMN_DEFAULT") || q.Contains("COMMENT") || q.Contains("ACTION_STATEMENT") || q.Contains("CHECK_CLAUSE")), "sin SQL/defaults/cuerpos");
    Exigir(s.Contains("FOREIGN KEY") && s.Contains("BEFORE"), "claves y trigger sin cuerpo");
});
await Probar("conexión ya abierta no se reabre y se libera", c => c.Open(), Correcta);
await Probar("contadores inicializados y filas inválidas solo agregadas", c => c.Semilla = true, (c, n, s, e) =>
{
    Correcta(c, n, s, e);
    Exigir(s.Contains("[2,1,0,0,1,49998,49998]"), "estado numérico agregado");
});
foreach (var usuario in new[] { "root@localhost", "berruti_test@otro" })
    await Probar("identidad incorrecta " + usuario, c => c.Usuario = usuario, (c, n, s, e) =>
    {
        Exigir(n == 1 && s == "" && e.Contains("Inspección no completada"), "rechazo seguro");
        Exigir(c.Consultas.Count == 1, "sin consultas tras identidad incorrecta");
    });
await Probar("base real incorrecta", c => c.Base = "berrutilocal", (c, n, s, e) =>
{
    Exigir(n == 1 && s == "" && e.Contains("Inspección no completada") && c.Consultas.Count == 1, "rechazo antes de inspección");
});
foreach (var fallo in new[] { "abrir", "START TRANSACTION READ ONLY", "information_schema.COLUMNS", "ROLLBACK" })
    await Probar("error seguro y limpieza " + fallo, c => c.Fallo = fallo, (c, n, s, e) =>
    {
        Exigir(n == 1 && s == "" && e.Contains("Inspección no completada"), "sin éxito ni excepción del proveedor");
        if (c.Consultas.Contains("START TRANSACTION READ ONLY")) Exigir(c.Consultas.Last() == "ROLLBACK", "intento rollback incluso ante error");
    });
await Probar("columna contador no numérica falla sin consultar filas", c => c.TipoUltimo = "varchar", (c, n, s, e) =>
{
    Exigir(n == 1 && s == "" && e.Contains("Inspección no completada"), "fallo genérico");
    Exigir(!c.Consultas.Any(q => q.Contains("MIN(")), "sin convertir texto de filas");
    Exigir(c.Consultas.Last() == "ROLLBACK", "rollback");
});
await Probar("metadatos excesivos fallan acotadamente", c => c.Exceso = true, (c, n, s, e) =>
{
    Exigir(n == 1 && s == "" && e.Contains("Inspección no completada"), "sin salida parcial");
    Exigir(c.Consultas.Last() == "ROLLBACK", "rollback");
});
await Probar("vista con nombre conocido no se cuenta", c => c.VistaUsuario = true, (c, n, s, e) =>
{
    Correcta(c, n, s, e);
    Exigir(s.Contains("Ausente: usuario") && !c.Consultas.Contains("SELECT COUNT(*) FROM `berrutiauthtests`.`usuario`"), "sin conteo de vista conocida");
});
await Probar("contador textual del proveedor no se imprime", c => c.ValorTextual = true, (c, n, s, e) =>
{
    Exigir(n == 1 && s == "" && e.Contains("Inspección no completada"), "sin conversión o eco de strings");
    Exigir(c.Consultas.Last() == "ROLLBACK", "rollback ante dato inesperado");
});
Console.WriteLine($"Fallos: {fallos}. Solo conexiones falsas, sin MySQL ni fixtures persistentes.");
return fallos == 0 ? 0 : 1;

sealed class ConexionFalsa : DbConnection
{
    public List<string> Consultas { get; } = new();
    public List<int> Tiempos { get; } = new();
    public bool Vacio, Semilla, Exceso, Cerrada, VistaUsuario, ValorTextual;
    public int Creados, Liberados;
    public string Usuario = "berruti_test@localhost", Base = "berrutiauthtests", Fallo = "", TipoUltimo = "int";
    [AllowNull] public override string ConnectionString { get; set; } = "";
    public override string Database => Base;
    public override string DataSource => "falso";
    public override string ServerVersion => "falso";
    private ConnectionState _estado = ConnectionState.Closed;
    public override ConnectionState State => _estado;
    public override void Open()
    {
        if (_estado != ConnectionState.Closed) throw new Exception("conexión ya abierta");
        if (Fallo == "abrir") throw new Exception("SECRETO proveedor");
        _estado = ConnectionState.Open;
    }
    public override void Close() { Cerrada = true; _estado = ConnectionState.Closed; }
    protected override void Dispose(bool disposing) { Close(); base.Dispose(disposing); }
    public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
    protected override DbCommand CreateDbCommand() { Creados++; return new ComandoFalso(this); }
    public DataTable Responder(string sql, int timeout)
    {
        if (State != ConnectionState.Open) throw new Exception("conexión cerrada");
        Consultas.Add(sql); Tiempos.Add(timeout);
        if (!EsPermitida(sql))
            throw new Exception("consulta fuera de allowlist pública de prueba");
        if (Fallo != "" && sql.Contains(Fallo)) throw new Exception("SECRETO proveedor");
        DataTable Filas(int columnas, params object[][] filas)
        {
            var tabla = new DataTable();
            for (var i = 0; i < columnas; i++) tabla.Columns.Add("c" + i, typeof(object));
            foreach (var fila in filas) tabla.Rows.Add(fila);
            return tabla;
        }
        if (sql == "SELECT CURRENT_USER(), DATABASE()") return Filas(2, [Usuario, Base]);
        if (sql == "START TRANSACTION READ ONLY" || sql == "ROLLBACK") return Filas(1);
        if (!sql.Contains("'berrutiauthtests'") && !sql.Contains("`berrutiauthtests`")) throw new Exception("consulta fuera de base fija");
        if (sql.Contains("information_schema.TABLES"))
        {
            if (Exceso) return Filas(3, Enumerable.Range(0, 129).Select(i => new object[] { "tabla" + i, "BASE TABLE", "InnoDB" }).ToArray());
            if (Vacio) return Filas(3);
            return Filas(3, ["usuario", VistaUsuario ? "VIEW" : "BASE TABLE", "InnoDB"], ["rol", "BASE TABLE", "InnoDB"],
                ["usuario_rol", "BASE TABLE", "InnoDB"], ["contador_entidad", "BASE TABLE", "InnoDB"],
                ["sesion", "BASE TABLE", "InnoDB"], ["vista_extra", "VIEW", DBNull.Value], ["extra", "BASE TABLE", "InnoDB"]);
        }
        if (sql.Contains("information_schema.COLUMNS")) return Vacio ? Filas(5) : Filas(5,
            ["contador_entidad", "prefijo", "varchar", "NO", "ascii_bin"],
            ["contador_entidad", "ultimo", TipoUltimo, "NO", DBNull.Value]);
        if (sql.Contains("information_schema.TABLE_CONSTRAINTS")) return Filas(3, ["usuario_rol", "fk_usuario", "FOREIGN KEY"]);
        if (sql.Contains("information_schema.KEY_COLUMN_USAGE")) return Filas(7, ["usuario_rol", "fk_usuario", "usuario_id", 1, "berrutiauthtests", "usuario", "usuario_id"]);
        if (sql.Contains("information_schema.TRIGGERS")) return Filas(4, ["trigger_extra", "usuario", "BEFORE", "INSERT"]);
        if (sql.StartsWith("SELECT COUNT(*) FROM `berrutiauthtests`."))
        {
            if (!new[] { "usuario", "rol", "usuario_rol", "contador_entidad", "sesion" }.Any(t => sql == $"SELECT COUNT(*) FROM `berrutiauthtests`.`{t}`"))
                throw new Exception("conteo no permitido");
            return Filas(1, [0L]);
        }
        if (sql.Contains("MIN(") && sql.EndsWith("FROM `berrutiauthtests`.`contador_entidad`"))
            return ValorTextual ? Filas(7, ["SECRETO fila", 0L, 0L, 0L, 0L, 0L, 0L]) :
                Semilla ? Filas(7, [2L, 1L, 0L, 0L, 1L, 49998L, 49998L]) : Filas(7, [0L, 0L, DBNull.Value, DBNull.Value, 0L, DBNull.Value, DBNull.Value]);
        throw new Exception("consulta no permitida");
    }
    public static bool EsPermitida(string sql) => Permitidas.Contains(sql) ||
        new[] { "usuario", "rol", "usuario_rol", "contador_entidad", "sesion" }
            .Any(t => sql == $"SELECT COUNT(*) FROM `berrutiauthtests`.`{t}`");
    private static readonly HashSet<string> Permitidas = new()
    {
        "SELECT CURRENT_USER(), DATABASE()", "START TRANSACTION READ ONLY", "ROLLBACK",
        "SELECT TABLE_NAME, TABLE_TYPE, ENGINE FROM information_schema.TABLES WHERE TABLE_SCHEMA = 'berrutiauthtests' ORDER BY TABLE_NAME LIMIT 129",
        "SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLLATION_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = 'berrutiauthtests' ORDER BY TABLE_NAME, ORDINAL_POSITION LIMIT 1025",
        "SELECT TABLE_NAME, CONSTRAINT_NAME, CONSTRAINT_TYPE FROM information_schema.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = 'berrutiauthtests' ORDER BY TABLE_NAME, CONSTRAINT_NAME LIMIT 513",
        "SELECT TABLE_NAME, CONSTRAINT_NAME, COLUMN_NAME, ORDINAL_POSITION, REFERENCED_TABLE_SCHEMA, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME FROM information_schema.KEY_COLUMN_USAGE WHERE CONSTRAINT_SCHEMA = 'berrutiauthtests' ORDER BY TABLE_NAME, CONSTRAINT_NAME, ORDINAL_POSITION LIMIT 1025",
        "SELECT TRIGGER_NAME, EVENT_OBJECT_TABLE, ACTION_TIMING, EVENT_MANIPULATION FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA = 'berrutiauthtests' ORDER BY TRIGGER_NAME LIMIT 129",
        "SELECT COUNT(CASE WHEN prefijo IS NULL OR BINARY prefijo NOT IN ('US-','RO-') OR ultimo IS NULL OR ultimo < 0 OR ultimo > 50000 THEN 1 END), " +
        "COUNT(CASE WHEN BINARY prefijo = 'US-' THEN 1 END), MIN(CASE WHEN BINARY prefijo = 'US-' AND ultimo BETWEEN 0 AND 50000 THEN ultimo END), MAX(CASE WHEN BINARY prefijo = 'US-' AND ultimo BETWEEN 0 AND 50000 THEN ultimo END), " +
        "COUNT(CASE WHEN BINARY prefijo = 'RO-' THEN 1 END), MIN(CASE WHEN BINARY prefijo = 'RO-' AND ultimo BETWEEN 0 AND 50000 THEN ultimo END), MAX(CASE WHEN BINARY prefijo = 'RO-' AND ultimo BETWEEN 0 AND 50000 THEN ultimo END) FROM `berrutiauthtests`.`contador_entidad`"
    };
}
sealed class ComandoFalso(ConexionFalsa conexion) : DbCommand
{
    [AllowNull] public override string CommandText { get; set; } = "";
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; }
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection? DbConnection { get; set; } = conexion;
    protected override DbTransaction? DbTransaction { get; set; }
    protected override DbParameterCollection DbParameterCollection => throw new NotSupportedException();
    protected override void Dispose(bool disposing) { if (disposing) conexion.Liberados++; base.Dispose(disposing); }
    public override void Cancel() => throw new NotSupportedException();
    public override void Prepare() => throw new NotSupportedException();
    protected override DbParameter CreateDbParameter() => throw new NotSupportedException();
    public override int ExecuteNonQuery() { conexion.Responder(CommandText, CommandTimeout); return 0; }
    public override object? ExecuteScalar() => throw new NotSupportedException();
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => conexion.Responder(CommandText, CommandTimeout).CreateDataReader();
}
