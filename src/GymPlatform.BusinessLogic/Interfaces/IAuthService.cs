using GymPlatform.Shared.DTOs;

namespace GymPlatform.BusinessLogic.Interfaces;

/// <summary>
/// Contrato del servicio de autenticación y autorización del sistema GymUp.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Autentica las credenciales de un usuario y retorna el token JWT con sus respectivos claims.
    /// </summary>
    Task<LoginResponseDTO> LoginAsync(LoginRequestDTO request);

    /// <summary>
    /// Da de alta un nuevo usuario en el sistema con su rol correspondiente.
    /// </summary>
    Task<UsuarioResponseDTO> RegistrarUsuarioAsync(UsuarioCreateDTO request);
}
