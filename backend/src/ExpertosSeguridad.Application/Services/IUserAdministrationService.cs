using ExpertosSeguridad.Application.Contracts;

namespace ExpertosSeguridad.Application.Services;

public interface IUserAdministrationService
{
    Task<PagedResult<UserSummaryDto>> SearchAsync(UserQuery query, CancellationToken cancellationToken = default);

    Task<UserSummaryDto> ChangeRoleAsync(Guid id, ChangeUserRoleCommand command, CancellationToken cancellationToken = default);

    Task<UserSummaryDto> ChangeStatusAsync(Guid id, ChangeUserStatusCommand command, CancellationToken cancellationToken = default);
}
