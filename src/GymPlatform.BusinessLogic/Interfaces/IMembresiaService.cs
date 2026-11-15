using GymPlatform.Shared.DTOs;

namespace GymPlatform.BusinessLogic.Interfaces;

public interface IMembresiaService
{
    Task<MembresiaResponseDTO> ContratarMembresiaAsync(MembresiaCreateDTO dto);
    Task<IEnumerable<MembresiaResponseDTO>> GetMembresiasBySocioAsync(Guid socioId);
    Task<IEnumerable<MembresiaResponseDTO>> GetAllAsync();
}

