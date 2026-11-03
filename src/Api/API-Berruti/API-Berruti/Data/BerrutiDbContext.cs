using API_Berruti.Models.Authentication;
using Microsoft.EntityFrameworkCore;

namespace API_Berruti.Data;

public class BerrutiDbContext : DbContext
{
    public BerrutiDbContext(DbContextOptions<BerrutiDbContext> opciones) : base(opciones) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Sesion> Sesiones => Set<Sesion>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<UsuarioRol> UsuariosRoles => Set<UsuarioRol>();
    public DbSet<ContadorEntidad> Contadores => Set<ContadorEntidad>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        var usuario = modelo.Entity<Usuario>();
        usuario.ToTable("usuario");
        usuario.HasKey(u => u.Identificador);
        usuario.Property(u => u.Identificador).HasColumnName("usuario_id").HasMaxLength(8).ValueGeneratedNever();
        usuario.Property(u => u.IdentificadorPublico).HasColumnName("identificador_publico").HasMaxLength(43).IsRequired();
        usuario.HasIndex(u => u.IdentificadorPublico).IsUnique();
        usuario.Property(u => u.Correo).HasColumnName("correo").HasMaxLength(254).IsRequired();
        usuario.HasIndex(u => u.Correo).IsUnique();
        usuario.Property(u => u.Contrasena).HasColumnName("contrasena").HasMaxLength(255).IsRequired();
        usuario.Property(u => u.Activo).HasColumnName("activo");

        var rol = modelo.Entity<Rol>();
        rol.ToTable("rol");
        rol.HasKey(r => r.Identificador);
        rol.Property(r => r.Identificador).HasColumnName("rol_id").HasMaxLength(8).ValueGeneratedNever();
        rol.Property(r => r.Nombre).HasColumnName("nombre").HasMaxLength(13).IsRequired();
        rol.HasIndex(r => r.Nombre).IsUnique();

        var membresia = modelo.Entity<UsuarioRol>();
        membresia.ToTable("usuario_rol");
        membresia.HasKey(m => new { m.UsuarioId, m.RolId });
        membresia.Property(m => m.UsuarioId).HasColumnName("usuario_id").HasMaxLength(8);
        membresia.Property(m => m.RolId).HasColumnName("rol_id").HasMaxLength(8);
        membresia.HasOne<Usuario>().WithMany().HasForeignKey(m => m.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        membresia.HasOne<Rol>().WithMany().HasForeignKey(m => m.RolId).OnDelete(DeleteBehavior.Restrict);

        var sesion = modelo.Entity<Sesion>();
        sesion.ToTable("sesion");
        sesion.HasKey(s => s.Huella);
        sesion.Property(s => s.Huella).HasColumnName("huella").HasMaxLength(64).ValueGeneratedNever();
        sesion.Property(s => s.UsuarioId).HasColumnName("usuario_id").HasMaxLength(8);
        sesion.Property(s => s.CreadaUtc).HasColumnName("creada_utc").HasColumnType("datetime(6)");
        sesion.Property(s => s.VenceUtc).HasColumnName("vence_utc").HasColumnType("datetime(6)");
        sesion.Property(s => s.RevocadaUtc).HasColumnName("revocada_utc").HasColumnType("datetime(6)");
        sesion.HasOne<Usuario>().WithMany().HasForeignKey(s => s.UsuarioId).OnDelete(DeleteBehavior.Restrict);

        var contador = modelo.Entity<ContadorEntidad>();
        contador.ToTable("contador_entidad");
        contador.HasKey(c => c.Prefijo);
        contador.Property(c => c.Prefijo).HasColumnName("prefijo").HasMaxLength(3).ValueGeneratedNever();
        contador.Property(c => c.Ultimo).HasColumnName("ultimo");
    }
}
