using ExpertosSeguridad.Application.Contracts;

namespace ExpertosSeguridad.Application.Services;

public interface IMaintenanceRequestService
{
    Task<MaintenanceRequestDetailDto> CreateAsync(
        CreateMaintenanceRequestCommand command,
        CancellationToken cancellationToken = default);

    Task<PagedResult<MaintenanceRequestListItemDto>> SearchAsync(
        MaintenanceRequestQuery query,
        CancellationToken cancellationToken = default);

    Task<MaintenanceRequestDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<MaintenanceRequestDetailDto> ChangeStatusAsync(
        Guid id,
        ChangeStatusCommand command,
        CancellationToken cancellationToken = default);

    Task<MaintenanceRequestDetailDto> ResolveAsync(
        Guid id,
        ResolveRequestCommand command,
        CancellationToken cancellationToken = default);

    Task<MaintenanceRequestDetailDto> AssignResponsibleAsync(
        Guid id,
        AssignResponsibleCommand command,
        CancellationToken cancellationToken = default);

    Task<RequestSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
