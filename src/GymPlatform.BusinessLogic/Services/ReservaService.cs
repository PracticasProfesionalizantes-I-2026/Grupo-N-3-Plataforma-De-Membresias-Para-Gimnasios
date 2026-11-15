using GymPlatform.BusinessLogic.Interfaces;
using GymPlatform.DataAccess.Entities;
using GymPlatform.DataAccess.Repositories;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace GymPlatform.BusinessLogic.Services;

public class ReservaService : IReservaService
{
    private readonly IReservaRepository _reservaRepository;
    private readonly ISocioRepository _socioRepository;
    private readonly IHorarioRepository _horarioRepository;
    private readonly IMembresiaRepository _membresiaRepository;
    private readonly ILogger<ReservaService>? _logger;

    public ReservaService(
        IReservaRepository reservaRepository,
        ISocioRepository socioRepository,
        IHorarioRepository horarioRepository,
        IMembresiaRepository membresiaRepository,
        ILogger<ReservaService>? logger = null)
    {
        _reservaRepository = reservaRepository;
        _socioRepository = socioRepository;
        _horarioRepository = horarioRepository;
        _membresiaRepository = membresiaRepository;
        _logger = logger;
    }

    public async Task<ReservaResponseDTO> ReservarTurnoAsync(ReservaCreateDTO dto)
    {
        _logger?.LogInformation("Iniciando solicitud de reserva de turno. Socio: {SocioId}, Horario: {HorarioId}, Fecha: {Fecha}",
            dto.SocioId, dto.HorarioId, dto.FechaTurno);

        // 1. Validar existencia y estado del socio
        var socio = await _socioRepository.GetByIdAsync(dto.SocioId);
        if (socio is null || !socio.Activo)
        {
            _logger?.LogWarning("Socio inexistente o inactivo para reserva: {SocioId}", dto.SocioId);
            throw new NotFoundException($"No existe un socio activo con identificador {dto.SocioId}.");
        }

        // 2. Validar existencia del horario
        var horario = await _horarioRepository.GetByIdAsync(dto.HorarioId);
        if (horario is null)
        {
            _logger?.LogWarning("Horario no encontrado para reserva: {HorarioId}", dto.HorarioId);
            throw new NotFoundException($"No existe el horario con identificador {dto.HorarioId}.");
        }

        // 3. Validar correspondencia de día de la semana
        if (dto.FechaTurno.DayOfWeek != horario.DiaSemana)
        {
            _logger?.LogWarning("La fecha de turno {Fecha} ({DiaFecha}) no coincide con el día del horario ({DiaHorario})",
                dto.FechaTurno, dto.FechaTurno.DayOfWeek, horario.DiaSemana);
            throw new ValidationException($"La fecha {dto.FechaTurno:yyyy-MM-dd} cae en {dto.FechaTurno.DayOfWeek}, pero el horario está programado para los días {horario.DiaSemana}.");
        }

        // 4. Validar membresía activa del socio para el plan requerido por el horario
        var membresiasSocio = await _membresiaRepository.GetBySocioIdAsync(dto.SocioId);
        var ahora = DateTime.UtcNow;
        var membresiaValida = membresiasSocio.FirstOrDefault(m =>
            m.PlanMembresiaId == horario.PlanMembresiaId &&
            m.Activo &&
            m.FechaVencimiento >= ahora &&
            m.FechaInicio <= ahora);

        if (membresiaValida is null)
        {
            _logger?.LogWarning("El socio {SocioId} no tiene una membresía activa y vigente para el plan {PlanId}", dto.SocioId, horario.PlanMembresiaId);
            throw new ConflictException("El socio no posee una membresía activa y vigente correspondiente al plan de este horario.");
        }

        // 5. Validar que no tenga una reserva previa para el mismo turno
        var yaTieneReserva = await _reservaRepository.HasReservaActivaAsync(dto.SocioId, dto.HorarioId, dto.FechaTurno);
        if (yaTieneReserva)
        {
            _logger?.LogWarning("El socio {SocioId} ya tiene una reserva para el horario {HorarioId} en la fecha {Fecha}",
                dto.SocioId, dto.HorarioId, dto.FechaTurno);
            throw new DuplicateResourceException("El socio ya cuenta con una reserva activa para este horario en la fecha seleccionada.");
        }

        // 6. Validar cupo máximo disponible en el horario
        var reservasOcupadas = await _reservaRepository.CountReservasByHorarioAndFechaAsync(dto.HorarioId, dto.FechaTurno);
        if (reservasOcupadas >= horario.CupoMaximo)
        {
            _logger?.LogWarning("Cupo máximo agotado ({Ocupadas}/{Maximo}) para horario {HorarioId} en fecha {Fecha}",
                reservasOcupadas, horario.CupoMaximo, dto.HorarioId, dto.FechaTurno);
            throw new ConflictException($"No hay cupo disponible para el turno seleccionado. Capacidad máxima alcanzada ({horario.CupoMaximo} personas).");
        }

        var reserva = new Reserva
        {
            SocioId = dto.SocioId,
            HorarioId = dto.HorarioId,
            FechaTurno = dto.FechaTurno,
            Asistencia = false,
            FechaCreacion = DateTime.UtcNow
        };

        var creada = await _reservaRepository.CreateAsync(reserva);
        creada.Socio = socio;
        creada.Horario = horario;

        _logger?.LogInformation("Reserva registrada con éxito. ID: {ReservaId}", creada.Id);
        return MapToResponseDTO(creada);
    }

    public async Task<IEnumerable<ReservaResponseDTO>> GetReservasBySocioAsync(Guid socioId)
    {
        var reservas = await _reservaRepository.GetBySocioIdAsync(socioId);
        return reservas.Select(MapToResponseDTO);
    }

    public async Task<IEnumerable<ReservaResponseDTO>> GetAllAsync()
    {
        var reservas = await _reservaRepository.GetAllAsync();
        return reservas.Select(MapToResponseDTO);
    }

    public async Task CancelarReservaAsync(Guid id)
    {
        var reserva = await _reservaRepository.GetByIdAsync(id);
        if (reserva is null)
        {
            throw new NotFoundException($"No se encontró la reserva con identificador {id}.");
        }

        await _reservaRepository.DeleteAsync(reserva);
        _logger?.LogInformation("Reserva ID {ReservaId} cancelada y eliminada.", id);
    }

    public async Task MarcarAsistenciaAsync(Guid id, bool asistio)
    {
        var reserva = await _reservaRepository.GetByIdAsync(id);
        if (reserva is null)
        {
            throw new NotFoundException($"No se encontró la reserva con identificador {id}.");
        }

        reserva.Asistencia = asistio;
        await _reservaRepository.UpdateAsync(reserva);
        _logger?.LogInformation("Asistencia actualizada para reserva ID {ReservaId}: {Asistio}", id, asistio);
    }

    private static ReservaResponseDTO MapToResponseDTO(Reserva r)
    {
        return new ReservaResponseDTO(
            r.Id,
            r.SocioId,
            r.Socio?.Nombre ?? string.Empty,
            r.HorarioId,
            r.Horario?.Actividad?.Nombre ?? string.Empty,
            r.Horario?.Profesor ?? string.Empty,
            r.Horario?.Sala ?? string.Empty,
            r.Horario?.DiaSemana ?? DayOfWeek.Monday,
            r.Horario?.HoraInicio ?? TimeOnly.MinValue,
            r.Horario?.HoraFin ?? TimeOnly.MinValue,
            r.FechaTurno,
            r.Asistencia,
            r.FechaCreacion
        );
    }
}

