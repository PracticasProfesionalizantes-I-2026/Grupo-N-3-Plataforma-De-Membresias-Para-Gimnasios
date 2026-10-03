using GymPlatform.BusinessLogic.Interfaces;
using GymPlatform.DataAccess.Entities;
using GymPlatform.DataAccess.Repositories;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.Exceptions;

namespace GymPlatform.BusinessLogic.Services;

public class ActividadService : IActividadService
{
    private readonly IActividadRepository _actividadRepository;

    public ActividadService(IActividadRepository actividadRepository)
    {
        _actividadRepository = actividadRepository;
    }

    public async Task<IEnumerable<ActividadResponseDTO>> GetAllAsync()
    {
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
        var actividad = await _actividadRepository.GetByIdAsync(id);
        if (actividad is null)
        {
            throw new NotFoundException($"No se encontró la actividad con identificador {id}.");
        }
        return MapToResponseDTO(actividad);
    }

    public async Task<ActividadResponseDTO> CreateAsync(ActividadCreateDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            throw new ValidationException("El nombre de la actividad es obligatorio.");
        }

        var existente = await _actividadRepository.GetByNombreAsync(dto.Nombre.Trim());
        if (existente is not null)
        {
            throw new DuplicateResourceException($"Ya existe una actividad registrada con el nombre '{dto.Nombre}'.");
        }

        var actividad = new Actividad
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion?.Trim() ?? string.Empty
        };

        var creada = await _actividadRepository.CreateAsync(actividad);
        return MapToResponseDTO(creada);
    }

    public async Task<ActividadResponseDTO> UpdateAsync(Guid id, ActividadUpdateDTO dto)
    {
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
            throw new DuplicateResourceException($"Ya existe otra actividad con el nombre '{dto.Nombre}'.");
        }

        actividad.Nombre = dto.Nombre.Trim();
        actividad.Descripcion = dto.Descripcion?.Trim() ?? string.Empty;

        await _actividadRepository.UpdateAsync(actividad);
        return MapToResponseDTO(actividad);
    }

    public async Task DeleteAsync(Guid id)
    {
        var actividad = await _actividadRepository.GetByIdAsync(id);
        if (actividad is null)
        {
            throw new NotFoundException($"No se encontró la actividad con identificador {id}.");
        }

        await _actividadRepository.DeleteAsync(actividad);
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
