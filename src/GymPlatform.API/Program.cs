using GymPlatform.BusinessLogic.Interfaces;
using GymPlatform.BusinessLogic.Services;
using GymPlatform.DataAccess.Context;
using GymPlatform.DataAccess.Data;
using GymPlatform.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configuración de EF Core SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? "Data Source=gymplatform.db";

builder.Services.AddDbContext<GymDbContext>(options =>
    options.UseSqlite(connectionString, b => b.MigrationsAssembly("GymPlatform.Migrations")));

// Inyección de Dependencias por Constructor (DataAccess / Repositories)
builder.Services.AddScoped<ISocioRepository, SocioRepository>();
builder.Services.AddScoped<IPlanMembresiaRepository, PlanMembresiaRepository>();
builder.Services.AddScoped<IActividadRepository, ActividadRepository>();
builder.Services.AddScoped<IHorarioRepository, HorarioRepository>();

// Inyección de Dependencias por Constructor (BusinessLogic / Services)
builder.Services.AddScoped<ISocioService, SocioService>();
builder.Services.AddScoped<IPlanMembresiaService, PlanMembresiaService>();
builder.Services.AddScoped<IActividadService, ActividadService>();
builder.Services.AddScoped<IHorarioService, HorarioService>();

// Controladores y OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Inicialización de la base de datos y seeding de datos de prueba al arranque
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GymDbContext>();
    await DbInitializer.InitializeAsync(context);
}

// Configuración del pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("GYM UP API Documentation")
               .WithTheme(ScalarTheme.Moon);
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
