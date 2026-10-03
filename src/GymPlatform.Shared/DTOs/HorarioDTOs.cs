namespace GymPlatform.Shared.DTOs;

public record HorarioCreateDTO(
    Guid ActividadId,
    Guid PlanMembresiaId,
    string Profesor,
    string Sala,
    DayOfWeek DiaSemana,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    int CupoMaximo
);

public record HorarioUpdateDTO(
    Guid ActividadId,
    Guid PlanMembresiaId,
    string Profesor,
    string Sala,
    DayOfWeek DiaSemana,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
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
