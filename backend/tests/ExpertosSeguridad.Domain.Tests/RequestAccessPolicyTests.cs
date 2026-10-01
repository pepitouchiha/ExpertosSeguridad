using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Domain.Exceptions;
using ExpertosSeguridad.Domain.Policies;
using FluentAssertions;

namespace ExpertosSeguridad.Domain.Tests;

/// <summary>
/// Las reglas de autorización, ejercitadas sin HTTP, sin token y sin base de datos: justamente
/// la razón de tenerlas en el dominio y no en atributos de los controladores.
/// </summary>
public class RequestAccessPolicyTests
{
    private static readonly Guid Ana = Guid.NewGuid();
    private static readonly Guid Carlos = Guid.NewGuid();

    [Fact]
    public void Requesters_OpenRequests_AndStaffDoNot()
    {
        RequestAccessPolicy.CanCreateRequests(UserRole.Requester).Should().BeTrue();
        RequestAccessPolicy.CanCreateRequests(UserRole.Staff).Should().BeFalse();
    }

    [Fact]
    public void Staff_DriveTheLifecycle_AndRequestersDoNot()
    {
        RequestAccessPolicy.CanManageLifecycle(UserRole.Staff).Should().BeTrue();
        RequestAccessPolicy.CanManageLifecycle(UserRole.Requester).Should().BeFalse();
    }

    [Fact]
    public void Staff_SeeEveryRequest()
    {
        RequestAccessPolicy.CanViewEveryRequest(UserRole.Staff).Should().BeTrue();
        RequestAccessPolicy.CanViewRequest(UserRole.Staff, Carlos, requesterId: Ana).Should().BeTrue();
    }

    [Fact]
    public void Requester_SeesOwnRequestsOnly()
    {
        RequestAccessPolicy.CanViewEveryRequest(UserRole.Requester).Should().BeFalse();
        RequestAccessPolicy.CanViewRequest(UserRole.Requester, Ana, requesterId: Ana).Should().BeTrue();
        RequestAccessPolicy.CanViewRequest(UserRole.Requester, Ana, requesterId: Carlos).Should().BeFalse();
    }

    [Fact]
    public void EnsureCanCreateRequests_RejectsStaffWithAnExplicitFailure()
    {
        var act = () => RequestAccessPolicy.EnsureCanCreateRequests(UserRole.Staff);

        act.Should().Throw<ForbiddenOperationException>();
    }

    [Fact]
    public void EnsureCanManageLifecycle_RejectsRequestersWithAnExplicitFailure()
    {
        var act = () => RequestAccessPolicy.EnsureCanManageLifecycle(UserRole.Requester);

        act.Should().Throw<ForbiddenOperationException>();
    }

    [Fact]
    public void Admin_OverseesEveryRequest_ButNeitherOpensNorOperatesThem()
    {
        // Separación de funciones: quien otorga roles no los usa también sobre el proceso.
        RequestAccessPolicy.CanViewEveryRequest(UserRole.Admin).Should().BeTrue();
        RequestAccessPolicy.CanViewRequest(UserRole.Admin, Carlos, requesterId: Ana).Should().BeTrue();
        RequestAccessPolicy.CanManageLifecycle(UserRole.Admin).Should().BeFalse();
        RequestAccessPolicy.CanCreateRequests(UserRole.Admin).Should().BeFalse();
    }

    [Theory]
    [InlineData(UserRole.Admin, true)]
    [InlineData(UserRole.Staff, false)]
    [InlineData(UserRole.Requester, false)]
    public void OnlyAdmins_ManageUsers(UserRole role, bool expected)
    {
        UserAdministrationPolicy.CanManageUsers(role).Should().Be(expected);

        var act = () => UserAdministrationPolicy.EnsureCanManageUsers(role);

        if (expected)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<ForbiddenOperationException>();
        }
    }

    [Fact]
    public void EnsureMethods_AllowTheRoleTheyGuard()
    {
        var create = () => RequestAccessPolicy.EnsureCanCreateRequests(UserRole.Requester);
        var manage = () => RequestAccessPolicy.EnsureCanManageLifecycle(UserRole.Staff);

        create.Should().NotThrow();
        manage.Should().NotThrow();
    }
}
