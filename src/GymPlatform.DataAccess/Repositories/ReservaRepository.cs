using GymPlatform.DataAccess.Context;
using GymPlatform.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymPlatform.DataAccess.Repositories;

public class ReservaRepository : IReservaRepository
{
    private readonly GymDbContext _context;

    public ReservaRepository(GymDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Reserva>> GetAllAsync()
    {
        return await _context.Reservas
            .Include(r => r.Socio)
            .Include(r => r.Horario)
                .ThenInclude(h => h!.Actividad)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Reserva?> GetByIdAsync(Guid id)
    {
        return await _context.Reservas
            .Include(r => r.Socio)
            .Include(r => r.Horario)
                .ThenInclude(h => h!.Actividad)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Reserva>> GetBySocioIdAsync(Guid socioId)
    {
        return await _context.Reservas
            .Include(r => r.Socio)
            .Include(r => r.Horario)
                .ThenInclude(h => h!.Actividad)
            .Where(r => r.SocioId == socioId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> CountReservasByHorarioAndFechaAsync(Guid horarioId, DateOnly fechaTurno)
    {
        return await _context.Reservas
            .AsNoTracking()
            .CountAsync(r => r.HorarioId == horarioId && r.FechaTurno == fechaTurno);
    }

    public async Task<bool> HasReservaActivaAsync(Guid socioId, Guid horarioId, DateOnly fechaTurno)
    {
        return await _context.Reservas
            .AsNoTracking()
            .AnyAsync(r => r.SocioId == socioId && r.HorarioId == horarioId && r.FechaTurno == fechaTurno);
    }

    public async Task<Reserva> CreateAsync(Reserva reserva)
    {
        reserva.Id = Guid.NewGuid();
        reserva.FechaCreacion = DateTime.UtcNow;
        await _context.Reservas.AddAsync(reserva);
        await _context.SaveChangesAsync();
        return reserva;
    }

    public async Task UpdateAsync(Reserva reserva)
    {
        _context.Reservas.Update(reserva);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Reserva reserva)
    {
        _context.Reservas.Remove(reserva);
        await _context.SaveChangesAsync();
    }
}

