using GymPlatform.Shared.Enums;

namespace GymPlatform.Shared.UseCases;

public record DatosTarjetaDTO(
    string NumeroTarjeta,
    string Titular,
    int MesVencimiento,
    int AnioVencimiento,
    string Cvv
);

public record RenovarMembresiaRequestDTO(
    Guid SocioId,
    Guid PlanMembresiaId,
    MetodoPago MetodoPago,
    DatosTarjetaDTO? DatosTarjeta = null
);

public record RenovacionResultDTO(
    Guid MembresiaId,
    Guid SocioId,
    string NombreSocio,
    string PlanNombre,
    decimal MontoAbonado,
    DateTime FechaInicio,
    DateTime NuevaFechaVencimiento,
    string NumeroComprobante,
    string CodigoTransaccion,
    DateTime FechaPago
);

public record MembresiaResumenDTO(
    Guid MembresiaId,
    Guid PlanId,
    string PlanNombre,
    decimal Precio,
    DateTime FechaInicio,
    DateTime FechaVencimiento,
    EstadoMembresia Estado,
    int DiasRestantes
);
