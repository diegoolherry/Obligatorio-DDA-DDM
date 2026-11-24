using API_Berruti.Models.Authentication;
using API_Berruti.Models.Catalogo;
using API_Berruti.Models.Location;
using API_Berruti.Models.Operational;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MySql.EntityFrameworkCore.Extensions;

namespace API_Berruti.Data;

public class BerrutiDbContext : DbContext
{
    public BerrutiDbContext(DbContextOptions<BerrutiDbContext> opciones) : base(opciones) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<UsuarioRol> UsuariosRoles => Set<UsuarioRol>();
    public DbSet<ContadorEntidad> Contadores => Set<ContadorEntidad>();
    public DbSet<Parada> Paradas => Set<Parada>();
    public DbSet<Tramo> Tramos => Set<Tramo>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<Salida> Salidas => Set<Salida>();
    public DbSet<ParadaSalida> ParadasSalida => Set<ParadaSalida>();
    public DbSet<AsientoSalida> AsientosSalida => Set<AsientoSalida>();
    public DbSet<Tarifa> Tarifas => Set<Tarifa>();
    public DbSet<IntentoCompra> IntentosCompra => Set<IntentoCompra>();
    public DbSet<TramoIntentoCompra> TramosIntentoCompra => Set<TramoIntentoCompra>();
    public DbSet<HoldAsiento> HoldsAsiento => Set<HoldAsiento>();
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

        var contador = modelo.Entity<ContadorEntidad>();
        contador.ToTable("contador_entidad", t =>
        {
            t.HasCheckConstraint("ck_contador_rango", "ultimo BETWEEN 0 AND 50000");
            t.HasCheckConstraint("ck_contador_prefijo", "prefijo IN ('US-', 'RO-', 'SA-', 'IC-', 'PG-', 'CO-', 'CS-', 'TI-')");
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
        {
            t.HasCheckConstraint("ck_servicio_llegada", "llegada > salida");
            t.HasCheckConstraint("ck_servicio_rango_paradas",
                "((orden_embarque IS NULL AND orden_desembarque IS NULL) OR " +
                "(salida_id IS NOT NULL AND orden_embarque IS NOT NULL AND orden_desembarque IS NOT NULL " +
                "AND orden_embarque < orden_desembarque))");
        });
        servicio.HasKey(s => s.Id);
        servicio.Property(s => s.Id).HasColumnName("servicio_id").HasMaxLength(36).ValueGeneratedNever();
        servicio.Property(s => s.TramoId).HasColumnName("tramo_id").HasMaxLength(36).IsRequired();
        servicio.Property(s => s.Salida).HasColumnName("salida").HasColumnType("datetime(6)").IsRequired();
        servicio.Property(s => s.Llegada).HasColumnName("llegada").HasColumnType("datetime(6)").IsRequired();
        servicio.Property(s => s.Capacidad).HasColumnName("capacidad").IsRequired();
        servicio.Property(s => s.PoliticaAsiento).HasColumnName("politica_asiento").HasConversion<string>().HasMaxLength(24).IsRequired();
        servicio.Property(s => s.OrdenEmbarque).HasColumnName("orden_embarque");
        servicio.Property(s => s.OrdenDesembarque).HasColumnName("orden_desembarque");
        servicio.HasOne<Tramo>().WithMany().HasForeignKey(s => s.TramoId).OnDelete(DeleteBehavior.Restrict);

        var salida = modelo.Entity<Salida>();
        salida.ToTable("salida", t => t.HasCheckConstraint("ck_salida_id", $"salida_id REGEXP '{RangoId("SA-")}'"));
        salida.HasKey(s => s.Id);
        salida.Property(s => s.Id).HasColumnName("salida_id").HasMaxLength(8)
            .ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).ValueGeneratedNever();
        salida.Property(s => s.Fecha).HasColumnName("fecha").HasColumnType("date").IsRequired();

