using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Application.Exceptions;
using ExpertosSeguridad.Domain.Entities;
using ExpertosSeguridad.Domain.Policies;

namespace ExpertosSeguridad.Application.Services;

/// <summary>
/// Casos de uso del panel de administración. La misma forma que el servicio de solicitudes:
/// verificar al llamante contra una política del dominio, delegar la regla en la entidad y
/// confirmar una vez. Las restricciones de «no sobre uno mismo» no están aquí: son invariantes de
/// <see cref="User"/>.
/// </summary>
public sealed class UserAdministrationService : IUserAdministrationService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserProvider _currentUserProvider;

    public UserAdministrationService(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        ICurrentUserProvider currentUserProvider)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<PagedResult<UserSummaryDto>> SearchAsync(
        UserQuery query,
        CancellationToken cancellationToken = default)
    {
        UserAdministrationPolicy.EnsureCanManageUsers(_currentUserProvider.GetCurrentUser().Role);

        var page = await _users.SearchAsync(query.Normalise(), cancellationToken);

        return new PagedResult<UserSummaryDto>(
            page.Items.Select(ToDto).ToList(),
            page.Page,
            page.PageSize,
            page.TotalItems);
    }

    public async Task<UserSummaryDto> ChangeRoleAsync(
        Guid id,
        ChangeUserRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        var admin = _currentUserProvider.GetCurrentUser();
        UserAdministrationPolicy.EnsureCanManageUsers(admin.Role);

        var user = await _users.GetForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException("el usuario", id);

        user.ChangeRole(command.Role, admin.Id);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    public async Task<UserSummaryDto> ChangeStatusAsync(
        Guid id,
        ChangeUserStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var admin = _currentUserProvider.GetCurrentUser();
        UserAdministrationPolicy.EnsureCanManageUsers(admin.Role);

        var user = await _users.GetForUpdateAsync(id, cancellationToken)
            ?? throw new NotFoundException("el usuario", id);

        if (command.IsActive)
        {
            user.Activate();
        }
        else
        {
            user.Deactivate(admin.Id);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    private static UserSummaryDto ToDto(User user) =>
        new(user.Id, user.FullName, user.Email, user.Role, user.IsActive, user.CreatedAt);
}
