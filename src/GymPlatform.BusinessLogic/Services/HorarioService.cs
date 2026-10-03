using GymPlatform.BusinessLogic.Interfaces;
using GymPlatform.DataAccess.Entities;
using GymPlatform.DataAccess.Repositories;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.Exceptions;

namespace GymPlatform.BusinessLogic.Services;

public class HorarioService : IHorarioService
{
    private readonly IHorarioRepository _horarioRepository;
    private readonly IActividadRepository _actividadRepository;
    private readonly IPlanMembresiaRepository _planRepository;

    public HorarioService(
        IHorarioRepository horarioRepository,
        IActividadRepository actividadRepository,
        IPlanMembresiaRepository planRepository)
    {
        _horarioRepository = horarioRepository;
        _actividadRepository = actividadRepository;
        _planRepository = planRepository;
    }

    public async Task<IEnumerable<HorarioResponseDTO>> GetAllAsync()
    {
        var horarios = await _horarioRepository.GetAllAsync();
        var response = new List<HorarioResponseDTO>();
        foreach (var horario in horarios)
        {
            response.Add(MapToResponseDTO(horario));
        }
        return response;
    }

    public async Task<HorarioResponseDTO> GetByIdAsync(Guid id)
    {
        var horario = await _horarioRepository.GetByIdAsync(id);
        if (horario is null)
        {
            throw new NotFoundException($"No se encontró el horario con identificador {id}.");
        }
        return MapToResponseDTO(horario);
    }

