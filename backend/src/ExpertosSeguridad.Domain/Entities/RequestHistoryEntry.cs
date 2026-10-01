using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Domain.ValueObjects;

namespace ExpertosSeguridad.Domain.Entities;

/// <summary>
/// Registro de auditoría inmutable. Solo <see cref="MaintenanceRequest"/> crea instancias, así
/// que ningún cambio puede llegar a la base de datos sin su entrada de historial.
/// </summary>
public sealed class RequestHistoryEntry
{
    private RequestHistoryEntry()
    {
        ActorName = string.Empty;
    }

    private RequestHistoryEntry(
        Guid requestId,
        HistoryEventType eventType,
        string? previousValue,
        string? newValue,
        Actor actor,
        DateTimeOffset occurredAt)
    {
        Id = Guid.NewGuid();
        RequestId = requestId;
        EventType = eventType;
        PreviousValue = previousValue;
        NewValue = newValue;
        ActorId = actor.Id;
        ActorName = actor.Name;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }

    public Guid RequestId { get; private set; }

    public HistoryEventType EventType { get; private set; }

    public string? PreviousValue { get; private set; }

    public string? NewValue { get; private set; }

    public Guid ActorId { get; private set; }

    public string ActorName { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    internal static RequestHistoryEntry ForCreation(Guid requestId, Actor actor, DateTimeOffset occurredAt) =>
        new(requestId, HistoryEventType.Created, null, RequestStatus.Pending.ToString(), actor, occurredAt);

    internal static RequestHistoryEntry ForStatusChange(
        Guid requestId,
        RequestStatus previous,
        RequestStatus current,
        Actor actor,
        DateTimeOffset occurredAt) =>
        new(requestId, HistoryEventType.StatusChanged, previous.ToString(), current.ToString(), actor, occurredAt);

    internal static RequestHistoryEntry ForResponsibleChange(
        Guid requestId,
        Actor? previous,
        Actor? current,
        Actor actor,
        DateTimeOffset occurredAt) =>
        new(requestId, HistoryEventType.ResponsibleChanged, previous?.Name, current?.Name, actor, occurredAt);
}
