using ExpertosSeguridad.Domain.Enums;

namespace ExpertosSeguridad.Domain.Policies;

/// <summary>
/// Single source of truth for the request lifecycle. Keeping the transition table here
/// (instead of inside a controller or service) means the rule is testable without any
/// infrastructure and can be queried by the API so the UI never has to restate it.
/// </summary>
public static class RequestStatusTransitionPolicy
{
    private static readonly IReadOnlyDictionary<RequestStatus, IReadOnlyList<RequestStatus>> AllowedTransitions =
        new Dictionary<RequestStatus, IReadOnlyList<RequestStatus>>
        {
            [RequestStatus.Pending] = new[] { RequestStatus.InProgress, RequestStatus.Cancelled },
            [RequestStatus.InProgress] = new[] { RequestStatus.OnHold, RequestStatus.Resolved, RequestStatus.Cancelled },
            [RequestStatus.OnHold] = new[] { RequestStatus.InProgress, RequestStatus.Cancelled },
            [RequestStatus.Resolved] = Array.Empty<RequestStatus>(),
            [RequestStatus.Cancelled] = Array.Empty<RequestStatus>()
        };

    public static IReadOnlyList<RequestStatus> AllowedFrom(RequestStatus current) =>
        AllowedTransitions.TryGetValue(current, out var allowed) ? allowed : Array.Empty<RequestStatus>();

    public static bool IsAllowed(RequestStatus from, RequestStatus to) => AllowedFrom(from).Contains(to);

    public static bool IsTerminal(RequestStatus status) => AllowedFrom(status).Count == 0;
}
