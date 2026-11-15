using System.Data;
using GymPlatform.BusinessLogic.Interfaces;
using GymPlatform.DataAccess.Entities;
using GymPlatform.DataAccess.Repositories;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace GymPlatform.BusinessLogic.Services;

public class HorarioService : IHorarioService
{
    private readonly IHorarioRepository _horarioRepository;
    private readonly IActividadRepository _actividadRepository;
    private readonly IPlanMembresiaRepository _planRepository;
    private readonly IUnitOfWork? _unitOfWork;
    private readonly ILogger<HorarioService>? _logger;

    public HorarioService(
        IHorarioRepository horarioRepository,
        IActividadRepository actividadRepository,
        IPlanMembresiaRepository planRepository,
        IUnitOfWork? unitOfWork = null,
        ILogger<HorarioService>? logger = null)
    {
        _horarioRepository = horarioRepository;
        _actividadRepository = actividadRepository;
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<HorarioResponseDTO>> GetAllAsync()
    {
        _logger?.LogInformation("Consultando todos los horarios.");
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
        _logger?.LogInformation("Buscando horario por ID: {HorarioId}", id);
        var horario = await _horarioRepository.GetByIdAsync(id);
        if (horario is null)
        {
            _logger?.LogWarning("No se encontró el horario con ID: {HorarioId}", id);
            throw new NotFoundException($"No se encontró el horario con identificador {id}.");
        }
        return MapToResponseDTO(horario);
    }

    public async Task<HorarioResponseDTO> CreateAsync(HorarioCreateDTO dto)
    {
        _logger?.LogInformation("Iniciando creación de horario para actividad {ActividadId} y plan {PlanId}", dto.ActividadId, dto.PlanMembresiaId);

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

        // Issue #11: Rango estricto 1 a 500 para CupoMaximo
        if (dto.CupoMaximo < 1 || dto.CupoMaximo > 500)
        {
            throw new ValidationException("El cupo máximo debe estar comprendido estrictamente entre 1 y 500.");
        }

        // Issue #5: Nivel de aislamiento serializable para prevenir carreras y solapamientos
        if (_unitOfWork != null)
        {
            await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);
        }

        try
        {
            // 2. Existencia de entidades relacionadas (HTTP 404)
            var actividad = await _actividadRepository.GetByIdAsync(dto.ActividadId);
            if (actividad is null)
            {
                _logger?.LogWarning("Actividad no encontrada: {ActividadId}", dto.ActividadId);
                throw new NotFoundException($"No existe la actividad especificada con ID {dto.ActividadId}.");
            }

            var plan = await _planRepository.GetByIdAsync(dto.PlanMembresiaId);
            if (plan is null)
            {
                _logger?.LogWarning("Plan de membresía no encontrado: {PlanId}", dto.PlanMembresiaId);
                throw new NotFoundException($"No existe el plan de membresía especificado con ID {dto.PlanMembresiaId}.");
            }

            // Issue #6: Rechazar asignación de horarios a planes inactivos
            if (!plan.Activo)
            {
                _logger?.LogWarning("Intento de asignar horario a un plan inactivo: {PlanId}", dto.PlanMembresiaId);
                throw new ValidationException("No se pueden asignar horarios a un plan de membresía inactivo.");
            }

            // 3. RN-03: Detección de solapamiento de horarios (HTTP 409) bajo aislamiento Serializable
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

            if (_unitOfWork != null)
            {
                await _unitOfWork.CommitTransactionAsync();
            }

            _logger?.LogInformation("Horario creado exitosamente con ID: {HorarioId}", creado.Id);
            return MapToResponseDTO(creado);
        }
        catch
        {
            if (_unitOfWork != null)
            {
                await _unitOfWork.RollbackTransactionAsync();
            }
            throw;
        }
    }

    public async Task<HorarioResponseDTO> UpdateAsync(Guid id, HorarioUpdateDTO dto)
    {
        _logger?.LogInformation("Actualizando horario ID: {HorarioId}", id);
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

        if (dto.CupoMaximo < 1 || dto.CupoMaximo > 500)
        {
            throw new ValidationException("El cupo máximo debe estar comprendido estrictamente entre 1 y 500.");
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

        if (!plan.Activo)
        {
            throw new ValidationException("No se pueden asignar horarios a un plan de membresía inactivo.");
        }

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

        _logger?.LogInformation("Horario ID {HorarioId} actualizado.", id);
        return MapToResponseDTO(horario);
    }

    public async Task DeleteAsync(Guid id)
    {
        _logger?.LogInformation("Eliminando horario ID: {HorarioId}", id);
        var horario = await _horarioRepository.GetByIdAsync(id);
        if (horario is null)
        {
            throw new NotFoundException($"No se encontró el horario con identificador {id}.");
        }

        await _horarioRepository.DeleteAsync(horario);
        _logger?.LogInformation("Horario ID {HorarioId} eliminado.", id);
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
                _logger?.LogWarning("Conflicto de horario para profesor '{Profesor}' los días {Dia}", profesor, diaSemana);
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
                _logger?.LogWarning("Conflicto de horario para sala '{Sala}' los días {Dia}", sala, diaSemana);
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