    public async Task<HorarioResponseDTO> CreateAsync(HorarioCreateDTO dto)
    {
        // 1. Validaciones básicas de campos y tiempos (HTTP 400)
        if (string.IsNullOrWhiteSpace(dto.Profesor))
        {
            throw new ValidationException("El nombre del profesor es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(dto.Sala))
        {
            throw new ValidationException("La sala o espacio asignado es obligatorio.");
        }

        if (dto.HoraInicio >= dto.HoraFin)
        {
            throw new ValidationException("La hora de inicio debe ser estrictamente anterior a la hora de fin.");
        }

        if (dto.CupoMaximo <= 0)
        {
            throw new ValidationException("El cupo máximo debe ser mayor a cero.");
        }

        // 2. Existencia de entidades relacionadas (HTTP 404)
        var actividad = await _actividadRepository.GetByIdAsync(dto.ActividadId);
        if (actividad is null)
        {
            throw new NotFoundException($"No existe la actividad especificada con ID {dto.ActividadId}.");
        }

        var plan = await _planRepository.GetByIdAsync(dto.PlanMembresiaId);
        if (plan is null)
        {
            throw new NotFoundException($"No existe el plan de membresía especificado con ID {dto.PlanMembresiaId}.");
        }

        // 3. RN-03: Detección de solapamiento de horarios (HTTP 409)
        await ValidarSolapamientoAsync(dto.Profesor, dto.Sala, dto.DiaSemana, dto.HoraInicio, dto.HoraFin, null);

        var horario = new Horario
        {
            ActividadId = dto.ActividadId,
            PlanMembresiaId = dto.PlanMembresiaId,
            Profesor = dto.Profesor.Trim(),
            Sala = dto.Sala.Trim(),
            DiaSemana = dto.DiaSemana,
            HoraInicio = dto.HoraInicio,
            HoraFin = dto.HoraFin,
            CupoMaximo = dto.CupoMaximo
        };

        var creado = await _horarioRepository.CreateAsync(horario);
        creado.Actividad = actividad;
        creado.PlanMembresia = plan;

        return MapToResponseDTO(creado);
    }

    public async Task<HorarioResponseDTO> UpdateAsync(Guid id, HorarioUpdateDTO dto)
    {
        var horario = await _horarioRepository.GetByIdAsync(id);
        if (horario is null)
        {
            throw new NotFoundException($"No se encontró el horario con identificador {id}.");
        }

        if (string.IsNullOrWhiteSpace(dto.Profesor))
        {
            throw new ValidationException("El nombre del profesor es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(dto.Sala))
        {
            throw new ValidationException("La sala o espacio asignado es obligatorio.");
        }

        if (dto.HoraInicio >= dto.HoraFin)
        {
            throw new ValidationException("La hora de inicio debe ser estrictamente anterior a la hora de fin.");
        }

        if (dto.CupoMaximo <= 0)
        {
            throw new ValidationException("El cupo máximo debe ser mayor a cero.");
        }

        var actividad = await _actividadRepository.GetByIdAsync(dto.ActividadId);
        if (actividad is null)
        {
            throw new NotFoundException($"No existe la actividad especificada con ID {dto.ActividadId}.");
        }

        var plan = await _planRepository.GetByIdAsync(dto.PlanMembresiaId);
        if (plan is null)
        {
            throw new NotFoundException($"No existe el plan de membresía especificado con ID {dto.PlanMembresiaId}.");
        }

        // RN-03: Validar solapamientos excluyendo el horario actual
        await ValidarSolapamientoAsync(dto.Profesor, dto.Sala, dto.DiaSemana, dto.HoraInicio, dto.HoraFin, id);

        horario.ActividadId = dto.ActividadId;
        horario.PlanMembresiaId = dto.PlanMembresiaId;
        horario.Profesor = dto.Profesor.Trim();
        horario.Sala = dto.Sala.Trim();
        horario.DiaSemana = dto.DiaSemana;
        horario.HoraInicio = dto.HoraInicio;
        horario.HoraFin = dto.HoraFin;
        horario.CupoMaximo = dto.CupoMaximo;

        await _horarioRepository.UpdateAsync(horario);
        horario.Actividad = actividad;
        horario.PlanMembresia = plan;

        return MapToResponseDTO(horario);
    }

    public async Task DeleteAsync(Guid id)
    {
        var horario = await _horarioRepository.GetByIdAsync(id);
        if (horario is null)
        {
            throw new NotFoundException($"No se encontró el horario con identificador {id}.");
        }

        await _horarioRepository.DeleteAsync(horario);
    }

    private async Task ValidarSolapamientoAsync(string profesor, string sala, DayOfWeek diaSemana, TimeOnly inicio, TimeOnly fin, Guid? horarioIdExcluido)
    {
        // 1. Solapamiento de profesor
        var horariosProfesor = await _horarioRepository.GetByProfesorAndDiaAsync(profesor.Trim(), diaSemana);
        foreach (var h in horariosProfesor)
        {
            if (horarioIdExcluido.HasValue && h.Id == horarioIdExcluido.Value)
            {
                continue;
            }

            if (HaySolapamiento(inicio, fin, h.HoraInicio, h.HoraFin))
            {
                throw new ScheduleConflictException($"El profesor '{profesor}' ya tiene asignada una actividad los días {diaSemana} entre las {h.HoraInicio} y las {h.HoraFin}.");
            }
        }

        // 2. Solapamiento de sala
        var horariosSala = await _horarioRepository.GetBySalaAndDiaAsync(sala.Trim(), diaSemana);
        foreach (var h in horariosSala)
        {
            if (horarioIdExcluido.HasValue && h.Id == horarioIdExcluido.Value)
            {
                continue;
            }

            if (HaySolapamiento(inicio, fin, h.HoraInicio, h.HoraFin))
            {
                throw new ScheduleConflictException($"La sala '{sala}' ya se encuentra ocupada los días {diaSemana} entre las {h.HoraInicio} y las {h.HoraFin}.");
            }
        }
    }

    private static bool HaySolapamiento(TimeOnly inicio1, TimeOnly fin1, TimeOnly inicio2, TimeOnly fin2)
    {
        return inicio1 < fin2 && fin1 > inicio2;
    }

    private static HorarioResponseDTO MapToResponseDTO(Horario horario)
    {
        return new HorarioResponseDTO(
            horario.Id,
            horario.ActividadId,
            horario.Actividad?.Nombre ?? string.Empty,
            horario.PlanMembresiaId,
            horario.PlanMembresia?.Nombre ?? string.Empty,
            horario.Profesor,
            horario.Sala,
            horario.DiaSemana,
            horario.HoraInicio,
            horario.HoraFin,
            horario.CupoMaximo
        );
    }
}
