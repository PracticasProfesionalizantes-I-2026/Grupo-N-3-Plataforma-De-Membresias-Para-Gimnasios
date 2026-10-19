using System.Security.Claims;
using System.Text;
using GymPlatform.BusinessLogic.ExternalServices;
using GymPlatform.BusinessLogic.Interfaces;
using GymPlatform.BusinessLogic.Services;
using GymPlatform.BusinessLogic.UseCases.CU01_RegistrarSocio;
using GymPlatform.BusinessLogic.UseCases.CU02_GestionarPlanes;
using GymPlatform.BusinessLogic.UseCases.CU03_RenovarMembresia;
using GymPlatform.BusinessLogic.UseCases.CU04_GestionarClasesHorarios;
using GymPlatform.DataAccess.Context;
using GymPlatform.DataAccess.Data;
using GymPlatform.DataAccess.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();

// Inyección de Dependencias por Constructor (BusinessLogic / Services)
builder.Services.AddScoped<ISocioService, SocioService>();
builder.Services.AddScoped<IPlanMembresiaService, PlanMembresiaService>();
builder.Services.AddScoped<IActividadService, ActividadService>();
builder.Services.AddScoped<IHorarioService, HorarioService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Repositorios adicionales requeridos por los Casos de Uso (CU-01, CU-03)
builder.Services.AddScoped<IMembresiaRepository, MembresiaRepository>();
builder.Services.AddScoped<IPagoRepository, PagoRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Servicios externos simulados (Notificación y Pasarela de Pago)
// En producción: reemplazar por implementaciones reales (SMTP, Stripe, MercadoPago, etc.)
builder.Services.AddScoped<INotificacionService, SimulatedEmailNotificacionService>();
builder.Services.AddScoped<IPasarelaPagoService, SimulatedPasarelaPagoService>();

// ── Casos de Uso (Use Cases) ──────────────────────────────────────────────────
// CU-01: Registrar Socio (con membresía inicial y notificación de bienvenida resiliente)
builder.Services.AddScoped<IRegistrarSocioUseCase, RegistrarSocioUseCase>();
// CU-02: Gestionar Planes de Membresía (CRUD con validación defensiva y restricciones amigables)
builder.Services.AddScoped<IGestionarPlanesUseCase, GestionarPlanesUseCase>();
// CU-03: Renovar Membresía (pago transaccional con pasarela externa y manejo de fallos)
builder.Services.AddScoped<IRenovarMembresiaUseCase, RenovarMembresiaUseCase>();
// CU-04: Gestionar Clases y Horarios (con detección de solapamiento RN-03)
builder.Services.AddScoped<IGestionarClasesHorariosUseCase, GestionarClasesHorariosUseCase>();

// Configuración de Autenticación y Autorización basada en Roles mediante Tokens JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] 
        ?? throw new InvalidOperationException("La clave JWT (Jwt:Key) no está configurada."));

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        RoleClaimType = ClaimTypes.Role, // Configuración explícita del RoleClaimType
        NameClaimType = ClaimTypes.Name
    };
});
builder.Services.AddAuthorization();

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

// Orden estricto de middlewares de seguridad: UseAuthentication ANTES de UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
