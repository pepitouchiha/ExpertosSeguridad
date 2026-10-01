using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Domain.Entities;
using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Domain.Policies;
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
        // Todos los filtros, la búsqueda y el orden se traducen a SQL: el navegador nunca recibe más
        // filas que la página pedida.
        var source = _context.MaintenanceRequests.AsNoTracking();

        // Primero el alcance por dueño: para un solicitante, todos los demás filtros acotan sus propias
        // filas, nunca la tabla entera.
        if (query.RequesterId.HasValue)
        {
            source = source.Where(request => request.RequesterId == query.RequesterId.Value);
        }

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
            var pattern = $"%{LikePattern.Escape(query.Search)}%";
            source = source.Where(request => EF.Functions.ILike(request.Title, pattern, "\\"));
        }

        var totalItems = await source.CountAsync(cancellationToken);

        // Cuando hay a quién priorizar: primero todas las solicitudes asignadas a esa persona y, entre
        // ellas, las que siguen en curso antes que las cerradas; después, todo lo demás por fecha.
        // Ordenar por estos booleanos en SQL lo mantiene en una sola consulta paginada: «primero lo mío»
        // se respeta entre páginas, cosa que no pasaría si se ordenara en memoria la página actual. Las
        // páginas conservan su tamaño y los demás filtros se aplican igual a ambos grupos.
        if (query.PrioritiseResponsibleId is { } responsibleId)
        {
            var underAttention = RequestStatusTransitionPolicy.UnderAttention.ToArray();
            var mineFirst = source
                .OrderByDescending(request => request.ResponsibleId == responsibleId)
                .ThenByDescending(request =>
                    request.ResponsibleId == responsibleId && underAttention.Contains(request.Status));

            source = query.SortByCreatedAt == SortDirection.Asc
                ? mineFirst.ThenBy(request => request.CreatedAt).ThenBy(request => request.Id)
                : mineFirst.ThenByDescending(request => request.CreatedAt).ThenByDescending(request => request.Id);
        }
        else
        {
            source = query.SortByCreatedAt == SortDirection.Asc
                ? source.OrderBy(request => request.CreatedAt).ThenBy(request => request.Id)
                : source.OrderByDescending(request => request.CreatedAt).ThenByDescending(request => request.Id);
        }

        var items = await source
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<MaintenanceRequest>(items, query.Page, query.PageSize, totalItems);
    }

    public async Task<IReadOnlyDictionary<RequestStatus, int>> CountByStatusAsync(
        Guid? requesterId = null,
        CancellationToken cancellationToken = default)
    {
        var source = _context.MaintenanceRequests.AsNoTracking();

        if (requesterId.HasValue)
        {
            source = source.Where(request => request.RequesterId == requesterId.Value);
        }

        // La base de datos hace la agregación, así que el panel refleja los datos persistidos sin traer
        // todas las filas a memoria.
        var counts = await source
            .GroupBy(request => request.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(entry => entry.Status, entry => entry.Count);
    }
}
