using GymPlatform.BusinessLogic.Services;
using GymPlatform.DataAccess.Entities;
using GymPlatform.DataAccess.Repositories;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.Exceptions;
using Moq;
using Xunit;

namespace GymPlatform.BusinessLogic.Tests;

public class SocioServiceTests
{
    private readonly Mock<ISocioRepository> _socioRepositoryMock;
    private readonly SocioService _service;

    public SocioServiceTests()
    {
        _socioRepositoryMock = new Mock<ISocioRepository>();
        _service = new SocioService(_socioRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidData_ReturnsCreatedSocioResponseDTO()
    {
        // Arrange (RN-01: Mayor de edad y datos correctos)
        var dto = new SocioCreateDTO("Juan Perez", "12345678", "juan.perez@example.com", "1122334455", new DateOnly(1995, 1, 1));
        _socioRepositoryMock.Setup(r => r.GetByDniAsync("12345678")).ReturnsAsync((Socio?)null);
        _socioRepositoryMock.Setup(r => r.GetByEmailAsync("juan.perez@example.com")).ReturnsAsync((Socio?)null);
        _socioRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Socio>()))
            .ReturnsAsync((Socio s) => { s.Id = Guid.NewGuid(); return s; });

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Nombre, result.Nombre);
        Assert.Equal(dto.Dni, result.Dni);
        Assert.Equal(dto.Email, result.Email);
        _socioRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Socio>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Underage_ThrowsValidationException()
    {
        // Arrange (RN-01: Menor de 18 años)
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var dto = new SocioCreateDTO("Menor de edad", "12345678", "menor@example.com", "1122334455", hoy.AddYears(-17));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        Assert.Contains("mayor de 18 años", ex.Message);
        _socioRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Socio>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidEmail_ThrowsValidationException()
    {
        // Arrange
        var dto = new SocioCreateDTO("Juan Perez", "12345678", "email-invalido", "1122334455", new DateOnly(1990, 1, 1));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        Assert.Contains("correo electrónico", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_DuplicateDni_ThrowsDuplicateResourceException()
    {
        // Arrange (RN-01: Duplicidad DNI)
        var dto = new SocioCreateDTO("Juan Perez", "12345678", "juan@example.com", "1122334455", new DateOnly(1990, 1, 1));
        _socioRepositoryMock.Setup(r => r.GetByDniAsync("12345678")).ReturnsAsync(new Socio { Id = Guid.NewGuid(), Dni = "12345678" });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DuplicateResourceException>(() => _service.CreateAsync(dto));
        Assert.Contains("ya se encuentran registrados", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ThrowsDuplicateResourceException()
    {
        // Arrange (RN-01: Duplicidad Email)
        var dto = new SocioCreateDTO("Juan Perez", "12345678", "juan@example.com", "1122334455", new DateOnly(1990, 1, 1));
        _socioRepositoryMock.Setup(r => r.GetByDniAsync("12345678")).ReturnsAsync((Socio?)null);
        _socioRepositoryMock.Setup(r => r.GetByEmailAsync("juan@example.com")).ReturnsAsync(new Socio { Id = Guid.NewGuid(), Email = "juan@example.com" });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DuplicateResourceException>(() => _service.CreateAsync(dto));
        Assert.Contains("ya se encuentran registrados", ex.Message);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _socioRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Socio?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(id));
    }
}
