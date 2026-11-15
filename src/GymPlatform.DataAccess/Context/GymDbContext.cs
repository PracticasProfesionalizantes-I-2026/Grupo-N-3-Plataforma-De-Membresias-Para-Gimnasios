using GymPlatform.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymPlatform.DataAccess.Context;

public class GymDbContext : DbContext
{
    public GymDbContext(DbContextOptions<GymDbContext> options) : base(options)
    {
    }

    public DbSet<Socio> Socios => Set<Socio>();
    public DbSet<PlanMembresia> PlanesMembresia => Set<PlanMembresia>();
    public DbSet<Actividad> Actividades => Set<Actividad>();
    public DbSet<Horario> Horarios => Set<Horario>();
    public DbSet<Membresia> Membresias => Set<Membresia>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Reserva> Reservas => Set<Reserva>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Socio configuration con intercalación NOCASE para búsquedas optimizadas sin LOWER()
        modelBuilder.Entity<Socio>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100).UseCollation("NOCASE");
            entity.Property(e => e.Dni).IsRequired().HasMaxLength(8);
            entity.HasIndex(e => e.Dni).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150).UseCollation("NOCASE");
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Telefono).HasMaxLength(30);
            entity.Property(e => e.Activo).HasDefaultValue(true);
        });

        // PlanMembresia configuration con intercalación NOCASE
        modelBuilder.Entity<PlanMembresia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100).UseCollation("NOCASE");
            entity.HasIndex(e => e.Nombre).IsUnique();
            entity.Property(e => e.Descripcion).HasMaxLength(250);
            entity.Property(e => e.Precio).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Activo).HasDefaultValue(true);
        });

        // Actividad configuration con intercalación NOCASE
        modelBuilder.Entity<Actividad>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100).UseCollation("NOCASE");
            entity.HasIndex(e => e.Nombre).IsUnique();
            entity.Property(e => e.Descripcion).HasMaxLength(250);
        });

        // Horario configuration con índices compuestos e intercalación NOCASE
        modelBuilder.Entity<Horario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Profesor).IsRequired().HasMaxLength(100).UseCollation("NOCASE");
            entity.Property(e => e.Sala).IsRequired().HasMaxLength(50).UseCollation("NOCASE");

            // Índices compuestos para optimizar búsquedas por profesor/sala y día
            entity.HasIndex(e => new { e.Profesor, e.DiaSemana });
            entity.HasIndex(e => new { e.Sala, e.DiaSemana });

            entity.HasOne(e => e.Actividad)
                  .WithMany(a => a.Horarios)
                  .HasForeignKey(e => e.ActividadId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PlanMembresia)
                  .WithMany(p => p.Horarios)
                  .HasForeignKey(e => e.PlanMembresiaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Membresia configuration
        modelBuilder.Entity<Membresia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Activo).HasDefaultValue(true);

            entity.HasOne(e => e.Socio)
                  .WithMany(s => s.Membresias)
                  .HasForeignKey(e => e.SocioId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PlanMembresia)
                  .WithMany(p => p.Membresias)
                  .HasForeignKey(e => e.PlanMembresiaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Pago configuration
        modelBuilder.Entity<Pago>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Monto).HasColumnType("decimal(18,2)");
            entity.Property(e => e.NumeroComprobante).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.NumeroComprobante).IsUnique();
            entity.Property(e => e.TransaccionExternaId).HasMaxLength(100);

            entity.HasOne(e => e.Membresia)
                  .WithMany(m => m.Pagos)
                  .HasForeignKey(e => e.MembresiaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Usuario configuration
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NombreUsuario).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.NombreUsuario).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.NombreCompleto).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Rol).IsRequired().HasConversion<string>();
        });

        // Reserva configuration con Restrict DeleteBehavior
        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FechaTurno).IsRequired();
            entity.Property(e => e.Asistencia).HasDefaultValue(false);

            entity.HasOne(e => e.Socio)
                  .WithMany(s => s.Reservas)
                  .HasForeignKey(e => e.SocioId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Horario)
                  .WithMany(h => h.Reservas)
                  .HasForeignKey(e => e.HorarioId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.HorarioId, e.FechaTurno });
        });
    }
}
