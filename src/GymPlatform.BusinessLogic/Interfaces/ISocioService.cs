using GymPlatform.Shared.DTOs;

namespace GymPlatform.BusinessLogic.Interfaces;

public interface ISocioService
{
    Task<IEnumerable<SocioResponseDTO>> GetAllAsync();
    Task<SocioResponseDTO> GetByIdAsync(Guid id);
    Task<SocioResponseDTO> CreateAsync(SocioCreateDTO dto);
    Task<SocioResponseDTO> UpdateAsync(Guid id, SocioUpdateDTO dto);
    Task DeleteAsync(Guid id);
}
