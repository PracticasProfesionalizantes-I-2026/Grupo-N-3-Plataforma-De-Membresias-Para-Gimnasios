using GymPlatform.DataAccess.Entities;

namespace GymPlatform.DataAccess.Repositories;

public interface ISocioRepository
{
    Task<IEnumerable<Socio>> GetAllAsync();
    Task<Socio?> GetByIdAsync(Guid id);
    Task<Socio?> GetByDniAsync(string dni);
    Task<Socio?> GetByEmailAsync(string email);
    Task<Socio> CreateAsync(Socio socio);
    Task UpdateAsync(Socio socio);
    Task DeleteAsync(Socio socio);
}
