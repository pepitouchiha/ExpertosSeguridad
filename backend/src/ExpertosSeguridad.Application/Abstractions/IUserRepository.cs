using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Domain.Entities;
using ExpertosSeguridad.Domain.Enums;

namespace ExpertosSeguridad.Application.Abstractions;

/// <summary>
/// Contrato de persistencia del catálogo de usuarios. Los usuarios son filas con un rol, una
/// credencial y un indicador de cuenta activa, y las tablas de solicitudes tienen claves
/// foráneas reales hacia ellos.
/// </summary>
public interface IUserRepository
{
    /// <summary>Búsqueda de solo lectura, para el inicio de sesión.</summary>
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Búsqueda de solo lectura, para consultas y para la verificación del token en cada request.</summary>
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Búsqueda con seguimiento de cambios, para los casos de uso que modifican al usuario y luego
    /// confirman con <see cref="IUnitOfWork"/>. Va aparte para que las lecturas nunca paguen el
    /// costo del seguimiento.
    /// </summary>
    Task<User?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default);

    Task<PagedResult<User>> SearchAsync(UserQuery query, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
