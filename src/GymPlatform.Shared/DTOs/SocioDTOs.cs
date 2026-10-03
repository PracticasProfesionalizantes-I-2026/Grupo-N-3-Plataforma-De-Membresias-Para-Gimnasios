namespace GymPlatform.Shared.DTOs;

public record SocioCreateDTO(
    string Nombre,
    string Dni,
    string Email,
    string Telefono,
    DateOnly FechaNacimiento
);

public record SocioUpdateDTO(
    string Nombre,
    string Telefono,
    bool Activo
);

public record SocioResponseDTO(
    Guid Id,
    string Nombre,
    string Dni,
    string Email,
    string Telefono,
    DateOnly FechaNacimiento,
    DateTime FechaRegistro,
    bool Activo
);
