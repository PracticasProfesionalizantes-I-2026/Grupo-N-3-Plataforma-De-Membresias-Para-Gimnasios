using GymPlatform.Shared.DTOs;

namespace GymPlatform.BusinessLogic.Interfaces;

public interface IPlanMembresiaService
{
    Task<IEnumerable<PlanMembresiaResponseDTO>> GetAllAsync();
    Task<PlanMembresiaResponseDTO> GetByIdAsync(Guid id);
    Task<PlanMembresiaResponseDTO> CreateAsync(PlanMembresiaCreateDTO dto);
    Task<PlanMembresiaResponseDTO> UpdateAsync(Guid id, PlanMembresiaUpdateDTO dto);
    Task DeleteAsync(Guid id);
}
