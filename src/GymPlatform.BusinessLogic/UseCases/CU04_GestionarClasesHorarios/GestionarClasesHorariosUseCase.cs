using GymPlatform.DataAccess.Entities;
using GymPlatform.DataAccess.Repositories;
using GymPlatform.Shared.Common;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GymPlatform.BusinessLogic.UseCases.CU04_GestionarClasesHorarios;

public class GestionarClasesHorariosUseCase : IGestionarClasesHorariosUseCase
{
    private readonly IActividadRepository _actividadRepository;
    private readonly IHorarioRepository _horarioRepository;
    private readonly IPlanMembresiaRepository _planRepository;

    public GestionarClasesHorariosUseCase(
        IActividadRepository actividadRepository,
        IHorarioRepository horarioRepository,
        IPlanMembresiaRepository planRepository)
    {
        _actividadRepository = actividadRepository;
        _horarioRepository = horarioRepository;
        _planRepository = planRepository;
    }

    public async Task<Result<IEnumerable<ActividadResponseDTO>>> ListarActividadesAsync()
    {
        try
        {
            var actividades = await _actividadRepository.GetAllAsync();
            var dtos = actividades.Select(a => new ActividadResponseDTO(a.Id, a.Nombre, a.Descripcion)).ToList();
            return Result<IEnumerable<ActividadResponseDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<ActividadResponseDTO>>.Failure("Error al consultar actividades: " + ex.Message);
        }
    }

