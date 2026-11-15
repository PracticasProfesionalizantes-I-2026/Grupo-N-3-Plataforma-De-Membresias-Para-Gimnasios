using GymPlatform.DataAccess.Context;
using GymPlatform.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymPlatform.DataAccess.Repositories;

public class ActividadRepository : IActividadRepository
{
    private readonly GymDbContext _context;

    public ActividadRepository(GymDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Actividad>> GetAllAsync()
    {
        return await _context.Actividades.AsNoTracking().ToListAsync();
    }

    public async Task<Actividad?> GetByIdAsync(Guid id)
    {
        return await _context.Actividades.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Actividad?> GetByNombreAsync(string nombre)
    {
        // Optimizado con intercalación NOCASE sin .ToLower()
        return await _context.Actividades.AsNoTracking().FirstOrDefaultAsync(a => a.Nombre == nombre);
    }

    public async Task<bool> HasAssignedHorariosAsync(Guid id)
    {
        return await _context.Horarios.AsNoTracking().AnyAsync(h => h.ActividadId == id);
    }

    public async Task<Actividad> CreateAsync(Actividad actividad)
    {
        actividad.Id = Guid.NewGuid();
        await _context.Actividades.AddAsync(actividad);
        await _context.SaveChangesAsync();
        return actividad;
    }

    public async Task UpdateAsync(Actividad actividad)
    {
        _context.Actividades.Update(actividad);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Actividad actividad)
    {
        _context.Actividades.Remove(actividad);
        await _context.SaveChangesAsync();
    }
}
