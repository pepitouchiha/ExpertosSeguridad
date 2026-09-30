using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Domain.ValueObjects;

namespace ExpertosSeguridad.Infrastructure.Identity;

/// <summary>
/// Fixed catalogue of test users. The brief rules out real authentication, so identities are
/// constants instead of database rows or an identity provider. Documented as a simplification:
/// swapping this for a real directory only requires another <see cref="IUserDirectory"/>.
/// </summary>
public sealed class InMemoryUserDirectory : IUserDirectory
{
    public static readonly Actor AnaTorres = new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Ana Torres");
    public static readonly Actor CarlosRuiz = new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Carlos Ruiz");
    public static readonly Actor DianaLopez = new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Diana López");
    public static readonly Actor EstebanMora = new(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Esteban Mora");

    private static readonly IReadOnlyList<Actor> Users = new[] { AnaTorres, CarlosRuiz, DianaLopez, EstebanMora };

    private static readonly IReadOnlyDictionary<Guid, Actor> UsersById = Users.ToDictionary(user => user.Id);

    public IReadOnlyList<Actor> GetAll() => Users;

    public Actor? Find(Guid id) => UsersById.TryGetValue(id, out var user) ? user : null;
}
