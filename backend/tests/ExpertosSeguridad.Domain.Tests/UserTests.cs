using ExpertosSeguridad.Domain.Entities;
using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Domain.Exceptions;
using FluentAssertions;

namespace ExpertosSeguridad.Domain.Tests;

public class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 5, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_NormalisesTheEmailAndTrimsTheName()
    {
        var user = User.Create("  Ana Torres  ", "  Ana.Torres@Cliente.COM ", "hash", UserRole.Requester, Now);

        user.FullName.Should().Be("Ana Torres");
        // Se guarda en minúsculas, que es lo que permite que el índice único y la búsqueda del login
        // coincidan sin una función sobre la columna.
        user.Email.Should().Be("ana.torres@cliente.com");
        user.Role.Should().Be(UserRole.Requester);
        user.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sin-arroba")]
    [InlineData("@sindominio")]
    [InlineData("sinusuario@")]
    [InlineData("dos@arrobas@ejemplo.com")]
    public void Create_WithInvalidEmail_IsRejected(string email)
    {
        var act = () => User.Create("Ana Torres", email, "hash", UserRole.Requester, Now);

        act.Should().Throw<DomainValidationException>().Which.Field.Should().Be("Email");
    }

    [Fact]
    public void Create_WithoutACredential_IsRejected()
    {
        var act = () => User.Create("Ana Torres", "ana@cliente.com", "  ", UserRole.Requester, Now);

        act.Should().Throw<DomainValidationException>().Which.Field.Should().Be("PasswordHash");
    }

    [Fact]
    public void Create_WithUndefinedRole_IsRejected()
    {
        var act = () => User.Create("Ana Torres", "ana@cliente.com", "hash", (UserRole)42, Now);

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void ToActor_CarriesTheIdentityTheHistoryRecords()
    {
        var user = User.Create("Diana López", "diana@expertoseguridad.com", "hash", UserRole.Staff, Now);

        var actor = user.ToActor();

        actor.Id.Should().Be(user.Id);
        actor.Name.Should().Be("Diana López");
    }

    [Fact]
    public void Register_AlwaysProducesAnActiveRequester()
    {
        var user = User.Register("Laura Pérez", "laura@cliente.com", "hash", Now);

        user.Role.Should().Be(UserRole.Requester);
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ChangeRole_ByAnotherUser_AppliesTheNewRole()
    {
        var user = User.Register("Laura Pérez", "laura@cliente.com", "hash", Now);

        user.ChangeRole(UserRole.Staff, actingUserId: Guid.NewGuid());

        user.Role.Should().Be(UserRole.Staff);
    }

    [Fact]
    public void ChangeRole_OnOneself_IsForbidden_SoTheLastAdministratorCannotDisappear()
    {
        var admin = User.Create("Marcela Rincón", "admin@expertoseguridad.com", "hash", UserRole.Admin, Now);

        var act = () => admin.ChangeRole(UserRole.Requester, actingUserId: admin.Id);

        act.Should().Throw<ForbiddenOperationException>();
        admin.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public void ChangeRole_ToTheSameRole_IsRejected()
    {
        var user = User.Register("Laura Pérez", "laura@cliente.com", "hash", Now);

        var act = () => user.ChangeRole(UserRole.Requester, actingUserId: Guid.NewGuid());

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void ChangeRole_ToAnUndefinedRole_IsRejected()
    {
        var user = User.Register("Laura Pérez", "laura@cliente.com", "hash", Now);

        var act = () => user.ChangeRole((UserRole)42, actingUserId: Guid.NewGuid());

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Deactivate_BlocksSignIn_AndActivateRestoresIt()
    {
        var user = User.Register("Laura Pérez", "laura@cliente.com", "hash", Now);

        user.Deactivate(actingUserId: Guid.NewGuid());

        user.IsActive.Should().BeFalse();
        user.Invoking(u => u.EnsureCanSignIn()).Should().Throw<ForbiddenOperationException>();

        user.Activate();

        user.IsActive.Should().BeTrue();
        user.Invoking(u => u.EnsureCanSignIn()).Should().NotThrow();
    }

    [Fact]
    public void Deactivate_OnOneself_IsForbidden()
    {
        var admin = User.Create("Marcela Rincón", "admin@expertoseguridad.com", "hash", UserRole.Admin, Now);

        var act = () => admin.Deactivate(actingUserId: admin.Id);

        act.Should().Throw<ForbiddenOperationException>();
        admin.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_AnAlreadyDisabledAccount_IsRejected()
    {
        var user = User.Register("Laura Pérez", "laura@cliente.com", "hash", Now);
        user.Deactivate(actingUserId: Guid.NewGuid());

        var act = () => user.Deactivate(actingUserId: Guid.NewGuid());

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Activate_AnAlreadyActiveAccount_IsRejected()
    {
        var user = User.Register("Laura Pérez", "laura@cliente.com", "hash", Now);

        var act = () => user.Activate();

        act.Should().Throw<DomainValidationException>();
    }
}
