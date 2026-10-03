namespace GymPlatform.DataAccess.Entities;

public class PlanMembresia
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int DuracionDias { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Horario> Horarios { get; set; } = new List<Horario>();
}
