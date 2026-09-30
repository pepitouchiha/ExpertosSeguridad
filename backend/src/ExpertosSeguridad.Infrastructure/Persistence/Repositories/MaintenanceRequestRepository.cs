using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Domain.Entities;
using ExpertosSeguridad.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpertosSeguridad.Infrastructure.Persistence.Repositories;

public sealed class MaintenanceRequestRepository : IMaintenanceRequestRepository
{
    private readonly MaintenanceDbContext _context;

    public MaintenanceRequestRepository(MaintenanceDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(MaintenanceRequest request, CancellationToken cancellationToken = default) =>
        await _context.MaintenanceRequests.AddAsync(request, cancellationToken);

    public Task<MaintenanceRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.MaintenanceRequests.FirstOrDefaultAsync(request => request.Id == id, cancellationToken);

    public Task<MaintenanceRequest?> GetByIdWithHistoryAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.MaintenanceRequests
            .Include(request => request.History)
            .FirstOrDefaultAsync(request => request.Id == id, cancellationToken);

    public async Task<PagedResult<MaintenanceRequest>> SearchAsync(
        MaintenanceRequestQuery query,
        CancellationToken cancellationToken = default)
    {
        // Every filter, the search and the sort are translated to SQL: the browser never
        // receives more rows than the requested page.
        var source = _context.MaintenanceRequests.AsNoTracking();

        if (query.Status.HasValue)
        {
            source = source.Where(request => request.Status == query.Status.Value);
        }

        if (query.Priority.HasValue)
        {
            source = source.Where(request => request.Priority == query.Priority.Value);
        }

        if (query.Category.HasValue)
        {
            source = source.Where(request => request.Category == query.Category.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLikePattern(query.Search)}%";
            source = source.Where(request => EF.Functions.ILike(request.Title, pattern, "\\"));
        }

        var totalItems = await source.CountAsync(cancellationToken);

        source = query.SortByCreatedAt == SortDirection.Asc
            ? source.OrderBy(request => request.CreatedAt).ThenBy(request => request.Id)
            : source.OrderByDescending(request => request.CreatedAt).ThenByDescending(request => request.Id);

        var items = await source
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<MaintenanceRequest>(items, query.Page, query.PageSize, totalItems);
    }

    public async Task<IReadOnlyDictionary<RequestStatus, int>> CountByStatusAsync(
        CancellationToken cancellationToken = default)
    {
        // Aggregated by the database so the dashboard reflects persisted data without
        // pulling every row into memory.
        var counts = await _context.MaintenanceRequests
            .AsNoTracking()
            .GroupBy(request => request.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(entry => entry.Status, entry => entry.Count);
    }

    /// <summary>
    /// Neutralises LIKE wildcards typed by the user so a search for "50%" is a literal search.
    /// </summary>
    private static string EscapeLikePattern(string term) => term
        .Replace("\\", "\\\\")
        .Replace("%", "\\%")
        .Replace("_", "\\_");
}
