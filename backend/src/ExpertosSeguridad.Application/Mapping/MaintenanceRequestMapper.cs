using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Domain.Entities;
using ExpertosSeguridad.Domain.Policies;

namespace ExpertosSeguridad.Application.Mapping;

/// <summary>
/// Mapeo explícito entre el agregado y los contratos de la API. Escrito a mano a propósito: las
/// formas son pocas y estables, y una proyección explícita es más fácil de leer y depurar que la
/// configuración de un mapeador por convención.
/// </summary>
internal static class MaintenanceRequestMapper
{
    public static MaintenanceRequestListItemDto ToListItem(this MaintenanceRequest request) => new(
        request.Id,
        request.Number,
        request.Title,
        request.Category,
        request.Priority,
        request.Status,
        request.ResponsibleId.HasValue ? new UserDto(request.ResponsibleId.Value, request.ResponsibleName!) : null,
        request.CreatedAt);

    public static MaintenanceRequestDetailDto ToDetail(this MaintenanceRequest request, CurrentUser viewer) => new(
        request.Id,
        request.Number,
        request.Title,
        request.Description,
        request.Category,
        request.Priority,
        request.Status,
        new UserDto(request.RequesterId, request.RequesterName),
        request.ResponsibleId.HasValue ? new UserDto(request.ResponsibleId.Value, request.ResponsibleName!) : null,
        request.CreatedAt,
        request.UpdatedAt,
        request.AllowedNextStatuses,
        ActionsFor(request, viewer),
        request.Resolution is { } resolution
            ? new ResolutionDto(
                resolution.Title,
                resolution.Description,
                new UserDto(resolution.RespondedById, resolution.RespondedByName),
                resolution.RespondedAt)
            : null,
        request.History
            .OrderBy(entry => entry.OccurredAt)
            // Los eventos del mismo instante (una asignación y el inicio que provoca) se desempatan por
            // el orden declarado en HistoryEventType: la causa se lee antes que su efecto.
            .ThenBy(entry => entry.EventType)
            .Select(entry => new HistoryEntryDto(
                entry.Id,
                entry.EventType,
                entry.PreviousValue,
                entry.NewValue,
                new UserDto(entry.ActorId, entry.ActorName),
                entry.OccurredAt))
            .ToList());

    /// <summary>
    /// Las transiciones posibles de la solicitud, reducidas a las que puede tomar quien consulta.
    /// Un solicitante o un administrador no recibe ninguna; alguien del personal que no es el
    /// responsable recibe la cancelación; el responsable recibe el conjunto completo.
    /// </summary>
    private static RequestActionsDto ActionsFor(MaintenanceRequest request, CurrentUser viewer) => new(
        RequestAccessPolicy.CanManageLifecycle(viewer.Role) && !RequestStatusTransitionPolicy.IsTerminal(request.Status),
        request.AllowedNextStatuses
            .Where(target => RequestAccessPolicy.CanChangeStatus(viewer.Role, viewer.Id, request.ResponsibleId, target))
            .ToList());
}
