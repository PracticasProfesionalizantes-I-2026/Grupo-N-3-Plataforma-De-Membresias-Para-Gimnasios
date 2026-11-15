using System.Text.RegularExpressions;
using GymPlatform.BusinessLogic.ExternalServices;
using GymPlatform.DataAccess.Entities;
using GymPlatform.DataAccess.Repositories;
using GymPlatform.Shared.Common;
using GymPlatform.Shared.Enums;
using GymPlatform.Shared.Exceptions;
using GymPlatform.Shared.UseCases;
using Microsoft.EntityFrameworkCore;

namespace GymPlatform.BusinessLogic.UseCases.CU01_RegistrarSocio;

public class RegistrarSocioUseCase : IRegistrarSocioUseCase
{
    private readonly ISocioRepository _socioRepository;
    private readonly IPlanMembresiaRepository _planRepository;
    private readonly IMembresiaRepository _membresiaRepository;
    private readonly IPagoRepository _pagoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificacionService _notificacionService;

    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DniRegex = new(@"^\d{7,10}$", RegexOptions.Compiled);

    public RegistrarSocioUseCase(
        ISocioRepository socioRepository,
        IPlanMembresiaRepository planRepository,
        IMembresiaRepository membresiaRepository,
        IPagoRepository pagoRepository,
        IUnitOfWork unitOfWork,
        INotificacionService notificacionService)
    {
        _socioRepository = socioRepository;
        _planRepository = planRepository;
        _membresiaRepository = membresiaRepository;
        _pagoRepository = pagoRepository;
        _unitOfWork = unitOfWork;
        _notificacionService = notificacionService;
    }

