namespace GymPlatform.DataAccess.Entities;

public class Horario
{
    public Guid Id { get; set; }
    public Guid ActividadId { get; set; }
    public Actividad? Actividad { get; set; }

    public Guid PlanMembresiaId { get; set; }
    public PlanMembresia? PlanMembresia { get; set; }

    public string Profesor { get; set; } = string.Empty;
    public string Sala { get; set; } = string.Empty;
    public DayOfWeek DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public int CupoMaximo { get; set; }
}
