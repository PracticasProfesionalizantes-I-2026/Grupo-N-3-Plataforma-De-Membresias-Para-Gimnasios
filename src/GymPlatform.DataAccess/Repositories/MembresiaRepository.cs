using GymPlatform.DataAccess.Context;
using GymPlatform.DataAccess.Entities;
using GymPlatform.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymPlatform.DataAccess.Repositories;

public class MembresiaRepository : IMembresiaRepository
{
    private readonly GymDbContext _context;

    public MembresiaRepository(GymDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Membresia>> GetAllAsync()
    {
        return await _context.Membresias
            .AsNoTracking()
            .Include(m => m.Socio)
            .Include(m => m.PlanMembresia)
            .Include(m => m.Pagos)
            .ToListAsync();
    }

    public async Task<Membresia?> GetByIdAsync(Guid id)
    {
        return await _context.Membresias
            .Include(m => m.Socio)
            .Include(m => m.PlanMembresia)
            .Include(m => m.Pagos)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<Membresia>> GetBySocioIdAsync(Guid socioId)
    {
        return await _context.Membresias
            .AsNoTracking()
            .Include(m => m.PlanMembresia)
            .Where(m => m.SocioId == socioId)
            .OrderByDescending(m => m.FechaVencimiento)
            .ToListAsync();
    }

    public async Task<Membresia?> GetActiveBySocioIdAsync(Guid socioId)
    {
        return await _context.Membresias
            .Include(m => m.PlanMembresia)
            .Where(m => m.SocioId == socioId && m.Estado == EstadoMembresia.Activa && m.FechaVencimiento >= DateTime.UtcNow)
            .OrderByDescending(m => m.FechaVencimiento)
            .FirstOrDefaultAsync();
    }

    public async Task<Membresia?> GetLatestBySocioAndPlanAsync(Guid socioId, Guid planId)
    {
        return await _context.Membresias
            .Include(m => m.PlanMembresia)
            .Where(m => m.SocioId == socioId && m.PlanMembresiaId == planId)
            .OrderByDescending(m => m.FechaVencimiento)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> HasActiveMembresiasForPlanAsync(Guid planId)
    {
        return await _context.Membresias
            .AsNoTracking()
            .AnyAsync(m => m.PlanMembresiaId == planId && m.Estado == EstadoMembresia.Activa);
    }

    public async Task<Membresia> CreateAsync(Membresia membresia)
    {
        if (membresia.Id == Guid.Empty)
        {
            membresia.Id = Guid.NewGuid();
        }
        await _context.Membresias.AddAsync(membresia);
        await _context.SaveChangesAsync();
        return membresia;
    }

    public async Task UpdateAsync(Membresia membresia)
    {
        _context.Membresias.Update(membresia);
        await _context.SaveChangesAsync();
    }
}
