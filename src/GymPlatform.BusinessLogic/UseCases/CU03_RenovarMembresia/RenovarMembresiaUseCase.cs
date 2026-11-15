using GymPlatform.BusinessLogic.ExternalServices;
using GymPlatform.DataAccess.Entities;
using GymPlatform.DataAccess.Repositories;
using GymPlatform.Shared.Common;
using GymPlatform.Shared.Enums;
using GymPlatform.Shared.UseCases;
using Microsoft.EntityFrameworkCore;

namespace GymPlatform.BusinessLogic.UseCases.CU03_RenovarMembresia;

public class RenovarMembresiaUseCase : IRenovarMembresiaUseCase
{
    private readonly ISocioRepository _socioRepository;
    private readonly IPlanMembresiaRepository _planRepository;
    private readonly IMembresiaRepository _membresiaRepository;
    private readonly IPagoRepository _pagoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasarelaPagoService _pasarelaPagoService;
    private readonly INotificacionService _notificacionService;

    public RenovarMembresiaUseCase(
        ISocioRepository socioRepository,
        IPlanMembresiaRepository planRepository,
        IMembresiaRepository membresiaRepository,
        IPagoRepository pagoRepository,
        IUnitOfWork unitOfWork,
        IPasarelaPagoService pasarelaPagoService,
        INotificacionService notificacionService)
    {
        _socioRepository = socioRepository;
        _planRepository = planRepository;
        _membresiaRepository = membresiaRepository;
        _pagoRepository = pagoRepository;
        _unitOfWork = unitOfWork;
        _pasarelaPagoService = pasarelaPagoService;
        _notificacionService = notificacionService;
    }

    public async Task<Result<IEnumerable<MembresiaResumenDTO>>> ObtenerMembresiasPorSocioAsync(Guid socioId)
    {
        try
        {
            var socio = await _socioRepository.GetByIdAsync(socioId);
            if (socio is null)
            {
                return Result<IEnumerable<MembresiaResumenDTO>>.Failure($"No se encontró el socio con identificador {socioId}.");
            }

            var membresias = await _membresiaRepository.GetBySocioIdAsync(socioId);
            var ahora = DateTime.UtcNow;

            var lista = membresias.Select(m =>
            {
                var diasRestantes = (int)Math.Max(0, (m.FechaVencimiento - ahora).TotalDays);
                var estadoActual = m.FechaVencimiento < ahora ? EstadoMembresia.Vencida : m.Estado;

                return new MembresiaResumenDTO(
                    m.Id,
                    m.PlanMembresiaId,
                    m.PlanMembresia?.Nombre ?? "Plan no identificado",
                    m.PlanMembresia?.Precio ?? 0m,
                    m.FechaInicio,
                    m.FechaVencimiento,
                    estadoActual,
                    diasRestantes
                );
            }).ToList();

            return Result<IEnumerable<MembresiaResumenDTO>>.Success(lista);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<MembresiaResumenDTO>>.Failure("Error al consultar las membresías del socio: " + ex.Message);
        }
    }

