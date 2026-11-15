using System.ComponentModel.DataAnnotations;

namespace GymPlatform.Shared.DTOs;

public record ActividadCreateDTO(
    [Required(ErrorMessage = "El nombre de la actividad es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre de la actividad no puede superar los 100 caracteres.")]
    string Nombre,

    [MaxLength(250, ErrorMessage = "La descripción no puede superar los 250 caracteres.")]
    string Descripcion
);

public record ActividadUpdateDTO(
    [Required(ErrorMessage = "El nombre de la actividad es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre de la actividad no puede superar los 100 caracteres.")]
    string Nombre,

    [MaxLength(250, ErrorMessage = "La descripción no puede superar los 250 caracteres.")]
    string Descripcion
);

public record ActividadResponseDTO(
    Guid Id,
    string Nombre,
    string Descripcion
);
