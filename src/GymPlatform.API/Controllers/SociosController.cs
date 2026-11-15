using GymPlatform.BusinessLogic.Interfaces;
using GymPlatform.Shared.Common;
using GymPlatform.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymPlatform.API.Controllers;

[ApiController]
[Route("api/socios")]
[Route("api/members")] // Alias para alineación con especificación de dominio
[Produces("application/json")]
[Authorize(Roles = "Recepcionista,Administrador")]
public class SociosController : ControllerBase
{
    private readonly ISocioService _socioService;
    private readonly ILogger<SociosController> _logger;

    public SociosController(ISocioService socioService, ILogger<SociosController> logger)
    {
        _socioService = socioService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SocioResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedResult<SocioResponseDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? search = null)
    {
        var socios = await _socioService.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            socios = socios.Where(s =>
                s.Nombre.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.Dni.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.Email.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        // Issue #13: Paginación si se especifican page o pageSize
        if (page.HasValue || pageSize.HasValue)
        {
            var pageNumber = Math.Max(1, page ?? 1);
            var size = Math.Clamp(pageSize ?? 10, 1, 100);
            var total = socios.Count();
            var pagedItems = socios.Skip((pageNumber - 1) * size).Take(size).ToList();

            return Ok(new PagedResult<SocioResponseDTO>(pagedItems, total, pageNumber, size));
        }

        return Ok(socios);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Recepcionista,Administrador,Socio")]
    [ProducesResponseType(typeof(SocioResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var socio = await _socioService.GetByIdAsync(id);
        return Ok(socio);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SocioResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] SocioCreateDTO dto)
    {
        var created = await _socioService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SocioResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SocioUpdateDTO dto)
    {
        var updated = await _socioService.UpdateAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _socioService.DeleteAsync(id);
        return NoContent();
    }
}
