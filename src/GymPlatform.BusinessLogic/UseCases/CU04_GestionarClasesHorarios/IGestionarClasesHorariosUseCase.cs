using GymPlatform.Shared.Common;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.UseCases;

namespace GymPlatform.BusinessLogic.UseCases.CU04_GestionarClasesHorarios;

public interface IGestionarClasesHorariosUseCase
{
    Task<Result<IEnumerable<ActividadResponseDTO>>> ListarActividadesAsync();
    Task<Result<ActividadResponseDTO>> CrearActividadAsync(ActividadCreateDTO dto);
    Task<Result<ActividadResponseDTO>> ModificarActividadAsync(Guid id, ActividadUpdateDTO dto);
    Task<Result<bool>> EliminarActividadAsync(Guid id);

    Task<Result<IEnumerable<HorarioResponseDTO>>> ListarHorariosAsync();
    Task<Result<HorarioResponseDTO>> GetHorarioByIdAsync(Guid id);
    Task<Result<HorarioResponseDTO>> CrearHorarioAsync(HorarioCreateDTO dto);
    Task<Result<HorarioResponseDTO>> ModificarHorarioAsync(Guid id, HorarioUpdateDTO dto);
    Task<Result<bool>> EliminarHorarioAsync(Guid id);
}
