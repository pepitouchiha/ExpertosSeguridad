using System.Net;
using System.Net.Http.Json;
using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Infrastructure.Persistence;
using FluentAssertions;

namespace ExpertosSeguridad.IntegrationTests;

/// <summary>
/// Las reglas que hacen reales los roles. Todas se verifican en el servidor: el frontend también
/// oculta las acciones que no debe ofrecer, pero aquí no se confía en nada de eso.
/// </summary>
[Collection(MaintenanceApiCollection.Name)]
public sealed class AuthorizationApiTests : IAsyncLifetime
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = MaintenanceApiFactory.JsonOptions;

    private readonly MaintenanceApiFactory _factory;

    private HttpClient _anonymous = null!;
    private HttpClient _ana = null!;
    private HttpClient _carlos = null!;
    private HttpClient _diana = null!;

    public AuthorizationApiTests(MaintenanceApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();

        _anonymous = _factory.CreateClient();
        _ana = await _factory.CreateSignedInClientAsync(TestAccounts.AnaEmail);
        _carlos = await _factory.CreateSignedInClientAsync(TestAccounts.CarlosEmail);
        _diana = await _factory.CreateSignedInClientAsync(TestAccounts.DianaEmail);
    }

    public Task DisposeAsync()
    {
        _anonymous.Dispose();
        _ana.Dispose();
        _carlos.Dispose();
        _diana.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Login_WithWrongPassword_IsRejectedWithoutSayingWhy()
    {
        var response = await _anonymous.PostAsJsonAsync(
            "/api/auth/login",
            new LoginCommand(TestAccounts.AnaEmail, "contraseña-incorrecta"),
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problem!.Detail.Should().NotContain("contraseña-incorrecta");
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReportsTheSameFailureAsAWrongPassword()
    {
        var unknown = await _anonymous.PostAsJsonAsync(
            "/api/auth/login",
            new LoginCommand("nadie@cliente.com", MaintenanceApiFactory.DemoPassword),
            JsonOptions);

        var wrongPassword = await _anonymous.PostAsJsonAsync(
            "/api/auth/login",
            new LoginCommand(TestAccounts.AnaEmail, "otra-cosa"),
            JsonOptions);

        unknown.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        wrongPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Cuerpos idénticos: el endpoint no sirve para averiguar qué cuentas existen.
        var unknownBody = await unknown.Content.ReadAsStringAsync();
        var wrongPasswordBody = await wrongPassword.Content.ReadAsStringAsync();
        unknownBody.Should().Be(wrongPasswordBody);
    }

    [Theory]
    [InlineData("/api/maintenance-requests")]
    [InlineData("/api/maintenance-requests/summary")]
    [InlineData("/api/users/staff")]
    public async Task ProtectedEndpoints_RejectAnonymousCallers(string path)
    {
        var response = await _anonymous.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Requester_OnlySeesTheirOwnRequests()
    {
        await CreateRequestAsync(_ana, "Fuga de agua reportada por Ana");
        await CreateRequestAsync(_carlos, "Aire acondicionado reportado por Carlos");

        var anaList = await _ana.GetFromJsonAsync<PagedResult<MaintenanceRequestListItemDto>>(
            "/api/maintenance-requests",
            JsonOptions);

        anaList!.TotalItems.Should().Be(1);
        anaList.Items.Should().ContainSingle().Which.Title.Should().Be("Fuga de agua reportada por Ana");

        // El personal ve todas las solicitudes.
        var staffList = await _diana.GetFromJsonAsync<PagedResult<MaintenanceRequestListItemDto>>(
            "/api/maintenance-requests",
            JsonOptions);

        staffList!.TotalItems.Should().Be(2);
    }

    [Fact]
    public async Task Requester_CannotReadSomebodyElsesRequest()
    {
        var carlosRequest = await CreateRequestAsync(_carlos, "Aire acondicionado reportado por Carlos");

        var response = await _ana.GetAsync($"/api/maintenance-requests/{carlosRequest.Id}");

        // Se reporta como inexistente y no como prohibida: un 403 confirmaría que el id existe.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Requester_SummaryCountsOnlyTheirOwnRequests()
    {
        await CreateRequestAsync(_ana, "Primera solicitud de Ana");
        await CreateRequestAsync(_ana, "Segunda solicitud de Ana");
        await CreateRequestAsync(_carlos, "Solicitud de Carlos");

        var anaSummary = await _ana.GetFromJsonAsync<RequestSummaryDto>(
            "/api/maintenance-requests/summary",
            JsonOptions);
        var staffSummary = await _diana.GetFromJsonAsync<RequestSummaryDto>(
            "/api/maintenance-requests/summary",
            JsonOptions);

        anaSummary!.Total.Should().Be(2);
        anaSummary.Pending.Should().Be(2);
        staffSummary!.Total.Should().Be(3);
    }

    [Fact]
    public async Task Requester_CannotChangeStatus()
    {
        var request = await CreateRequestAsync(_ana, "Solicitud que Ana intenta avanzar");

        var response = await _ana.PatchAsJsonAsync(
            $"/api/maintenance-requests/{request.Id}/status",
            new ChangeStatusCommand(RequestStatus.InProgress),
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Y nada se movió.
        var detail = await _ana.GetFromJsonAsync<MaintenanceRequestDetailDto>(
            $"/api/maintenance-requests/{request.Id}",
            JsonOptions);
        detail!.Status.Should().Be(RequestStatus.Pending);
        detail.History.Should().ContainSingle();
    }

    [Fact]
    public async Task Requester_CannotAssignAResponsible()
    {
        var request = await CreateRequestAsync(_ana, "Solicitud que Ana intenta asignar");

        var response = await _ana.PatchAsJsonAsync(
            $"/api/maintenance-requests/{request.Id}/responsible",
            new AssignResponsibleCommand(Guid.NewGuid()),
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Requester_CannotListTheStaffCatalogue()
    {
        var response = await _ana.GetAsync("/api/users/staff");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Staff_CannotOpenRequests()
    {
        var response = await _diana.PostAsJsonAsync(
            "/api/maintenance-requests",
            new CreateMaintenanceRequestCommand(
                "Solicitud abierta por el personal",
                "El personal de la empresa atiende solicitudes, no las registra.",
                RequestCategory.Equipment,
                RequestPriority.Medium),
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssigningARequesterAsResponsible_IsRejected()
    {
        var request = await CreateRequestAsync(_ana, "Solicitud para asignación inválida");

        var me = await _ana.GetFromJsonAsync<AuthenticatedUserDto>("/api/auth/me", JsonOptions);

        var response = await _diana.PatchAsJsonAsync(
            $"/api/maintenance-requests/{request.Id}/responsible",
            new AssignResponsibleCommand(me!.Id),
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Me_ReturnsTheSignedInUserWithTheirRole()
    {
        var requester = await _ana.GetFromJsonAsync<AuthenticatedUserDto>("/api/auth/me", JsonOptions);
        var staff = await _diana.GetFromJsonAsync<AuthenticatedUserDto>("/api/auth/me", JsonOptions);

        requester!.Role.Should().Be(UserRole.Requester);
        requester.Email.Should().Be(TestAccounts.AnaEmail);
        staff!.Role.Should().Be(UserRole.Staff);
    }

    private static async Task<MaintenanceRequestDetailDto> CreateRequestAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync(
            "/api/maintenance-requests",
            new CreateMaintenanceRequestCommand(
                title,
                "Descripción de prueba con longitud suficiente para superar la validación.",
                RequestCategory.Infrastructure,
                RequestPriority.Medium),
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<MaintenanceRequestDetailDto>(JsonOptions))!;
    }
}