    public async Task<Result<RenovacionResultDTO>> ExecuteAsync(RenovarMembresiaRequestDTO request)
    {
        try
        {
            // 1. Validaciones previas de existencia
            var socio = await _socioRepository.GetByIdAsync(request.SocioId);
            if (socio is null)
            {
                return Result<RenovacionResultDTO>.Failure("El socio especificado no existe en el sistema.");
            }

            var plan = await _planRepository.GetByIdAsync(request.PlanMembresiaId);
            if (plan is null)
            {
                return Result<RenovacionResultDTO>.Failure("El plan de membresía seleccionado no existe en el catálogo.");
            }

            // Validar existencia de membresía previa o crear nueva para el plan
            var membresia = await _membresiaRepository.GetLatestBySocioAndPlanAsync(request.SocioId, request.PlanMembresiaId);

            // Validaciones de datos de tarjeta si el pago es digital
            if (request.MetodoPago is MetodoPago.TarjetaCredito or MetodoPago.TarjetaDebito)
            {
                if (request.DatosTarjeta is null)
                {
                    return Result<RenovacionResultDTO>.Failure("Debe ingresar los datos completos de la tarjeta para abonar la renovación.");
                }

                if (string.IsNullOrWhiteSpace(request.DatosTarjeta.NumeroTarjeta) || request.DatosTarjeta.NumeroTarjeta.Replace(" ", "").Length < 13)
                {
                    return Result<RenovacionResultDTO>.Failure("El número de tarjeta ingresado no es válido (debe tener entre 13 y 19 dígitos).");
                }

                if (string.IsNullOrWhiteSpace(request.DatosTarjeta.Titular))
                {
                    return Result<RenovacionResultDTO>.Failure("El nombre del titular de la tarjeta es obligatorio.");
                }

                if (string.IsNullOrWhiteSpace(request.DatosTarjeta.Cvv) || request.DatosTarjeta.Cvv.Trim().Length < 3)
                {
                    return Result<RenovacionResultDTO>.Failure("El código de seguridad CVV debe tener al menos 3 dígitos.");
                }

                if (request.DatosTarjeta.MesVencimiento < 1 || request.DatosTarjeta.MesVencimiento > 12)
                {
                    return Result<RenovacionResultDTO>.Failure("El mes de vencimiento de la tarjeta debe estar entre 1 y 12.");
                }

                if (request.DatosTarjeta.AnioVencimiento < DateTime.UtcNow.Year % 100 && request.DatosTarjeta.AnioVencimiento < DateTime.UtcNow.Year)
                {
                    return Result<RenovacionResultDTO>.Failure("La tarjeta ingresada se encuentra expirada.");
                }
            }

            // 2. Procesamiento de pago a través de pasarela externa protegida
            ResultadoTransaccionPago resultadoPago;
            try
            {
                var solicitudPago = new SolicitudPagoPasarelaDTO(
                    plan.Precio,
                    $"Renovación Membresía {plan.Nombre} - Socio: {socio.Nombre}",
                    request.MetodoPago,
                    request.DatosTarjeta
                );

                resultadoPago = await _pasarelaPagoService.ProcesarPagoAsync(solicitudPago);
            }
            catch (HttpRequestException httpEx)
            {
                // Manejo controlado de fallos de red / timeouts de pasarela externa sin que la app crashee
                return Result<RenovacionResultDTO>.Failure(
                    "Error de conexión con la pasarela de pagos externa (Timeout / Servidor inaccesible). Por favor, compruebe su conexión o reintente en unos minutos. Mensaje: " + httpEx.Message
                );
            }
            catch (TimeoutException timeEx)
            {
                return Result<RenovacionResultDTO>.Failure(
                    "Tiempo de espera agotado al conectar con el procesador de pagos. La operación fue cancelada de forma segura. Detalle: " + timeEx.Message
                );
            }
            catch (Exception ex)
            {
                return Result<RenovacionResultDTO>.Failure(
                    "Ocurrió un error inesperado al invocar la pasarela de pagos: " + ex.Message
                );
            }

            // 3. Verificación de rechazo de pago
            if (!resultadoPago.Aprobado)
            {
                return Result<RenovacionResultDTO>.Failure(
                    $"El pago no pudo ser procesado: {resultadoPago.Mensaje}"
                );
            }

            // 4. Registro de pago y actualización de vigencia bajo transacción
            var ahora = DateTime.UtcNow;
            DateTime nuevaFechaInicio;
            DateTime nuevaFechaVencimiento;

            await _unitOfWork.BeginTransactionAsync();

            if (membresia is null)
            {
                // Si el socio no tenía membresía previa de este plan, se da de alta una nueva
                nuevaFechaInicio = ahora;
                nuevaFechaVencimiento = ahora.AddDays(plan.DuracionDias);

                membresia = new Membresia
                {
                    Id = Guid.NewGuid(),
                    SocioId = socio.Id,
                    PlanMembresiaId = plan.Id,
                    FechaInicio = nuevaFechaInicio,
                    FechaVencimiento = nuevaFechaVencimiento,
                    Estado = EstadoMembresia.Activa,
                    FechaRegistro = ahora
                };

                await _membresiaRepository.CreateAsync(membresia);
            }
            else
            {
                // Si ya tenía membresía de este plan:
                // Si sigue vigente, se extiende a partir de la fecha de vencimiento actual.
                // Si ya venció, se reactiva a partir de la fecha de hoy.
                if (membresia.FechaVencimiento > ahora)
                {
                    nuevaFechaInicio = membresia.FechaInicio;
                    nuevaFechaVencimiento = membresia.FechaVencimiento.AddDays(plan.DuracionDias);
                }
                else
                {
                    nuevaFechaInicio = ahora;
                    nuevaFechaVencimiento = ahora.AddDays(plan.DuracionDias);
                }

                membresia.FechaInicio = nuevaFechaInicio;
                membresia.FechaVencimiento = nuevaFechaVencimiento;
                membresia.Estado = EstadoMembresia.Activa;

                await _membresiaRepository.UpdateAsync(membresia);
            }

            var nroComprobante = $"REC-{ahora:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
            var pago = new Pago
            {
                Id = Guid.NewGuid(),
                MembresiaId = membresia.Id,
                Monto = plan.Precio,
                FechaPago = ahora,
                MetodoPago = request.MetodoPago,
                NumeroComprobante = nroComprobante,
                Estado = EstadoPago.Aprobado,
                TransaccionExternaId = resultadoPago.CodigoTransaccion
            };

            await _pagoRepository.CreateAsync(pago);

            await _unitOfWork.CommitTransactionAsync();

            // 5. Envío seguro de comprobante
            string? warningNotif = null;
            try
            {
                await _notificacionService.EnviarComprobanteRenovacionAsync(
                    socio.Email,
                    socio.Nombre,
                    plan.Nombre,
                    nroComprobante,
                    plan.Precio,
                    nuevaFechaVencimiento
                );
            }
            catch (Exception ex)
            {
                warningNotif = $"Pago y renovación registrados exitosamente. Advertencia: No se pudo despachar el comprobante por correo electrónico ({ex.Message}).";
            }

            var resultadoDto = new RenovacionResultDTO(
                membresia.Id,
                socio.Id,
                socio.Nombre,
                plan.Nombre,
                plan.Precio,
                nuevaFechaInicio,
                nuevaFechaVencimiento,
                nroComprobante,
                resultadoPago.CodigoTransaccion,
                ahora
            );

            return Result<RenovacionResultDTO>.Success(
                resultadoDto,
                $"¡Membresía renovada con éxito! La nueva vigencia es hasta el {nuevaFechaVencimiento:dd/MM/yyyy}.",
                warningNotif
            );
        }
        catch (DbUpdateException dbEx)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result<RenovacionResultDTO>.Failure(
                "Error al actualizar la base de datos tras el pago: " + (dbEx.InnerException?.Message ?? dbEx.Message)
            );
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result<RenovacionResultDTO>.Failure(
                "Ocurrió un error inesperado al procesar la renovación: " + ex.Message
            );
        }
    }
}
