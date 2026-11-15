using GymPlatform.Shared.Enums;

namespace GymPlatform.BusinessLogic.ExternalServices;

public class SimulatedPasarelaPagoService : IPasarelaPagoService
{
    public async Task<ResultadoTransaccionPago> ProcesarPagoAsync(SolicitudPagoPasarelaDTO solicitud)
    {
        // Simular latencia de red
        await Task.Delay(150);

        if (solicitud.Monto <= 0)
        {
            return new ResultadoTransaccionPago(
                false,
                string.Empty,
                "El monto de la operación debe ser mayor a cero.",
                null
            );
        }

        if (solicitud.MetodoPago == MetodoPago.TarjetaCredito || solicitud.MetodoPago == MetodoPago.TarjetaDebito)
        {
            if (solicitud.DatosTarjeta is null)
            {
                return new ResultadoTransaccionPago(
                    false,
                    string.Empty,
                    "Se requieren los datos de la tarjeta para procesar el pago electrónico.",
                    null
                );
            }

            var numClean = solicitud.DatosTarjeta.NumeroTarjeta.Replace(" ", "").Replace("-", "").Trim();

            // Simulación de timeout o fallo de red
            if (numClean.EndsWith("9999"))
            {
                throw new HttpRequestException("Error de comunicación HTTP con la pasarela de pagos: Gateway Timeout (504).");
            }

            // Simulación de tarjeta rechazada / fondos insuficientes
            if (numClean.EndsWith("0000"))
            {
                return new ResultadoTransaccionPago(
                    false,
                    string.Empty,
                    "Transacción rechazada por el emisor: Fondos insuficientes o límite excedido.",
                    null
                );
            }

            if (solicitud.DatosTarjeta.Cvv.Trim() == "000")
            {
                return new ResultadoTransaccionPago(
                    false,
                    string.Empty,
                    "Código de seguridad (CVV) inválido.",
                    null
                );
            }
        }

        // Transacción aprobada
        var txId = $"TX-PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
        var authCode = $"AUTH-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        return new ResultadoTransaccionPago(
            true,
            txId,
            "Transacción aprobada satisfactoriamente por la entidad financiera.",
            authCode
        );
    }
}
