using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Infrastructure.Identity;
using FluentAssertions;

namespace ExpertosSeguridad.IntegrationTests;

/// <summary>
/// End-to-end flow over HTTP with a real PostgreSQL instance: create, assign, transition,
/// reject an invalid transition and read back the persisted history.
/// </summary>
public sealed class MaintenanceRequestsApiTests : IClassFixture<MaintenanceApiFactory>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly MaintenanceApiFactory _factory;
    private HttpClient _client = null!;

    public MaintenanceRequestsApiTests(MaintenanceApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Actor-Id", InMemoryUserDirectory.CarlosRuiz.Id.ToString());
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task FullLifecycle_IsPersistedWithItsHistory()
    {
        var created = await CreateRequestAsync("Fuga de agua en el baño del segundo piso");

        created.Status.Should().Be(RequestStatus.Pending);
        created.Responsible.Should().BeNull();
        created.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));
        created.History.Should().ContainSingle(entry => entry.EventType == HistoryEventType.Created);

        var assignResponse = await _client.PatchAsJsonAsync(
            $"/api/maintenance-requests/{created.Id}/responsible",
            new AssignResponsibleCommand(InMemoryUserDirectory.DianaLopez.Id),
            JsonOptions);
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var statusResponse = await _client.PatchAsJsonAsync(
            $"/api/maintenance-requests/{created.Id}/status",
            new ChangeStatusCommand(RequestStatus.InProgress),
            JsonOptions);
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Read it back from the database through a fresh request, not from the write response.
        var detail = await GetDetailAsync(created.Id);

        detail.Status.Should().Be(RequestStatus.InProgress);
        detail.Responsible!.Id.Should().Be(InMemoryUserDirectory.DianaLopez.Id);
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
        responsibleEntry.NewValue.Should().Be(InMemoryUserDirectory.DianaLopez.Name);
        responsibleEntry.Actor.Id.Should().Be(InMemoryUserDirectory.CarlosRuiz.Id);

        var statusEntry = detail.History.Single(entry => entry.EventType == HistoryEventType.StatusChanged);
        statusEntry.PreviousValue.Should().Be(nameof(RequestStatus.Pending));
        statusEntry.NewValue.Should().Be(nameof(RequestStatus.InProgress));
    }

    [Fact]
    public async Task ForbiddenTransition_IsRejectedAndLeavesNoTrace()
    {
        var created = await CreateRequestAsync("Licencia del antivirus por vencer");

        var response = await _client.PatchAsJsonAsync(
            $"/api/maintenance-requests/{created.Id}/status",
            new ChangeStatusCommand(RequestStatus.Resolved),
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var detail = await GetDetailAsync(created.Id);
        detail.Status.Should().Be(RequestStatus.Pending);
        detail.History.Should().ContainSingle();
    }

    [Fact]
    public async Task InvalidPayload_IsRejectedWithFieldLevelErrors()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/maintenance-requests",
            new CreateMaintenanceRequestCommand(
                "abc",
                "breve",
                RequestCategory.Software,
                RequestPriority.Low,
                InMemoryUserDirectory.AnaTorres.Id),
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
        var response = await _client.GetAsync($"/api/maintenance-requests/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_FiltersSearchesAndPagesOnTheServer()
    {
        await CreateRequestAsync("Fuga de agua en el sótano", RequestPriority.High);
        await CreateRequestAsync("Fuga de agua en la terraza", RequestPriority.Low);
        await CreateRequestAsync("Mantenimiento de ascensores", RequestPriority.High);

        var filtered = await _client.GetFromJsonAsync<PagedResult<MaintenanceRequestListItemDto>>(
            "/api/maintenance-requests?search=fuga&priority=High&page=1&pageSize=10",
            JsonOptions);

        filtered!.TotalItems.Should().Be(1);
        filtered.Items.Should().ContainSingle()
            .Which.Title.Should().Be("Fuga de agua en el sótano");

        var firstPage = await _client.GetFromJsonAsync<PagedResult<MaintenanceRequestListItemDto>>(
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
        var first = await CreateRequestAsync("Revisión de planta eléctrica");
        await CreateRequestAsync("Cambio de cerraduras");

        await _client.PatchAsJsonAsync(
            $"/api/maintenance-requests/{first.Id}/status",
            new ChangeStatusCommand(RequestStatus.InProgress),
            JsonOptions);

        var summary = await _client.GetFromJsonAsync<RequestSummaryDto>(
            "/api/maintenance-requests/summary",
            JsonOptions);

        summary!.Total.Should().Be(2);
        summary.Pending.Should().Be(1);
        summary.InProgress.Should().Be(1);
        summary.Resolved.Should().Be(0);
    }

    private async Task<MaintenanceRequestDetailDto> CreateRequestAsync(
        string title,
        RequestPriority priority = RequestPriority.Medium)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/maintenance-requests",
            new CreateMaintenanceRequestCommand(
                title,
                "Descripción de prueba con longitud suficiente para superar la validación.",
                RequestCategory.Infrastructure,
                priority,
                InMemoryUserDirectory.AnaTorres.Id),
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<MaintenanceRequestDetailDto>(JsonOptions))!;
    }

    private async Task<MaintenanceRequestDetailDto> GetDetailAsync(Guid id) =>
        (await _client.GetFromJsonAsync<MaintenanceRequestDetailDto>(
            $"/api/maintenance-requests/{id}",
            JsonOptions))!;
}
