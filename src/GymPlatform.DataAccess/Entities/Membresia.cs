using GymPlatform.Shared.Enums;

namespace GymPlatform.DataAccess.Entities;

public class Membresia
{
    public Guid Id { get; set; }

    public Guid SocioId { get; set; }
    public Socio? Socio { get; set; }

    public Guid PlanMembresiaId { get; set; }
    public PlanMembresia? PlanMembresia { get; set; }

    public DateTime FechaInicio { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public EstadoMembresia Estado { get; set; } = EstadoMembresia.Activa;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}
