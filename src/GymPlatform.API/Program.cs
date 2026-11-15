using System.Security.Claims;
using System.Text;
using GymPlatform.API.Handlers;
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
using Microsoft.OpenApi;
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
builder.Services.AddScoped<IMembresiaRepository, MembresiaRepository>();
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();
builder.Services.AddScoped<IPagoRepository, PagoRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Inyección de Dependencias por Constructor (BusinessLogic / Services)
builder.Services.AddScoped<ISocioService, SocioService>();
builder.Services.AddScoped<IPlanMembresiaService, PlanMembresiaService>();
builder.Services.AddScoped<IActividadService, ActividadService>();
builder.Services.AddScoped<IHorarioService, HorarioService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMembresiaService, MembresiaService>();
builder.Services.AddScoped<IReservaService, ReservaService>();

// Servicios externos simulados (Notificación y Pasarela de Pago)
builder.Services.AddScoped<INotificacionService, SimulatedEmailNotificacionService>();
builder.Services.AddScoped<IPasarelaPagoService, SimulatedPasarelaPagoService>();

// Casos de Uso (Use Cases)
builder.Services.AddScoped<IRegistrarSocioUseCase, RegistrarSocioUseCase>();
builder.Services.AddScoped<IGestionarPlanesUseCase, GestionarPlanesUseCase>();
builder.Services.AddScoped<IRenovarMembresiaUseCase, RenovarMembresiaUseCase>();
builder.Services.AddScoped<IGestionarClasesHorariosUseCase, GestionarClasesHorariosUseCase>();

// Issue #9: Centralizar Manejo de Excepciones Global (RFC 7807 ProblemDetails)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Issue #2: Configuración de Autenticación JWT y Roles
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
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.Name
    };
});
builder.Services.AddAuthorization();

// Controladores
builder.Services.AddControllers();

// Issue #2: Configurar esquema SecurityScheme Bearer en OpenAPI / Scalar UI
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "GYM UP API Documentation";
        document.Info.Version = "v1";
        document.Info.Description = "API RESTful backend construida con .NET 10 para la gestión integral de gimnasios.";

        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "Ingrese el token JWT Bearer en el formato: Bearer {token}",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = securityScheme;

        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        });

        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Manejador global de excepciones al inicio del pipeline HTTP
app.UseExceptionHandler();

// Inicialización de la base de datos y seeding de datos de prueba al arranque
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GymDbContext>();
    await DbInitializer.InitializeAsync(context);
}

// Configuración de OpenAPI y Scalar UI en Development
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
