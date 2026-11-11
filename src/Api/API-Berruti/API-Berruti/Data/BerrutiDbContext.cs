using API_Berruti.Models.Authentication;
using API_Berruti.Models.Catalogo;
using API_Berruti.Models.Location;
using API_Berruti.Models.Operational;
using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;

namespace API_Berruti.Data;

public class BerrutiDbContext : DbContext
{
    public BerrutiDbContext(DbContextOptions<BerrutiDbContext> opciones) : base(opciones) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Sesion> Sesiones => Set<Sesion>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<UsuarioRol> UsuariosRoles => Set<UsuarioRol>();
    public DbSet<ContadorEntidad> Contadores => Set<ContadorEntidad>();
    public DbSet<Parada> Paradas => Set<Parada>();
    public DbSet<Tramo> Tramos => Set<Tramo>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<Tarifa> Tarifas => Set<Tarifa>();
    public DbSet<AsignacionCobrador> AsignacionesCobrador => Set<AsignacionCobrador>();
    public DbSet<PosicionRecibida> PosicionesRecibidas => Set<PosicionRecibida>();
    public DbSet<BloqueHorario> BloquesHorario => Set<BloqueHorario>();
    public DbSet<RutaCatalogo> Rutas => Set<RutaCatalogo>();
    public DbSet<ParadaRuta> ParadasRuta => Set<ParadaRuta>();
    public DbSet<SalidaCatalogo> SalidasCatalogo => Set<SalidaCatalogo>();
    public DbSet<HoraSalida> HorasSalida => Set<HoraSalida>();
    public DbSet<Feriado> Feriados => Set<Feriado>();
    public DbSet<ExcepcionCalendario> ExcepcionesCalendario => Set<ExcepcionCalendario>();

    // Mismas restricciones que el esquema aprobado en scripts/database/create-auth-demo.sql.
    private const string Ascii = "ascii_bin";
    private const string Utf8 = "utf8mb4_bin";

    private static string RangoId(string prefijo) => $"^{prefijo}([1-9][0-9]{{0,3}}|[1-4][0-9]{{4}}|50000)$";

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        var usuario = modelo.Entity<Usuario>();
        usuario.ToTable("usuario", t =>
        {
            t.HasCheckConstraint("ck_usuario_id", $"usuario_id REGEXP '{RangoId("US-")}'");
            t.HasCheckConstraint("ck_usuario_activo", "activo IN (0, 1)");
        });
        usuario.HasKey(u => u.Identificador);
        usuario.Property(u => u.Identificador).HasColumnName("usuario_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).ValueGeneratedNever();
        usuario.Property(u => u.IdentificadorPublico).HasColumnName("identificador_publico").HasMaxLength(43).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).IsRequired();
        usuario.HasIndex(u => u.IdentificadorPublico).IsUnique().HasDatabaseName("uq_usuario_publico");
        usuario.Property(u => u.Correo).HasColumnName("correo").HasMaxLength(254).ForMySQLHasCharset("utf8mb4").ForMySQLHasCollation(Utf8).IsRequired();
        usuario.HasIndex(u => u.Correo).IsUnique().HasDatabaseName("uq_usuario_correo");
        usuario.Property(u => u.Contrasena).HasColumnName("contrasena").HasMaxLength(255).ForMySQLHasCharset("utf8mb4").ForMySQLHasCollation(Utf8).IsRequired();
        usuario.Property(u => u.Activo).HasColumnName("activo");

