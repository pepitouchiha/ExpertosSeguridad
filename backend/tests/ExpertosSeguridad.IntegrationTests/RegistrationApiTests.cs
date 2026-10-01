using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Infrastructure.Persistence;
using FluentAssertions;

namespace ExpertosSeguridad.IntegrationTests;

/// <summary>Registro público de clientes.</summary>
[Collection(MaintenanceApiCollection.Name)]
public sealed class RegistrationApiTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = MaintenanceApiFactory.JsonOptions;

    private readonly MaintenanceApiFactory _factory;
    private HttpClient _anonymous = null!;

    public RegistrationApiTests(MaintenanceApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        _anonymous = _factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        _anonymous.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Register_CreatesARequester_ThatCanOpenRequestsStraightAway()
    {
        var response = await _anonymous.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterCommand("Laura Pérez", "Laura.Perez@Cliente.com", "Segura2026"),
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<AuthResultDto>(JsonOptions);
        result!.User.Role.Should().Be(UserRole.Requester);
        result.User.Email.Should().Be("laura.perez@cliente.com");

        // El token devuelto se puede usar de inmediato: no hace falta un segundo login.
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

        var create = await client.PostAsJsonAsync(
            "/api/maintenance-requests",
            new CreateMaintenanceRequestCommand(
                "Puerta de acceso principal atascada",
                "La puerta del acceso principal no cierra completamente desde esta mañana.",
                RequestCategory.Infrastructure,
                RequestPriority.High),
            JsonOptions);

        create.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_IgnoresARoleSmuggledIntoThePayload()
    {
        // El contrato no tiene campo de rol; una propiedad extra no debe poder agregarlo.
        var response = await _anonymous.PostAsJsonAsync(
            "/api/auth/register",
            new { fullName = "Intruso", email = "intruso@cliente.com", password = "Segura2026", role = "Admin" },
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<AuthResultDto>(JsonOptions);
        result!.User.Role.Should().Be(UserRole.Requester);
    }

    [Fact]
    public async Task Register_WithAnEmailAlreadyTaken_IsAConflictOnTheEmailField()
    {
        // Con otras mayúsculas a propósito: la verificación se hace sobre el correo normalizado.
        var response = await _anonymous.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterCommand("Otra Ana", TestAccounts.AnaEmail.ToUpperInvariant(), "Segura2026"),
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errors").TryGetProperty("email", out _).Should().BeTrue();
    }

    [Fact]
    public async Task MalformedBody_IsRejectedWithoutExposingInternalTypes()
    {
        using var content = new StringContent("{\"fullName\": 42, \"email\": ", System.Text.Encoding.UTF8, "application/json");

        var response = await _anonymous.PostAsync("/api/auth/register", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // El mensaje por defecto del framework nombra el tipo del DTO y su espacio de nombres; la API
        // debe responder con su propio mensaje genérico.
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("ExpertosSeguridad");
        body.Should().NotContain("could not be converted");
        body.Should().Contain("Solicitud inválida");
    }

    [Theory]
    [InlineData("corta1")]
    [InlineData("sololetrasaqui")]
    [InlineData("1234567890")]
    public async Task Register_WithAWeakPassword_IsRejectedOnThePasswordField(string password)
    {
        var response = await _anonymous.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterCommand("Laura Pérez", "laura.debil@cliente.com", password),
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errors").TryGetProperty("password", out _).Should().BeTrue();
    }
}
