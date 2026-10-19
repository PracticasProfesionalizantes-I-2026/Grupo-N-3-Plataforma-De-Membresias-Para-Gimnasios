namespace GymPlatform.DataAccess.Entities;

/// <summary>
/// Representa un usuario del sistema GymUp con credenciales de acceso y rol asignado.
/// </summary>
public class Usuario
{
    public Guid Id { get; set; }

    /// <summary>Nombre de usuario único para el login.</summary>
    public string NombreUsuario { get; set; } = string.Empty;

    /// <summary>Email del usuario (también puede usarse como login).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Contraseña almacenada como hash BCrypt.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Rol asignado que determina los permisos del usuario.</summary>
    public Shared.Enums.RolUsuario Rol { get; set; }

    /// <summary>Nombre para mostrar del usuario.</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
