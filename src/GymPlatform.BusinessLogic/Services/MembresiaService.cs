using GymPlatform.BusinessLogic.Interfaces;
using GymPlatform.DataAccess.Entities;
using GymPlatform.DataAccess.Repositories;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.Enums;
using GymPlatform.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace GymPlatform.BusinessLogic.Services;

public class MembresiaService : IMembresiaService
{
    private readonly IMembresiaRepository _membresiaRepository;
    private readonly ISocioRepository _socioRepository;
    private readonly IPlanMembresiaRepository _planRepository;
    private readonly ILogger<MembresiaService>? _logger;

    public MembresiaService(
        IMembresiaRepository membresiaRepository,
        ISocioRepository socioRepository,
        IPlanMembresiaRepository planRepository,
        ILogger<MembresiaService>? logger = null)
    {
        _membresiaRepository = membresiaRepository;
        _socioRepository = socioRepository;
        _planRepository = planRepository;
        _logger = logger;
    }

    public async Task<MembresiaResponseDTO> ContratarMembresiaAsync(MembresiaCreateDTO dto)
    {
        _logger?.LogInformation("Iniciando contratación de membresía para socio {SocioId} y plan {PlanId}", dto.SocioId, dto.PlanMembresiaId);

        var socio = await _socioRepository.GetByIdAsync(dto.SocioId);
        if (socio is null || !socio.Activo)
        {
            _logger?.LogWarning("Socio no encontrado o inactivo: {SocioId}", dto.SocioId);
            throw new NotFoundException($"No existe un socio activo con identificador {dto.SocioId}.");
        }

        var plan = await _planRepository.GetByIdAsync(dto.PlanMembresiaId);
        if (plan is null || !plan.Activo)
        {
            _logger?.LogWarning("Plan de membresía no encontrado o inactivo: {PlanId}", dto.PlanMembresiaId);
            throw new ValidationException($"No existe un plan de membresía activo con identificador {dto.PlanMembresiaId}.");
        }

        var fechaInicio = dto.FechaInicio.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dto.FechaInicio, DateTimeKind.Utc)
            : dto.FechaInicio.ToUniversalTime();

        var fechaVencimiento = fechaInicio.AddDays(plan.DuracionDias);

        var membresia = new Membresia
        {
            SocioId = dto.SocioId,
            PlanMembresiaId = dto.PlanMembresiaId,
            FechaInicio = fechaInicio,
            FechaVencimiento = fechaVencimiento,
            Activo = true,
            Estado = EstadoMembresia.Activa,
            FechaRegistro = DateTime.UtcNow
        };

        var creada = await _membresiaRepository.CreateAsync(membresia);
        creada.Socio = socio;
        creada.PlanMembresia = plan;

        _logger?.LogInformation("Membresía creada exitosamente con ID: {MembresiaId}", creada.Id);
        return MapToResponseDTO(creada);
    }

    public async Task<IEnumerable<MembresiaResponseDTO>> GetMembresiasBySocioAsync(Guid socioId)
    {
        var socio = await _socioRepository.GetByIdAsync(socioId);
        if (socio is null)
        {
            throw new NotFoundException($"No se encontró el socio con identificador {socioId}.");
        }

        var membresias = await _membresiaRepository.GetBySocioIdAsync(socioId);
        return membresias.Select(MapToResponseDTO);
    }

    public async Task<IEnumerable<MembresiaResponseDTO>> GetAllAsync()
    {
        var membresias = await _membresiaRepository.GetAllAsync();
        return membresias.Select(MapToResponseDTO);
    }

    private static MembresiaResponseDTO MapToResponseDTO(Membresia m)
    {
        return new MembresiaResponseDTO(
            m.Id,
            m.SocioId,
            m.Socio?.Nombre ?? string.Empty,
            m.PlanMembresiaId,
            m.PlanMembresia?.Nombre ?? string.Empty,
            m.FechaInicio,
            m.FechaVencimiento,
            m.Activo,
            m.Estado,
            m.FechaRegistro
        );
    }
}

