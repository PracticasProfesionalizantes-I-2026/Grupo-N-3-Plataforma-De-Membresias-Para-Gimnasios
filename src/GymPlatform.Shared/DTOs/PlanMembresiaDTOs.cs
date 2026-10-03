namespace GymPlatform.Shared.DTOs;

public record PlanMembresiaCreateDTO(
    string Nombre,
    string Descripcion,
    decimal Precio,
    int DuracionDias
);

public record PlanMembresiaUpdateDTO(
    string Nombre,
    string Descripcion,
    decimal Precio,
    int DuracionDias,
    bool Activo
);

public record PlanMembresiaResponseDTO(
    Guid Id,
    string Nombre,
    string Descripcion,
    decimal Precio,
    int DuracionDias,
    bool Activo
);
