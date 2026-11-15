using GymPlatform.DataAccess.Entities;

namespace GymPlatform.DataAccess.Repositories;

public interface IPagoRepository
{
    Task<IEnumerable<Pago>> GetAllAsync();
    Task<Pago?> GetByIdAsync(Guid id);
    Task<IEnumerable<Pago>> GetByMembresiaIdAsync(Guid membresiaId);
    Task<Pago> CreateAsync(Pago pago);
}
