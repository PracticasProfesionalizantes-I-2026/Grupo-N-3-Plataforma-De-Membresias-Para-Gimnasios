using GymPlatform.DataAccess.Context;
using GymPlatform.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymPlatform.DataAccess.Repositories;

public class HorarioRepository : IHorarioRepository
{
    private readonly GymDbContext _context;

    public HorarioRepository(GymDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Horario>> GetAllAsync()
    {
        return await _context.Horarios
            .Include(h => h.Actividad)
            .Include(h => h.PlanMembresia)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Horario?> GetByIdAsync(Guid id)
    {
        return await _context.Horarios
            .Include(h => h.Actividad)
            .Include(h => h.PlanMembresia)
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id);
    }

    public async Task<IEnumerable<Horario>> GetByProfesorAndDiaAsync(string profesor, DayOfWeek diaSemana)
    {
        return await _context.Horarios
            .AsNoTracking()
            .Where(h => h.Profesor.ToLower() == profesor.ToLower() && h.DiaSemana == diaSemana)
            .ToListAsync();
    }

    public async Task<IEnumerable<Horario>> GetBySalaAndDiaAsync(string sala, DayOfWeek diaSemana)
    {
        return await _context.Horarios
            .AsNoTracking()
            .Where(h => h.Sala.ToLower() == sala.ToLower() && h.DiaSemana == diaSemana)
            .ToListAsync();
    }

    public async Task<Horario> CreateAsync(Horario horario)
    {
        horario.Id = Guid.NewGuid();
        await _context.Horarios.AddAsync(horario);
        await _context.SaveChangesAsync();
        return horario;
    }

    public async Task UpdateAsync(Horario horario)
    {
        _context.Horarios.Update(horario);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Horario horario)
    {
        _context.Horarios.Remove(horario);
        await _context.SaveChangesAsync();
    }
}
