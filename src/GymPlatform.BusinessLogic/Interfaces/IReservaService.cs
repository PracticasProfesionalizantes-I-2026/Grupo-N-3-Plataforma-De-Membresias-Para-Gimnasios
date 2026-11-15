using GymPlatform.Shared.DTOs;

namespace GymPlatform.BusinessLogic.Interfaces;

public interface IReservaService
{
    Task<ReservaResponseDTO> ReservarTurnoAsync(ReservaCreateDTO dto);
    Task<IEnumerable<ReservaResponseDTO>> GetReservasBySocioAsync(Guid socioId);
    Task<IEnumerable<ReservaResponseDTO>> GetAllAsync();
    Task CancelarReservaAsync(Guid id);
    Task MarcarAsistenciaAsync(Guid id, bool asistio);
}

