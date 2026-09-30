using ExpertosSeguridad.Domain.Enums;

namespace ExpertosSeguridad.Domain.Exceptions;

/// <summary>
/// Raised when a status change is not contemplated by the transition policy.
/// Maps to HTTP 409 because the request is well formed but conflicts with current state.
/// </summary>
public sealed class InvalidStatusTransitionException : DomainException
{
    public InvalidStatusTransitionException(RequestStatus from, RequestStatus to)
        : base($"No se permite la transición de '{from}' a '{to}'.")
    {
        From = from;
        To = to;
    }

    public RequestStatus From { get; }

    public RequestStatus To { get; }
}
