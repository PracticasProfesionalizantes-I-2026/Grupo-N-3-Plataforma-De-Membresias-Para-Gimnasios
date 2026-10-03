using GymPlatform.Shared.DTOs;

namespace GymPlatform.BusinessLogic.Interfaces;

public interface IHorarioService
{
    Task<IEnumerable<HorarioResponseDTO>> GetAllAsync();
    Task<HorarioResponseDTO> GetByIdAsync(Guid id);
    Task<HorarioResponseDTO> CreateAsync(HorarioCreateDTO dto);
    Task<HorarioResponseDTO> UpdateAsync(Guid id, HorarioUpdateDTO dto);
    Task DeleteAsync(Guid id);
}
