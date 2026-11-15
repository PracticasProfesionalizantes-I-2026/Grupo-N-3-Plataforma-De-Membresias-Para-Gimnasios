using GymPlatform.BusinessLogic.UseCases.CU04_GestionarClasesHorarios;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace GymPlatform.API.Controllers.UseCases;

/// <summary>
/// CU-04: Gestionar Clases y Horarios.
/// Permite al administrador gestionar actividades (clases) y sus horarios programados.
/// Incluye validación de solapamiento temporal por profesor y sala (RN-03),
/// y restricciones de eliminación si existen registros vinculados.
/// </summary>
[ApiController]
[Route("api/use-cases/clases")]
[Produces("application/json")]
[Tags("CU-04: Gestionar Clases y Horarios")]
public class CU04_GestionarClasesHorariosController : ControllerBase
{
    private readonly IGestionarClasesHorariosUseCase _useCase;
    private readonly ILogger<CU04_GestionarClasesHorariosController> _logger;

    public CU04_GestionarClasesHorariosController(
        IGestionarClasesHorariosUseCase useCase,
        ILogger<CU04_GestionarClasesHorariosController> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    // ──────────────────────── ACTIVIDADES ────────────────────────

    /// <summary>
    /// Lista todas las actividades o clases registradas (CU-04 - Flujo Principal: paso 1).
    /// </summary>
    [HttpGet("actividades")]
    [ProducesResponseType(typeof(IEnumerable<ActividadResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListarActividades()
    {
        try
        {
            var resultado = await _useCase.ListarActividadesAsync();

            if (resultado.IsFailure)
            {
                _logger.LogError("CU-04 error al listar actividades: {Mensaje}", resultado.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = resultado.Message });
            }

            return Ok(new { data = resultado.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CU-04 error inesperado al listar actividades.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error interno inesperado al obtener el listado de actividades." });
        }
    }

    /// <summary>
    /// Crea una nueva actividad o clase (CU-04 - Flujo Principal: paso 3).
    /// Valida que el nombre no esté vacío y que no exista duplicado.
    /// </summary>
    /// <param name="dto">Datos de la actividad a registrar.</param>
    [HttpPost("actividades")]
    [ProducesResponseType(typeof(ActividadResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CrearActividad([FromBody] ActividadCreateDTO dto)
    {
        try
        {
            var resultado = await _useCase.CrearActividadAsync(dto);

            if (resultado.IsFailure)
            {
                bool esConflicto = resultado.Message.Contains("Ya existe", StringComparison.OrdinalIgnoreCase);
                if (esConflicto)
                {
                    _logger.LogWarning("CU-04 conflicto al crear actividad: {Mensaje}", resultado.Message);
                    return Conflict(new { error = resultado.Message });
                }

                _logger.LogWarning("CU-04 validación fallida al crear actividad: {Mensaje}", resultado.Message);
                return BadRequest(new { error = resultado.Message, errores = resultado.Errors });
            }

            _logger.LogInformation("CU-04 actividad creada: {Nombre}", resultado.Value?.Nombre);
            return CreatedAtAction(nameof(ListarActividades), new { },
                new { message = resultado.Message, data = resultado.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CU-04 error inesperado al crear actividad.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error interno inesperado al registrar la actividad." });
        }
    }

    /// <summary>
    /// Modifica una actividad existente (CU-04 - Flujo Principal: paso 3).
    /// </summary>
    /// <param name="id">Identificador GUID de la actividad.</param>
    /// <param name="dto">Datos actualizados de la actividad.</param>
    [HttpPut("actividades/{id:guid}")]
    [ProducesResponseType(typeof(ActividadResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ModificarActividad(Guid id, [FromBody] ActividadUpdateDTO dto)
    {
        try
        {
            var resultado = await _useCase.ModificarActividadAsync(id, dto);

            if (resultado.IsFailure)
            {
                bool noEncontrado = resultado.Message.Contains("No se encontró", StringComparison.OrdinalIgnoreCase);
                bool esConflicto = resultado.Message.Contains("Ya existe", StringComparison.OrdinalIgnoreCase);

                if (noEncontrado) return NotFound(new { error = resultado.Message });
                if (esConflicto) return Conflict(new { error = resultado.Message });

                _logger.LogWarning("CU-04 validación fallida al modificar actividad {Id}: {Mensaje}", id, resultado.Message);
                return BadRequest(new { error = resultado.Message, errores = resultado.Errors });
            }

            _logger.LogInformation("CU-04 actividad modificada. ID: {Id}", id);
            return Ok(new { message = resultado.Message, data = resultado.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CU-04 error inesperado al modificar actividad {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error interno inesperado al modificar la actividad." });
        }
    }

    /// <summary>
    /// Elimina una actividad (CU-04 - Flujo Alternativo: No se puede eliminar).
    /// Si la actividad tiene horarios asociados, la eliminación es rechazada con mensaje amigable (409).
    /// </summary>
    /// <param name="id">Identificador GUID de la actividad a eliminar.</param>
    [HttpDelete("actividades/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EliminarActividad(Guid id)
    {
        try
        {
            var resultado = await _useCase.EliminarActividadAsync(id);

            if (resultado.IsFailure)
            {
                bool noEncontrado = resultado.Message.Contains("No se encontró", StringComparison.OrdinalIgnoreCase);
                bool esRestriccion = resultado.Message.Contains("No se puede eliminar", StringComparison.OrdinalIgnoreCase)
                                  || resultado.Message.Contains("registros vinculados", StringComparison.OrdinalIgnoreCase);

                if (noEncontrado) return NotFound(new { error = resultado.Message });
                if (esRestriccion)
                {
                    _logger.LogWarning("CU-04 intento de eliminar actividad con dependencias. ID: {Id}.", id);
                    return Conflict(new { error = resultado.Message });
                }

                return StatusCode(StatusCodes.Status500InternalServerError, new { error = resultado.Message });
            }

            _logger.LogInformation("CU-04 actividad eliminada. ID: {Id}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CU-04 error inesperado al eliminar actividad {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error interno inesperado al eliminar la actividad." });
        }
    }

    // ──────────────────────── HORARIOS ────────────────────────

    /// <summary>
    /// Lista todos los horarios programados de clases (CU-04 - Flujo Principal: paso 1).
    /// </summary>
    [HttpGet("horarios")]
    [ProducesResponseType(typeof(IEnumerable<HorarioResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListarHorarios()
    {
        try
        {
            var resultado = await _useCase.ListarHorariosAsync();

            if (resultado.IsFailure)
            {
                _logger.LogError("CU-04 error al listar horarios: {Mensaje}", resultado.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = resultado.Message });
            }

            return Ok(new { data = resultado.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CU-04 error inesperado al listar horarios.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error interno inesperado al obtener el listado de horarios." });
        }
    }

    /// <summary>
    /// Obtiene un horario específico por su identificador único (CU-04).
    /// </summary>
    /// <param name="id">Identificador GUID del horario.</param>
    [HttpGet("horarios/{id:guid}")]
    [ProducesResponseType(typeof(HorarioResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObtenerHorario(Guid id)
    {
        try
        {
            var resultado = await _useCase.GetHorarioByIdAsync(id);

            if (resultado.IsFailure)
            {
                bool noEncontrado = resultado.Message.Contains("No se encontró", StringComparison.OrdinalIgnoreCase);
                if (noEncontrado) return NotFound(new { error = resultado.Message });
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = resultado.Message });
            }

            return Ok(new { data = resultado.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CU-04 error inesperado al obtener horario {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error interno inesperado al consultar el horario." });
        }
    }

    /// <summary>
    /// Crea un nuevo horario de clase (CU-04 - Flujo Principal: paso 3).
    /// Aplica las siguientes validaciones (RN-03):
    /// - Hora de inicio debe ser anterior a hora de fin.
    /// - Cupo máximo mayor a cero.
    /// - Verifica solapamiento de horario del mismo profesor en el mismo día.
    /// - Verifica solapamiento de uso de sala en el mismo día.
    /// </summary>
    /// <param name="dto">Datos del horario a programar.</param>
    [HttpPost("horarios")]
    [ProducesResponseType(typeof(HorarioResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CrearHorario([FromBody] HorarioCreateDTO dto)
    {
        try
        {
            var resultado = await _useCase.CrearHorarioAsync(dto);

            if (resultado.IsFailure)
            {
                bool esSolapamiento = resultado.Message.Contains("ya tiene asignada", StringComparison.OrdinalIgnoreCase)
                                   || resultado.Message.Contains("ya se encuentra ocupada", StringComparison.OrdinalIgnoreCase);

                if (esSolapamiento)
                {
                    _logger.LogWarning("CU-04 solapamiento de horario detectado: {Mensaje}", resultado.Message);
                    return Conflict(new { error = resultado.Message, errores = resultado.Errors });
                }

                _logger.LogWarning("CU-04 validación fallida al crear horario: {Mensaje}", resultado.Message);
                return BadRequest(new { error = resultado.Message, errores = resultado.Errors });
            }

            _logger.LogInformation("CU-04 horario creado. ID: {Id}", resultado.Value?.Id);
            return CreatedAtAction(
                nameof(ObtenerHorario),
                new { id = resultado.Value?.Id },
                new { message = resultado.Message, data = resultado.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CU-04 error inesperado al crear horario.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error interno inesperado al programar el horario." });
        }
    }

    /// <summary>
    /// Modifica un horario de clase existente (CU-04 - Flujo Principal: paso 3).
    /// Re-evalúa solapamiento de profesor y sala, excluyendo el propio horario editado.
    /// </summary>
    /// <param name="id">Identificador GUID del horario a modificar.</param>
    /// <param name="dto">Datos actualizados del horario.</param>
    [HttpPut("horarios/{id:guid}")]
    [ProducesResponseType(typeof(HorarioResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ModificarHorario(Guid id, [FromBody] HorarioUpdateDTO dto)
    {
        try
        {
            var resultado = await _useCase.ModificarHorarioAsync(id, dto);

            if (resultado.IsFailure)
            {
                bool noEncontrado = resultado.Message.Contains("No se encontró", StringComparison.OrdinalIgnoreCase);
                bool esSolapamiento = resultado.Message.Contains("ya tiene asignada", StringComparison.OrdinalIgnoreCase)
                                   || resultado.Message.Contains("ya se encuentra ocupada", StringComparison.OrdinalIgnoreCase);

                if (noEncontrado) return NotFound(new { error = resultado.Message });
                if (esSolapamiento)
                {
                    _logger.LogWarning("CU-04 solapamiento al modificar horario {Id}: {Mensaje}", id, resultado.Message);
                    return Conflict(new { error = resultado.Message, errores = resultado.Errors });
                }

                _logger.LogWarning("CU-04 validación fallida al modificar horario {Id}: {Mensaje}", id, resultado.Message);
                return BadRequest(new { error = resultado.Message, errores = resultado.Errors });
            }

            _logger.LogInformation("CU-04 horario modificado. ID: {Id}", id);
            return Ok(new { message = resultado.Message, data = resultado.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CU-04 error inesperado al modificar horario {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error interno inesperado al modificar el horario." });
        }
    }

    /// <summary>
    /// Elimina un horario de clase (CU-04 - Flujo Alternativo: No se puede eliminar).
    /// Si el horario tiene reservas o asistencias activas, la eliminación es rechazada con mensaje amigable (409).
    /// </summary>
    /// <param name="id">Identificador GUID del horario a eliminar.</param>
    [HttpDelete("horarios/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EliminarHorario(Guid id)
    {
        try
        {
            var resultado = await _useCase.EliminarHorarioAsync(id);

            if (resultado.IsFailure)
            {
                bool noEncontrado = resultado.Message.Contains("No se encontró", StringComparison.OrdinalIgnoreCase);
                bool esRestriccion = resultado.Message.Contains("No se puede eliminar", StringComparison.OrdinalIgnoreCase)
                                  || resultado.Message.Contains("reservas", StringComparison.OrdinalIgnoreCase)
                                  || resultado.Message.Contains("asistencias", StringComparison.OrdinalIgnoreCase);

                if (noEncontrado) return NotFound(new { error = resultado.Message });
                if (esRestriccion)
                {
                    _logger.LogWarning("CU-04 intento de eliminar horario con dependencias. ID: {Id}.", id);
                    return Conflict(new { error = resultado.Message });
                }

                return StatusCode(StatusCodes.Status500InternalServerError, new { error = resultado.Message });
            }

            _logger.LogInformation("CU-04 horario eliminado. ID: {Id}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CU-04 error inesperado al eliminar horario {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Error interno inesperado al eliminar el horario." });
        }
    }
}
