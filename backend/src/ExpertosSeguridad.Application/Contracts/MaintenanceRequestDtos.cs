using ExpertosSeguridad.Domain.Enums;

namespace ExpertosSeguridad.Application.Contracts;

/// <summary>
/// Cuerpo de creación. No trae solicitante: el solicitante es el llamante autenticado, así que
/// un cliente no puede abrir una solicitud a nombre de otra persona. Es el mismo razonamiento que
/// deja el estado inicial y la fecha de creación fuera del contrato.
/// </summary>
public sealed record CreateMaintenanceRequestCommand(
    string Title,
    string Description,
    RequestCategory Category,
    RequestPriority Priority);

public sealed record ChangeStatusCommand(RequestStatus NewStatus);

/// <summary>
/// Cierra la solicitud con la respuesta del responsable al solicitante. No hay campo para quién
/// responde: es el usuario autenticado, que tiene que ser el responsable.
/// </summary>
public sealed record ResolveRequestCommand(string Title, string Description);

public sealed record ResolutionDto(string Title, string Description, UserDto RespondedBy, DateTimeOffset RespondedAt);

/// <summary>
/// Anulable solo para que un valor ausente reciba un mensaje claro en vez de un Guid vacío: el
/// responsable se puede reemplazar, pero nunca quitar.
/// </summary>
public sealed record AssignResponsibleCommand(Guid? ResponsibleId);

/// <summary>
/// Lo que quien consulta puede hacer con esta solicitud en este momento, calculado en el backend
/// con las mismas políticas que validan cada operación. La interfaz dibuja los botones a partir
/// de aquí y nunca repite quién puede hacer qué.
/// </summary>
public sealed record RequestActionsDto(bool CanAssign, IReadOnlyList<RequestStatus> StatusTransitions);

public sealed record UserDto(Guid Id, string Name);

public sealed record MaintenanceRequestListItemDto(
    Guid Id,
    int Number,
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
    int Number,
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
    RequestActionsDto AvailableActions,
    ResolutionDto? Resolution,
    IReadOnlyList<HistoryEntryDto> History);

public sealed record RequestSummaryDto(
    int Total,
    int Pending,
    int InProgress,
    int OnHold,
    int Resolved,
    int Cancelled);
