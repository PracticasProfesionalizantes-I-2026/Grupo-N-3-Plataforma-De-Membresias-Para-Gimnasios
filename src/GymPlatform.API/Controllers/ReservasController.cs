using GymPlatform.BusinessLogic.Interfaces;
using GymPlatform.Shared.Common;
using GymPlatform.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymPlatform.API.Controllers;

[ApiController]
[Route("api/reservas")]
[Produces("application/json")]
[Authorize]
public class ReservasController : ControllerBase
{
    private readonly IReservaService _reservaService;
    private readonly ILogger<ReservasController> _logger;

    public ReservasController(IReservaService reservaService, ILogger<ReservasController> logger)
    {
        _reservaService = reservaService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Administrador,Recepcionista,Entrenador")]
    [ProducesResponseType(typeof(IEnumerable<ReservaResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedResult<ReservaResponseDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? search = null)
    {
        var reservas = await _reservaService.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            reservas = reservas.Where(r =>
                r.SocioNombre.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.ActividadNombre.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.Profesor.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.Sala.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (page.HasValue || pageSize.HasValue)
        {
            var pageNumber = Math.Max(1, page ?? 1);
            var size = Math.Clamp(pageSize ?? 10, 1, 100);
            var total = reservas.Count();
            var pagedItems = reservas.Skip((pageNumber - 1) * size).Take(size).ToList();

            return Ok(new PagedResult<ReservaResponseDTO>(pagedItems, total, pageNumber, size));
        }

        return Ok(reservas);
    }

    [HttpGet("socio/{socioId:guid}")]
    [Authorize(Roles = "Administrador,Recepcionista,Socio")]
    [ProducesResponseType(typeof(IEnumerable<ReservaResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySocio(Guid socioId)
    {
        var reservas = await _reservaService.GetReservasBySocioAsync(socioId);
        return Ok(reservas);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador,Recepcionista,Socio")]
    [ProducesResponseType(typeof(ReservaResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] ReservaCreateDTO dto)
    {
        var created = await _reservaService.ReservarTurnoAsync(dto);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}/asistencia")]
    [Authorize(Roles = "Administrador,Entrenador,Recepcionista")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarcarAsistencia(Guid id, [FromQuery] bool asistio = true)
    {
        await _reservaService.MarcarAsistenciaAsync(id, asistio);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrador,Recepcionista,Socio")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancelar(Guid id)
    {
        await _reservaService.CancelarReservaAsync(id);
        return NoContent();
    }
}

