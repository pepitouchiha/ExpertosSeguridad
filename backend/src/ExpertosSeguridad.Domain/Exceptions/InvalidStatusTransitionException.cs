using ExpertosSeguridad.Domain.Enums;

namespace ExpertosSeguridad.Domain.Exceptions;

/// <summary>
/// Se lanza cuando la política de transiciones no contempla un cambio de estado. Corresponde a
/// HTTP 409 porque la petición está bien formada pero entra en conflicto con el estado actual.
/// </summary>
public sealed class InvalidStatusTransitionException : DomainException
{
    public InvalidStatusTransitionException(RequestStatus from, RequestStatus to)
        : base($"No se permite la transición de '{from}' a '{to}'.")
    {
        From = from;
        To = to;
    }

    /// <summary>
    /// Para una transición que la tabla permite pero el estado actual de la solicitud no: iniciar una
    /// solicitud que no tiene responsable. Sigue siendo un conflicto de estado, sigue siendo un 409.
    /// </summary>
    public InvalidStatusTransitionException(RequestStatus from, RequestStatus to, string reason)
        : base(reason)
    {
        From = from;
        To = to;
    }

    public RequestStatus From { get; }

    public RequestStatus To { get; }
}
