using GymPlatform.BusinessLogic.UseCases.CU01_RegistrarSocio;
using GymPlatform.Shared.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace GymPlatform.API.Controllers.UseCases;

/// <summary>
/// CU-01: Registrar Socio.
/// Permite dar de alta un nuevo socio con validación defensiva, unicidad de DNI/Email,
/// membresía inicial opcional y notificación de bienvenida asíncrona y resiliente.
/// </summary>
[ApiController]
[Route("api/use-cases/socios")]
[Produces("application/json")]
[Tags("CU-01: Registrar Socio")]
public class CU01_RegistrarSocioController : ControllerBase
{
    private readonly IRegistrarSocioUseCase _useCase;
    private readonly ILogger<CU01_RegistrarSocioController> _logger;

    public CU01_RegistrarSocioController(
        IRegistrarSocioUseCase useCase,
        ILogger<CU01_RegistrarSocioController> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    /// <summary>
    /// Registra un nuevo socio en el sistema (CU-01).
    /// Valida datos personales (DNI numérico, email, mayoría de edad),
    /// verifica unicidad en BD y, opcionalmente, crea la membresía inicial.
    /// La notificación de bienvenida se envía de forma segura sin revertir el registro ante fallos.
    /// </summary>
    /// <param name="request">Datos del socio a registrar (nombre, DNI, email, fecha de nacimiento, plan opcional).</param>
    /// <returns>Datos del socio registrado, incluyendo membresía y advertencia de notificación si aplica.</returns>
    [HttpPost("registrar")]
    [ProducesResponseType(typeof(SocioRegistradoResultDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegistrarSocio([FromBody] RegistrarSocioRequestDTO request)
    {
        try
        {
            var resultado = await _useCase.ExecuteAsync(request);

            if (resultado.IsFailure)
            {
                // Distinguimos conflicto (duplicado) de error de validación por el texto del mensaje
                bool esDuplicado = resultado.Message.Contains("ya se encuentra registrado", StringComparison.OrdinalIgnoreCase)
                                || resultado.Message.Contains("ya existe", StringComparison.OrdinalIgnoreCase);

                if (esDuplicado)
                {
                    _logger.LogWarning("CU-01 conflicto al registrar socio: {Mensaje}", resultado.Message);
                    return Conflict(new { error = resultado.Message, errores = resultado.Errors });
                }

                _logger.LogWarning("CU-01 validación fallida al registrar socio: {Mensaje}", resultado.Message);
                return BadRequest(new { error = resultado.Message, errores = resultado.Errors });
            }

            if (!string.IsNullOrWhiteSpace(resultado.WarningMessage))
            {
                _logger.LogWarning("CU-01 advertencia de notificación: {Warning}", resultado.WarningMessage);
            }

            _logger.LogInformation("CU-01 socio registrado exitosamente. ID: {SocioId}", resultado.Value?.SocioId);

            return CreatedAtAction(
                nameof(RegistrarSocio),
                new { },
                new
                {
                    message = resultado.Message,
                    warning = resultado.WarningMessage,
                    data = resultado.Value
                });
        }
        catch (Exception ex)
        {
            // Última línea de defensa: nunca debe llegar aquí, pero protegemos ante cualquier imprevisto
            _logger.LogError(ex, "CU-01 error inesperado al procesar el registro del socio.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Ocurrió un error interno inesperado. Por favor, contacte al administrador del sistema." });
        }
    }
}