        var asientoSalida = modelo.Entity<AsientoSalida>();
        asientoSalida.ToTable("asiento_salida", t => t.HasCheckConstraint("ck_asiento_salida_numero", "numero_asiento >= 1"));
        asientoSalida.HasKey(a => new { a.SalidaId, a.NumeroAsiento });
        asientoSalida.Property(a => a.SalidaId).HasColumnName("salida_id").HasMaxLength(8)
            .ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii);
        asientoSalida.Property(a => a.NumeroAsiento).HasColumnName("numero_asiento");
        asientoSalida.HasOne<Salida>().WithMany().HasForeignKey(a => a.SalidaId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_asiento_salida_salida");
        servicio.Property(s => s.SalidaId).HasColumnName("salida_id").HasMaxLength(8)
            .ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii);
        servicio.HasOne<Salida>().WithMany().HasForeignKey(s => s.SalidaId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_servicio_salida");
        servicio.HasIndex(s => s.SalidaId).HasDatabaseName("IX_servicio_salida_id");

        var paradaSalida = modelo.Entity<ParadaSalida>();
        paradaSalida.ToTable("parada_salida", t => t.HasCheckConstraint("ck_parada_salida_orden", "orden >= 1"));
        paradaSalida.HasKey(p => new { p.SalidaId, p.Orden });
        paradaSalida.Property(p => p.SalidaId).HasColumnName("salida_id").HasMaxLength(8)
            .ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).IsRequired();
        paradaSalida.Property(p => p.Orden).HasColumnName("orden").IsRequired();
        paradaSalida.Property(p => p.ParadaId).HasColumnName("parada_id").HasMaxLength(36).IsRequired();
        paradaSalida.HasOne<Salida>().WithMany().HasForeignKey(p => p.SalidaId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_parada_salida_salida");
        paradaSalida.HasOne<Parada>().WithMany().HasForeignKey(p => p.ParadaId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_parada_salida_parada");
        servicio.HasOne<ParadaSalida>().WithMany().HasForeignKey(s => new { s.SalidaId, s.OrdenEmbarque })
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_servicio_parada_embarque");
        servicio.HasOne<ParadaSalida>().WithMany().HasForeignKey(s => new { s.SalidaId, s.OrdenDesembarque })
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_servicio_parada_desembarque");

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
        bloque.Property(b => b.Activo).HasColumnName("activo").IsRequired();
        bloque.HasIndex(b => b.Clave).IsUnique().HasDatabaseName("ux_bloque_clave");

        var ruta = modelo.Entity<RutaCatalogo>();
        ruta.ToTable("ruta");
        ruta.HasKey(r => r.Id);
        ruta.Property(r => r.Id).HasColumnName("ruta_id").HasMaxLength(36).ValueGeneratedNever();
        ruta.Property(r => r.BloqueId).HasColumnName("bloque_id").HasMaxLength(36).IsRequired();
        ruta.Property(r => r.Clave).HasColumnName("clave").HasMaxLength(16).IsRequired();
        ruta.Property(r => r.Familia).HasColumnName("familia").HasMaxLength(100).IsRequired();
        ruta.Property(r => r.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
        ruta.Property(r => r.Activo).HasColumnName("activo").IsRequired();
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
        // FIN-008: baja lógica. Sin valor por defecto en el modelo: EF enviaría siempre el valor explícito.
        salida.Property(s => s.Activo).HasColumnName("activo").IsRequired();
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

        ConfigurarIntentosCompra(modelo);
    }

    private static void ConfigurarIntentosCompra(ModelBuilder modelo)
    {
        var intento = modelo.Entity<IntentoCompra>();
        intento.ToTable("intento_compra", t => t.HasCheckConstraint("ck_intento_compra_plazo", "hold_vence_utc > creado_utc"));
        intento.HasKey(i => i.Id);
        intento.Property(i => i.Id).HasColumnName("intento_id").HasMaxLength(8)
            .ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).ValueGeneratedNever();
        intento.Property(i => i.UsuarioId).HasColumnName("usuario_id").HasMaxLength(8)
            .ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).IsRequired();
        intento.Property(i => i.CreadoUtc).HasColumnName("creado_utc").HasColumnType("datetime(6)").IsRequired()
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        intento.Property(i => i.HoldVenceUtc).HasColumnName("hold_vence_utc").HasColumnType("datetime(6)").IsRequired()
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        intento.HasOne<Usuario>().WithMany().HasForeignKey(i => i.UsuarioId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_intento_compra_usuario");

        var seleccion = modelo.Entity<TramoIntentoCompra>();
        seleccion.ToTable("tramo_intento_compra", t =>
        {
            t.HasCheckConstraint("ck_tramo_intento_orden", "orden >= 1");
            t.HasCheckConstraint("ck_tramo_intento_importe", "importe >= 0");
            t.HasCheckConstraint("ck_tramo_intento_llegada", "llegada > salida");
            t.HasCheckConstraint("ck_tramo_intento_asiento", "(numero_asiento IS NULL AND salida_id IS NULL AND orden_embarque IS NULL AND orden_desembarque IS NULL) OR (numero_asiento IS NOT NULL AND salida_id IS NOT NULL AND orden_embarque IS NOT NULL AND orden_desembarque IS NOT NULL AND orden_embarque < orden_desembarque)");
        });
        seleccion.HasKey(s => new { s.IntentoId, s.Orden });
        seleccion.Property(s => s.IntentoId).HasColumnName("intento_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii);
        seleccion.Property(s => s.Orden).HasColumnName("orden");
        seleccion.Property(s => s.ServicioId).HasColumnName("servicio_id").HasMaxLength(36).IsRequired();
        seleccion.Property(s => s.TramoId).HasColumnName("tramo_id").HasMaxLength(36).IsRequired();
        seleccion.Property(s => s.OrigenId).HasColumnName("origen_id").HasMaxLength(36).IsRequired();
        seleccion.Property(s => s.OrigenNombre).HasColumnName("origen_nombre").HasMaxLength(100).IsRequired();
        seleccion.Property(s => s.DestinoId).HasColumnName("destino_id").HasMaxLength(36).IsRequired();
        seleccion.Property(s => s.DestinoNombre).HasColumnName("destino_nombre").HasMaxLength(100).IsRequired();
        seleccion.Property(s => s.Salida).HasColumnName("salida").HasColumnType("datetime(6)").IsRequired();
        seleccion.Property(s => s.Llegada).HasColumnName("llegada").HasColumnType("datetime(6)").IsRequired();
        seleccion.Property(s => s.Importe).HasColumnName("importe").HasPrecision(18, 2).IsRequired();
        seleccion.Property(s => s.PoliticaAsiento).HasColumnName("politica_asiento").HasConversion<string>().HasMaxLength(24).IsRequired();
        seleccion.Property(s => s.NumeroAsiento).HasColumnName("numero_asiento");
        seleccion.Property(s => s.SalidaId).HasColumnName("salida_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii);
        seleccion.Property(s => s.OrdenEmbarque).HasColumnName("orden_embarque");
        seleccion.Property(s => s.OrdenDesembarque).HasColumnName("orden_desembarque");
        seleccion.HasOne<IntentoCompra>().WithMany().HasForeignKey(s => s.IntentoId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tramo_intento_intento");
        seleccion.HasOne<Servicio>().WithMany().HasForeignKey(s => s.ServicioId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tramo_intento_servicio");
        seleccion.HasOne<Salida>().WithMany().HasForeignKey(s => s.SalidaId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tramo_intento_salida");
        seleccion.HasIndex(s => s.ServicioId).HasDatabaseName("ix_tramo_intento_servicio");

        var hold = modelo.Entity<HoldAsiento>();
        hold.ToTable("hold_asiento", t =>
        {
            t.HasCheckConstraint("ck_hold_asiento_numero", "numero_asiento >= 1");
            t.HasCheckConstraint("ck_hold_asiento_intervalo", "orden_embarque >= 1 AND orden_embarque < orden_desembarque");
        });
        hold.HasKey(h => new { h.IntentoId, h.Orden });
        hold.Property(h => h.IntentoId).HasColumnName("intento_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii);
        hold.Property(h => h.Orden).HasColumnName("orden");
        hold.Property(h => h.SalidaId).HasColumnName("salida_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii);
        hold.Property(h => h.NumeroAsiento).HasColumnName("numero_asiento");
        hold.Property(h => h.OrdenEmbarque).HasColumnName("orden_embarque");
        hold.Property(h => h.OrdenDesembarque).HasColumnName("orden_desembarque");
        hold.HasOne<TramoIntentoCompra>().WithMany().HasForeignKey(h => new { h.IntentoId, h.Orden }).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_hold_asiento_seleccion");
        hold.HasOne<AsientoSalida>().WithMany().HasForeignKey(h => new { h.SalidaId, h.NumeroAsiento }).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_hold_asiento_inventario");
        hold.HasOne<ParadaSalida>().WithMany().HasForeignKey(h => new { h.SalidaId, h.OrdenEmbarque }).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_hold_asiento_embarque");
        hold.HasOne<ParadaSalida>().WithMany().HasForeignKey(h => new { h.SalidaId, h.OrdenDesembarque }).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_hold_asiento_desembarque");
        hold.HasIndex(h => new { h.SalidaId, h.NumeroAsiento }).HasDatabaseName("ix_hold_asiento_inventario");

        ConfigurarPersistenciaPago(modelo);
    }

    private static void ConfigurarPersistenciaPago(ModelBuilder modelo)
    {
        var pago = modelo.Entity<Pago>();
        pago.ToTable("pago", t =>
        {
            t.HasCheckConstraint("ck_pago_id", $"pago_id REGEXP '{RangoId("PG-")}'");
            t.HasCheckConstraint("ck_pago_estado", "estado IN ('Pendiente', 'Desconocido', 'Aprobado', 'Rechazado', 'DevolucionPendiente', 'Devuelto')");
            t.HasCheckConstraint("ck_pago_importes", "importe_esperado > 0 AND importe_pagado >= 0 AND importe_pagado <= importe_esperado");
            t.HasCheckConstraint("ck_pago_moneda", "moneda = 'UYU'");
        });
        pago.HasKey(p => p.Id);
        pago.Property(p => p.Id).HasColumnName("pago_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).ValueGeneratedNever();
        pago.Property(p => p.IntentoId).HasColumnName("intento_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).IsRequired();
        pago.Property(p => p.ImporteEsperado).HasColumnName("importe_esperado").HasPrecision(10, 2).IsRequired();
        pago.Property(p => p.ImportePagado).HasColumnName("importe_pagado").HasPrecision(10, 2).IsRequired();
        pago.Property(p => p.Moneda).HasColumnName("moneda").HasMaxLength(3).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).IsRequired();
        pago.Property(p => p.Estado).HasColumnName("estado").HasConversion<string>().HasMaxLength(24).IsRequired();
        pago.Property(p => p.ClaveCreacion).HasColumnName("clave_creacion").HasColumnType("char(36)").ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).IsRequired();
        pago.Property(p => p.ClaveDevolucion).HasColumnName("clave_devolucion").HasColumnType("char(36)").ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii);
        pago.Property(p => p.OrdenProveedorId).HasColumnName("orden_proveedor_id").HasMaxLength(256);
        pago.Property(p => p.CheckoutUrl).HasColumnName("checkout_url").HasMaxLength(2048);
        ConfigurarUtc(pago.Property(p => p.CreadoUtc).HasColumnName("creado_utc").HasColumnType("datetime(6)").IsRequired());
        ConfigurarUtc(pago.Property(p => p.ActualizadoUtc).HasColumnName("actualizado_utc").HasColumnType("datetime(6)").IsRequired());
        ConfigurarUtcNullable(pago.Property(p => p.UltimaVerificacionUtc).HasColumnName("ultima_verificacion_utc").HasColumnType("datetime(6)"));
        ConfigurarUtcNullable(pago.Property(p => p.DevolucionSolicitadaUtc).HasColumnName("devolucion_solicitada_utc").HasColumnType("datetime(6)"));
        ConfigurarUtcNullable(pago.Property(p => p.DevolucionConfirmadaUtc).HasColumnName("devolucion_confirmada_utc").HasColumnType("datetime(6)"));
        pago.HasOne<IntentoCompra>().WithMany().HasForeignKey(p => p.IntentoId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_pago_intento");
        pago.HasAlternateKey(p => new { p.Id, p.IntentoId }).HasName("ak_pago_intento");
        pago.HasIndex(p => p.ClaveCreacion).IsUnique().HasDatabaseName("uq_pago_clave_creacion");
        pago.HasIndex(p => p.ClaveDevolucion).IsUnique().HasDatabaseName("uq_pago_clave_devolucion");
        pago.HasIndex(p => p.OrdenProveedorId).IsUnique().HasDatabaseName("uq_pago_orden_proveedor");
        pago.HasIndex(p => new { p.IntentoId, p.CreadoUtc }).HasDatabaseName("ix_pago_intento_creado");

        var tramoPago = modelo.Entity<TramoPago>();
        tramoPago.ToTable("tramo_pago", t =>
        {
            t.HasCheckConstraint("ck_tramo_pago_orden", "orden >= 1");
            t.HasCheckConstraint("ck_tramo_pago_importe", "importe >= 0");
        });
        tramoPago.HasKey(p => new { p.PagoId, p.Orden });
        tramoPago.Property(p => p.PagoId).HasColumnName("pago_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii);
        tramoPago.Property(p => p.IntentoId).HasColumnName("intento_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).IsRequired();
        tramoPago.Property(p => p.Orden).HasColumnName("orden").IsRequired();
        tramoPago.Property(p => p.Importe).HasColumnName("importe").HasPrecision(10, 2).IsRequired();
        tramoPago.HasOne<Pago>().WithMany().HasForeignKey(p => new { p.PagoId, p.IntentoId }).HasPrincipalKey(p => new { p.Id, p.IntentoId })
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tramo_pago_pago_intento");
        tramoPago.HasOne<TramoIntentoCompra>().WithMany().HasForeignKey(p => new { p.IntentoId, p.Orden })
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tramo_pago_seleccion");

        var compra = modelo.Entity<Compra>();
        compra.ToTable("compra", t =>
        {
            t.HasCheckConstraint("ck_compra_id", $"compra_id REGEXP '{RangoId("CO-")}'");
            t.HasCheckConstraint("ck_compra_importe", "importe_total > 0");
        });
        compra.HasKey(c => c.Id);
        compra.Property(c => c.Id).HasColumnName("compra_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).ValueGeneratedNever();
        compra.Property(c => c.IntentoId).HasColumnName("intento_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).IsRequired();
        compra.Property(c => c.PagoId).HasColumnName("pago_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).IsRequired();
        compra.Property(c => c.ImporteTotal).HasColumnName("importe_total").HasPrecision(10, 2).IsRequired();
        ConfigurarUtc(compra.Property(c => c.ConfirmadoUtc).HasColumnName("confirmado_utc").HasColumnType("datetime(6)").IsRequired());
        compra.HasOne<IntentoCompra>().WithMany().HasForeignKey(c => c.IntentoId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_compra_intento");
        compra.HasOne<Pago>().WithMany().HasForeignKey(c => new { c.PagoId, c.IntentoId }).HasPrincipalKey(p => new { p.Id, p.IntentoId })
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_compra_pago_intento");
        compra.HasAlternateKey(c => new { c.Id, c.IntentoId, c.PagoId }).HasName("ak_compra_intento_pago");
        compra.HasIndex(c => c.IntentoId).IsUnique().HasDatabaseName("uq_compra_intento");
        compra.HasIndex(c => c.PagoId).IsUnique().HasDatabaseName("uq_compra_pago");

        var compraServicio = modelo.Entity<CompraServicio>();
        compraServicio.ToTable("compra_servicio", t =>
        {
            t.HasCheckConstraint("ck_compra_servicio_id", $"compra_servicio_id REGEXP '{RangoId("CS-")}'");
            t.HasCheckConstraint("ck_compra_servicio_orden", "orden >= 1");
            t.HasCheckConstraint("ck_compra_servicio_importe", "importe >= 0");
        });
        compraServicio.HasKey(c => c.Id);
        compraServicio.Property(c => c.Id).HasColumnName("compra_servicio_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).ValueGeneratedNever();
        compraServicio.Property(c => c.CompraId).HasColumnName("compra_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).IsRequired();
        compraServicio.Property(c => c.IntentoId).HasColumnName("intento_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).IsRequired();
        compraServicio.Property(c => c.PagoId).HasColumnName("pago_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).IsRequired();
        compraServicio.Property(c => c.Orden).HasColumnName("orden").IsRequired();
        compraServicio.Property(c => c.Importe).HasColumnName("importe").HasPrecision(10, 2).IsRequired();
        compraServicio.HasOne<Compra>().WithMany().HasForeignKey(c => new { c.CompraId, c.IntentoId, c.PagoId }).HasPrincipalKey(c => new { c.Id, c.IntentoId, c.PagoId })
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_compra_servicio_compra_intento_pago");
        compraServicio.HasOne<TramoPago>().WithMany().HasForeignKey(c => new { c.PagoId, c.Orden })
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_compra_servicio_tramo_pago");
        compraServicio.HasIndex(c => new { c.CompraId, c.Orden }).IsUnique().HasDatabaseName("uq_compra_servicio_orden");

        var ticket = modelo.Entity<Ticket>();
        ticket.ToTable("ticket", t =>
        {
            t.HasCheckConstraint("ck_ticket_id", $"ticket_id REGEXP '{RangoId("TI-")}'");
            // MVP-005: la marca de uso y el cobrador que validó se escriben juntos o ninguno.
            t.HasCheckConstraint("ck_ticket_uso", "(usado_utc IS NULL AND validado_por_usuario_id IS NULL) OR (usado_utc IS NOT NULL AND validado_por_usuario_id IS NOT NULL)");
        });
        ticket.HasKey(t => t.Id);
        ticket.Property(t => t.Id).HasColumnName("ticket_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).ValueGeneratedNever();
        ticket.Property(t => t.CompraServicioId).HasColumnName("compra_servicio_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).IsRequired();
        ticket.Property(t => t.QrOpaco).HasColumnName("qr_opaco").HasMaxLength(128).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii).IsRequired();
        ConfigurarUtc(ticket.Property(t => t.EmitidoUtc).HasColumnName("emitido_utc").HasColumnType("datetime(6)").IsRequired());
        ConfigurarUtcNullable(ticket.Property(t => t.UsadoUtc).HasColumnName("usado_utc").HasColumnType("datetime(6)"));
        ticket.Property(t => t.ValidadoPorUsuarioId).HasColumnName("validado_por_usuario_id").HasMaxLength(8).ForMySQLHasCharset("ascii").ForMySQLHasCollation(Ascii);
        ticket.HasOne<Usuario>().WithMany().HasForeignKey(t => t.ValidadoPorUsuarioId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_ticket_validado_por_usuario");
        ticket.HasOne<CompraServicio>().WithMany().HasForeignKey(t => t.CompraServicioId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_ticket_compra_servicio");
        ticket.HasIndex(t => t.CompraServicioId).IsUnique().HasDatabaseName("uq_ticket_compra_servicio");
        ticket.HasIndex(t => t.QrOpaco).IsUnique().HasDatabaseName("uq_ticket_qr_opaco");
    }

    private static void ConfigurarUtc(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<DateTime> property) =>
        property.HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static void ConfigurarUtcNullable(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<DateTime?> property) =>
        property.HasConversion(new ValueConverter<DateTime?, DateTime?>(
            value => value,
            value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null));
}
