namespace GymPlatform.BusinessLogic.ExternalServices;

public class SimulatedEmailNotificacionService : INotificacionService
{
    public async Task EnviarBienvenidaAsync(string email, string nombreSocio, string? planNombre)
    {
        await Task.Delay(100);

        // Permite simular una falla de red/SMTP controlada en pruebas
        if (email.EndsWith("@fail.test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Servidor SMTP de notificaciones no disponible (Error 500 Connection Timeout).");
        }

        // Simulación exitosa de envío
    }

    public async Task EnviarComprobanteRenovacionAsync(string email, string nombreSocio, string planNombre, string nroComprobante, decimal monto, DateTime nuevaFechaVencimiento)
    {
        await Task.Delay(100);

        if (email.EndsWith("@fail.test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("No se pudo conectar al servidor de correos salientes.");
        }
    }
}
