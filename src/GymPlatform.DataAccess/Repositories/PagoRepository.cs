using GymPlatform.DataAccess.Context;
using GymPlatform.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymPlatform.DataAccess.Repositories;

public class PagoRepository : IPagoRepository
{
    private readonly GymDbContext _context;

    public PagoRepository(GymDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Pago>> GetAllAsync()
    {
        return await _context.Pagos
            .AsNoTracking()
            .Include(p => p.Membresia)
            .OrderByDescending(p => p.FechaPago)
            .ToListAsync();
    }

    public async Task<Pago?> GetByIdAsync(Guid id)
    {
        return await _context.Pagos
            .Include(p => p.Membresia)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Pago>> GetByMembresiaIdAsync(Guid membresiaId)
    {
        return await _context.Pagos
            .AsNoTracking()
            .Where(p => p.MembresiaId == membresiaId)
            .OrderByDescending(p => p.FechaPago)
            .ToListAsync();
    }

    public async Task<Pago> CreateAsync(Pago pago)
    {
        if (pago.Id == Guid.Empty)
        {
            pago.Id = Guid.NewGuid();
        }
        await _context.Pagos.AddAsync(pago);
        await _context.SaveChangesAsync();
        return pago;
    }
}
