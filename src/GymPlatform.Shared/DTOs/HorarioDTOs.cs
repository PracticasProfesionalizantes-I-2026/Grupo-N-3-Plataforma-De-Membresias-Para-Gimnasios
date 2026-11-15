using System.ComponentModel.DataAnnotations;

namespace GymPlatform.Shared.DTOs;

public record HorarioCreateDTO(
    [Required(ErrorMessage = "El identificador de la actividad es obligatorio.")]
    Guid ActividadId,

    [Required(ErrorMessage = "El identificador del plan de membresía es obligatorio.")]
    Guid PlanMembresiaId,

    [Required(ErrorMessage = "El nombre del profesor es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre del profesor no puede superar los 100 caracteres.")]
    string Profesor,

    [Required(ErrorMessage = "La sala o espacio asignado es obligatorio.")]
    [MaxLength(50, ErrorMessage = "La sala no puede superar los 50 caracteres.")]
    string Sala,

    [Required(ErrorMessage = "El día de la semana es obligatorio.")]
    DayOfWeek DiaSemana,

    [Required(ErrorMessage = "La hora de inicio es obligatoria.")]
    TimeOnly HoraInicio,

    [Required(ErrorMessage = "La hora de fin es obligatoria.")]
    TimeOnly HoraFin,

    [Range(1, 500, ErrorMessage = "El cupo máximo debe estar comprendido estrictamente entre 1 y 500.")]
    int CupoMaximo
);

public record HorarioUpdateDTO(
    [Required(ErrorMessage = "El identificador de la actividad es obligatorio.")]
    Guid ActividadId,

    [Required(ErrorMessage = "El identificador del plan de membresía es obligatorio.")]
    Guid PlanMembresiaId,

    [Required(ErrorMessage = "El nombre del profesor es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre del profesor no puede superar los 100 caracteres.")]
    string Profesor,

    [Required(ErrorMessage = "La sala o espacio asignado es obligatorio.")]
    [MaxLength(50, ErrorMessage = "La sala no puede superar los 50 caracteres.")]
    string Sala,

    [Required(ErrorMessage = "El día de la semana es obligatorio.")]
    DayOfWeek DiaSemana,

    [Required(ErrorMessage = "La hora de inicio es obligatoria.")]
    TimeOnly HoraInicio,

    [Required(ErrorMessage = "La hora de fin es obligatoria.")]
    TimeOnly HoraFin,

    [Range(1, 500, ErrorMessage = "El cupo máximo debe estar comprendido estrictamente entre 1 y 500.")]
    int CupoMaximo
);

public record HorarioResponseDTO(
    Guid Id,
    Guid ActividadId,
    string ActividadNombre,
    Guid PlanMembresiaId,
    string PlanNombre,
    string Profesor,
    string Sala,
    DayOfWeek DiaSemana,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    int CupoMaximo
);
