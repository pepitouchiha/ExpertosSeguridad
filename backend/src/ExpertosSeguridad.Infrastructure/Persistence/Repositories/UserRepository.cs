using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Domain.Entities;
using ExpertosSeguridad.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpertosSeguridad.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly MaintenanceDbContext _context;

    public UserRepository(MaintenanceDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// La columna ya se guarda en minúsculas, así que esta es una comparación de igualdad respaldada
    /// por el índice único, y no una función sobre la columna que no podría usarlo.
    /// </summary>
    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _context.Users.AnyAsync(user => user.Email == email, cancellationToken);

    public async Task<IReadOnlyList<User>> GetByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default) =>
        await _context.Users
            .AsNoTracking()
            .Where(user => user.Role == role && user.IsActive)
            .OrderBy(user => user.FullName)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<User>> SearchAsync(UserQuery query, CancellationToken cancellationToken = default)
    {
        var source = _context.Users.AsNoTracking();

        if (query.Role.HasValue)
        {
            source = source.Where(user => user.Role == query.Role.Value);
        }

        if (query.IsActive.HasValue)
        {
            source = source.Where(user => user.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{LikePattern.Escape(query.Search)}%";
            source = source.Where(user =>
                EF.Functions.ILike(user.FullName, pattern, "\\") ||
                EF.Functions.ILike(user.Email, pattern, "\\"));
        }

        var totalItems = await source.CountAsync(cancellationToken);

        var items = await source
            .OrderBy(user => user.FullName)
            .ThenBy(user => user.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<User>(items, query.Page, query.PageSize, totalItems);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await _context.Users.AddAsync(user, cancellationToken);
}
