using GymPlatform.BusinessLogic.Interfaces;
using GymPlatform.Shared.Common;
using GymPlatform.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymPlatform.API.Controllers;

[ApiController]
[Route("api/planes")]
[Produces("application/json")]
[Authorize]
public class PlanesController : ControllerBase
{
    private readonly IPlanMembresiaService _planService;
    private readonly ILogger<PlanesController> _logger;

    public PlanesController(IPlanMembresiaService planService, ILogger<PlanesController> logger)
    {
        _planService = planService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Administrador,Entrenador,Socio,Recepcionista")]
    [ProducesResponseType(typeof(IEnumerable<PlanMembresiaResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedResult<PlanMembresiaResponseDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? search = null)
    {
        var planes = await _planService.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            planes = planes.Where(p =>
                p.Nombre.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Descripcion.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        // Issue #13: Paginación si se especifican page o pageSize
        if (page.HasValue || pageSize.HasValue)
        {
            var pageNumber = Math.Max(1, page ?? 1);
            var size = Math.Clamp(pageSize ?? 10, 1, 100);
            var total = planes.Count();
            var pagedItems = planes.Skip((pageNumber - 1) * size).Take(size).ToList();

            return Ok(new PagedResult<PlanMembresiaResponseDTO>(pagedItems, total, pageNumber, size));
        }

        return Ok(planes);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Administrador,Entrenador,Socio,Recepcionista")]
    [ProducesResponseType(typeof(PlanMembresiaResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var plan = await _planService.GetByIdAsync(id);
        return Ok(plan);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(PlanMembresiaResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] PlanMembresiaCreateDTO dto)
    {
        var created = await _planService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(PlanMembresiaResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] PlanMembresiaUpdateDTO dto)
    {
        var updated = await _planService.UpdateAsync(id, dto);
        return Ok(updated);
    }

    // Issue #6: Endpoint PATCH para desactivación lógica de planes
    [HttpPatch("{id:guid}/desactivar")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(PlanMembresiaResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desactivar(Guid id)
    {
        var plan = await _planService.DesactivarAsync(id);
        return Ok(plan);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _planService.DeleteAsync(id);
        return NoContent();
    }
}