    public async Task<Result<SocioRegistradoResultDTO>> ExecuteAsync(RegistrarSocioRequestDTO request)
    {
        try
        {
            // 1. Defensa y validaciones a prueba de usuarios
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Nombre))
            {
                validationErrors.Add("El nombre completo del socio es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(request.Dni))
            {
                validationErrors.Add("El DNI es obligatorio.");
            }
            else if (!DniRegex.IsMatch(request.Dni.Trim()))
            {
                validationErrors.Add("El DNI debe contener entre 7 y 10 dígitos numéricos sin puntos ni letras.");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                validationErrors.Add("El correo electrónico es obligatorio.");
            }
            else if (!EmailRegex.IsMatch(request.Email.Trim()))
            {
                validationErrors.Add("El formato del correo electrónico es inválido.");
            }

            // RN-01: Mayoría de edad (18 años o más)
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            var edad = hoy.Year - request.FechaNacimiento.Year;
            if (request.FechaNacimiento > hoy.AddYears(-edad))
            {
                edad--;
            }

            if (edad < 18)
            {
                validationErrors.Add("El socio debe tener al menos 18 años cumplidos para registrarse.");
            }

            if (validationErrors.Count > 0)
            {
                return Result<SocioRegistradoResultDTO>.Failure(validationErrors[0], validationErrors);
            }

            // 2. Unicidad en Base de Datos (DNI / Email)
            var socioExistenteDni = await _socioRepository.GetByDniAsync(request.Dni.Trim());
            if (socioExistenteDni is not null)
            {
                return Result<SocioRegistradoResultDTO>.Failure("El DNI ingresado ya se encuentra registrado en el sistema.");
            }

            var socioExistenteEmail = await _socioRepository.GetByEmailAsync(request.Email.Trim().ToLower());
            if (socioExistenteEmail is not null)
            {
                return Result<SocioRegistradoResultDTO>.Failure("El correo electrónico ingresado ya se encuentra registrado en el sistema.");
            }

            // 3. Plan inicial opcional
            PlanMembresia? planSeleccionado = null;
            if (request.PlanMembresiaId.HasValue)
            {
                planSeleccionado = await _planRepository.GetByIdAsync(request.PlanMembresiaId.Value);
                if (planSeleccionado is null)
                {
                    return Result<SocioRegistradoResultDTO>.Failure($"No se encontró el plan de membresía seleccionado (ID: {request.PlanMembresiaId.Value}).");
                }
            }

            // 4. Persistencia transaccional
            Guid socioId = Guid.NewGuid();
            var socio = new Socio
            {
                Id = socioId,
                Nombre = request.Nombre.Trim(),
                Dni = request.Dni.Trim(),
                Email = request.Email.Trim().ToLower(),
                Telefono = request.Telefono?.Trim() ?? string.Empty,
                FechaNacimiento = request.FechaNacimiento,
                FechaRegistro = DateTime.UtcNow,
                Activo = true
            };

            Guid? membresiaId = null;
            string? planNombre = null;
            DateTime? fechaVencimiento = null;
            string? numeroComprobante = null;

            await _unitOfWork.BeginTransactionAsync();

            await _socioRepository.CreateAsync(socio);

            if (planSeleccionado is not null)
            {
                membresiaId = Guid.NewGuid();
                planNombre = planSeleccionado.Nombre;
                var fechaInicio = DateTime.UtcNow;
                fechaVencimiento = fechaInicio.AddDays(planSeleccionado.DuracionDias);

                var membresia = new Membresia
                {
                    Id = membresiaId.Value,
                    SocioId = socio.Id,
                    PlanMembresiaId = planSeleccionado.Id,
                    FechaInicio = fechaInicio,
                    FechaVencimiento = fechaVencimiento.Value,
                    Estado = EstadoMembresia.Activa,
                    FechaRegistro = fechaInicio
                };

                await _membresiaRepository.CreateAsync(membresia);

                numeroComprobante = $"REC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
                var pago = new Pago
                {
                    Id = Guid.NewGuid(),
                    MembresiaId = membresia.Id,
                    Monto = planSeleccionado.Precio,
                    FechaPago = fechaInicio,
                    MetodoPago = request.MetodoPago ?? MetodoPago.Efectivo,
                    NumeroComprobante = numeroComprobante,
                    Estado = EstadoPago.Aprobado,
                    TransaccionExternaId = $"TX-INIT-{Guid.NewGuid().ToString()[..6].ToUpper()}"
                };

                await _pagoRepository.CreateAsync(pago);
            }

            await _unitOfWork.CommitTransactionAsync();

            // 5. Extensión: Notificación de bienvenida asíncrona y segura
            bool notificacionEnviada = false;
            string? warningNotificacion = null;
            try
            {
                await _notificacionService.EnviarBienvenidaAsync(socio.Email, socio.Nombre, planNombre);
                notificacionEnviada = true;
            }
            catch (Exception ex)
            {
                // La falla de notificación NO debe revertir el registro ni romper la aplicación
                notificacionEnviada = false;
                warningNotificacion = $"Socio registrado con éxito. Advertencia: No se pudo enviar el correo de bienvenida ({ex.Message}).";
            }

            var resultadoDto = new SocioRegistradoResultDTO(
                socio.Id,
                socio.Nombre,
                socio.Dni,
                socio.Email,
                socio.Telefono,
                socio.FechaNacimiento,
                membresiaId,
                planNombre,
                fechaVencimiento,
                numeroComprobante,
                notificacionEnviada
            );

            return Result<SocioRegistradoResultDTO>.Success(
                resultadoDto,
                "Socio registrado exitosamente.",
                warningNotificacion
            );
        }
        catch (DbUpdateException dbEx)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result<SocioRegistradoResultDTO>.Failure("Error al persistir el socio en la base de datos: Violación de integridad o restricción de unicidad.", new List<string> { dbEx.InnerException?.Message ?? dbEx.Message });
        }
        catch (GymException gymEx)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result<SocioRegistradoResultDTO>.Failure(gymEx.Message);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result<SocioRegistradoResultDTO>.Failure("Ocurrió un error inesperado durante el registro del socio: " + ex.Message);
        }
    }
}
