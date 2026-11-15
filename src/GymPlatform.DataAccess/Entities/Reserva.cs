namespace GymPlatform.DataAccess.Entities;

public class Reserva
{
    public Guid Id { get; set; }

    public Guid SocioId { get; set; }
    public Socio? Socio { get; set; }

    public Guid HorarioId { get; set; }
    public Horario? Horario { get; set; }

    public DateOnly FechaTurno { get; set; }
    public bool Asistencia { get; set; } = false;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

