using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Infrastructure.Persistence;
using FluentAssertions;

namespace ExpertosSeguridad.IntegrationTests;

/// <summary>
/// El flujo alrededor del responsable: asignar inicia una solicitud, el responsable la ejecuta
/// (pausar, reanudar, resolver) y cualquiera del personal la coordina (asignar, reasignar, cancelar).
/// </summary>
[Collection(MaintenanceApiCollection.Name)]
public sealed class ResponsibleWorkflowApiTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = MaintenanceApiFactory.JsonOptions;

    private readonly MaintenanceApiFactory _factory;

    private HttpClient _ana = null!;
    private HttpClient _diana = null!;
    private HttpClient _esteban = null!;
    private UserDto _dianaUser = null!;
    private UserDto _estebanUser = null!;

    public ResponsibleWorkflowApiTests(MaintenanceApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();

        _ana = await _factory.CreateSignedInClientAsync(TestAccounts.AnaEmail);
        _diana = await _factory.CreateSignedInClientAsync(TestAccounts.DianaEmail);
        _esteban = await _factory.CreateSignedInClientAsync(TestAccounts.EstebanEmail);

        var staff = await _diana.GetFromJsonAsync<List<UserDto>>("/api/users/staff", JsonOptions);
        _dianaUser = staff!.Single(user => user.Name == "Diana López");
        _estebanUser = staff!.Single(user => user.Name == "Esteban Mora");
    }

    public Task DisposeAsync()
    {
        _ana.Dispose();
        _diana.Dispose();
        _esteban.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task APendingRequest_CannotBeStartedByHand_OnlyByAssigningIt()
    {
        var request = await CreateRequestAsync();

        var start = await ChangeStatusAsync(_diana, request.Id, RequestStatus.InProgress);
        start.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Una solicitud pendiente solo le ofrece al personal la asignación y la cancelación.
        var detail = await GetAsync(_diana, request.Id);
        detail.AvailableActions.CanAssign.Should().BeTrue();
        detail.AvailableActions.StatusTransitions.Should().Equal(RequestStatus.Cancelled);

        (await AssignAsync(_esteban, request.Id, _dianaUser.Id)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetAsync(_diana, request.Id)).Status.Should().Be(RequestStatus.InProgress);
    }

    [Fact]
    public async Task OnlyTheResponsible_CanPauseResumeAndResolve()
    {
        var request = await CreateRequestAsync();
        await AssignAsync(_esteban, request.Id, _dianaUser.Id);

        // Esteban la asignó pero no es el responsable: no puede ejecutarla...
        (await ChangeStatusAsync(_esteban, request.Id, RequestStatus.Resolved))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ChangeStatusAsync(_esteban, request.Id, RequestStatus.OnHold))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // ...y la API tampoco le ofrece esas acciones.
        var asEsteban = await GetAsync(_esteban, request.Id);
        asEsteban.AvailableActions.StatusTransitions.Should().Equal(RequestStatus.Cancelled);

        // Diana sí: pausar, reanudar, resolver.
        (await ChangeStatusAsync(_diana, request.Id, RequestStatus.OnHold)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await ChangeStatusAsync(_diana, request.Id, RequestStatus.InProgress)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await ResolveAsync(_diana, request.Id)).StatusCode.Should().Be(HttpStatusCode.OK);

        var resolved = await GetAsync(_ana, request.Id);
        resolved.Status.Should().Be(RequestStatus.Resolved);
        resolved.History.Last().Actor.Name.Should().Be("Diana López");
    }

    [Fact]
    public async Task Resolving_RequiresAnAnswer_ThatTheRequesterCanRead()
    {
        var request = await CreateRequestAsync();
        await AssignAsync(_esteban, request.Id, _dianaUser.Id);

        // No por el endpoint de estado, ni siquiera para el responsable...
        (await ChangeStatusAsync(_diana, request.Id, RequestStatus.Resolved))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // ...ni con una respuesta vacía, que se reporta campo por campo.
        var empty = await _diana.PostAsJsonAsync(
            $"/api/maintenance-requests/{request.Id}/resolution",
            new ResolveRequestCommand("", "corta"),
            JsonOptions);
        empty.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await empty.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errors").TryGetProperty("title", out _).Should().BeTrue();
        problem.GetProperty("errors").TryGetProperty("description", out _).Should().BeTrue();
        (await GetAsync(_diana, request.Id)).Status.Should().Be(RequestStatus.InProgress);

        (await ResolveAsync(_diana, request.Id)).StatusCode.Should().Be(HttpStatusCode.OK);

        var asRequester = await GetAsync(_ana, request.Id);
        asRequester.Status.Should().Be(RequestStatus.Resolved);
        asRequester.Resolution!.Title.Should().Be("Trabajo terminado");
        asRequester.Resolution.Description.Should().Be("Se completó la reparación solicitada en sitio.");
        asRequester.Resolution.RespondedBy.Name.Should().Be("Diana López");
    }

    [Fact]
    public async Task AnyStaffMember_CanCancelARequestAssignedToSomeoneElse()
    {
        var request = await CreateRequestAsync();
        await AssignAsync(_diana, request.Id, _dianaUser.Id);

        var cancel = await ChangeStatusAsync(_esteban, request.Id, RequestStatus.Cancelled);

        cancel.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetAsync(_diana, request.Id)).Status.Should().Be(RequestStatus.Cancelled);
    }

    [Fact]
    public async Task Reassigning_HandsTheExecutionOverToTheNewResponsible()
    {
        var request = await CreateRequestAsync();
        await AssignAsync(_esteban, request.Id, _dianaUser.Id);

        (await AssignAsync(_diana, request.Id, _estebanUser.Id)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await ResolveAsync(_diana, request.Id)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ResolveAsync(_esteban, request.Id)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AStartedRequest_CannotBeLeftWithoutResponsible()
    {
        var request = await CreateRequestAsync();
        await AssignAsync(_diana, request.Id, _dianaUser.Id);

        var unassign = await _diana.PatchAsJsonAsync(
            $"/api/maintenance-requests/{request.Id}/responsible",
            new AssignResponsibleCommand(null),
            JsonOptions);

        unassign.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetAsync(_diana, request.Id)).Responsible!.Id.Should().Be(_dianaUser.Id);
    }

    [Fact]
    public async Task StaffList_ShowsEverythingAssignedToThemFirst_OpenBeforeClosed_ThenTheRestByDate()
    {
        // Creadas en este orden, así que por fecha (más recientes primero) la lista sería D, C, B, A.
        var a = await CreateRequestAsync("A - asignada a Diana, en progreso");
        var b = await CreateRequestAsync("B - asignada a Diana, ya resuelta");
        var c = await CreateRequestAsync("C - asignada a Esteban");
        var d = await CreateRequestAsync("D - sin asignar");

        await AssignAsync(_esteban, a.Id, _dianaUser.Id);
        await AssignAsync(_esteban, b.Id, _dianaUser.Id);
        await ResolveAsync(_diana, b.Id);
        await AssignAsync(_diana, c.Id, _estebanUser.Id);

        // Diana: sus dos primero aunque sean las más antiguas —la abierta antes que la resuelta— y
        // solo después el resto, más recientes primero.
        (await TitlesAsync(_diana, "")).Should().Equal(a.Title, b.Title, d.Title, c.Title);

        // Esteban, en cambio, ve primero la suya.
        (await TitlesAsync(_esteban, "")).First().Should().Be(c.Title);

        // La prioridad es parte del orden en SQL, así que se mantiene entre páginas...
        (await TitlesAsync(_diana, "&pageSize=1&page=1")).Should().Equal(a.Title);

        // ...y los filtros siguen aplicándose a ambos grupos.
        (await TitlesAsync(_diana, "&status=Resolved")).Should().Equal(b.Title);
        (await TitlesAsync(_diana, "&pageSize=2&page=2")).Should().Equal(d.Title, c.Title);

        // Un solicitante no tiene nada asignado: orden simple por fecha.
        (await TitlesAsync(_ana, "")).First().Should().Be(d.Title);
    }

    private static async Task<List<string>> TitlesAsync(HttpClient client, string query)
    {
        var page = await client.GetFromJsonAsync<PagedResult<MaintenanceRequestListItemDto>>(
            $"/api/maintenance-requests?sortByCreatedAt=Desc{query}",
            JsonOptions);

        return page!.Items.Select(item => item.Title).ToList();
    }

    private async Task<MaintenanceRequestDetailDto> CreateRequestAsync(string title = "Cerradura de la bodega dañada")
    {
        var response = await _ana.PostAsJsonAsync(
            "/api/maintenance-requests",
            new CreateMaintenanceRequestCommand(
                title,
                "La cerradura electrónica de la bodega no reconoce las tarjetas de acceso.",
                RequestCategory.Equipment,
                RequestPriority.High),
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<MaintenanceRequestDetailDto>(JsonOptions))!;
    }

    private static Task<HttpResponseMessage> AssignAsync(HttpClient client, Guid requestId, Guid responsibleId) =>
        client.PatchAsJsonAsync(
            $"/api/maintenance-requests/{requestId}/responsible",
            new AssignResponsibleCommand(responsibleId),
            JsonOptions);

    private static Task<HttpResponseMessage> ResolveAsync(HttpClient client, Guid requestId) =>
        client.PostAsJsonAsync(
            $"/api/maintenance-requests/{requestId}/resolution",
            new ResolveRequestCommand("Trabajo terminado", "Se completó la reparación solicitada en sitio."),
            JsonOptions);

    private static Task<HttpResponseMessage> ChangeStatusAsync(HttpClient client, Guid requestId, RequestStatus status) =>
        client.PatchAsJsonAsync(
            $"/api/maintenance-requests/{requestId}/status",
            new ChangeStatusCommand(status),
            JsonOptions);

    private static async Task<MaintenanceRequestDetailDto> GetAsync(HttpClient client, Guid requestId) =>
        (await client.GetFromJsonAsync<MaintenanceRequestDetailDto>(
            $"/api/maintenance-requests/{requestId}",
            JsonOptions))!;
}
