using ExpertosSeguridad.Domain.Enums;

namespace ExpertosSeguridad.Application.Contracts;

public sealed record CreateMaintenanceRequestCommand(
    string Title,
    string Description,
    RequestCategory Category,
    RequestPriority Priority,
    Guid RequesterId);

public sealed record ChangeStatusCommand(RequestStatus NewStatus);

public sealed record AssignResponsibleCommand(Guid? ResponsibleId);

public sealed record UserDto(Guid Id, string Name);

public sealed record MaintenanceRequestListItemDto(
    Guid Id,
    string Title,
    RequestCategory Category,
    RequestPriority Priority,
    RequestStatus Status,
    UserDto? Responsible,
    DateTimeOffset CreatedAt);

public sealed record HistoryEntryDto(
    Guid Id,
    HistoryEventType EventType,
    string? PreviousValue,
    string? NewValue,
    UserDto Actor,
    DateTimeOffset OccurredAt);

public sealed record MaintenanceRequestDetailDto(
    Guid Id,
    string Title,
    string Description,
    RequestCategory Category,
    RequestPriority Priority,
    RequestStatus Status,
    UserDto Requester,
    UserDto? Responsible,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<RequestStatus> AllowedNextStatuses,
    IReadOnlyList<HistoryEntryDto> History);

public sealed record RequestSummaryDto(
    int Total,
    int Pending,
    int InProgress,
    int OnHold,
    int Resolved,
    int Cancelled);
