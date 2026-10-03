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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Socio configuration
        modelBuilder.Entity<Socio>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Dni).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.Dni).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Telefono).HasMaxLength(30);
            entity.Property(e => e.Activo).HasDefaultValue(true);
        });

        // PlanMembresia configuration
        modelBuilder.Entity<PlanMembresia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Nombre).IsUnique();
            entity.Property(e => e.Descripcion).HasMaxLength(250);
            entity.Property(e => e.Precio).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Activo).HasDefaultValue(true);
        });

        // Actividad configuration
        modelBuilder.Entity<Actividad>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Nombre).IsUnique();
            entity.Property(e => e.Descripcion).HasMaxLength(250);
        });

        // Horario configuration with Restrict DeleteBehavior
        modelBuilder.Entity<Horario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Profesor).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Sala).IsRequired().HasMaxLength(50);

            entity.HasOne(e => e.Actividad)
                  .WithMany(a => a.Horarios)
                  .HasForeignKey(e => e.ActividadId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PlanMembresia)
                  .WithMany(p => p.Horarios)
                  .HasForeignKey(e => e.PlanMembresiaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
