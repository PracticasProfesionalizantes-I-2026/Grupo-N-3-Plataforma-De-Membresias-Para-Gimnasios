using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GymPlatform.Shared.DTOs;
using Xunit;

namespace GymPlatform.API.Tests;

public class AuthEndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200AndJwtToken()
    {
        // Arrange
        var request = new LoginRequestDTO("admin", "Admin123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(body);
        Assert.NotEmpty(body.Token);
        Assert.Equal("admin", body.NombreUsuario);
        Assert.Equal("Administrador", body.Rol);
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns401Unauthorized()
    {
        // Arrange
        var request = new LoginRequestDTO("admin", "WrongPassword!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401Unauthorized()
    {
        // Act - Intentar acceder a planes sin cabecera Authorization
        var unauthenticatedClient = _client;
        unauthenticatedClient.DefaultRequestHeaders.Authorization = null;

        var response = await unauthenticatedClient.GetAsync("/api/planes");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithForbiddenRole_Returns403Forbidden()
    {
        // Arrange - Login como 'socio'
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO("socio", "Socio123!"));
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
        var loginData = await loginResp.Content.ReadFromJsonAsync<LoginResponseDTO>();

        var socioClient = _client;
        socioClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginData!.Token);

        // Act - El socio intenta crear un Plan de Membresía (requiere Rol 'Administrador')
        var createPlanDto = new PlanMembresiaCreateDTO("Plan Prohibido", "Descripcion", 50000m, 30);
        var response = await socioClient.PostAsJsonAsync("/api/planes", createPlanDto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithAuthorizedRole_ReturnsSuccess()
    {
        // Arrange - Login como 'admin'
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO("admin", "Admin123!"));
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
        var loginData = await loginResp.Content.ReadFromJsonAsync<LoginResponseDTO>();

        var adminClient = _client;
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginData!.Token);

        // Act - El administrador puede consultar socios
        var response = await adminClient.GetAsync("/api/socios");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsAuthenticatedClaims()
    {
        // Arrange - Login como 'entrenador'
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO("entrenador", "Entrenador123!"));
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
        var loginData = await loginResp.Content.ReadFromJsonAsync<LoginResponseDTO>();

        var trainerClient = _client;
        trainerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginData!.Token);

        // Act
        var response = await trainerClient.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("entrenador", json);
        Assert.Contains("Entrenador", json);
    }
}
