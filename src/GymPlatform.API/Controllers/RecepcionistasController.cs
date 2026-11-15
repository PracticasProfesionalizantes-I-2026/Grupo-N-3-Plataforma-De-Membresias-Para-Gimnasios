using GymPlatform.DataAccess.Context;
using GymPlatform.DataAccess.Entities;
using GymPlatform.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymPlatform.API.Controllers;

/// <summary>
/// Controlador para la gestión y alta de recepcionistas del sistema.
/// Requiere estrictamente rol de Administrador.
/// </summary>
[ApiController]
[Route("api/recepcionistas")]
[Authorize(Roles = "Administrador")]
[Produces("application/json")]
public class RecepcionistasController : ControllerBase
{
    private readonly GymDbContext _context;

    public RecepcionistasController(GymDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retorna la lista de usuarios con el rol de Recepcionista.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Usuario>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecepcionistas()
    {
        var recepcionistas = await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.Rol == RolUsuario.Recepcionista)
            .ToListAsync();

        return Ok(recepcionistas);
    }

    /// <summary>
    /// Crea un nuevo recepcionista configurando su Guid, rol, hash de contraseña por defecto y fecha de creación.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Usuario), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRecepcionista([FromBody] Usuario usuario)
    {
        if (usuario == null)
        {
            return BadRequest(new { error = "Los datos del recepcionista son requeridos." });
        }

        usuario.Id = Guid.NewGuid();
        usuario.Rol = RolUsuario.Recepcionista;

        var defaultPassword = string.IsNullOrWhiteSpace(usuario.PasswordHash) 
            ? "Recep123!" 
            : usuario.PasswordHash;
        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);

        usuario.FechaCreacion = DateTime.UtcNow;

        await _context.Usuarios.AddAsync(usuario);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRecepcionistas), new { id = usuario.Id }, usuario);
    }
}

