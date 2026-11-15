using GymPlatform.DataAccess.Entities;

namespace GymPlatform.DataAccess.Repositories;

public interface IMembresiaRepository
{
    Task<IEnumerable<Membresia>> GetAllAsync();
    Task<Membresia?> GetByIdAsync(Guid id);
    Task<IEnumerable<Membresia>> GetBySocioIdAsync(Guid socioId);
    Task<Membresia?> GetActiveBySocioIdAsync(Guid socioId);
    Task<Membresia?> GetLatestBySocioAndPlanAsync(Guid socioId, Guid planId);
    Task<bool> HasActiveMembresiasForPlanAsync(Guid planId);
    Task<Membresia> CreateAsync(Membresia membresia);
    Task UpdateAsync(Membresia membresia);
}
