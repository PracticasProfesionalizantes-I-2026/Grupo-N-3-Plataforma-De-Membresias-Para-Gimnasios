using GymPlatform.Shared.Enums;
using GymPlatform.Shared.UseCases;

namespace GymPlatform.BusinessLogic.ExternalServices;

public record SolicitudPagoPasarelaDTO(
    decimal Monto,
    string Descripcion,
    MetodoPago MetodoPago,
    DatosTarjetaDTO? DatosTarjeta
);

public record ResultadoTransaccionPago(
    bool Aprobado,
    string CodigoTransaccion,
    string Mensaje,
    string? CodigoAutorizacion
);

public interface IPasarelaPagoService
{
    Task<ResultadoTransaccionPago> ProcesarPagoAsync(SolicitudPagoPasarelaDTO solicitud);
}
