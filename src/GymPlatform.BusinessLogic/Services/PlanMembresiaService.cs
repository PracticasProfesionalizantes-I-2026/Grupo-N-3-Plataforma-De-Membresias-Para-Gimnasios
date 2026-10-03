using GymPlatform.BusinessLogic.Interfaces;
using GymPlatform.DataAccess.Entities;
using GymPlatform.DataAccess.Repositories;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.Exceptions;

namespace GymPlatform.BusinessLogic.Services;

public class PlanMembresiaService : IPlanMembresiaService
{
    private readonly IPlanMembresiaRepository _planRepository;

    public PlanMembresiaService(IPlanMembresiaRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<IEnumerable<PlanMembresiaResponseDTO>> GetAllAsync()
    {
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
        var plan = await _planRepository.GetByIdAsync(id);
        if (plan is null)
        {
            throw new NotFoundException($"No se encontró el plan de membresía con identificador {id}.");
        }
        return MapToResponseDTO(plan);
    }

    public async Task<PlanMembresiaResponseDTO> CreateAsync(PlanMembresiaCreateDTO dto)
    {
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
        return MapToResponseDTO(creado);
    }

    public async Task<PlanMembresiaResponseDTO> UpdateAsync(Guid id, PlanMembresiaUpdateDTO dto)
    {
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
            throw new DuplicateResourceException($"Ya existe otro plan de membresía con el nombre '{dto.Nombre}'.");
        }

        plan.Nombre = dto.Nombre.Trim();
        plan.Descripcion = dto.Descripcion?.Trim() ?? string.Empty;
        plan.Precio = dto.Precio;
        plan.DuracionDias = dto.DuracionDias;
        plan.Activo = dto.Activo;

        await _planRepository.UpdateAsync(plan);
        return MapToResponseDTO(plan);
    }

    public async Task DeleteAsync(Guid id)
    {
        var plan = await _planRepository.GetByIdAsync(id);
        if (plan is null)
        {
            throw new NotFoundException($"No se encontró el plan de membresía con identificador {id}.");
        }

        // Validación de dependencia de integridad de negocio
        var hasHorarios = await _planRepository.HasAssignedHorariosAsync(id);
        if (hasHorarios)
        {
            throw new DependencyConflictException("No se puede eliminar el plan de membresía porque tiene horarios o actividades asociadas.");
        }

        await _planRepository.DeleteAsync(plan);
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
