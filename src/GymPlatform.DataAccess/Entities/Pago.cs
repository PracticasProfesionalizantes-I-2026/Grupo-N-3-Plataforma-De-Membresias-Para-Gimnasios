using GymPlatform.Shared.Enums;

namespace GymPlatform.DataAccess.Entities;

public class Pago
{
    public Guid Id { get; set; }

    public Guid MembresiaId { get; set; }
    public Membresia? Membresia { get; set; }

    public decimal Monto { get; set; }
    public DateTime FechaPago { get; set; } = DateTime.UtcNow;
    public MetodoPago MetodoPago { get; set; } = MetodoPago.TarjetaCredito;
    public string NumeroComprobante { get; set; } = string.Empty;
    public EstadoPago Estado { get; set; } = EstadoPago.Aprobado;
    public string TransaccionExternaId { get; set; } = string.Empty;
}
