using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Domain.Exceptions;

namespace ExpertosSeguridad.Domain.Policies;

/// <summary>
/// Única fuente de verdad sobre «quién puede hacer qué» con una solicitud, y la contraparte de
/// <see cref="RequestStatusTransitionPolicy"/>: aquella responde <i>si la solicitud puede
/// moverse</i>; esta, <i>si este actor puede moverla</i>.
///
/// Vive en el dominio porque es una regla de negocio, no un asunto de transporte: la API además
/// protege los endpoints con <c>[Authorize]</c>, pero eso solo detiene el tráfico anónimo. La
/// pertenencia («un solicitante ve sus propias solicitudes») no se puede expresar como un atributo
/// de ruta, así que la regla se declara una vez aquí y se aplica en los casos de uso, donde se
/// puede probar sin HTTP.
/// </summary>
public static class RequestAccessPolicy
{
    /// <summary>Solo los solicitantes abren solicitudes; el personal atiende las ya reportadas.</summary>
    public static bool CanCreateRequests(UserRole role) => role == UserRole.Requester;

    /// <summary>
    /// Cambiar el estado y asignar responsable son operaciones del personal. Los administradores
    /// quedan fuera a propósito: quien otorga roles no opera el proceso, así que ninguna cuenta puede
    /// darse un permiso y usarlo.
    /// </summary>
    public static bool CanManageLifecycle(UserRole role) => role == UserRole.Staff;

    /// <summary>
    /// El personal ve todas las solicitudes para atenderlas, y los administradores para supervisarlas;
    /// el listado de un solicitante se acota a sus propias filas.
    /// </summary>
    public static bool CanViewEveryRequest(UserRole role) => role is UserRole.Staff or UserRole.Admin;

    /// <summary>
    /// Transiciones que son parte de <i>hacer</i> el trabajo: iniciarlo, pausarlo, reanudarlo,
    /// terminarlo. Le corresponden solo al responsable asignado, y no tienen sentido sin él. Cancelar
    /// no está a propósito: es una decisión de coordinación que puede tomar cualquiera del personal,
    /// para que una solicitud nunca quede bloqueada porque su responsable no está disponible.
    /// </summary>
    private static readonly HashSet<RequestStatus> ExecutionStatuses = new()
    {
        RequestStatus.InProgress,
        RequestStatus.OnHold,
        RequestStatus.Resolved
    };

    public static bool IsReservedForResponsible(RequestStatus target) => ExecutionStatuses.Contains(target);

    /// <summary>
    /// Si quien llama puede llevar a este estado una solicitud con este responsable. Responde solo
    /// la pregunta sobre el actor; si el movimiento existe es asunto de la política de transiciones.
    /// </summary>
    public static bool CanChangeStatus(UserRole role, Guid userId, Guid? responsibleId, RequestStatus target) =>
        CanManageLifecycle(role) && (!IsReservedForResponsible(target) || responsibleId == userId);

    /// <summary>Un solicitante solo puede leer las solicitudes que abrió.</summary>
    public static bool CanViewRequest(UserRole role, Guid userId, Guid requesterId) =>
        CanViewEveryRequest(role) || userId == requesterId;

    public static void EnsureCanCreateRequests(UserRole role)
    {
        if (!CanCreateRequests(role))
        {
            throw new ForbiddenOperationException(
                "Solo los usuarios solicitantes pueden registrar solicitudes de mantenimiento.");
        }
    }

    public static void EnsureCanManageLifecycle(UserRole role)
    {
        if (!CanManageLifecycle(role))
        {
            throw new ForbiddenOperationException(
                "Solo el personal de la empresa puede asignar responsables y cambiar el estado de una solicitud.");
        }
    }
}
