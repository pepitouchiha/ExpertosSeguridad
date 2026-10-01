using System.Security.Claims;
using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Domain.Enums;

namespace ExpertosSeguridad.Api.Identity;

/// <summary>
/// Adapta el principal autenticado al <see cref="CurrentUser"/> que esperan los casos de uso. Es
/// la única clase de la solución que sabe que el actor de la operación viene de un token Bearer:
/// cambiar de proveedor de identidad reemplaza este archivo y ningún otro.
///
/// Reemplazó a un encabezado <c>X-Actor-Id</c> que cualquier llamante podía fijar a voluntad; por
/// eso ahora se puede confiar en el actor que queda registrado en el historial.
/// </summary>
public sealed class ClaimsCurrentUserProvider : ICurrentUserProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClaimsCurrentUserProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CurrentUser GetCurrentUser()
    {
        var principal = _httpContextAccessor.HttpContext?.User;

        if (principal?.Identity is null || !principal.Identity.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("La operación requiere un usuario autenticado.");
        }

        // Un token que pasó la validación pero trae un contenido ilegible es un error del emisor,
        // no del cliente, así que se reporta como tal en lugar de asumir valores por defecto.
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = principal.FindFirstValue(ClaimTypes.Name);
        var role = principal.FindFirstValue(ClaimTypes.Role);

        if (!Guid.TryParse(id, out var userId) ||
            string.IsNullOrWhiteSpace(name) ||
            !Enum.TryParse<UserRole>(role, ignoreCase: false, out var userRole))
        {
            throw new InvalidOperationException("El token no contiene la identidad esperada.");
        }

        return new CurrentUser(userId, name, userRole);
    }
}
