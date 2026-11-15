using GymPlatform.DataAccess.Entities;
using GymPlatform.DataAccess.Repositories;
using GymPlatform.Shared.Common;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GymPlatform.BusinessLogic.UseCases.CU02_GestionarPlanes;

public class GestionarPlanesUseCase : IGestionarPlanesUseCase
{
    private readonly IPlanMembresiaRepository _planRepository;

    public GestionarPlanesUseCase(IPlanMembresiaRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<Result<IEnumerable<PlanMembresiaResponseDTO>>> ListarPlanesAsync()
    {
        try
        {
            var planes = await _planRepository.GetAllAsync();
            var response = planes.Select(p => new PlanMembresiaResponseDTO(
                p.Id,
                p.Nombre,
                p.Descripcion,
                p.Precio,
                p.DuracionDias,
                p.Activo
            )).ToList();

            return Result<IEnumerable<PlanMembresiaResponseDTO>>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<PlanMembresiaResponseDTO>>.Failure("No se pudo obtener la lista de planes: " + ex.Message);
        }
    }

    public async Task<Result<PlanMembresiaResponseDTO>> GetByIdAsync(Guid id)
    {
        try
        {
            var plan = await _planRepository.GetByIdAsync(id);
            if (plan is null)
            {
                return Result<PlanMembresiaResponseDTO>.Failure($"No se encontró el plan de membresía con ID {id}.");
            }

            return Result<PlanMembresiaResponseDTO>.Success(new PlanMembresiaResponseDTO(
                plan.Id,
                plan.Nombre,
                plan.Descripcion,
                plan.Precio,
                plan.DuracionDias,
                plan.Activo
            ));
        }
        catch (Exception ex)
        {
            return Result<PlanMembresiaResponseDTO>.Failure("Error al consultar el plan: " + ex.Message);
        }
    }

    public async Task<Result<PlanMembresiaResponseDTO>> CrearPlanAsync(PlanMembresiaCreateDTO dto)
    {
        try
        {
            // Validaciones defensivas (RN-02)
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.Nombre))
            {
                validationErrors.Add("El nombre del plan de membresía es obligatorio.");
            }

            if (dto.Precio <= 0)
            {
                validationErrors.Add("El precio del plan debe ser un valor positivo estrictamente mayor a cero.");
            }

            if (dto.DuracionDias <= 0)
            {
                validationErrors.Add("La duración del plan debe ser de al menos 1 día.");
            }

            if (validationErrors.Count > 0)
            {
                return Result<PlanMembresiaResponseDTO>.Failure(validationErrors[0], validationErrors);
            }

            // Unicidad de nombre
            var existente = await _planRepository.GetByNombreAsync(dto.Nombre.Trim());
            if (existente is not null)
            {
                return Result<PlanMembresiaResponseDTO>.Failure($"Ya existe un plan de membresía registrado con el nombre '{dto.Nombre.Trim()}'.");
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

            return Result<PlanMembresiaResponseDTO>.Success(
                new PlanMembresiaResponseDTO(creado.Id, creado.Nombre, creado.Descripcion, creado.Precio, creado.DuracionDias, creado.Activo),
                "Plan de membresía creado exitosamente."
            );
        }
        catch (DbUpdateException dbEx)
        {
            return Result<PlanMembresiaResponseDTO>.Failure("Error al persistir el plan en la base de datos: " + (dbEx.InnerException?.Message ?? dbEx.Message));
        }
        catch (Exception ex)
        {
            return Result<PlanMembresiaResponseDTO>.Failure("Error inesperado al crear el plan de membresía: " + ex.Message);
        }
    }

    public async Task<Result<PlanMembresiaResponseDTO>> ModificarPlanAsync(Guid id, PlanMembresiaUpdateDTO dto)
    {
        try
        {
            var plan = await _planRepository.GetByIdAsync(id);
            if (plan is null)
            {
                return Result<PlanMembresiaResponseDTO>.Failure($"No se encontró el plan de membresía con identificador {id}.");
            }

            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.Nombre))
            {
                validationErrors.Add("El nombre del plan de membresía es obligatorio.");
            }

            if (dto.Precio <= 0)
            {
                validationErrors.Add("El precio del plan debe ser mayor a cero.");
            }

            if (dto.DuracionDias <= 0)
            {
                validationErrors.Add("La duración del plan debe ser de al menos 1 día.");
            }

            if (validationErrors.Count > 0)
            {
                return Result<PlanMembresiaResponseDTO>.Failure(validationErrors[0], validationErrors);
            }

            // Unicidad si cambia el nombre
            var existente = await _planRepository.GetByNombreAsync(dto.Nombre.Trim());
            if (existente is not null && existente.Id != id)
            {
                return Result<PlanMembresiaResponseDTO>.Failure($"Ya existe otro plan registrado con el nombre '{dto.Nombre.Trim()}'.");
            }

            plan.Nombre = dto.Nombre.Trim();
            plan.Descripcion = dto.Descripcion?.Trim() ?? string.Empty;
            plan.Precio = dto.Precio;
            plan.DuracionDias = dto.DuracionDias;
            plan.Activo = dto.Activo;

            await _planRepository.UpdateAsync(plan);

            return Result<PlanMembresiaResponseDTO>.Success(
                new PlanMembresiaResponseDTO(plan.Id, plan.Nombre, plan.Descripcion, plan.Precio, plan.DuracionDias, plan.Activo),
                "Plan de membresía actualizado exitosamente."
            );
        }
        catch (DbUpdateException dbEx)
        {
            return Result<PlanMembresiaResponseDTO>.Failure("Error al actualizar el plan en la base de datos: " + (dbEx.InnerException?.Message ?? dbEx.Message));
        }
        catch (Exception ex)
        {
            return Result<PlanMembresiaResponseDTO>.Failure("Error inesperado al modificar el plan: " + ex.Message);
        }
    }

    public async Task<Result<bool>> EliminarPlanAsync(Guid id)
    {
        try
        {
            var plan = await _planRepository.GetByIdAsync(id);
            if (plan is null)
            {
                return Result<bool>.Failure($"No se encontró el plan de membresía con identificador {id}.");
            }

            // Restricción amigable de eliminación si tiene horarios o membresías activas
            if (await _planRepository.HasAssignedHorariosAsync(id))
            {
                return Result<bool>.Failure("No se puede eliminar el plan porque tiene horarios o actividades de clases asociadas.");
            }

            if (await _planRepository.HasActiveMembresiasAsync(id))
            {
                return Result<bool>.Failure("No se puede eliminar el plan porque tiene socios con membresías activas contratadas actualmente.");
            }

            await _planRepository.DeleteAsync(plan);
            return Result<bool>.Success(true, "Plan de membresía eliminado exitosamente.");
        }
        catch (DbUpdateException)
        {
            return Result<bool>.Failure("No se puede eliminar el plan de membresía debido a restricciones de integridad referencial.");
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure("Error al eliminar el plan de membresía: " + ex.Message);
        }
    }
}
