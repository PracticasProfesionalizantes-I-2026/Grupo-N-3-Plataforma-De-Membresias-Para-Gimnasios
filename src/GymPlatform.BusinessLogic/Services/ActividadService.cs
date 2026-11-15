using GymPlatform.BusinessLogic.Interfaces;
using GymPlatform.DataAccess.Entities;
using GymPlatform.DataAccess.Repositories;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace GymPlatform.BusinessLogic.Services;

public class ActividadService : IActividadService
{
    private readonly IActividadRepository _actividadRepository;
    private readonly ILogger<ActividadService>? _logger;

    public ActividadService(IActividadRepository actividadRepository, ILogger<ActividadService>? logger = null)
    {
        _actividadRepository = actividadRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<ActividadResponseDTO>> GetAllAsync()
    {
        _logger?.LogInformation("Consultando todas las actividades.");
        var actividades = await _actividadRepository.GetAllAsync();
        var response = new List<ActividadResponseDTO>();
        foreach (var act in actividades)
        {
            response.Add(MapToResponseDTO(act));
        }
        return response;
    }

    public async Task<ActividadResponseDTO> GetByIdAsync(Guid id)
    {
        _logger?.LogInformation("Buscando actividad con ID: {ActividadId}", id);
        var actividad = await _actividadRepository.GetByIdAsync(id);
        if (actividad is null)
        {
            _logger?.LogWarning("No se encontró la actividad con ID: {ActividadId}", id);
            throw new NotFoundException($"No se encontró la actividad con identificador {id}.");
        }
        return MapToResponseDTO(actividad);
    }

    public async Task<ActividadResponseDTO> CreateAsync(ActividadCreateDTO dto)
    {
        _logger?.LogInformation("Registrando nueva actividad: '{Nombre}'", dto.Nombre);
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            throw new ValidationException("El nombre de la actividad es obligatorio.");
        }

        // RN-04: Unicidad de nombre de actividad
        var existente = await _actividadRepository.GetByNombreAsync(dto.Nombre.Trim());
        if (existente is not null)
        {
            _logger?.LogWarning("Conflicto de unicidad en actividad: '{Nombre}' ya existe.", dto.Nombre);
            throw new DuplicateResourceException($"Ya existe una actividad registrada con el nombre '{dto.Nombre}'.");
        }

        var actividad = new Actividad
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion?.Trim() ?? string.Empty
        };

        var creada = await _actividadRepository.CreateAsync(actividad);
        _logger?.LogInformation("Actividad creada con ID: {ActividadId}", creada.Id);
        return MapToResponseDTO(creada);
    }

    public async Task<ActividadResponseDTO> UpdateAsync(Guid id, ActividadUpdateDTO dto)
    {
        _logger?.LogInformation("Actualizando actividad ID: {ActividadId}", id);
        var actividad = await _actividadRepository.GetByIdAsync(id);
        if (actividad is null)
        {
            throw new NotFoundException($"No se encontró la actividad con identificador {id}.");
        }

        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            throw new ValidationException("El nombre de la actividad es obligatorio.");
        }

        var existente = await _actividadRepository.GetByNombreAsync(dto.Nombre.Trim());
        if (existente is not null && existente.Id != id)
        {
            _logger?.LogWarning("Conflicto al actualizar actividad: nombre '{Nombre}' en uso.", dto.Nombre);
            throw new DuplicateResourceException($"Ya existe otra actividad con el nombre '{dto.Nombre}'.");
        }

        actividad.Nombre = dto.Nombre.Trim();
        actividad.Descripcion = dto.Descripcion?.Trim() ?? string.Empty;

        await _actividadRepository.UpdateAsync(actividad);
        _logger?.LogInformation("Actividad ID {ActividadId} actualizada.", id);
        return MapToResponseDTO(actividad);
    }

    public async Task DeleteAsync(Guid id)
    {
        _logger?.LogInformation("Solicitando eliminación de la actividad ID: {ActividadId}", id);
        var actividad = await _actividadRepository.GetByIdAsync(id);
        if (actividad is null)
        {
            throw new NotFoundException($"No se encontró la actividad con identificador {id}.");
        }

        // Issue #4: Prevenir error 500 por clave foránea verificando si tiene horarios asociados
        var hasHorarios = await _actividadRepository.HasAssignedHorariosAsync(id);
        if (hasHorarios)
        {
            _logger?.LogWarning("Intento de eliminar actividad ID {ActividadId} bloqueado por horarios asociados.", id);
            throw new DependencyConflictException("No se puede eliminar la actividad porque tiene horarios asignados en el sistema.");
        }

        await _actividadRepository.DeleteAsync(actividad);
        _logger?.LogInformation("Actividad ID {ActividadId} eliminada exitosamente.", id);
    }

    private static ActividadResponseDTO MapToResponseDTO(Actividad actividad)
    {
        return new ActividadResponseDTO(
            actividad.Id,
            actividad.Nombre,
            actividad.Descripcion
        );
    }
}
