using GymPlatform.Shared.DTOs;

namespace GymPlatform.BusinessLogic.Interfaces;

public interface IActividadService
{
    Task<IEnumerable<ActividadResponseDTO>> GetAllAsync();
    Task<ActividadResponseDTO> GetByIdAsync(Guid id);
    Task<ActividadResponseDTO> CreateAsync(ActividadCreateDTO dto);
    Task<ActividadResponseDTO> UpdateAsync(Guid id, ActividadUpdateDTO dto);
    Task DeleteAsync(Guid id);
}
