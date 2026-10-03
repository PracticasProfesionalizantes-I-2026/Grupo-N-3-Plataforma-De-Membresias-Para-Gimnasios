using GymPlatform.DataAccess.Context;
using GymPlatform.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymPlatform.DataAccess.Repositories;

public class SocioRepository : ISocioRepository
{
    private readonly GymDbContext _context;

    public SocioRepository(GymDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Socio>> GetAllAsync()
    {
        return await _context.Socios.AsNoTracking().ToListAsync();
    }

    public async Task<Socio?> GetByIdAsync(Guid id)
    {
        return await _context.Socios.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Socio?> GetByDniAsync(string dni)
    {
        return await _context.Socios.AsNoTracking().FirstOrDefaultAsync(s => s.Dni == dni);
    }

    public async Task<Socio?> GetByEmailAsync(string email)
    {
        return await _context.Socios.AsNoTracking().FirstOrDefaultAsync(s => s.Email == email);
    }

    public async Task<Socio> CreateAsync(Socio socio)
    {
        socio.Id = Guid.NewGuid();
        socio.FechaRegistro = DateTime.UtcNow;
        await _context.Socios.AddAsync(socio);
        await _context.SaveChangesAsync();
        return socio;
    }

    public async Task UpdateAsync(Socio socio)
    {
        _context.Socios.Update(socio);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Socio socio)
    {
        _context.Socios.Remove(socio);
        await _context.SaveChangesAsync();
    }
}
