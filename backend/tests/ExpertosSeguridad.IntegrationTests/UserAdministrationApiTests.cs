using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Infrastructure.Persistence;
using FluentAssertions;

namespace ExpertosSeguridad.IntegrationTests;

/// <summary>
/// El panel de administración, y la propiedad que lo hace confiable: un cambio de rol o una
/// desactivación tiene efecto en la siguiente request, no cuando expira el token anterior.
/// </summary>
[Collection(MaintenanceApiCollection.Name)]
public sealed class UserAdministrationApiTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = MaintenanceApiFactory.JsonOptions;

    private readonly MaintenanceApiFactory _factory;

    private HttpClient _admin = null!;
    private HttpClient _ana = null!;
    private HttpClient _diana = null!;

    public UserAdministrationApiTests(MaintenanceApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();

        _admin = await _factory.CreateSignedInClientAsync(TestAccounts.AdminEmail);
        _ana = await _factory.CreateSignedInClientAsync(TestAccounts.AnaEmail);
        _diana = await _factory.CreateSignedInClientAsync(TestAccounts.DianaEmail);
    }

    public Task DisposeAsync()
    {
        _admin.Dispose();
        _ana.Dispose();
        _diana.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Admin_ListsAndFiltersAccounts()
    {
        var all = await _admin.GetFromJsonAsync<PagedResult<UserSummaryDto>>("/api/admin/users", JsonOptions);
        all!.TotalItems.Should().Be(TestAccounts.All.Count);

        var staff = await _admin.GetFromJsonAsync<PagedResult<UserSummaryDto>>(
            "/api/admin/users?role=Staff",
            JsonOptions);
        staff!.Items.Should().OnlyContain(user => user.Role == UserRole.Staff).And.HaveCount(2);

        var search = await _admin.GetFromJsonAsync<PagedResult<UserSummaryDto>>(
            "/api/admin/users?search=cliente.com",
            JsonOptions);
        search!.Items.Should().OnlyContain(user => user.Email.EndsWith("@cliente.com"));
    }

    [Theory]
    [InlineData(TestAccounts.AnaEmail)]
    [InlineData(TestAccounts.DianaEmail)]
    public async Task NonAdmins_CannotReachTheAdministrationEndpoints(string email)
    {
        using var client = await _factory.CreateSignedInClientAsync(email);

        var response = await client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PromotingARequester_InvalidatesTheOldToken_AndTheNewOneCarriesTheNewRole()
    {
        var ana = await FindAsync(TestAccounts.AnaEmail);

        var promote = await _admin.PatchAsJsonAsync(
            $"/api/admin/users/{ana.Id}/role",
            new ChangeUserRoleCommand(UserRole.Staff),
            JsonOptions);
        promote.StatusCode.Should().Be(HttpStatusCode.OK);

        // El token que tiene Ana todavía dice "Requester": debe dejar de funcionar de inmediato en
        // lugar de conservar los permisos anteriores hasta que expire.
        var withOldToken = await _ana.GetAsync("/api/maintenance-requests");
        withOldToken.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var promoted = await _factory.CreateSignedInClientAsync(TestAccounts.AnaEmail);
        var me = await promoted.GetFromJsonAsync<AuthenticatedUserDto>("/api/auth/me", JsonOptions);
        me!.Role.Should().Be(UserRole.Staff);

        // Y el rol nuevo de verdad otorga el permiso del personal.
        var staffList = await promoted.GetAsync("/api/users/staff");
        staffList.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeactivatingAnAccount_CutsItsSessionAndBlocksSignIn_UntilReactivated()
    {
        var diana = await FindAsync(TestAccounts.DianaEmail);

        var deactivate = await _admin.PatchAsJsonAsync(
            $"/api/admin/users/{diana.Id}/status",
            new ChangeUserStatusCommand(false),
            JsonOptions);
        deactivate.StatusCode.Should().Be(HttpStatusCode.OK);

        var withOldToken = await _diana.GetAsync("/api/maintenance-requests");
        withOldToken.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var login = await LoginAsync(TestAccounts.DianaEmail);
        login.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Al personal desactivado tampoco se le ofrece como responsable.
        var staff = await (await _factory.CreateSignedInClientAsync(TestAccounts.EstebanEmail))
            .GetFromJsonAsync<List<UserDto>>("/api/users/staff", JsonOptions);
        staff!.Should().NotContain(user => user.Id == diana.Id);

        var reactivate = await _admin.PatchAsJsonAsync(
            $"/api/admin/users/{diana.Id}/status",
            new ChangeUserStatusCommand(true),
            JsonOptions);
        reactivate.StatusCode.Should().Be(HttpStatusCode.OK);

        (await LoginAsync(TestAccounts.DianaEmail)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Admin_CannotChangeTheirOwnRole_OrDisableTheirOwnAccount()
    {
        var admin = await FindAsync(TestAccounts.AdminEmail);

        var demote = await _admin.PatchAsJsonAsync(
            $"/api/admin/users/{admin.Id}/role",
            new ChangeUserRoleCommand(UserRole.Requester),
            JsonOptions);
        var disable = await _admin.PatchAsJsonAsync(
            $"/api/admin/users/{admin.Id}/status",
            new ChangeUserStatusCommand(false),
            JsonOptions);

        demote.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        disable.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Sigue siendo administradora y sigue pudiendo trabajar.
        (await _admin.GetAsync("/api/admin/users")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Admin_OverseesEveryRequest_ButCannotOperateThem()
    {
        var created = await _ana.PostAsJsonAsync(
            "/api/maintenance-requests",
            new CreateMaintenanceRequestCommand(
                "Filtración en el techo de la bodega",
                "Se observa humedad en el techo de la bodega después de la lluvia de anoche.",
                RequestCategory.Infrastructure,
                RequestPriority.Medium),
            JsonOptions);
        var request = await created.Content.ReadFromJsonAsync<MaintenanceRequestDetailDto>(JsonOptions);

        var list = await _admin.GetFromJsonAsync<PagedResult<MaintenanceRequestListItemDto>>(
            "/api/maintenance-requests",
            JsonOptions);
        list!.TotalItems.Should().Be(1);

        (await _admin.GetAsync($"/api/maintenance-requests/{request!.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var transition = await _admin.PatchAsJsonAsync(
            $"/api/maintenance-requests/{request.Id}/status",
            new ChangeStatusCommand(RequestStatus.InProgress),
            JsonOptions);
        transition.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var create = await _admin.PostAsJsonAsync(
            "/api/maintenance-requests",
            new CreateMaintenanceRequestCommand(
                "Solicitud abierta por el administrador",
                "El administrador supervisa el proceso, no registra solicitudes.",
                RequestCategory.Other,
                RequestPriority.Low),
            JsonOptions);
        create.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<UserSummaryDto> FindAsync(string email)
    {
        var page = await _admin.GetFromJsonAsync<PagedResult<UserSummaryDto>>(
            $"/api/admin/users?search={Uri.EscapeDataString(email)}",
            JsonOptions);

        return page!.Items.Single(user => user.Email == email);
    }

    private Task<HttpResponseMessage> LoginAsync(string email) =>
        _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/login",
            new LoginCommand(email, MaintenanceApiFactory.DemoPassword),
            JsonOptions);
}
