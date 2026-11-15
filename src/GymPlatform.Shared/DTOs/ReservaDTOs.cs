using System.ComponentModel.DataAnnotations;

namespace GymPlatform.Shared.DTOs;

public record ReservaCreateDTO(
    [Required(ErrorMessage = "El identificador del socio es obligatorio.")]
    Guid SocioId,

    [Required(ErrorMessage = "El identificador del horario es obligatorio.")]
    Guid HorarioId,

    [Required(ErrorMessage = "La fecha del turno es obligatoria.")]
    DateOnly FechaTurno
);

public record ReservaResponseDTO(
    Guid Id,
    Guid SocioId,
    string SocioNombre,
    Guid HorarioId,
    string ActividadNombre,
    string Profesor,
    string Sala,
    DayOfWeek DiaSemana,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    DateOnly FechaTurno,
    bool Asistencia,
    DateTime FechaCreacion
);
