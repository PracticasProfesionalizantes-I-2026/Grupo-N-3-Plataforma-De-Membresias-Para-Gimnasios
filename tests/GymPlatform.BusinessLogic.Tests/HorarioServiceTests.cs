using GymPlatform.BusinessLogic.Services;
using GymPlatform.DataAccess.Entities;
using GymPlatform.DataAccess.Repositories;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.Exceptions;
using Moq;
using Xunit;

namespace GymPlatform.BusinessLogic.Tests;

public class HorarioServiceTests
{
    private readonly Mock<IHorarioRepository> _horarioRepositoryMock;
    private readonly Mock<IActividadRepository> _actividadRepositoryMock;
    private readonly Mock<IPlanMembresiaRepository> _planRepositoryMock;
    private readonly HorarioService _service;

    public HorarioServiceTests()
    {
        _horarioRepositoryMock = new Mock<IHorarioRepository>();
        _actividadRepositoryMock = new Mock<IActividadRepository>();
        _planRepositoryMock = new Mock<IPlanMembresiaRepository>();
        _service = new HorarioService(_horarioRepositoryMock.Object, _actividadRepositoryMock.Object, _planRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidHorario_ReturnsResponseDTO()
    {
        // Arrange
        var actId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var dto = new HorarioCreateDTO(actId, planId, "Martin Gomez", "Sala 1", DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 0), 20);

        _actividadRepositoryMock.Setup(r => r.GetByIdAsync(actId)).ReturnsAsync(new Actividad { Id = actId, Nombre = "Spinning" });
        _planRepositoryMock.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(new PlanMembresia { Id = planId, Nombre = "Plan Total" });
        _horarioRepositoryMock.Setup(r => r.GetByProfesorAndDiaAsync("Martin Gomez", DayOfWeek.Monday)).ReturnsAsync(new List<Horario>());
        _horarioRepositoryMock.Setup(r => r.GetBySalaAndDiaAsync("Sala 1", DayOfWeek.Monday)).ReturnsAsync(new List<Horario>());
        _horarioRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Horario>()))
            .ReturnsAsync((Horario h) => { h.Id = Guid.NewGuid(); return h; });

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Martin Gomez", result.Profesor);
        Assert.Equal("Sala 1", result.Sala);
        Assert.Equal(20, result.CupoMaximo);
    }

    [Fact]
    public async Task CreateAsync_InvalidHours_ThrowsValidationException()
    {
        // Arrange (Inicio posterior a fin)
        var dto = new HorarioCreateDTO(Guid.NewGuid(), Guid.NewGuid(), "Martin Gomez", "Sala 1", DayOfWeek.Monday, new TimeOnly(12, 0), new TimeOnly(10, 0), 20);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        Assert.Contains("anterior a la hora de fin", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_ProfessorScheduleOverlap_ThrowsScheduleConflictException()
    {
        // Arrange (RN-03: Solapamiento horario del profesor)
        var actId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var dto = new HorarioCreateDTO(actId, planId, "Martin Gomez", "Sala 2", DayOfWeek.Monday, new TimeOnly(10, 30), new TimeOnly(11, 30), 20);

        _actividadRepositoryMock.Setup(r => r.GetByIdAsync(actId)).ReturnsAsync(new Actividad { Id = actId, Nombre = "Spinning" });
        _planRepositoryMock.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(new PlanMembresia { Id = planId, Nombre = "Plan Total" });

        var existingHorario = new Horario
        {
            Id = Guid.NewGuid(),
            Profesor = "Martin Gomez",
            Sala = "Sala 1",
            DiaSemana = DayOfWeek.Monday,
            HoraInicio = new TimeOnly(10, 0),
            HoraFin = new TimeOnly(11, 0)
        };

        _horarioRepositoryMock.Setup(r => r.GetByProfesorAndDiaAsync("Martin Gomez", DayOfWeek.Monday))
            .ReturnsAsync(new List<Horario> { existingHorario });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ScheduleConflictException>(() => _service.CreateAsync(dto));
        Assert.Contains("profesor 'Martin Gomez' ya tiene asignada", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_RoomScheduleOverlap_ThrowsScheduleConflictException()
    {
        // Arrange (RN-03: Solapamiento horario de la sala)
        var actId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var dto = new HorarioCreateDTO(actId, planId, "Laura Perez", "Sala 1", DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(11, 0), 20);

        _actividadRepositoryMock.Setup(r => r.GetByIdAsync(actId)).ReturnsAsync(new Actividad { Id = actId, Nombre = "Crossfit" });
        _planRepositoryMock.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(new PlanMembresia { Id = planId, Nombre = "Plan Total" });

        _horarioRepositoryMock.Setup(r => r.GetByProfesorAndDiaAsync("Laura Perez", DayOfWeek.Monday)).ReturnsAsync(new List<Horario>());

        var existingHorarioSala = new Horario
        {
            Id = Guid.NewGuid(),
            Profesor = "Martin Gomez",
            Sala = "Sala 1",
            DiaSemana = DayOfWeek.Monday,
            HoraInicio = new TimeOnly(10, 30),
            HoraFin = new TimeOnly(11, 30)
        };

        _horarioRepositoryMock.Setup(r => r.GetBySalaAndDiaAsync("Sala 1", DayOfWeek.Monday))
            .ReturnsAsync(new List<Horario> { existingHorarioSala });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ScheduleConflictException>(() => _service.CreateAsync(dto));
        Assert.Contains("sala 'Sala 1' ya se encuentra ocupada", ex.Message);
    }
}
