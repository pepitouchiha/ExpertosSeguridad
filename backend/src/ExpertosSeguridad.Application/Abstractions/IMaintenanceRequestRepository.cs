using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Domain.Entities;
using ExpertosSeguridad.Domain.Enums;

namespace ExpertosSeguridad.Application.Abstractions;

/// <summary>
/// Contrato de persistencia del agregado. Se declara en la capa de aplicación y se implementa
/// en infraestructura, así que la dependencia apunta hacia adentro (DIP).
/// </summary>
public interface IMaintenanceRequestRepository
{
    Task AddAsync(MaintenanceRequest request, CancellationToken cancellationToken = default);

    Task<MaintenanceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<MaintenanceRequest?> GetByIdWithHistoryAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<MaintenanceRequest>> SearchAsync(
        MaintenanceRequestQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cuenta por estado, opcionalmente restringido a un solicitante, para que el panel que ve un
    /// solicitante coincida con la lista que tiene permitido leer.
    /// </summary>
    Task<IReadOnlyDictionary<RequestStatus, int>> CountByStatusAsync(
        Guid? requesterId = null,
        CancellationToken cancellationToken = default);
}
