using GymPlatform.DataAccess.Entities;

namespace GymPlatform.DataAccess.Repositories;

public interface IPlanMembresiaRepository
{
    Task<IEnumerable<PlanMembresia>> GetAllAsync();
    Task<PlanMembresia?> GetByIdAsync(Guid id);
    Task<PlanMembresia?> GetByNombreAsync(string nombre);
    Task<bool> HasAssignedHorariosAsync(Guid id);
    Task<PlanMembresia> CreateAsync(PlanMembresia plan);
    Task UpdateAsync(PlanMembresia plan);
    Task DeleteAsync(PlanMembresia plan);
}
