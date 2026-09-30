using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Domain.Entities;

namespace ExpertosSeguridad.Application.Mapping;

/// <summary>
/// Explicit mapping between the aggregate and the API contracts. Hand-written on purpose:
/// the shapes are few and stable, and an explicit projection is easier to read and debug
/// than a convention-based mapper configuration.
/// </summary>
internal static class MaintenanceRequestMapper
{
    public static MaintenanceRequestListItemDto ToListItem(this MaintenanceRequest request) => new(
        request.Id,
        request.Title,
        request.Category,
        request.Priority,
        request.Status,
        request.ResponsibleId.HasValue ? new UserDto(request.ResponsibleId.Value, request.ResponsibleName!) : null,
        request.CreatedAt);

    public static MaintenanceRequestDetailDto ToDetail(this MaintenanceRequest request) => new(
        request.Id,
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
        request.History
            .OrderBy(entry => entry.OccurredAt)
            .ThenBy(entry => entry.EventType)
            .Select(entry => new HistoryEntryDto(
                entry.Id,
                entry.EventType,
                entry.PreviousValue,
                entry.NewValue,
                new UserDto(entry.ActorId, entry.ActorName),
                entry.OccurredAt))
            .ToList());
}
