namespace GymPlatform.Shared.DTOs;

// ──────────────────────────────────────────────────────────────────
//  DTOs de Autenticación (Auth)
// ──────────────────────────────────────────────────────────────────

/// <summary>Payload recibido en el endpoint de login.</summary>
public record LoginRequestDTO(
    string NombreUsuario,
    string Password
);

/// <summary>Respuesta devuelta tras un login exitoso.</summary>
public record LoginResponseDTO(
    string Token,
    string NombreUsuario,
    string NombreCompleto,
    string Rol,
    DateTime Expiracion
);

/// <summary>DTO para registrar un nuevo usuario del sistema (solo Administrador).</summary>
public record UsuarioCreateDTO(
    string NombreUsuario,
    string Email,
    string Password,
    string NombreCompleto,
    string Rol
);

/// <summary>DTO de respuesta con datos públicos del usuario (sin password).</summary>
public record UsuarioResponseDTO(
    Guid Id,
    string NombreUsuario,
    string Email,
    string NombreCompleto,
    string Rol,
    bool Activo,
    DateTime FechaCreacion
);
