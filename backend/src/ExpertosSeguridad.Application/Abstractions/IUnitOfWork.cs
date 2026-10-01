namespace ExpertosSeguridad.Application.Abstractions;

/// <summary>
/// Confirma todos los cambios pendientes en una sola transacción de base de datos. Es lo que
/// convierte «un cambio de estado nunca se guarda sin su entrada de historial» en una garantía
/// estructural en lugar de una convención.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
