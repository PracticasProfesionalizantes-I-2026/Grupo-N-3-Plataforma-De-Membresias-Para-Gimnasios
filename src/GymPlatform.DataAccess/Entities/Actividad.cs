namespace GymPlatform.DataAccess.Entities;

public class Actividad
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    public ICollection<Horario> Horarios { get; set; } = new List<Horario>();
}
