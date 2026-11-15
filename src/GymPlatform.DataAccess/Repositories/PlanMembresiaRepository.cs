using GymPlatform.DataAccess.Context;
using GymPlatform.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymPlatform.DataAccess.Repositories;

public class PlanMembresiaRepository : IPlanMembresiaRepository
{
    private readonly GymDbContext _context;

    public PlanMembresiaRepository(GymDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PlanMembresia>> GetAllAsync()
    {
        // Por defecto se retornan únicamente planes activos
        return await _context.PlanesMembresia
            .Where(p => p.Activo)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<PlanMembresia?> GetByIdAsync(Guid id)
    {
        return await _context.PlanesMembresia.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PlanMembresia?> GetByNombreAsync(string nombre)
    {
        // Optimizado con intercalación NOCASE sin .ToLower()
        return await _context.PlanesMembresia.AsNoTracking().FirstOrDefaultAsync(p => p.Nombre == nombre);
    }

    public async Task<bool> HasAssignedHorariosAsync(Guid id)
    {
        return await _context.Horarios.AsNoTracking().AnyAsync(h => h.PlanMembresiaId == id);
    }

    public async Task<bool> HasActiveMembresiasAsync(Guid id)
    {
        return await _context.Membresias.AsNoTracking().AnyAsync(m => m.PlanMembresiaId == id && m.Estado == Shared.Enums.EstadoMembresia.Activa);
    }

    public async Task<PlanMembresia> CreateAsync(PlanMembresia plan)
    {
        plan.Id = Guid.NewGuid();
        await _context.PlanesMembresia.AddAsync(plan);
        await _context.SaveChangesAsync();
        return plan;
    }

    public async Task UpdateAsync(PlanMembresia plan)
    {
        _context.PlanesMembresia.Update(plan);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(PlanMembresia plan)
    {
        _context.PlanesMembresia.Remove(plan);
        await _context.SaveChangesAsync();
    }
}
