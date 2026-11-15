using GymPlatform.DataAccess.Entities;

namespace GymPlatform.DataAccess.Repositories;

public interface IReservaRepository
{
    Task<IEnumerable<Reserva>> GetAllAsync();
    Task<Reserva?> GetByIdAsync(Guid id);
    Task<IEnumerable<Reserva>> GetBySocioIdAsync(Guid socioId);
    Task<int> CountReservasByHorarioAndFechaAsync(Guid horarioId, DateOnly fechaTurno);
    Task<bool> HasReservaActivaAsync(Guid socioId, Guid horarioId, DateOnly fechaTurno);
    Task<Reserva> CreateAsync(Reserva reserva);
    Task UpdateAsync(Reserva reserva);
    Task DeleteAsync(Reserva reserva);
}

