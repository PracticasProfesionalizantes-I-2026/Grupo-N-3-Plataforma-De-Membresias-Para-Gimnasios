using System.Security.Claims;
using GymPlatform.BusinessLogic.Interfaces;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymPlatform.API.Controllers;

/// <summary>
/// Controlador de autenticación y gestión de accesos con tokens JWT.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Inicia sesión con credenciales de usuario y genera un token JWT con los roles correspondientes.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (GymException ex)
        {
            _logger.LogError(ex, "Error de sistema durante el login");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Registra un nuevo usuario en la plataforma. Requiere rol de Administrador.
    /// </summary>
    [HttpPost("registrar")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(UsuarioResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar([FromBody] UsuarioCreateDTO request)
    {
        try
        {
            var usuario = await _authService.RegistrarUsuarioAsync(request);
            return CreatedAtAction(nameof(Me), null, usuario);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (DuplicateResourceException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (GymException ex)
        {
            _logger.LogError(ex, "Error durante el registro de usuario");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Devuelve los datos del usuario autenticado a partir de los claims del token JWT.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var rol = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var usuario = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        return Ok(new
        {
            Id = id,
            NombreUsuario = usuario,
            Email = email,
            Rol = rol
        });
    }
}