    public async Task<Result<ActividadResponseDTO>> CrearActividadAsync(ActividadCreateDTO dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
            {
                return Result<ActividadResponseDTO>.Failure("El nombre de la actividad o clase es obligatorio.");
            }

            var existente = await _actividadRepository.GetByNombreAsync(dto.Nombre.Trim());
            if (existente is not null)
            {
                return Result<ActividadResponseDTO>.Failure($"Ya existe una actividad registrada con el nombre '{dto.Nombre.Trim()}'.");
            }

            var actividad = new Actividad
            {
                Nombre = dto.Nombre.Trim(),
                Descripcion = dto.Descripcion?.Trim() ?? string.Empty
            };

            var creada = await _actividadRepository.CreateAsync(actividad);
            return Result<ActividadResponseDTO>.Success(
                new ActividadResponseDTO(creada.Id, creada.Nombre, creada.Descripcion),
                "Actividad registrada con éxito."
            );
        }
        catch (Exception ex)
        {
            return Result<ActividadResponseDTO>.Failure("Error al crear la actividad: " + ex.Message);
        }
    }

    public async Task<Result<ActividadResponseDTO>> ModificarActividadAsync(Guid id, ActividadUpdateDTO dto)
    {
        try
        {
            var actividad = await _actividadRepository.GetByIdAsync(id);
            if (actividad is null)
            {
                return Result<ActividadResponseDTO>.Failure($"No se encontró la actividad con identificador {id}.");
            }

            if (string.IsNullOrWhiteSpace(dto.Nombre))
            {
                return Result<ActividadResponseDTO>.Failure("El nombre de la actividad es obligatorio.");
            }

            var existente = await _actividadRepository.GetByNombreAsync(dto.Nombre.Trim());
            if (existente is not null && existente.Id != id)
            {
                return Result<ActividadResponseDTO>.Failure($"Ya existe otra actividad registrada con el nombre '{dto.Nombre.Trim()}'.");
            }

            actividad.Nombre = dto.Nombre.Trim();
            actividad.Descripcion = dto.Descripcion?.Trim() ?? string.Empty;

            await _actividadRepository.UpdateAsync(actividad);
            return Result<ActividadResponseDTO>.Success(
                new ActividadResponseDTO(actividad.Id, actividad.Nombre, actividad.Descripcion),
                "Actividad modificada exitosamente."
            );
        }
        catch (Exception ex)
        {
            return Result<ActividadResponseDTO>.Failure("Error al modificar la actividad: " + ex.Message);
        }
    }

    public async Task<Result<bool>> EliminarActividadAsync(Guid id)
    {
        try
        {
            var actividad = await _actividadRepository.GetByIdAsync(id);
            if (actividad is null)
            {
                return Result<bool>.Failure($"No se encontró la actividad con identificador {id}.");
            }

            var horariosAsociados = await _horarioRepository.GetAllAsync();
            if (horariosAsociados.Any(h => h.ActividadId == id))
            {
                return Result<bool>.Failure("No se puede eliminar la actividad porque tiene horarios programados asociados.");
            }

            await _actividadRepository.DeleteAsync(actividad);
            return Result<bool>.Success(true, "Actividad eliminada exitosamente.");
        }
        catch (DbUpdateException)
        {
            return Result<bool>.Failure("No se puede eliminar la actividad debido a que existen registros vinculados.");
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure("Error al eliminar la actividad: " + ex.Message);
        }
    }

    public async Task<Result<IEnumerable<HorarioResponseDTO>>> ListarHorariosAsync()
    {
        try
        {
            var horarios = await _horarioRepository.GetAllAsync();
            var dtos = horarios.Select(h => new HorarioResponseDTO(
                h.Id,
                h.ActividadId,
                h.Actividad?.Nombre ?? string.Empty,
                h.PlanMembresiaId,
                h.PlanMembresia?.Nombre ?? string.Empty,
                h.Profesor,
                h.Sala,
                h.DiaSemana,
                h.HoraInicio,
                h.HoraFin,
                h.CupoMaximo
            )).ToList();

            return Result<IEnumerable<HorarioResponseDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<HorarioResponseDTO>>.Failure("Error al listar horarios: " + ex.Message);
        }
    }

    public async Task<Result<HorarioResponseDTO>> GetHorarioByIdAsync(Guid id)
    {
        try
        {
            var horario = await _horarioRepository.GetByIdAsync(id);
            if (horario is null)
            {
                return Result<HorarioResponseDTO>.Failure($"No se encontró el horario con identificador {id}.");
            }

            return Result<HorarioResponseDTO>.Success(new HorarioResponseDTO(
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
            ));
        }
        catch (Exception ex)
        {
            return Result<HorarioResponseDTO>.Failure("Error al consultar horario: " + ex.Message);
        }
    }

    public async Task<Result<HorarioResponseDTO>> CrearHorarioAsync(HorarioCreateDTO dto)
    {
        try
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.Profesor))
            {
                validationErrors.Add("El nombre del profesor o entrenador es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(dto.Sala))
            {
                validationErrors.Add("La sala o espacio asignado es obligatorio.");
            }

            if (dto.HoraInicio >= dto.HoraFin)
            {
                validationErrors.Add("La hora de inicio debe ser estrictamente anterior a la hora de fin.");
            }

            if (dto.CupoMaximo <= 0)
            {
                validationErrors.Add("El cupo máximo debe ser mayor a cero.");
            }

            var actividad = await _actividadRepository.GetByIdAsync(dto.ActividadId);
            if (actividad is null)
            {
                validationErrors.Add($"No existe la actividad especificada con ID {dto.ActividadId}.");
            }

            var plan = await _planRepository.GetByIdAsync(dto.PlanMembresiaId);
            if (plan is null)
            {
                validationErrors.Add($"No existe el plan de membresía especificado con ID {dto.PlanMembresiaId}.");
            }

            if (validationErrors.Count > 0)
            {
                return Result<HorarioResponseDTO>.Failure(validationErrors[0], validationErrors);
            }

            // RN-03: Detección de solapamiento de horarios (Profesor y Sala)
            var errorSolapamiento = await ValidarSolapamientoAsync(dto.Profesor, dto.Sala, dto.DiaSemana, dto.HoraInicio, dto.HoraFin, null);
            if (errorSolapamiento is not null)
            {
                return Result<HorarioResponseDTO>.Failure(errorSolapamiento);
            }

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

            return Result<HorarioResponseDTO>.Success(
                new HorarioResponseDTO(
                    creado.Id,
                    creado.ActividadId,
                    actividad!.Nombre,
                    creado.PlanMembresiaId,
                    plan!.Nombre,
                    creado.Profesor,
                    creado.Sala,
                    creado.DiaSemana,
                    creado.HoraInicio,
                    creado.HoraFin,
                    creado.CupoMaximo
                ),
                "Horario programado exitosamente."
            );
        }
        catch (Exception ex)
        {
            return Result<HorarioResponseDTO>.Failure("Error al crear el horario: " + ex.Message);
        }
    }

    public async Task<Result<HorarioResponseDTO>> ModificarHorarioAsync(Guid id, HorarioUpdateDTO dto)
    {
        try
        {
            var horario = await _horarioRepository.GetByIdAsync(id);
            if (horario is null)
            {
                return Result<HorarioResponseDTO>.Failure($"No se encontró el horario con identificador {id}.");
            }

            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.Profesor))
            {
                validationErrors.Add("El nombre del profesor es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(dto.Sala))
            {
                validationErrors.Add("La sala es obligatoria.");
            }

            if (dto.HoraInicio >= dto.HoraFin)
            {
                validationErrors.Add("La hora de inicio debe ser anterior a la hora de fin.");
            }

            if (dto.CupoMaximo <= 0)
            {
                validationErrors.Add("El cupo máximo debe ser mayor a cero.");
            }

            var actividad = await _actividadRepository.GetByIdAsync(dto.ActividadId);
            if (actividad is null)
            {
                validationErrors.Add($"No existe la actividad especificada con ID {dto.ActividadId}.");
            }

            var plan = await _planRepository.GetByIdAsync(dto.PlanMembresiaId);
            if (plan is null)
            {
                validationErrors.Add($"No existe el plan de membresía especificado con ID {dto.PlanMembresiaId}.");
            }

            if (validationErrors.Count > 0)
            {
                return Result<HorarioResponseDTO>.Failure(validationErrors[0], validationErrors);
            }

            // RN-03: Solapamiento excluyendo el actual
            var errorSolapamiento = await ValidarSolapamientoAsync(dto.Profesor, dto.Sala, dto.DiaSemana, dto.HoraInicio, dto.HoraFin, id);
            if (errorSolapamiento is not null)
            {
                return Result<HorarioResponseDTO>.Failure(errorSolapamiento);
            }

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

            return Result<HorarioResponseDTO>.Success(
                new HorarioResponseDTO(
                    horario.Id,
                    horario.ActividadId,
                    actividad!.Nombre,
                    horario.PlanMembresiaId,
                    plan!.Nombre,
                    horario.Profesor,
                    horario.Sala,
                    horario.DiaSemana,
                    horario.HoraInicio,
                    horario.HoraFin,
                    horario.CupoMaximo
                ),
                "Horario actualizado exitosamente."
            );
        }
        catch (Exception ex)
        {
            return Result<HorarioResponseDTO>.Failure("Error al actualizar el horario: " + ex.Message);
        }
    }

    public async Task<Result<bool>> EliminarHorarioAsync(Guid id)
    {
        try
        {
            var horario = await _horarioRepository.GetByIdAsync(id);
            if (horario is null)
            {
                return Result<bool>.Failure($"No se encontró el horario con identificador {id}.");
            }

            await _horarioRepository.DeleteAsync(horario);
            return Result<bool>.Success(true, "Horario eliminado exitosamente.");
        }
        catch (DbUpdateException)
        {
            return Result<bool>.Failure("No se puede eliminar el horario debido a reservas o asistencias activas asociadas.");
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure("Error al eliminar el horario: " + ex.Message);
        }
    }

    private async Task<string?> ValidarSolapamientoAsync(string profesor, string sala, DayOfWeek diaSemana, TimeOnly inicio, TimeOnly fin, Guid? horarioIdExcluido)
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
                return $"El profesor '{profesor}' ya tiene asignada una actividad los días {diaSemana} entre las {h.HoraInicio} y las {h.HoraFin}.";
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
                return $"La sala '{sala}' ya se encuentra ocupada los días {diaSemana} entre las {h.HoraInicio} y las {h.HoraFin}.";
            }
        }

        return null;
    }

    private static bool HaySolapamiento(TimeOnly inicio1, TimeOnly fin1, TimeOnly inicio2, TimeOnly fin2)
    {
        return inicio1 < fin2 && fin1 > inicio2;
    }
}
