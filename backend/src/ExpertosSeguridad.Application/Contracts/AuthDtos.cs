using ExpertosSeguridad.Domain.Enums;

namespace ExpertosSeguridad.Application.Contracts;

public sealed record LoginCommand(string Email, string Password);

/// <summary>
/// Registro público. No tiene campo de rol, y no por olvido: el enlazador ignora una propiedad
/// JSON desconocida, así que un cliente que envía <c>"role": "Admin"</c> igual obtiene una cuenta
/// de solicitante. La promoción solo ocurre desde el panel de administración.
/// </summary>
public sealed record RegisterCommand(string FullName, string Email, string Password);

/// <summary>El usuario de la sesión, tal como lo necesita el frontend para decidir qué mostrar.</summary>
public sealed record AuthenticatedUserDto(Guid Id, string Name, string Email, UserRole Role);

/// <summary>
/// Resultado del login. El token va en el cuerpo y no en una cookie porque la API no guarda
/// estado y se consume desde otro origen; el frontend decide cómo almacenarlo.
/// </summary>
public sealed record AuthResultDto(string AccessToken, DateTimeOffset ExpiresAt, AuthenticatedUserDto User);
