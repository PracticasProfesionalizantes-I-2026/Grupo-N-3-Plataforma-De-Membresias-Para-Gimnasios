using GymPlatform.DataAccess.Context;
using GymPlatform.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymPlatform.DataAccess.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(GymDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (await context.Socios.AnyAsync())
        {
            return; // Ya fue inicializada la base de datos con datos de prueba
        }

        // 1. Sembrado de Planes
        var planBasico = new PlanMembresia
        {
            Id = Guid.NewGuid(),
            Nombre = "Plan Básico Mensual",
            Descripcion = "Acceso a sala de musculación y cardio de lunes a viernes.",
            Precio = 25000.00m,
            DuracionDias = 30,
            Activo = true
        };

        var planFull = new PlanMembresia
        {
            Id = Guid.NewGuid(),
            Nombre = "Plan Full Black",
            Descripcion = "Acceso ilimitado a todas las sedes, clases grupales y musculación.",
            Precio = 45000.00m,
            DuracionDias = 30,
            Activo = true
        };

        var planAnual = new PlanMembresia
        {
            Id = Guid.NewGuid(),
            Nombre = "Plan Anual VIP",
            Descripcion = "Pase libre anual con descuento exclusivo y nutricionista incluido.",
            Precio = 420000.00m,
            DuracionDias = 365,
            Activo = true
        };

        await context.PlanesMembresia.AddRangeAsync(planBasico, planFull, planAnual);

        // 2. Sembrado de Actividades
        var spinning = new Actividad
        {
            Id = Guid.NewGuid(),
            Nombre = "Spinning Indoor",
            Descripcion = "Entrenamiento cardiovascular de alta intensidad en bicicleta estática."
        };

        var crossfit = new Actividad
        {
            Id = Guid.NewGuid(),
            Nombre = "Cross Training",
            Descripcion = "Ejercicios funcionales ejecutados a alta intensidad."
        };

        var yoga = new Actividad
        {
            Id = Guid.NewGuid(),
            Nombre = "Yoga & Flex",
            Descripcion = "Clase de estiramiento, flexibilidad, respiración y postura."
        };

        await context.Actividades.AddRangeAsync(spinning, crossfit, yoga);

        // 3. Sembrado de Socios de Prueba
        var socio1 = new Socio
        {
            Id = Guid.NewGuid(),
            Nombre = "Carlos Rodriguez",
            Dni = "35123456",
            Email = "carlos.rodriguez@email.com",
            Telefono = "+54 9 11 4433-2211",
            FechaNacimiento = new DateOnly(1990, 5, 14),
            FechaRegistro = DateTime.UtcNow.AddDays(-60),
            Activo = true
        };

        var socio2 = new Socio
        {
            Id = Guid.NewGuid(),
            Nombre = "Mariana Fernandez",
            Dni = "38765432",
            Email = "mariana.fernandez@email.com",
            Telefono = "+54 9 11 5566-7788",
            FechaNacimiento = new DateOnly(1995, 11, 23),
            FechaRegistro = DateTime.UtcNow.AddDays(-30),
            Activo = true
        };

        await context.Socios.AddRangeAsync(socio1, socio2);

        // 4. Sembrado de Horarios
        var horario1 = new Horario
        {
            Id = Guid.NewGuid(),
            ActividadId = spinning.Id,
            PlanMembresiaId = planFull.Id,
            Profesor = "Lucas Gomez",
            Sala = "Sala de Ciclismo 1",
            DiaSemana = DayOfWeek.Monday,
            HoraInicio = new TimeOnly(18, 0),
            HoraFin = new TimeOnly(19, 0),
            CupoMaximo = 25
        };

        var horario2 = new Horario
        {
            Id = Guid.NewGuid(),
            ActividadId = crossfit.Id,
            PlanMembresiaId = planFull.Id,
            Profesor = "Valeria Rios",
            Sala = "Box Funcional",
            DiaSemana = DayOfWeek.Wednesday,
            HoraInicio = new TimeOnly(19, 0),
            HoraFin = new TimeOnly(20, 0),
            CupoMaximo = 20
        };

        var horario3 = new Horario
        {
            Id = Guid.NewGuid(),
            ActividadId = yoga.Id,
            PlanMembresiaId = planBasico.Id,
            Profesor = "Sofia Paez",
            Sala = "Salón Zen",
            DiaSemana = DayOfWeek.Friday,
            HoraInicio = new TimeOnly(9, 0),
            HoraFin = new TimeOnly(10, 0),
            CupoMaximo = 15
        };

        await context.Horarios.AddRangeAsync(horario1, horario2, horario3);

        await context.SaveChangesAsync();
    }
}
