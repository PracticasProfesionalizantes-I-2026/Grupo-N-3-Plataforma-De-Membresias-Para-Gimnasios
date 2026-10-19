using GymPlatform.BusinessLogic.UseCases.CU03_RenovarMembresia;
using GymPlatform.Shared.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace GymPlatform.API.Controllers.UseCases;

/// <summary>
/// CU-03: Renovar Membresía.
/// Permite a un socio o administrador procesar el pago de renovación a través de una pasarela
/// de pago externa, con manejo robusto de fallos de red, timeout y rechazo de tarjeta.
/// Las operaciones críticas (pago + actualización de membresía) se ejecutan bajo transacción.
/// </summary>
[ApiController]
[Route("api/use-cases/membresias")]
[Produces("application/json")]
[Tags("CU-03: Renovar Membresía")]
public class CU03_RenovarMembresiaController : ControllerBase
{
    private readonly IRenovarMembresiaUseCase _useCase;
    private readonly ILogger<CU03_RenovarMembresiaController> _logger;

    public CU03_RenovarMembresiaController(
        IRenovarMembresiaUseCase useCase,
        ILogger<CU03_RenovarMembresiaController> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el resumen de todas las membresías de un socio (CU-03 - Flujo Principal: paso 1).
    /// Permite visualizar los planes disponibles y el estado de vencimiento para seleccionar cuál renovar.
    /// </summary>
    /// <param name="socioId">Identificador GUID del socio.</param>
    [HttpGet("socio/{socioId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<MembresiaResumenDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObtenerMembresiasPorSocio(Guid socioId)
    {
        try
        {
            var resultado = await _useCase.ObtenerMembresiasPorSocioAsync(socioId);

            if (resultado.IsFailure)
            {
                bool noEncontrado = resultado.Message.Contains("No se encontró", StringComparison.OrdinalIgnoreCase);
                if (noEncontrado)
                {
                    return NotFound(new { error = resultado.Message });
                }
                _logger.LogError("CU-03 error al consultar membresías del socio {SocioId}: {Mensaje}", socioId, resultado.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = resultado.Message });
            }

            return Ok(new { data = resultado.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CU-03 error inesperado al consultar membresías del socio {SocioId}.", socioId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error interno inesperado al consultar las membresías del socio." });
        }
    }

    /// <summary>
    /// Procesa la renovación de membresía con pago mediante pasarela externa (CU-03 - Flujo Principal: pasos 2-4).
    /// Maneja de forma segura:
    /// - Rechazo de pago por la pasarela (422 Unprocessable Entity)
    /// - Fallos de red / timeout al conectar con la pasarela (503 Service Unavailable)
    /// - Membresía o socio no encontrado (404 Not Found)
    /// - Datos de tarjeta inválidos (400 Bad Request)
    /// Si el pago fue aprobado pero falla el envío del comprobante por correo, la operación
    /// igual se considera exitosa y se devuelve un aviso (warning) en la respuesta.
    /// </summary>
    /// <param name="request">Solicitud de renovación: SocioId, PlanId, método de pago y datos de tarjeta opcionales.</param>
    [HttpPost("renovar")]
    [ProducesResponseType(typeof(RenovacionResultDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RenovarMembresia([FromBody] RenovarMembresiaRequestDTO request)
    {
        try
        {
            var resultado = await _useCase.ExecuteAsync(request);

            if (resultado.IsFailure)
            {
                string mensaje = resultado.Message;

                // CU-03 FA: Membresía o socio no encontrado
                if (mensaje.Contains("no existe", StringComparison.OrdinalIgnoreCase)
                    || mensaje.Contains("No se encontró", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { error = mensaje });
                }

                // CU-03 FA: Datos de pago inválidos (tarjeta, CVV, etc.)
                if (mensaje.Contains("tarjeta", StringComparison.OrdinalIgnoreCase)
                    || mensaje.Contains("titular", StringComparison.OrdinalIgnoreCase)
                    || mensaje.Contains("CVV", StringComparison.OrdinalIgnoreCase)
                    || mensaje.Contains("mes de vencimiento", StringComparison.OrdinalIgnoreCase)
                    || mensaje.Contains("datos completos", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("CU-03 datos de tarjeta inválidos: {Mensaje}", mensaje);
                    return BadRequest(new { error = mensaje, errores = resultado.Errors });
                }

                // CU-03 FA: Fallo de red / timeout de la pasarela externa
                if (mensaje.Contains("conexión", StringComparison.OrdinalIgnoreCase)
                    || mensaje.Contains("Timeout", StringComparison.OrdinalIgnoreCase)
                    || mensaje.Contains("inaccesible", StringComparison.OrdinalIgnoreCase)
                    || mensaje.Contains("tiempo de espera agotado", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("CU-03 fallo de conectividad con pasarela de pago: {Mensaje}", mensaje);
                    return StatusCode(StatusCodes.Status503ServiceUnavailable,
                        new { error = mensaje, retry = true });
                }

                // CU-03 FA: Pago rechazado por la pasarela (tarjeta sin fondos, expirada, etc.)
                if (mensaje.Contains("pago no pudo ser procesado", StringComparison.OrdinalIgnoreCase)
                    || mensaje.Contains("rechazado", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("CU-03 pago rechazado: {Mensaje}", mensaje);
                    return UnprocessableEntity(new { error = mensaje });
                }

                _logger.LogError("CU-03 error en renovación de membresía: {Mensaje}", mensaje);
                return BadRequest(new { error = mensaje, errores = resultado.Errors });
            }

            if (!string.IsNullOrWhiteSpace(resultado.WarningMessage))
            {
                _logger.LogWarning("CU-03 advertencia de comprobante: {Warning}", resultado.WarningMessage);
            }

            _logger.LogInformation("CU-03 membresía renovada exitosamente. Comprobante: {Comprobante}", resultado.Value?.NumeroComprobante);

            return Ok(new
            {
                message = resultado.Message,
                warning = resultado.WarningMessage,
                data = resultado.Value
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CU-03 error inesperado al procesar la renovación de membresía.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error interno inesperado al procesar la renovación de membresía. Por favor, contacte al administrador." });
        }
    }
}
