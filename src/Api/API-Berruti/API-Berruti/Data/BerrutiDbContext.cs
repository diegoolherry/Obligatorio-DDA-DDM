using API_Berruti.Models.Authentication;
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
    }
}
