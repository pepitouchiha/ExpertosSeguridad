using ExpertosSeguridad.Domain.Enums;

namespace ExpertosSeguridad.Application.Contracts;

/// <summary>Una cuenta tal como la lista el panel de administración. Nunca incluye la credencial.</summary>
public sealed record UserSummaryDto(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record ChangeUserRoleCommand(UserRole Role);

public sealed record ChangeUserStatusCommand(bool IsActive);

/// <summary>Filtrado y paginación en el servidor para el panel de administración.</summary>
public sealed record UserQuery
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;

    public UserRole? Role { get; init; }

    public bool? IsActive { get; init; }

    /// <summary>Coincide con el nombre o con el correo.</summary>
    public string? Search { get; init; }

    /// <summary>Los mismos límites que el listado de solicitudes, por la misma razón.</summary>
    public UserQuery Normalise() => this with
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
