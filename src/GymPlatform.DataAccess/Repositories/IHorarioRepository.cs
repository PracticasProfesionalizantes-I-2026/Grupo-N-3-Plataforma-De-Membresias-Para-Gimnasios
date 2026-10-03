using GymPlatform.DataAccess.Entities;

namespace GymPlatform.DataAccess.Repositories;

public interface IHorarioRepository
{
    Task<IEnumerable<Horario>> GetAllAsync();
    Task<Horario?> GetByIdAsync(Guid id);
    Task<IEnumerable<Horario>> GetByProfesorAndDiaAsync(string profesor, DayOfWeek diaSemana);
    Task<IEnumerable<Horario>> GetBySalaAndDiaAsync(string sala, DayOfWeek diaSemana);
    Task<Horario> CreateAsync(Horario horario);
    Task UpdateAsync(Horario horario);
    Task DeleteAsync(Horario horario);
}
