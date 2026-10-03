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
        return await _context.PlanesMembresia.AsNoTracking().ToListAsync();
    }

    public async Task<PlanMembresia?> GetByIdAsync(Guid id)
    {
        return await _context.PlanesMembresia.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PlanMembresia?> GetByNombreAsync(string nombre)
    {
        return await _context.PlanesMembresia.AsNoTracking().FirstOrDefaultAsync(p => p.Nombre.ToLower() == nombre.ToLower());
    }

    public async Task<bool> HasAssignedHorariosAsync(Guid id)
    {
        return await _context.Horarios.AsNoTracking().AnyAsync(h => h.PlanMembresiaId == id);
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
