using GymPlatform.BusinessLogic.Services;
using GymPlatform.DataAccess.Entities;
using GymPlatform.DataAccess.Repositories;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.Exceptions;
using Moq;
using Xunit;

namespace GymPlatform.BusinessLogic.Tests;

public class PlanMembresiaServiceTests
{
    private readonly Mock<IPlanMembresiaRepository> _planRepositoryMock;
    private readonly PlanMembresiaService _service;

    public PlanMembresiaServiceTests()
    {
        _planRepositoryMock = new Mock<IPlanMembresiaRepository>();
        _service = new PlanMembresiaService(_planRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidPlan_ReturnsResponseDTO()
    {
        // Arrange
        var dto = new PlanMembresiaCreateDTO("Plan Full", "Acceso total", 35000m, 30);
        _planRepositoryMock.Setup(r => r.GetByNombreAsync("Plan Full")).ReturnsAsync((PlanMembresia?)null);
        _planRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<PlanMembresia>()))
            .ReturnsAsync((PlanMembresia p) => { p.Id = Guid.NewGuid(); return p; });

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Nombre, result.Nombre);
        Assert.Equal(dto.Precio, result.Precio);
        Assert.Equal(dto.DuracionDias, result.DuracionDias);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task CreateAsync_NonPositivePrice_ThrowsValidationException(decimal invalidPrice)
    {
        // Arrange (RN-02: Precios no negativos ni cero)
        var dto = new PlanMembresiaCreateDTO("Plan Invalido", "Desc", invalidPrice, 30);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        Assert.Contains("precio", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_DuplicatePlanName_ThrowsDuplicateResourceException()
    {
        // Arrange (RN-02: Nombre de plan único)
        var dto = new PlanMembresiaCreateDTO("Plan Existente", "Desc", 20000m, 30);
        _planRepositoryMock.Setup(r => r.GetByNombreAsync("Plan Existente"))
            .ReturnsAsync(new PlanMembresia { Id = Guid.NewGuid(), Nombre = "Plan Existente" });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DuplicateResourceException>(() => _service.CreateAsync(dto));
        Assert.Contains("Ya existe un plan", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_PlanWithActiveHorarios_ThrowsDependencyConflictException()
    {
        // Arrange
        var planId = Guid.NewGuid();
        _planRepositoryMock.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(new PlanMembresia { Id = planId, Nombre = "Plan Activo" });
        _planRepositoryMock.Setup(r => r.HasAssignedHorariosAsync(planId)).ReturnsAsync(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DependencyConflictException>(() => _service.DeleteAsync(planId));
        Assert.Contains("horarios o actividades asociadas", ex.Message);
        _planRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<PlanMembresia>()), Times.Never);
    }
}
