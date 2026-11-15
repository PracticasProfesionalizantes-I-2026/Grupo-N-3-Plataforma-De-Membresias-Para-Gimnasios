using GymPlatform.Shared.Common;
using GymPlatform.Shared.UseCases;

namespace GymPlatform.BusinessLogic.UseCases.CU03_RenovarMembresia;

public interface IRenovarMembresiaUseCase
{
    Task<Result<IEnumerable<MembresiaResumenDTO>>> ObtenerMembresiasPorSocioAsync(Guid socioId);
    Task<Result<RenovacionResultDTO>> ExecuteAsync(RenovarMembresiaRequestDTO request);
}
