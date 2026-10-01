using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Domain.Exceptions;

namespace ExpertosSeguridad.Domain.Policies;

/// <summary>
/// Quién puede gestionar cuentas. Separada de <see cref="RequestAccessPolicy"/> porque gobierna
/// otro agregado y cambia por otras razones. Las reglas sobre lo que un administrador puede hacer
/// con una cuenta en particular (no con la suya) viven en <see cref="Entities.User"/>, donde se
/// conoce la identidad del destino.
/// </summary>
public static class UserAdministrationPolicy
{
    public static bool CanManageUsers(UserRole role) => role == UserRole.Admin;

    public static void EnsureCanManageUsers(UserRole role)
    {
        if (!CanManageUsers(role))
        {
            throw new ForbiddenOperationException("Solo un administrador puede gestionar usuarios y roles.");
        }
    }
}
