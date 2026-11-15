using GymPlatform.Shared.Enums;

namespace GymPlatform.Shared.UseCases;

public record RegistrarSocioRequestDTO(
    string Nombre,
    string Dni,
    string Email,
    string Telefono,
    DateOnly FechaNacimiento,
    Guid? PlanMembresiaId = null,
    MetodoPago? MetodoPago = null
);

public record SocioRegistradoResultDTO(
    Guid SocioId,
    string Nombre,
    string Dni,
    string Email,
    string Telefono,
    DateOnly FechaNacimiento,
    Guid? MembresiaId,
    string? PlanNombre,
    DateTime? FechaVencimientoMembresia,
    string? NumeroComprobante,
    bool NotificacionEnviada
);
