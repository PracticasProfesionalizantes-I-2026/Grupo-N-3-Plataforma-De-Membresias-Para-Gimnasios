using GymPlatform.BusinessLogic.Interfaces;
using GymPlatform.Shared.Common;
using GymPlatform.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymPlatform.API.Controllers;

[ApiController]
[Route("api/actividades")]
[Produces("application/json")]
[Authorize]
public class ActividadesController : ControllerBase
{
    private readonly IActividadService _actividadService;
    private readonly ILogger<ActividadesController> _logger;

    public ActividadesController(IActividadService actividadService, ILogger<ActividadesController> logger)
    {
        _actividadService = actividadService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ActividadResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedResult<ActividadResponseDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? search = null)
    {
        var actividades = await _actividadService.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            actividades = actividades.Where(a =>
                a.Nombre.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                a.Descripcion.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        // Issue #13: Paginación si se especifican page o pageSize
        if (page.HasValue || pageSize.HasValue)
        {
            var pageNumber = Math.Max(1, page ?? 1);
            var size = Math.Clamp(pageSize ?? 10, 1, 100);
            var total = actividades.Count();
            var pagedItems = actividades.Skip((pageNumber - 1) * size).Take(size).ToList();

            return Ok(new PagedResult<ActividadResponseDTO>(pagedItems, total, pageNumber, size));
        }

        return Ok(actividades);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ActividadResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var actividad = await _actividadService.GetByIdAsync(id);
        return Ok(actividad);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador,Entrenador")]
    [ProducesResponseType(typeof(ActividadResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] ActividadCreateDTO dto)
    {
        var created = await _actividadService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrador,Entrenador")]
    [ProducesResponseType(typeof(ActividadResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ActividadUpdateDTO dto)
    {
        var updated = await _actividadService.UpdateAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrador,Entrenador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _actividadService.DeleteAsync(id);
        return NoContent();
    }
}
