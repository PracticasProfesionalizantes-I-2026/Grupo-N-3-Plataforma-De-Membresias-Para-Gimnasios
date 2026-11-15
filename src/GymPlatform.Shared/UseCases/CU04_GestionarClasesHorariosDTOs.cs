namespace GymPlatform.Shared.UseCases;

public record ActividadEstadoUpdateDTO(
    Guid Id,
    bool Activo
);

public record HorarioConDetalleCompletoDTO(
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
    int CupoMaximo,
    bool Activo
);
