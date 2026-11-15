using GymPlatform.BusinessLogic.Interfaces;
using GymPlatform.Shared.Common;
using GymPlatform.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymPlatform.API.Controllers;

[ApiController]
[Route("api/horarios")]
[Produces("application/json")]
[Authorize(Roles = "Administrador,Entrenador")]
public class HorariosController : ControllerBase
{
    private readonly IHorarioService _horarioService;
    private readonly ILogger<HorariosController> _logger;

    public HorariosController(IHorarioService horarioService, ILogger<HorariosController> logger)
    {
        _horarioService = horarioService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Administrador,Entrenador,Socio,Recepcionista")]
    [ProducesResponseType(typeof(IEnumerable<HorarioResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedResult<HorarioResponseDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? search = null)
    {
        var horarios = await _horarioService.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            horarios = horarios.Where(h =>
                h.Profesor.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                h.Sala.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                h.ActividadNombre.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                h.PlanNombre.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        // Issue #13: Paginación si se especifican page o pageSize
        if (page.HasValue || pageSize.HasValue)
        {
            var pageNumber = Math.Max(1, page ?? 1);
            var size = Math.Clamp(pageSize ?? 10, 1, 100);
            var total = horarios.Count();
            var pagedItems = horarios.Skip((pageNumber - 1) * size).Take(size).ToList();

            return Ok(new PagedResult<HorarioResponseDTO>(pagedItems, total, pageNumber, size));
        }

        return Ok(horarios);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Administrador,Entrenador,Socio,Recepcionista")]
    [ProducesResponseType(typeof(HorarioResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var horario = await _horarioService.GetByIdAsync(id);
        return Ok(horario);
    }

    [HttpPost]
    [ProducesResponseType(typeof(HorarioResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] HorarioCreateDTO dto)
    {
        var created = await _horarioService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(HorarioResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] HorarioUpdateDTO dto)
    {
        var updated = await _horarioService.UpdateAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _horarioService.DeleteAsync(id);
        return NoContent();
    }
}
