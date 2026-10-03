using GymPlatform.DataAccess.Entities;

namespace GymPlatform.DataAccess.Repositories;

public interface IActividadRepository
{
    Task<IEnumerable<Actividad>> GetAllAsync();
    Task<Actividad?> GetByIdAsync(Guid id);
    Task<Actividad?> GetByNombreAsync(string nombre);
    Task<Actividad> CreateAsync(Actividad actividad);
    Task UpdateAsync(Actividad actividad);
    Task DeleteAsync(Actividad actividad);
}
