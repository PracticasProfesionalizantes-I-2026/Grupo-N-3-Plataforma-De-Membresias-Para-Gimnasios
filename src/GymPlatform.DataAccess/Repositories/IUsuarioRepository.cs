using GymPlatform.DataAccess.Entities;

namespace GymPlatform.DataAccess.Repositories;

/// <summary>
/// Contrato de acceso a datos para la entidad <see cref="Usuario"/>.
/// </summary>
public interface IUsuarioRepository
{
    /// <summary>Obtiene un usuario por su nombre de usuario (login).</summary>
    Task<Usuario?> GetByNombreUsuarioAsync(string nombreUsuario);

    /// <summary>Obtiene un usuario por su email.</summary>
    Task<Usuario?> GetByEmailAsync(string email);

    /// <summary>Obtiene todos los usuarios activos.</summary>
    Task<IEnumerable<Usuario>> GetAllAsync();

    /// <summary>Obtiene un usuario por ID.</summary>
    Task<Usuario?> GetByIdAsync(Guid id);

    /// <summary>Verifica si existe un nombre de usuario en el sistema.</summary>
    Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario);

    /// <summary>Persiste un nuevo usuario.</summary>
    Task<Usuario> CreateAsync(Usuario usuario);
}
