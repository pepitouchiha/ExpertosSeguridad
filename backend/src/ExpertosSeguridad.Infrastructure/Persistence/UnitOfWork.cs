using ExpertosSeguridad.Application.Abstractions;

namespace ExpertosSeguridad.Infrastructure.Persistence;

/// <summary>
/// One commit per use case. The aggregate and its new history entry are tracked by the same
/// context, so a single SaveChanges writes both inside one database transaction: if the
/// history insert fails, the status or assignment change is rolled back with it.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly MaintenanceDbContext _context;

    public UnitOfWork(MaintenanceDbContext context)
    {
        _context = context;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
