namespace ExpertosSeguridad.Application.Abstractions;

/// <summary>
/// Commits every pending change as a single database transaction. This is what makes
/// "a status change is never persisted without its history entry" a structural guarantee
/// instead of a convention.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
