using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Domain.ValueObjects;

namespace ExpertosSeguridad.Application.Abstractions;

/// <summary>
/// El llamante autenticado de la operación actual, reducido a lo que necesitan los casos de uso:
/// una identidad, un nombre visible y un rol. Nada aquí menciona JWT, claims ni HTTP, así que los
/// servicios se pueden probar con una instancia simple.
/// </summary>
public sealed record CurrentUser(Guid Id, string Name, UserRole Role)
{
    public Actor ToActor() => new(Id, Name);
}

/// <summary>
/// Entrega el usuario autenticado que ejecuta la operación actual. La API lo obtiene de los
/// claims del token Bearer; cambiar de proveedor de identidad solo reemplaza este adaptador.
/// </summary>
public interface ICurrentUserProvider
{
    CurrentUser GetCurrentUser();
}
