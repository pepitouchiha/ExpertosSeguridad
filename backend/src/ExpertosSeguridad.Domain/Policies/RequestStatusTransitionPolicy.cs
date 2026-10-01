using ExpertosSeguridad.Domain.Enums;

namespace ExpertosSeguridad.Domain.Policies;

/// <summary>
/// Única fuente de verdad del ciclo de vida de la solicitud. Tener aquí la tabla de transiciones
/// (y no dentro de un controlador o un servicio) permite probar la regla sin infraestructura y
/// que la API la consulte, para que la interfaz nunca tenga que repetirla.
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

    /// <summary>
    /// Solicitudes en las que alguien está trabajando: iniciadas y sin cerrar. Las pendientes no
    /// están (nadie las tiene aún), ni las terminales (el trabajo ya acabó).
    /// </summary>
    public static readonly IReadOnlyList<RequestStatus> UnderAttention = new[]
    {
        RequestStatus.InProgress,
        RequestStatus.OnHold
    };

    public static IReadOnlyList<RequestStatus> AllowedFrom(RequestStatus current) =>
        AllowedTransitions.TryGetValue(current, out var allowed) ? allowed : Array.Empty<RequestStatus>();

    public static bool IsAllowed(RequestStatus from, RequestStatus to) => AllowedFrom(from).Contains(to);

    public static bool IsTerminal(RequestStatus status) => AllowedFrom(status).Count == 0;
}