        var rol = modelo.Entity<Rol>();
        rol.ToTable("rol", t =>
        {
            t.HasCheckConstraint("ck_rol_id", $"rol_id REGEXP '{RangoId("RO-")}'");
            t.HasCheckConstraint("ck_rol_nombre", "nombre IN ('Pasajero', 'Cobrador', 'Administrador')");
        });
        rol.HasKey(r => r.Identificador);
        rol.Property(r => r.Identificador).HasColumnName("rol_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).ValueGeneratedNever();
        rol.Property(r => r.Nombre).HasColumnName("nombre").HasMaxLength(13).ForMySQLHasCharset("utf8mb4").ForMySQLHasCollation(Utf8).IsRequired();
        rol.HasIndex(r => r.Nombre).IsUnique().HasDatabaseName("uq_rol_nombre");

        var membresia = modelo.Entity<UsuarioRol>();
        membresia.ToTable("usuario_rol");
        membresia.HasKey(m => new { m.UsuarioId, m.RolId });
        membresia.Property(m => m.UsuarioId).HasColumnName("usuario_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii);
        membresia.Property(m => m.RolId).HasColumnName("rol_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii);
        membresia.HasOne<Usuario>().WithMany().HasForeignKey(m => m.UsuarioId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_usuario_rol_usuario");
        membresia.HasOne<Rol>().WithMany().HasForeignKey(m => m.RolId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_usuario_rol_rol");

        var sesion = modelo.Entity<Sesion>();
        sesion.ToTable("sesion", t =>
            t.HasCheckConstraint("ck_sesion_vence", "vence_utc = DATE_ADD(creada_utc, INTERVAL 7 DAY)"));
        sesion.HasKey(s => s.Huella);
        sesion.Property(s => s.Huella).HasColumnName("huella").HasColumnType("char(64)").ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).ValueGeneratedNever();
        sesion.Property(s => s.UsuarioId).HasColumnName("usuario_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii);
        sesion.Property(s => s.CreadaUtc).HasColumnName("creada_utc").HasColumnType("datetime(6)");
        sesion.Property(s => s.VenceUtc).HasColumnName("vence_utc").HasColumnType("datetime(6)");
        sesion.Property(s => s.RevocadaUtc).HasColumnName("revocada_utc").HasColumnType("datetime(6)");
        sesion.HasOne<Usuario>().WithMany().HasForeignKey(s => s.UsuarioId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_sesion_usuario");

        var contador = modelo.Entity<ContadorEntidad>();
        contador.ToTable("contador_entidad", t =>
        {
            t.HasCheckConstraint("ck_contador_rango", "ultimo BETWEEN 0 AND 50000");
            t.HasCheckConstraint("ck_contador_prefijo", "prefijo IN ('US-', 'RO-')");
        });
        contador.HasKey(c => c.Prefijo);
        contador.Property(c => c.Prefijo).HasColumnName("prefijo").HasMaxLength(3).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).ValueGeneratedNever();
        contador.Property(c => c.Ultimo).HasColumnName("ultimo");

        var parada = modelo.Entity<Parada>();
        parada.ToTable("parada");
        parada.HasKey(p => p.Id);
        parada.Property(p => p.Id).HasColumnName("parada_id").HasMaxLength(36).ValueGeneratedNever();
        parada.Property(p => p.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();

        var tramo = modelo.Entity<Tramo>();
        tramo.ToTable("tramo");
        tramo.HasKey(t => t.Id);
        tramo.Property(t => t.Id).HasColumnName("tramo_id").HasMaxLength(36).ValueGeneratedNever();
        tramo.Property(t => t.OrigenId).HasColumnName("origen_id").HasMaxLength(36).IsRequired();
        tramo.Property(t => t.DestinoId).HasColumnName("destino_id").HasMaxLength(36).IsRequired();
        tramo.HasOne<Parada>().WithMany().HasForeignKey(t => t.OrigenId).OnDelete(DeleteBehavior.Restrict);
        tramo.HasOne<Parada>().WithMany().HasForeignKey(t => t.DestinoId).OnDelete(DeleteBehavior.Restrict);

        var servicio = modelo.Entity<Servicio>();
        servicio.ToTable("servicio", t =>
            t.HasCheckConstraint("ck_servicio_llegada", "llegada > salida"));
        servicio.HasKey(s => s.Id);
        servicio.Property(s => s.Id).HasColumnName("servicio_id").HasMaxLength(36).ValueGeneratedNever();
        servicio.Property(s => s.TramoId).HasColumnName("tramo_id").HasMaxLength(36).IsRequired();
        servicio.Property(s => s.Salida).HasColumnName("salida").HasColumnType("datetime(6)").IsRequired();
        servicio.Property(s => s.Llegada).HasColumnName("llegada").HasColumnType("datetime(6)").IsRequired();
        servicio.Property(s => s.Capacidad).HasColumnName("capacidad").IsRequired();
        servicio.Property(s => s.PoliticaAsiento).HasColumnName("politica_asiento").HasConversion<string>().HasMaxLength(24).IsRequired();
        servicio.HasOne<Tramo>().WithMany().HasForeignKey(s => s.TramoId).OnDelete(DeleteBehavior.Restrict);

        var tarifa = modelo.Entity<Tarifa>();
        tarifa.ToTable("tarifa");
        tarifa.HasKey(t => t.Id);
        tarifa.Property(t => t.Id).HasColumnName("tarifa_id").HasMaxLength(36).ValueGeneratedNever();
        tarifa.Property(t => t.TramoId).HasColumnName("tramo_id").HasMaxLength(36).IsRequired();
        tarifa.Property(t => t.Importe).HasColumnName("importe").HasPrecision(18, 2).IsRequired();
        tarifa.Property(t => t.VigenteDesde).HasColumnName("vigente_desde").HasColumnType("date").IsRequired();
        tarifa.Property(t => t.VigenteHasta).HasColumnName("vigente_hasta").HasColumnType("date").IsRequired();
        tarifa.HasOne<Tramo>().WithMany().HasForeignKey(t => t.TramoId).OnDelete(DeleteBehavior.Restrict);

        // MVP-009: el celular del cobrador asignado publica posiciones del servicio. Borrar un servicio
        // demo arrastra su asignación y posiciones, para no cambiar el DELETE administrativo existente.
        var asignacion = modelo.Entity<AsignacionCobrador>();
        asignacion.ToTable("cobrador_servicio");
        asignacion.HasKey(a => new { a.UsuarioId, a.ServicioId });
        asignacion.Property(a => a.UsuarioId).HasColumnName("usuario_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii);
        asignacion.Property(a => a.ServicioId).HasColumnName("servicio_id").HasMaxLength(36);
        asignacion.HasOne<Usuario>().WithMany().HasForeignKey(a => a.UsuarioId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_cobrador_servicio_usuario");
        asignacion.HasOne<Servicio>().WithMany().HasForeignKey(a => a.ServicioId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_cobrador_servicio_servicio");

        var posicion = modelo.Entity<PosicionRecibida>();
        posicion.ToTable("posicion_recibida", t =>
        {
            t.HasCheckConstraint("ck_posicion_latitud", "latitud BETWEEN -90 AND 90");
            t.HasCheckConstraint("ck_posicion_longitud", "longitud BETWEEN -180 AND 180");
        });
        posicion.HasKey(p => p.Id);
        posicion.Property(p => p.Id).HasColumnName("posicion_id").HasMaxLength(36).ValueGeneratedNever();
        posicion.Property(p => p.UsuarioId).HasColumnName("usuario_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).IsRequired();
        posicion.Property(p => p.ServicioId).HasColumnName("servicio_id").HasMaxLength(36).IsRequired();
        posicion.Property(p => p.Latitud).HasColumnName("latitud").HasPrecision(9, 6).IsRequired();
        posicion.Property(p => p.Longitud).HasColumnName("longitud").HasPrecision(9, 6).IsRequired();
        // MySQL no guarda el Kind: al leer se marca UTC para que el JSON lleve la zona ("Z").
        posicion.Property(p => p.RecibidaUtc).HasColumnName("recibida_utc").HasColumnType("datetime(6)").IsRequired()
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        // La posición siempre referencia una asociación cobrador/servicio existente.
        posicion.HasOne<AsignacionCobrador>().WithMany().HasForeignKey(p => new { p.UsuarioId, p.ServicioId })
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_posicion_asignacion");
        posicion.HasIndex(p => new { p.ServicioId, p.RecibidaUtc }).HasDatabaseName("ix_posicion_servicio_recibida");

        ConfigurarCatalogo(modelo);
    }

    // FIN-001: catálogo de rutas dirigidas y calendarios transcripto del horario publicado.
    private static void ConfigurarCatalogo(ModelBuilder modelo)
    {
        var bloque = modelo.Entity<BloqueHorario>();
        bloque.ToTable("bloque_horario", t =>
            t.HasCheckConstraint("ck_bloque_vigencia", "vigente_hasta IS NULL OR vigente_desde IS NULL OR vigente_hasta >= vigente_desde"));
        bloque.HasKey(b => b.Id);
        bloque.Property(b => b.Id).HasColumnName("bloque_id").HasMaxLength(36).ValueGeneratedNever();
        bloque.Property(b => b.Clave).HasColumnName("clave").HasMaxLength(16).IsRequired();
        bloque.Property(b => b.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
        bloque.Property(b => b.Empresa).HasColumnName("empresa").HasMaxLength(100).IsRequired();
        bloque.Property(b => b.VigenteDesde).HasColumnName("vigente_desde").HasColumnType("date");
        bloque.Property(b => b.VigenteHasta).HasColumnName("vigente_hasta").HasColumnType("date");
        bloque.Property(b => b.FuenteUrl).HasColumnName("fuente_url").HasMaxLength(200).IsRequired();
        bloque.HasIndex(b => b.Clave).IsUnique().HasDatabaseName("ux_bloque_clave");

        var ruta = modelo.Entity<RutaCatalogo>();
        ruta.ToTable("ruta");
        ruta.HasKey(r => r.Id);
        ruta.Property(r => r.Id).HasColumnName("ruta_id").HasMaxLength(36).ValueGeneratedNever();
        ruta.Property(r => r.BloqueId).HasColumnName("bloque_id").HasMaxLength(36).IsRequired();
        ruta.Property(r => r.Clave).HasColumnName("clave").HasMaxLength(16).IsRequired();
        ruta.Property(r => r.Familia).HasColumnName("familia").HasMaxLength(100).IsRequired();
        ruta.Property(r => r.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
        ruta.HasIndex(r => r.Clave).IsUnique().HasDatabaseName("ux_ruta_clave");
        ruta.HasOne<BloqueHorario>().WithMany().HasForeignKey(r => r.BloqueId).OnDelete(DeleteBehavior.Restrict);

        var paradaRuta = modelo.Entity<ParadaRuta>();
        paradaRuta.ToTable("parada_ruta", t => t.HasCheckConstraint("ck_parada_ruta_orden", "orden >= 1"));
        paradaRuta.HasKey(p => new { p.RutaId, p.Orden });
        paradaRuta.Property(p => p.RutaId).HasColumnName("ruta_id").HasMaxLength(36);
        paradaRuta.Property(p => p.Orden).HasColumnName("orden");
        paradaRuta.Property(p => p.ParadaId).HasColumnName("parada_id").HasMaxLength(36).IsRequired();
        paradaRuta.HasOne<RutaCatalogo>().WithMany().HasForeignKey(p => p.RutaId).OnDelete(DeleteBehavior.Cascade);
        paradaRuta.HasOne<Parada>().WithMany().HasForeignKey(p => p.ParadaId).OnDelete(DeleteBehavior.Restrict);

        var salida = modelo.Entity<SalidaCatalogo>();
        salida.ToTable("salida_catalogo");
        salida.HasKey(s => s.Id);
        salida.Property(s => s.Id).HasColumnName("salida_id").HasMaxLength(36).ValueGeneratedNever();
        salida.Property(s => s.RutaId).HasColumnName("ruta_id").HasMaxLength(36).IsRequired();
        salida.Property(s => s.Dias).HasColumnName("dias").HasConversion<string>().HasMaxLength(24).IsRequired();
        salida.Property(s => s.Codigos).HasColumnName("codigos").HasMaxLength(16);
        salida.Property(s => s.Nota).HasColumnName("nota").HasMaxLength(200);
        salida.HasOne<RutaCatalogo>().WithMany().HasForeignKey(s => s.RutaId).OnDelete(DeleteBehavior.Cascade);

        var hora = modelo.Entity<HoraSalida>();
        hora.ToTable("hora_salida", t => t.HasCheckConstraint("ck_hora_salida_orden", "orden >= 1"));
        hora.HasKey(h => new { h.SalidaId, h.Orden });
        hora.Property(h => h.SalidaId).HasColumnName("salida_id").HasMaxLength(36);
        hora.Property(h => h.Orden).HasColumnName("orden");
        hora.Property(h => h.Hora).HasColumnName("hora").HasColumnType("time(0)").IsRequired();
        hora.HasOne<SalidaCatalogo>().WithMany().HasForeignKey(h => h.SalidaId).OnDelete(DeleteBehavior.Cascade);

        var feriado = modelo.Entity<Feriado>();
        feriado.ToTable("feriado");
        feriado.HasKey(f => f.Fecha);
        feriado.Property(f => f.Fecha).HasColumnName("fecha").HasColumnType("date").ValueGeneratedNever();
        feriado.Property(f => f.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();

        var excepcion = modelo.Entity<ExcepcionCalendario>();
        excepcion.ToTable("excepcion_calendario", t => t.HasCheckConstraint("ck_excepcion_rango", "hasta >= desde"));
        excepcion.HasKey(e => e.Id);
        excepcion.Property(e => e.Id).HasColumnName("excepcion_id").HasMaxLength(36).ValueGeneratedNever();
        excepcion.Property(e => e.BloqueId).HasColumnName("bloque_id").HasMaxLength(36);
        excepcion.Property(e => e.Desde).HasColumnName("desde").HasColumnType("date").IsRequired();
        excepcion.Property(e => e.Hasta).HasColumnName("hasta").HasColumnType("date").IsRequired();
        excepcion.Property(e => e.Tipo).HasColumnName("tipo").HasConversion<string>().HasMaxLength(24).IsRequired();
        excepcion.Property(e => e.Motivo).HasColumnName("motivo").HasMaxLength(200).IsRequired();
        excepcion.HasOne<BloqueHorario>().WithMany().HasForeignKey(e => e.BloqueId).OnDelete(DeleteBehavior.Cascade);

        // Servicios generados desde el catálogo: el índice único impide duplicar una generación repetida.
        var servicio = modelo.Entity<Servicio>();
        servicio.Property(s => s.SalidaCatalogoId).HasColumnName("salida_catalogo_id").HasMaxLength(36);
        servicio.HasOne<SalidaCatalogo>().WithMany().HasForeignKey(s => s.SalidaCatalogoId).OnDelete(DeleteBehavior.Restrict);
        servicio.HasIndex(s => new { s.SalidaCatalogoId, s.TramoId, s.Salida }).IsUnique().HasDatabaseName("ux_servicio_generado");
    }
}
