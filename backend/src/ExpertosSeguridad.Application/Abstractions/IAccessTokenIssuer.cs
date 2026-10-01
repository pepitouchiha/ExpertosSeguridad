using ExpertosSeguridad.Domain.Entities;

namespace ExpertosSeguridad.Application.Abstractions;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

/// <summary>
/// Emite la credencial que el cliente presenta en las llamadas siguientes. Está detrás de una
/// interfaz para que el caso de uso de login no sepa que está generando un JWT: el formato es una
/// decisión de infraestructura y las pruebas no necesitan una clave de firma para ejercitar el flujo.
/// </summary>
public interface IAccessTokenIssuer
{
    AccessToken Issue(User user);
}
