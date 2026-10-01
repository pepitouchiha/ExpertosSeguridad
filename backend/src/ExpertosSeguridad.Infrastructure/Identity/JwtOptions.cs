using Microsoft.Extensions.Configuration;

namespace ExpertosSeguridad.Infrastructure.Identity;

/// <summary>
/// Configuración del token, enlazada desde la configuración (variables de entorno en docker
/// compose). La clave de firma nunca se versiona: <c>.env.example</c> trae un valor de desarrollo
/// claramente rotulado y un despliegue real inyecta el suyo.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Longitud mínima exigida al arrancar: una clave corta debilita HMAC-SHA256.</summary>
    public const int MinimumKeyLength = 32;

    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "expertos-seguridad-api";

    public string Audience { get; set; } = "expertos-seguridad-web";

    public int LifetimeMinutes { get; set; } = 480;

    /// <summary>
    /// Enlaza y valida la sección en un solo paso. Tanto el emisor de tokens como la validación
    /// Bearer del host leen la configuración por aquí, así que emitir y validar nunca se desalinean, y
    /// un despliegue mal configurado se detiene al arrancar con un mensaje legible en lugar de fallar
    /// en el primer login.
    /// </summary>
    public static JwtOptions FromConfiguration(IConfiguration configuration)
    {
        var options = configuration.GetSection(SectionName).Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(options.SigningKey) || options.SigningKey.Trim().Length < MinimumKeyLength)
        {
            throw new InvalidOperationException(
                $"Falta la clave de firma de los tokens o es demasiado corta. Defina Jwt__SigningKey como " +
                $"variable de entorno con al menos {MinimumKeyLength} caracteres.");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer) || string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("Jwt__Issuer y Jwt__Audience son obligatorios.");
        }

        if (options.LifetimeMinutes <= 0)
        {
            throw new InvalidOperationException("Jwt__LifetimeMinutes debe ser mayor que cero.");
        }

        return options;
    }
}
