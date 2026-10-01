using ExpertosSeguridad.Domain.Enums;

namespace ExpertosSeguridad.Application.Contracts;

public enum SortDirection
{
    Desc = 0,
    Asc = 1
}

/// <summary>
/// Parámetros de filtrado, búsqueda, orden y paginación del listado, aplicados en el servidor.
/// </summary>
public sealed record MaintenanceRequestQuery
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;

    public RequestStatus? Status { get; init; }

    public RequestPriority? Priority { get; init; }

    public RequestCategory? Category { get; init; }

    public string? Search { get; init; }

    public SortDirection SortByCreatedAt { get; init; } = SortDirection.Desc;

    /// <summary>
    /// Restringe el resultado a las filas de un solicitante. Nunca se enlaza desde la query string:
    /// el caso de uso lo fija a partir del usuario autenticado, así que un solicitante no puede
    /// ampliar su alcance editando la URL.
    /// </summary>
    public Guid? RequesterId { get; init; }

    /// <summary>
    /// Pone primero todas las solicitudes asignadas a este usuario —las que siguen en curso antes que
    /// las cerradas— por delante del orden por fecha elegido. Igual que <see cref="RequesterId"/>,
    /// nunca se enlaza desde la query string: el caso de uso lo fija para el personal a partir del
    /// usuario autenticado.
    /// </summary>
    public Guid? PrioritiseResponsibleId { get; init; }

    /// <summary>
    /// Acota los valores de paginación que envía el usuario, para que alguien malintencionado o
    /// descuidado no pueda pedir una página sin límite.
    /// </summary>
    public MaintenanceRequestQuery Normalise() => this with
    {
        Page = Page < 1 ? 1 : Page,
        PageSize = PageSize switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => PageSize
        },
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim()
    };
}
