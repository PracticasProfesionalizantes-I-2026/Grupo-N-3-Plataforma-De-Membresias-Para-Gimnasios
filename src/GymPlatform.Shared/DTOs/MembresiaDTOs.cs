using System.ComponentModel.DataAnnotations;
using GymPlatform.Shared.Enums;

namespace GymPlatform.Shared.DTOs;

public record MembresiaCreateDTO(
    [Required(ErrorMessage = "El identificador del socio es obligatorio.")]
    Guid SocioId,

    [Required(ErrorMessage = "El identificador del plan de membresía es obligatorio.")]
    Guid PlanMembresiaId,

    [Required(ErrorMessage = "La fecha de inicio de la membresía es obligatoria.")]
    DateTime FechaInicio
);

public record MembresiaResponseDTO(
    Guid Id,
    Guid SocioId,
    string SocioNombre,
    Guid PlanMembresiaId,
    string PlanNombre,
    DateTime FechaInicio,
    DateTime FechaVencimiento,
    bool Activo,
    EstadoMembresia Estado,
    DateTime FechaRegistro
);
