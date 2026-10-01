using System.Security.Claims;
using ExpertosSeguridad.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace ExpertosSeguridad.Api.Identity;

/// <summary>
/// Se ejecuta después de validar la firma y la vigencia del token, y lo contrasta con la cuenta
/// tal como está <i>ahora</i>. Un JWT es una foto: sin este paso, una cuenta que el administrador
/// desactivó, o alguien del personal degradado a solicitante, seguiría actuando con los permisos
/// anteriores hasta que el token expirara, hasta ocho horas.
///
/// El costo es una consulta por clave primaria en cada request autenticada: el precio de que los
/// cambios de rol y las desactivaciones tengan efecto inmediato. Cuando la cuenta ya no coincide
/// con el token, la request se rechaza con 401 y el frontend lleva al usuario a iniciar sesión
/// otra vez, así que la interfaz y la API nunca discrepan sobre lo que el usuario puede hacer.
/// </summary>
public static class ActiveSessionValidator
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        var userIdClaim = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleClaim = principal?.FindFirstValue(ClaimTypes.Role);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            context.Fail("El token no identifica a un usuario.");
            return;
        }

        var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
        var user = await users.FindByIdAsync(userId, context.HttpContext.RequestAborted);

        if (user is null || !user.IsActive)
        {
            context.Fail("La cuenta ya no está habilitada.");
            return;
        }

        if (!string.Equals(user.Role.ToString(), roleClaim, StringComparison.Ordinal))
        {
            context.Fail("El rol de la cuenta cambió desde que se emitió el token.");
        }
    }
}
