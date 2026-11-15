using GymPlatform.BusinessLogic.Interfaces;
using GymPlatform.Shared.Common;
using GymPlatform.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymPlatform.API.Controllers;

[ApiController]
[Route("api/membresias")]
[Produces("application/json")]
[Authorize]
public class MembresiasController : ControllerBase
{
    private readonly IMembresiaService _membresiaService;
    private readonly ILogger<MembresiasController> _logger;

    public MembresiasController(IMembresiaService membresiaService, ILogger<MembresiasController> logger)
    {
        _membresiaService = membresiaService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Administrador,Recepcionista")]
    [ProducesResponseType(typeof(IEnumerable<MembresiaResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedResult<MembresiaResponseDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? search = null)
    {
        var membresias = await _membresiaService.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            membresias = membresias.Where(m =>
                m.SocioNombre.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                m.PlanNombre.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (page.HasValue || pageSize.HasValue)
        {
            var pageNumber = Math.Max(1, page ?? 1);
            var size = Math.Clamp(pageSize ?? 10, 1, 100);
            var total = membresias.Count();
            var pagedItems = membresias.Skip((pageNumber - 1) * size).Take(size).ToList();

            return Ok(new PagedResult<MembresiaResponseDTO>(pagedItems, total, pageNumber, size));
        }

        return Ok(membresias);
    }

    [HttpGet("socio/{socioId:guid}")]
    [Authorize(Roles = "Administrador,Recepcionista,Socio")]
    [ProducesResponseType(typeof(IEnumerable<MembresiaResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySocio(Guid socioId)
    {
        var membresias = await _membresiaService.GetMembresiasBySocioAsync(socioId);
        return Ok(membresias);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador,Recepcionista")]
    [ProducesResponseType(typeof(MembresiaResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Contratar([FromBody] MembresiaCreateDTO dto)
    {
        var created = await _membresiaService.ContratarMembresiaAsync(dto);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }
}

