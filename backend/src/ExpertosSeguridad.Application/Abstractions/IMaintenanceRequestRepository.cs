using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Domain.Entities;
using ExpertosSeguridad.Domain.Enums;

namespace ExpertosSeguridad.Application.Abstractions;

/// <summary>
/// Persistence contract for the aggregate. Declared in the application layer and
/// implemented in infrastructure, so the dependency points inwards (DIP).
/// </summary>
public interface IMaintenanceRequestRepository
{
    Task AddAsync(MaintenanceRequest request, CancellationToken cancellationToken = default);

    Task<MaintenanceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<MaintenanceRequest?> GetByIdWithHistoryAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<MaintenanceRequest>> SearchAsync(
        MaintenanceRequestQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<RequestStatus, int>> CountByStatusAsync(CancellationToken cancellationToken = default);
}
