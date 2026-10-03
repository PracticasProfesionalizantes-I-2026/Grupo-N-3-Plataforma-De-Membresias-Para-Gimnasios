namespace GymPlatform.Shared.DTOs;

public record ActividadCreateDTO(
    string Nombre,
    string Descripcion
);

public record ActividadUpdateDTO(
    string Nombre,
    string Descripcion
);

public record ActividadResponseDTO(
    Guid Id,
    string Nombre,
    string Descripcion
);
