using GymPlatform.BusinessLogic.Interfaces;
using GymPlatform.DataAccess.Entities;
using GymPlatform.DataAccess.Repositories;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace GymPlatform.BusinessLogic.Services;

public class PlanMembresiaService : IPlanMembresiaService
{
    private readonly IPlanMembresiaRepository _planRepository;
    private readonly ILogger<PlanMembresiaService>? _logger;

    public PlanMembresiaService(IPlanMembresiaRepository planRepository, ILogger<PlanMembresiaService>? logger = null)
    {
        _planRepository = planRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<PlanMembresiaResponseDTO>> GetAllAsync()
    {
        _logger?.LogInformation("Consultando el listado de planes de membresía activos.");
        var planes = await _planRepository.GetAllAsync();
        var response = new List<PlanMembresiaResponseDTO>();
        foreach (var plan in planes)
        {
            response.Add(MapToResponseDTO(plan));
        }
        return response;
    }

    public async Task<PlanMembresiaResponseDTO> GetByIdAsync(Guid id)
    {
        _logger?.LogInformation("Buscando plan de membresía con ID: {PlanId}", id);
        var plan = await _planRepository.GetByIdAsync(id);
        if (plan is null)
        {
            _logger?.LogWarning("No se encontró el plan de membresía con ID {PlanId}", id);
            throw new NotFoundException($"No se encontró el plan de membresía con identificador {id}.");
        }
        return MapToResponseDTO(plan);
    }

    public async Task<PlanMembresiaResponseDTO> CreateAsync(PlanMembresiaCreateDTO dto)
    {
        _logger?.LogInformation("Creando nuevo plan de membresía con nombre: '{Nombre}'", dto.Nombre);

        // 1. Validaciones básicas (HTTP 400)
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            throw new ValidationException("El nombre del plan de membresía es obligatorio.");
        }

        // RN-02: Precio > 0 y DuracionDias > 0
        if (dto.Precio <= 0)
        {
            throw new ValidationException("El precio del plan debe ser mayor a cero.");
        }

        if (dto.DuracionDias <= 0)
        {
            throw new ValidationException("La duración del plan debe ser de al menos 1 día.");
        }

        // RN-02: Unicidad de nombre de plan (HTTP 409)
        var existente = await _planRepository.GetByNombreAsync(dto.Nombre.Trim());
        if (existente is not null)
        {
            _logger?.LogWarning("Conflicto de unicidad al crear plan con nombre ya existente: '{Nombre}'", dto.Nombre);
            throw new DuplicateResourceException($"Ya existe un plan de membresía registrado con el nombre '{dto.Nombre}'.");
        }

        var plan = new PlanMembresia
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion?.Trim() ?? string.Empty,
            Precio = dto.Precio,
            DuracionDias = dto.DuracionDias,
            Activo = true
        };

        var creado = await _planRepository.CreateAsync(plan);
        _logger?.LogInformation("Plan de membresía creado exitosamente con ID: {PlanId}", creado.Id);
        return MapToResponseDTO(creado);
    }

    public async Task<PlanMembresiaResponseDTO> UpdateAsync(Guid id, PlanMembresiaUpdateDTO dto)
    {
        _logger?.LogInformation("Actualizando plan de membresía ID: {PlanId}", id);
        var plan = await _planRepository.GetByIdAsync(id);
        if (plan is null)
        {
            throw new NotFoundException($"No se encontró el plan de membresía con identificador {id}.");
        }

        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            throw new ValidationException("El nombre del plan de membresía es obligatorio.");
        }

        if (dto.Precio <= 0)
        {
            throw new ValidationException("El precio del plan debe ser mayor a cero.");
        }

        if (dto.DuracionDias <= 0)
        {
            throw new ValidationException("La duración del plan debe ser de al menos 1 día.");
        }

        // RN-02: Unicidad de nombre si cambia
        var existente = await _planRepository.GetByNombreAsync(dto.Nombre.Trim());
        if (existente is not null && existente.Id != id)
        {
            _logger?.LogWarning("Conflicto al actualizar plan: nombre '{Nombre}' ya en uso por otro plan.", dto.Nombre);
            throw new DuplicateResourceException($"Ya existe otro plan de membresía con el nombre '{dto.Nombre}'.");
        }

        plan.Nombre = dto.Nombre.Trim();
        plan.Descripcion = dto.Descripcion?.Trim() ?? string.Empty;
        plan.Precio = dto.Precio;
        plan.DuracionDias = dto.DuracionDias;
        plan.Activo = dto.Activo;

        await _planRepository.UpdateAsync(plan);
        _logger?.LogInformation("Plan de membresía ID {PlanId} actualizado.", id);
        return MapToResponseDTO(plan);
    }

    public async Task<PlanMembresiaResponseDTO> DesactivarAsync(Guid id)
    {
        _logger?.LogInformation("Desactivando lógicamente plan de membresía ID: {PlanId}", id);
        var plan = await _planRepository.GetByIdAsync(id);
        if (plan is null)
        {
            throw new NotFoundException($"No se encontró el plan de membresía con identificador {id}.");
        }

        plan.Activo = false;
        await _planRepository.UpdateAsync(plan);
        _logger?.LogInformation("Plan de membresía ID {PlanId} desactivado.", id);
        return MapToResponseDTO(plan);
    }

    public async Task DeleteAsync(Guid id)
    {
        _logger?.LogInformation("Ejecutando baja lógica (Soft Delete) del plan de membresía ID: {PlanId}", id);
        var plan = await _planRepository.GetByIdAsync(id);
        if (plan is null)
        {
            throw new NotFoundException($"No se encontró el plan de membresía con identificador {id}.");
        }

        // Validación de dependencia de integridad de negocio
        var hasHorarios = await _planRepository.HasAssignedHorariosAsync(id);
        if (hasHorarios)
        {
            _logger?.LogWarning("Intento de eliminar plan con horarios asignados: {PlanId}", id);
            throw new DependencyConflictException("No se puede eliminar el plan de membresía porque tiene horarios o actividades asociadas.");
        }

        // Issue #6: Modificar el borrado físico por borrado lógico
        plan.Activo = false;
        await _planRepository.UpdateAsync(plan);
        _logger?.LogInformation("Plan de membresía ID {PlanId} marcado como inactivo (soft delete).", id);
    }

    private static PlanMembresiaResponseDTO MapToResponseDTO(PlanMembresia plan)
    {
        return new PlanMembresiaResponseDTO(
            plan.Id,
            plan.Nombre,
            plan.Descripcion,
            plan.Precio,
            plan.DuracionDias,
            plan.Activo
        );
    }
}
