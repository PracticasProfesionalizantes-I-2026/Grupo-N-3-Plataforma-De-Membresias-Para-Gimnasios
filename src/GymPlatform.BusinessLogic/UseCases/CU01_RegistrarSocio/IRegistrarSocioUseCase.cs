using GymPlatform.Shared.Common;
using GymPlatform.Shared.UseCases;

namespace GymPlatform.BusinessLogic.UseCases.CU01_RegistrarSocio;

public interface IRegistrarSocioUseCase
{
    Task<Result<SocioRegistradoResultDTO>> ExecuteAsync(RegistrarSocioRequestDTO request);
}
