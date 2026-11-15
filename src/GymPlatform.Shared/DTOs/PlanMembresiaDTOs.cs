using System.ComponentModel.DataAnnotations;

namespace GymPlatform.Shared.DTOs;

public record PlanMembresiaCreateDTO(
    [Required(ErrorMessage = "El nombre del plan de membresía es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    string Nombre,

    [MaxLength(250, ErrorMessage = "La descripción no puede superar los 250 caracteres.")]
    string Descripcion,

    [Range(0.01, 10000000.0, ErrorMessage = "El precio debe ser mayor a cero.")]
    decimal Precio,

    [Range(1, 3650, ErrorMessage = "La duración del plan debe ser de al menos 1 día.")]
    int DuracionDias
);

public record PlanMembresiaUpdateDTO(
    [Required(ErrorMessage = "El nombre del plan de membresía es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    string Nombre,

    [MaxLength(250, ErrorMessage = "La descripción no puede superar los 250 caracteres.")]
    string Descripcion,

    [Range(0.01, 10000000.0, ErrorMessage = "El precio debe ser mayor a cero.")]
    decimal Precio,

    [Range(1, 3650, ErrorMessage = "La duración del plan debe ser de al menos 1 día.")]
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
