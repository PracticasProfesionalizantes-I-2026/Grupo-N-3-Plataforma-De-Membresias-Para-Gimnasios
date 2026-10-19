using GymPlatform.BusinessLogic.UseCases.CU02_GestionarPlanes;
using GymPlatform.Shared.Common;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace GymPlatform.API.Controllers.UseCases;

/// <summary>
/// CU-02: Gestionar Planes de Membresía.
/// Permite al administrador listar, crear, modificar y eliminar planes de membresía
/// con validaciones defensivas y manejo amigable de restricciones de integridad referencial.
/// </summary>
[ApiController]
[Route("api/use-cases/planes")]
[Produces("application/json")]
[Tags("CU-02: Gestionar Planes de Membresía")]
public class CU02_GestionarPlanesController : ControllerBase
{
    private readonly IGestionarPlanesUseCase _useCase;
    private readonly ILogger<CU02_GestionarPlanesController> _logger;

    public CU02_GestionarPlanesController(
        IGestionarPlanesUseCase useCase,
        ILogger<CU02_GestionarPlanesController> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos los planes de membresía disponibles con estadísticas de uso (CU-02 - Flujo Principal: paso 1).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PlanMembresiaResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListarPlanes()
    {
        try
        {
            var resultado = await _useCase.ListarPlanesAsync();

            if (resultado.IsFailure)
            {
                _logger.LogError("CU-02 error al listar planes: {Mensaje}", resultado.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = resultado.Message });
            }

            return Ok(new { message = resultado.Message, data = resultado.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CU-02 error inesperado al listar planes.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error interno inesperado al obtener los planes de membresía." });
        }
    }

    /// <summary>
    /// Obtiene un plan de membresía por su identificador único (CU-02).
    /// </summary>
    /// <param name="id">Identificador GUID del plan.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PlanMembresiaResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObtenerPlan(Guid id)
    {
        try
        {
            var resultado = await _useCase.GetByIdAsync(id);

            if (resultado.IsFailure)
            {
                bool noEncontrado = resultado.Message.Contains("No se encontró", StringComparison.OrdinalIgnoreCase);
                if (noEncontrado)
                {
                    return NotFound(new { error = resultado.Message });
                }
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = resultado.Message });
            }

            return Ok(new { data = resultado.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CU-02 error inesperado al obtener plan {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error interno inesperado al consultar el plan de membresía." });
        }
    }

    /// <summary>
    /// Crea un nuevo plan de membresía (CU-02 - Flujo Principal: paso 2-4).
    /// Valida que el nombre no esté vacío, que el precio sea positivo y que la duración sea al menos 1 día.
    /// </summary>
    /// <param name="dto">Datos del plan a crear.</param>
    [HttpPost]
    [ProducesResponseType(typeof(PlanMembresiaResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CrearPlan([FromBody] PlanMembresiaCreateDTO dto)
    {
        try
        {
            var resultado = await _useCase.CrearPlanAsync(dto);

            if (resultado.IsFailure)
            {
                bool esConflicto = resultado.Message.Contains("Ya existe", StringComparison.OrdinalIgnoreCase);

                if (esConflicto)
                {
                    _logger.LogWarning("CU-02 conflicto al crear plan: {Mensaje}", resultado.Message);
                    return Conflict(new { error = resultado.Message, errores = resultado.Errors });
                }

                _logger.LogWarning("CU-02 validación fallida al crear plan: {Mensaje}", resultado.Message);
                return BadRequest(new { error = resultado.Message, errores = resultado.Errors });
            }

            _logger.LogInformation("CU-02 plan creado exitosamente: {Nombre}", resultado.Value?.Nombre);
            return CreatedAtAction(
                nameof(ObtenerPlan),
                new { id = resultado.Value?.Id },
                new { message = resultado.Message, data = resultado.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CU-02 error inesperado al crear plan.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error interno inesperado al crear el plan de membresía." });
        }
    }

    /// <summary>
    /// Modifica un plan de membresía existente (CU-02 - Flujo Principal: paso 2-4).
    /// Valida los mismos campos que la creación y verifica unicidad de nombre ante cambios.
    /// </summary>
    /// <param name="id">Identificador GUID del plan a modificar.</param>
    /// <param name="dto">Datos actualizados del plan.</param>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PlanMembresiaResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ModificarPlan(Guid id, [FromBody] PlanMembresiaUpdateDTO dto)
    {
        try
        {
            var resultado = await _useCase.ModificarPlanAsync(id, dto);

            if (resultado.IsFailure)
            {
                bool noEncontrado = resultado.Message.Contains("No se encontró", StringComparison.OrdinalIgnoreCase);
                bool esConflicto = resultado.Message.Contains("Ya existe", StringComparison.OrdinalIgnoreCase);

                if (noEncontrado) return NotFound(new { error = resultado.Message });
                if (esConflicto) return Conflict(new { error = resultado.Message, errores = resultado.Errors });

                _logger.LogWarning("CU-02 validación fallida al modificar plan {Id}: {Mensaje}", id, resultado.Message);
                return BadRequest(new { error = resultado.Message, errores = resultado.Errors });
            }

            _logger.LogInformation("CU-02 plan modificado exitosamente. ID: {Id}", id);
            return Ok(new { message = resultado.Message, data = resultado.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CU-02 error inesperado al modificar plan {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error interno inesperado al modificar el plan de membresía." });
        }
    }

    /// <summary>
    /// Elimina un plan de membresía (CU-02 - Flujo Alternativo: No se puede eliminar).
    /// Si el plan tiene membresías activas o horarios asociados, la eliminación es rechazada
    /// con un mensaje amigable SIN lanzar excepción de base de datos al cliente.
    /// </summary>
    /// <param name="id">Identificador GUID del plan a eliminar.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EliminarPlan(Guid id)
    {
        try
        {
            var resultado = await _useCase.EliminarPlanAsync(id);

            if (resultado.IsFailure)
            {
                bool noEncontrado = resultado.Message.Contains("No se encontró", StringComparison.OrdinalIgnoreCase);
                bool esRestriccion = resultado.Message.Contains("No se puede eliminar", StringComparison.OrdinalIgnoreCase)
                                  || resultado.Message.Contains("restricciones", StringComparison.OrdinalIgnoreCase);

                if (noEncontrado) return NotFound(new { error = resultado.Message });
                if (esRestriccion)
                {
                    _logger.LogWarning("CU-02 intento de eliminar plan con dependencias. ID: {Id}. Motivo: {Mensaje}", id, resultado.Message);
                    return Conflict(new { error = resultado.Message });
                }

                return StatusCode(StatusCodes.Status500InternalServerError, new { error = resultado.Message });
            }

            _logger.LogInformation("CU-02 plan eliminado exitosamente. ID: {Id}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CU-02 error inesperado al eliminar plan {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error interno inesperado al eliminar el plan de membresía." });
        }
    }
}
