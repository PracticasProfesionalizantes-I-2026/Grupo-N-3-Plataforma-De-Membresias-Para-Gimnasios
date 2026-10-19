using GymPlatform.Shared.Common;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.UseCases;

namespace GymPlatform.BusinessLogic.UseCases.CU02_GestionarPlanes;

public interface IGestionarPlanesUseCase
{
    Task<Result<IEnumerable<PlanMembresiaResponseDTO>>> ListarPlanesAsync();
    Task<Result<PlanMembresiaResponseDTO>> GetByIdAsync(Guid id);
    Task<Result<PlanMembresiaResponseDTO>> CrearPlanAsync(PlanMembresiaCreateDTO dto);
    Task<Result<PlanMembresiaResponseDTO>> ModificarPlanAsync(Guid id, PlanMembresiaUpdateDTO dto);
    Task<Result<bool>> EliminarPlanAsync(Guid id);
}
