using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GymPlatform.BusinessLogic.Services;
using GymPlatform.DataAccess.Entities;
using GymPlatform.DataAccess.Repositories;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.Enums;
using GymPlatform.Shared.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GymPlatform.BusinessLogic.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _usuarioRepoMock = new Mock<IUsuarioRepository>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        var jwtSectionMock = new Mock<IConfigurationSection>();
        jwtSectionMock.Setup(s => s["Key"]).Returns("SuperSecretKeyForTestingJwtAuthentication123456!");
        jwtSectionMock.Setup(s => s["Issuer"]).Returns("TestIssuer");
        jwtSectionMock.Setup(s => s["Audience"]).Returns("TestAudience");

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c.GetSection("Jwt")).Returns(jwtSectionMock.Object);
        _configuration = configMock.Object;

        _service = new AuthService(_usuarioRepoMock.Object, _configuration, _loggerMock.Object);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenWithRoleClaim()
    {
        // Arrange
        var passwordPlano = "SecurePass123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(passwordPlano);
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            NombreUsuario = "adminUser",
            Email = "admin@gym.com",
            PasswordHash = passwordHash,
            Rol = RolUsuario.Administrador,
            NombreCompleto = "Administrador Sistema",
            Activo = true
        };

        _usuarioRepoMock.Setup(r => r.GetByNombreUsuarioAsync("adminUser"))
            .ReturnsAsync(usuario);

        var loginDto = new LoginRequestDTO("adminUser", passwordPlano);

        // Act
        var result = await _service.LoginAsync(loginDto);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal("adminUser", result.NombreUsuario);
        Assert.Equal("Administrador", result.Rol);

        // Validar que el token JWT contiene el claim de Rol mediante ClaimTypes.Role
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(result.Token);
        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role");

        Assert.NotNull(roleClaim);
        Assert.Equal("Administrador", roleClaim.Value);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsValidationException()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!");
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            NombreUsuario = "trainerUser",
            Email = "trainer@gym.com",
            PasswordHash = passwordHash,
            Rol = RolUsuario.Entrenador,
            Activo = true
        };

        _usuarioRepoMock.Setup(r => r.GetByNombreUsuarioAsync("trainerUser"))
            .ReturnsAsync(usuario);

        var loginDto = new LoginRequestDTO("trainerUser", "WrongPassword123!");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.LoginAsync(loginDto));
    }

    [Fact]
    public async Task LoginAsync_NonExistentUser_ThrowsValidationException()
    {
        // Arrange
        _usuarioRepoMock.Setup(r => r.GetByNombreUsuarioAsync("noExiste"))
            .ReturnsAsync((Usuario?)null);
        _usuarioRepoMock.Setup(r => r.GetByEmailAsync("noExiste"))
            .ReturnsAsync((Usuario?)null);

        var loginDto = new LoginRequestDTO("noExiste", "SomePass123!");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.LoginAsync(loginDto));
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsValidationException()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Pass123!");
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            NombreUsuario = "inactiveUser",
            Email = "inactive@gym.com",
            PasswordHash = passwordHash,
            Rol = RolUsuario.Socio,
            Activo = false // inactivo
        };

        _usuarioRepoMock.Setup(r => r.GetByNombreUsuarioAsync("inactiveUser"))
            .ReturnsAsync(usuario);

        var loginDto = new LoginRequestDTO("inactiveUser", "Pass123!");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.LoginAsync(loginDto));
    }

    [Fact]
    public async Task RegistrarUsuarioAsync_ValidRequest_HashesPasswordAndPersists()
    {
        // Arrange
        var createDto = new UsuarioCreateDTO(
            "nuevoEntrenador",
            "coach@gym.com",
            "PassEntrenador123!",
            "Marcos Gomez",
            "Entrenador"
        );

        _usuarioRepoMock.Setup(r => r.ExisteNombreUsuarioAsync("nuevoEntrenador"))
            .ReturnsAsync(false);
        _usuarioRepoMock.Setup(r => r.GetByEmailAsync("coach@gym.com"))
            .ReturnsAsync((Usuario?)null);
        _usuarioRepoMock.Setup(r => r.CreateAsync(It.IsAny<Usuario>()))
            .ReturnsAsync((Usuario u) => u);

        // Act
        var result = await _service.RegistrarUsuarioAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("nuevoEntrenador", result.NombreUsuario);
        Assert.Equal("coach@gym.com", result.Email);
        Assert.Equal("Entrenador", result.Rol);
        Assert.True(result.Activo);

        _usuarioRepoMock.Verify(r => r.CreateAsync(It.Is<Usuario>(u =>
            u.NombreUsuario == "nuevoEntrenador" &&
            u.Rol == RolUsuario.Entrenador &&
            BCrypt.Net.BCrypt.Verify("PassEntrenador123!", u.PasswordHash)
        )), Times.Once);
    }

    [Fact]
    public async Task RegistrarUsuarioAsync_DuplicateUsername_ThrowsDuplicateResourceException()
    {
        // Arrange
        var createDto = new UsuarioCreateDTO(
            "duplicado",
            "duplicado@gym.com",
            "Pass123456!",
            "Usuario Duplicado",
            "Socio"
        );

        _usuarioRepoMock.Setup(r => r.ExisteNombreUsuarioAsync("duplicado"))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<DuplicateResourceException>(() => _service.RegistrarUsuarioAsync(createDto));
    }

    [Fact]
    public async Task RegistrarUsuarioAsync_InvalidRole_ThrowsValidationException()
    {
        // Arrange
        var createDto = new UsuarioCreateDTO(
            "userInvalido",
            "invalido@gym.com",
            "Pass123456!",
            "Rol Invalido",
            "RolQueNoExiste"
        );

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.RegistrarUsuarioAsync(createDto));
    }
}
