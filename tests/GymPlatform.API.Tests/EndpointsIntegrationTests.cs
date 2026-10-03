using System.Net;
using System.Net.Http.Json;
using GymPlatform.Shared.DTOs;
using Xunit;

namespace GymPlatform.API.Tests;

public class EndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public EndpointsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSocios_ReturnsSuccessAndList()
    {
        // Act
        var response = await _client.GetAsync("/api/socios");

        // Assert
        response.EnsureSuccessStatusCode();
        var socios = await response.Content.ReadFromJsonAsync<List<SocioResponseDTO>>();
        Assert.NotNull(socios);
    }

    [Fact]
    public async Task PostSocio_ValidPayload_Returns201Created()
    {
        // Arrange (CU-01 Camino feliz)
        var dto = new SocioCreateDTO(
            "Agustin Rossi",
            "40111222",
            "agustin.rossi@email.com",
            "+54 9 11 9988-7766",
            new DateOnly(1996, 4, 15)
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/socios", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var socio = await response.Content.ReadFromJsonAsync<SocioResponseDTO>();
        Assert.NotNull(socio);
        Assert.Equal("Agustin Rossi", socio.Nombre);
    }

    [Fact]
    public async Task PostSocio_DuplicateDni_Returns409Conflict()
    {
        // Arrange (CU-01 3a Duplicidad)
        var dto1 = new SocioCreateDTO(
            "Maria Becerra",
            "41000999",
            "maria.becerra1@email.com",
            "+54 9 11 1122-3344",
            new DateOnly(1998, 2, 12)
        );
        var dto2 = new SocioCreateDTO(
            "Maria Becerra Clone",
            "41000999",
            "maria.otra@email.com",
            "+54 9 11 1122-3344",
            new DateOnly(1998, 2, 12)
        );

        await _client.PostAsJsonAsync("/api/socios", dto1);

        // Act
        var response = await _client.PostAsJsonAsync("/api/socios", dto2);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostPlan_NegativePrice_Returns400BadRequest()
    {
        // Arrange (CU-02 2a Datos inválidos)
        var dto = new PlanMembresiaCreateDTO(
            "Plan Negativo",
            "Descripcion",
            -500m,
            30
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/planes", dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPlan_ValidPayload_Returns201Created()
    {
        // Arrange (CU-02 Camino feliz)
        var dto = new PlanMembresiaCreateDTO(
            "Plan Semestral Promo",
            "Acceso semestral con beneficios",
            120000m,
            180
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/planes", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostHorario_ConflictSchedule_Returns409Conflict()
    {
        // Arrange (CU-03 3a Solapamiento de horarios)
        // 1. Crear actividad y plan
        var actResp = await _client.PostAsJsonAsync("/api/actividades", new ActividadCreateDTO("Boxeo", "Clases de boxeo"));
        var act = await actResp.Content.ReadFromJsonAsync<ActividadResponseDTO>();

        var planResp = await _client.PostAsJsonAsync("/api/planes", new PlanMembresiaCreateDTO("Plan Box", "Plan de box", 28000m, 30));
        var plan = await planResp.Content.ReadFromJsonAsync<PlanMembresiaResponseDTO>();

        var horario1 = new HorarioCreateDTO(
            act!.Id,
            plan!.Id,
            "Profesor Gonzalez",
            "Ring 1",
            DayOfWeek.Tuesday,
            new TimeOnly(15, 0),
            new TimeOnly(16, 0),
            12
        );

        var horario2 = new HorarioCreateDTO(
            act.Id,
            plan.Id,
            "Profesor Gonzalez",
            "Ring 2",
            DayOfWeek.Tuesday,
            new TimeOnly(15, 30),
            new TimeOnly(16, 30),
            10
        );

        await _client.PostAsJsonAsync("/api/horarios", horario1);

        // Act
        var response = await _client.PostAsJsonAsync("/api/horarios", horario2);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
