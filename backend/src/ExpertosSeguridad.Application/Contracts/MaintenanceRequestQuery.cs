using ExpertosSeguridad.Domain.Enums;

namespace ExpertosSeguridad.Application.Contracts;

public enum SortDirection
{
    Desc = 0,
    Asc = 1
}

/// <summary>
/// Server-side filtering, searching, sorting and paging parameters for the request list.
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
    /// Clamps user-supplied paging values so a hostile or careless caller cannot ask for
    /// an unbounded page.
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
