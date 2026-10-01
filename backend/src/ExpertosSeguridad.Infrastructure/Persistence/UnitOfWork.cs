using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExpertosSeguridad.Infrastructure.Persistence;

/// <summary>
/// Una confirmación por caso de uso. El agregado y su nueva entrada de historial los sigue el
/// mismo contexto, así que un único SaveChanges escribe ambos dentro de una transacción: si falla
/// la inserción del historial, el cambio de estado o de asignación se revierte con ella.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    /// <summary>Código de error de PostgreSQL para una violación de restricción única.</summary>
    private const string UniqueViolation = "23505";

    private readonly MaintenanceDbContext _context;

    public UnitOfWork(MaintenanceDbContext context)
    {
        _context = context;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: UniqueViolation })
        {
            // Una regla de unicidad que el caso de uso ya verifica igual puede perder una carrera.
            // Traducirla aquí mantiene la capa de aplicación libre de tipos de EF y Npgsql, y el cliente
            // recibe un 409 en lugar de un 500.
            throw new ConflictException("Ya existe un registro con esos datos.");
        }
    }
}
