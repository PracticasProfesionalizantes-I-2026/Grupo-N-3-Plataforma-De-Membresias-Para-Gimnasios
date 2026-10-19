using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GymPlatform.BusinessLogic.Interfaces;
using GymPlatform.DataAccess.Entities;
using GymPlatform.DataAccess.Repositories;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.Enums;
using GymPlatform.Shared.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace GymPlatform.BusinessLogic.Services;

/// <summary>
/// Servicio de autenticación y autorización basado en JWT para GymUp.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUsuarioRepository usuarioRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _usuarioRepository = usuarioRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResponseDTO> LoginAsync(LoginRequestDTO request)
    {
        if (request == null)
        {
            throw new ValidationException("La solicitud de login no puede ser nula.");
        }

        if (string.IsNullOrWhiteSpace(request.NombreUsuario))
        {
            throw new ValidationException("El nombre de usuario es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException("La contraseña es obligatoria.");
        }

        var usuario = await _usuarioRepository.GetByNombreUsuarioAsync(request.NombreUsuario.Trim())
                      ?? await _usuarioRepository.GetByEmailAsync(request.NombreUsuario.Trim());

        if (usuario == null || !usuario.Activo)
        {
            _logger.LogWarning("Intento de login fallido: usuario no encontrado o inactivo ({Usuario})", request.NombreUsuario);
            throw new ValidationException("Credenciales incorrectas o usuario inactivo.");
        }

        bool passwordValido = false;
        try
        {
            passwordValido = BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar hash de contraseña para {Usuario}", request.NombreUsuario);
            throw new AuthenticationException("Error interno al verificar las credenciales.", ex);
        }

        if (!passwordValido)
        {
            _logger.LogWarning("Intento de login fallido: contraseña incorrecta para {Usuario}", request.NombreUsuario);
            throw new ValidationException("Credenciales incorrectas o usuario inactivo.");
        }

        var (token, expiracion) = GenerarJwtToken(usuario);

        _logger.LogInformation("Usuario autenticado exitosamente: {Usuario} con Rol {Rol}", usuario.NombreUsuario, usuario.Rol);

        return new LoginResponseDTO(
            token,
            usuario.NombreUsuario,
            usuario.NombreCompleto,
            usuario.Rol.ToString(),
            expiracion
        );
    }

    public async Task<UsuarioResponseDTO> RegistrarUsuarioAsync(UsuarioCreateDTO request)
    {
        if (request == null)
        {
            throw new ValidationException("La solicitud de registro no puede ser nula.");
        }

        if (string.IsNullOrWhiteSpace(request.NombreUsuario))
        {
            throw new ValidationException("El nombre de usuario es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ValidationException("El email es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            throw new ValidationException("La contraseña debe tener al menos 6 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(request.NombreCompleto))
        {
            throw new ValidationException("El nombre completo es obligatorio.");
        }

        if (!Enum.TryParse<RolUsuario>(request.Rol, true, out var rolUsuario))
        {
            throw new ValidationException($"El rol '{request.Rol}' no es válido. Roles válidos: {string.Join(", ", Enum.GetNames<RolUsuario>())}.");
        }

        if (await _usuarioRepository.ExisteNombreUsuarioAsync(request.NombreUsuario.Trim()))
        {
            throw new DuplicateResourceException($"El nombre de usuario '{request.NombreUsuario}' ya se encuentra registrado.");
        }

        var usuarioExistenteEmail = await _usuarioRepository.GetByEmailAsync(request.Email.Trim());
        if (usuarioExistenteEmail != null)
        {
            throw new DuplicateResourceException($"El email '{request.Email}' ya se encuentra registrado.");
        }

        var nuevoUsuario = new Usuario
        {
            Id = Guid.NewGuid(),
            NombreUsuario = request.NombreUsuario.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Rol = rolUsuario,
            NombreCompleto = request.NombreCompleto.Trim(),
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        var creado = await _usuarioRepository.CreateAsync(nuevoUsuario);

        return new UsuarioResponseDTO(
            creado.Id,
            creado.NombreUsuario,
            creado.Email,
            creado.NombreCompleto,
            creado.Rol.ToString(),
            creado.Activo,
            creado.FechaCreacion
        );
    }

    private (string token, DateTime expiracion) GenerarJwtToken(Usuario usuario)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var keyString = jwtSettings["Key"] 
            ?? throw new InvalidOperationException("La clave JWT (Jwt:Key) no está configurada en appsettings.");
        var issuer = jwtSettings["Issuer"] ?? "GymPlatformAPI";
        var audience = jwtSettings["Audience"] ?? "GymPlatformClient";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiracion = DateTime.UtcNow.AddHours(8);

        // Se incluye el rol del usuario como un claim mediante new Claim(ClaimTypes.Role, usuario.Rol)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.NombreUsuario),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Rol.ToString()),
            new Claim("NombreCompleto", usuario.NombreCompleto),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiracion,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(securityToken);

        return (tokenString, expiracion);
    }
}
