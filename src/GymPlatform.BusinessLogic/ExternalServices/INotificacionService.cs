namespace GymPlatform.BusinessLogic.ExternalServices;

public interface INotificacionService
{
    Task EnviarBienvenidaAsync(string email, string nombreSocio, string? planNombre);
    Task EnviarComprobanteRenovacionAsync(string email, string nombreSocio, string planNombre, string nroComprobante, decimal monto, DateTime nuevaFechaVencimiento);
}
