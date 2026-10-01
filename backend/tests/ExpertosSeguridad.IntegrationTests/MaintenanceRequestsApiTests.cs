using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Infrastructure.Persistence;
using FluentAssertions;

namespace ExpertosSeguridad.IntegrationTests;

/// <summary>
/// Flujos de punta a punta por HTTP con un PostgreSQL real: un solicitante abre una solicitud,
/// el personal la asigna y maneja su ciclo de vida, se rechaza una transición inválida y se
/// vuelve a leer el historial persistido.
/// </summary>
[Collection(MaintenanceApiCollection.Name)]
public sealed class MaintenanceRequestsApiTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = MaintenanceApiFactory.JsonOptions;

    private readonly MaintenanceApiFactory _factory;

    /// <summary>Ana y Carlos son solicitantes; Diana y Esteban son personal de la empresa.</summary>
    private HttpClient _ana = null!;
    private HttpClient _carlos = null!;
    private HttpClient _diana = null!;

    public MaintenanceRequestsApiTests(MaintenanceApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();

        _ana = await _factory.CreateSignedInClientAsync(TestAccounts.AnaEmail);
        _carlos = await _factory.CreateSignedInClientAsync(TestAccounts.CarlosEmail);
        _diana = await _factory.CreateSignedInClientAsync(TestAccounts.DianaEmail);
    }

    public Task DisposeAsync()
    {
        _ana.Dispose();
        _carlos.Dispose();
        _diana.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task FullLifecycle_IsPersistedWithItsHistory()
    {
        var created = await CreateRequestAsync(_ana, "Fuga de agua en el baño del segundo piso");

        created.Status.Should().Be(RequestStatus.Pending);
        created.Responsible.Should().BeNull();
        created.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));
        created.History.Should().ContainSingle(entry => entry.EventType == HistoryEventType.Created);

        // El solicitante se tomó del token, nunca del cuerpo de la petición.
        created.Requester.Name.Should().Be("Ana Torres");

        var diana = await GetStaffMemberAsync("Diana López");

        // Asignar es lo que inicia la solicitud: no hay una llamada «iniciar» aparte.
        var assignResponse = await _diana.PatchAsJsonAsync(
            $"/api/maintenance-requests/{created.Id}/responsible",
            new AssignResponsibleCommand(diana.Id),
            JsonOptions);
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Se vuelve a leer de la base de datos con una petición nueva, no desde la respuesta de escritura.
        var detail = await GetDetailAsync(_diana, created.Id);

        detail.Status.Should().Be(RequestStatus.InProgress);
        detail.Responsible!.Id.Should().Be(diana.Id);
        detail.AllowedNextStatuses.Should().BeEquivalentTo(new[]
        {
            RequestStatus.OnHold,
            RequestStatus.Resolved,
            RequestStatus.Cancelled
        });

        detail.History.Should().HaveCount(3);
        detail.History.Select(entry => entry.EventType).Should().ContainInOrder(
            HistoryEventType.Created,
            HistoryEventType.ResponsibleChanged,
            HistoryEventType.StatusChanged);

        var responsibleEntry = detail.History.Single(entry => entry.EventType == HistoryEventType.ResponsibleChanged);
        responsibleEntry.PreviousValue.Should().BeNull();
        responsibleEntry.NewValue.Should().Be("Diana López");
        // El actor registrado es el miembro del personal autenticado, no un valor enviado por el cliente.
        responsibleEntry.Actor.Name.Should().Be("Diana López");

        var statusEntry = detail.History.Single(entry => entry.EventType == HistoryEventType.StatusChanged);
        statusEntry.PreviousValue.Should().Be(nameof(RequestStatus.Pending));
        statusEntry.NewValue.Should().Be(nameof(RequestStatus.InProgress));
        statusEntry.Actor.Name.Should().Be("Diana López");

        // Diana es la responsable, así que la API le ofrece el conjunto completo de acciones.
        detail.AvailableActions.StatusTransitions.Should().BeEquivalentTo(new[]
        {
            RequestStatus.OnHold,
            RequestStatus.Resolved,
            RequestStatus.Cancelled
        });

        // El solicitante que la abrió la sigue viendo, con el mismo historial y sin acciones.
        var asRequester = await GetDetailAsync(_ana, created.Id);
        asRequester.Status.Should().Be(RequestStatus.InProgress);
        asRequester.History.Should().HaveCount(3);
        asRequester.AvailableActions.CanAssign.Should().BeFalse();
        asRequester.AvailableActions.StatusTransitions.Should().BeEmpty();

        // La responsable la cierra con una respuesta, que después lee el solicitante.
        var resolve = await _diana.PostAsJsonAsync(
            $"/api/maintenance-requests/{created.Id}/resolution",
            new ResolveRequestCommand("Fuga reparada", "Se cambió el sifón del lavamanos y se selló la conexión."),
            JsonOptions);
        resolve.StatusCode.Should().Be(HttpStatusCode.OK);

        var closed = await GetDetailAsync(_ana, created.Id);
        closed.Status.Should().Be(RequestStatus.Resolved);
        closed.Resolution!.Title.Should().Be("Fuga reparada");
        closed.Resolution.RespondedBy.Name.Should().Be("Diana López");
        closed.Number.Should().BePositive();
    }

    [Fact]
    public async Task ForbiddenTransition_IsRejectedAndLeavesNoTrace()
    {
        var created = await CreateRequestAsync(_ana, "Licencia del antivirus por vencer");

        var response = await _diana.PatchAsJsonAsync(
            $"/api/maintenance-requests/{created.Id}/status",
            new ChangeStatusCommand(RequestStatus.Resolved),
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var detail = await GetDetailAsync(_diana, created.Id);
        detail.Status.Should().Be(RequestStatus.Pending);
        detail.History.Should().ContainSingle();
    }

    [Fact]
    public async Task InvalidPayload_IsRejectedWithFieldLevelErrors()
    {
        var response = await _ana.PostAsJsonAsync(
            "/api/maintenance-requests",
            new CreateMaintenanceRequestCommand("abc", "breve", RequestCategory.Software, RequestPriority.Low),
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errors = problem.GetProperty("errors");
        errors.TryGetProperty("title", out _).Should().BeTrue();
        errors.TryGetProperty("description", out _).Should().BeTrue();
    }

    [Fact]
    public async Task UnknownRequest_ReturnsNotFound()
    {
        var response = await _diana.GetAsync($"/api/maintenance-requests/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_FiltersSearchesAndPagesOnTheServer()
    {
        await CreateRequestAsync(_ana, "Fuga de agua en el sótano", RequestPriority.High);
        await CreateRequestAsync(_ana, "Fuga de agua en la terraza", RequestPriority.Low);
        await CreateRequestAsync(_ana, "Mantenimiento de ascensores", RequestPriority.High);

        var filtered = await _diana.GetFromJsonAsync<PagedResult<MaintenanceRequestListItemDto>>(
            "/api/maintenance-requests?search=fuga&priority=High&page=1&pageSize=10",
            JsonOptions);

        filtered!.TotalItems.Should().Be(1);
        filtered.Items.Should().ContainSingle()
            .Which.Title.Should().Be("Fuga de agua en el sótano");

        var firstPage = await _diana.GetFromJsonAsync<PagedResult<MaintenanceRequestListItemDto>>(
            "/api/maintenance-requests?page=1&pageSize=2&sortByCreatedAt=Asc",
            JsonOptions);

        firstPage!.Items.Should().HaveCount(2);
        firstPage.TotalItems.Should().Be(3);
        firstPage.TotalPages.Should().Be(2);
        firstPage.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Summary_ReflectsPersistedData()
    {
        var first = await CreateRequestAsync(_ana, "Revisión de planta eléctrica");
        await CreateRequestAsync(_ana, "Cambio de cerraduras");

        var diana = await GetStaffMemberAsync("Diana López");
        await _diana.PatchAsJsonAsync(
            $"/api/maintenance-requests/{first.Id}/responsible",
            new AssignResponsibleCommand(diana.Id),
            JsonOptions);

        var summary = await _diana.GetFromJsonAsync<RequestSummaryDto>(
            "/api/maintenance-requests/summary",
            JsonOptions);

        summary!.Total.Should().Be(2);
        summary.Pending.Should().Be(1);
        summary.InProgress.Should().Be(1);
        summary.Resolved.Should().Be(0);
    }

    private async Task<MaintenanceRequestDetailDto> CreateRequestAsync(
        HttpClient client,
        string title,
        RequestPriority priority = RequestPriority.Medium)
    {
        var response = await client.PostAsJsonAsync(
            "/api/maintenance-requests",
            new CreateMaintenanceRequestCommand(
                title,
                "Descripción de prueba con longitud suficiente para superar la validación.",
                RequestCategory.Infrastructure,
                priority),
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<MaintenanceRequestDetailDto>(JsonOptions))!;
    }

    private static async Task<MaintenanceRequestDetailDto> GetDetailAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<MaintenanceRequestDetailDto>(
            $"/api/maintenance-requests/{id}",
            JsonOptions))!;

    private async Task<UserDto> GetStaffMemberAsync(string name)
    {
        var staff = await _diana.GetFromJsonAsync<List<UserDto>>("/api/users/staff", JsonOptions);

        return staff!.Single(user => user.Name == name);
    }
}
