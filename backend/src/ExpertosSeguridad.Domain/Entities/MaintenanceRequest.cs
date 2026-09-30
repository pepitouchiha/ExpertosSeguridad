using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Domain.Exceptions;
using ExpertosSeguridad.Domain.Policies;
using ExpertosSeguridad.Domain.ValueObjects;

namespace ExpertosSeguridad.Domain.Entities;

/// <summary>
/// Aggregate root of the maintenance request lifecycle. Every state mutation goes through
/// a behaviour method that validates the rule and appends the matching history entry, so
/// the invariants hold regardless of which caller (API, test, future job) drives the model.
/// </summary>
public sealed class MaintenanceRequest
{
    public const int TitleMinLength = 5;
    public const int TitleMaxLength = 120;
    public const int DescriptionMinLength = 10;
    public const int DescriptionMaxLength = 2000;

    private readonly List<RequestHistoryEntry> _history = new();

    private MaintenanceRequest()
    {
        Title = string.Empty;
        Description = string.Empty;
        RequesterName = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public RequestCategory Category { get; private set; }

    public RequestPriority Priority { get; private set; }

    public RequestStatus Status { get; private set; }

    public Guid RequesterId { get; private set; }

    public string RequesterName { get; private set; }

    public Guid? ResponsibleId { get; private set; }

    public string? ResponsibleName { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<RequestHistoryEntry> History => _history.AsReadOnly();

    public IReadOnlyList<RequestStatus> AllowedNextStatuses => RequestStatusTransitionPolicy.AllowedFrom(Status);

    /// <summary>
    /// Factory enforcing the creation invariants. The initial status is always
    /// <see cref="RequestStatus.Pending"/> and the timestamp comes from the caller
    /// (the API supplies the server clock), never from the HTTP payload.
    /// </summary>
    public static MaintenanceRequest Create(
        string title,
        string description,
        RequestCategory category,
        RequestPriority priority,
        Actor requester,
        DateTimeOffset createdAt)
    {
        var normalisedTitle = NormaliseTitle(title);
        var normalisedDescription = NormaliseDescription(description);
        EnsureDefinedEnum(category, nameof(category));
        EnsureDefinedEnum(priority, nameof(priority));

        var request = new MaintenanceRequest
        {
            Id = Guid.NewGuid(),
            Title = normalisedTitle,
            Description = normalisedDescription,
            Category = category,
            Priority = priority,
            Status = RequestStatus.Pending,
            RequesterId = requester.Id,
            RequesterName = requester.Name,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        request._history.Add(RequestHistoryEntry.ForCreation(request.Id, requester, createdAt));

        return request;
    }

    /// <summary>
    /// Applies a lifecycle transition. Rejects anything the policy does not contemplate,
    /// including a no-op change to the current status.
    /// </summary>
    public void ChangeStatus(RequestStatus newStatus, Actor actor, DateTimeOffset occurredAt)
    {
        EnsureDefinedEnum(newStatus, nameof(newStatus));

        if (!RequestStatusTransitionPolicy.IsAllowed(Status, newStatus))
        {
            throw new InvalidStatusTransitionException(Status, newStatus);
        }

        var previousStatus = Status;
        Status = newStatus;
        UpdatedAt = occurredAt;

        _history.Add(RequestHistoryEntry.ForStatusChange(Id, previousStatus, newStatus, actor, occurredAt));
    }

    /// <summary>
    /// Assigns or replaces the responsible. Passing <c>null</c> clears the assignment.
    /// </summary>
    public void AssignResponsible(Actor? responsible, Actor actor, DateTimeOffset occurredAt)
    {
        if (RequestStatusTransitionPolicy.IsTerminal(Status))
        {
            throw new DomainValidationException(
                nameof(ResponsibleId),
                $"No se puede modificar el responsable de una solicitud en estado '{Status}'.");
        }

        if (ResponsibleId == responsible?.Id)
        {
            throw new DomainValidationException(
                nameof(ResponsibleId),
                "El responsable indicado ya está asignado a la solicitud.");
        }

        var previous = ResponsibleId.HasValue
            ? new Actor(ResponsibleId.Value, ResponsibleName!)
            : null;

        ResponsibleId = responsible?.Id;
        ResponsibleName = responsible?.Name;
        UpdatedAt = occurredAt;

        _history.Add(RequestHistoryEntry.ForResponsibleChange(Id, previous, responsible, actor, occurredAt));
    }

    private static string NormaliseTitle(string title)
    {
        var value = title?.Trim() ?? string.Empty;

        if (value.Length is < TitleMinLength or > TitleMaxLength)
        {
            throw new DomainValidationException(
                nameof(Title),
                $"El título debe tener entre {TitleMinLength} y {TitleMaxLength} caracteres.");
        }

        return value;
    }

    private static string NormaliseDescription(string description)
    {
        var value = description?.Trim() ?? string.Empty;

        if (value.Length is < DescriptionMinLength or > DescriptionMaxLength)
        {
            throw new DomainValidationException(
                nameof(Description),
                $"La descripción debe tener entre {DescriptionMinLength} y {DescriptionMaxLength} caracteres.");
        }

        return value;
    }

    private static void EnsureDefinedEnum<TEnum>(TEnum value, string field) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new DomainValidationException(field, $"El valor '{value}' no es válido para {typeof(TEnum).Name}.");
        }
    }
}
