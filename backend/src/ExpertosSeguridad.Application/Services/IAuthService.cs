using ExpertosSeguridad.Application.Contracts;

namespace ExpertosSeguridad.Application.Services;

public interface IAuthService
{
    Task<AuthResultDto> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default);

    /// <summary>Crea una cuenta de solicitante e inicia su sesión.</summary>
    Task<AuthResultDto> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken = default);

    /// <summary>Vuelve a leer el usuario de la sesión, para que un cliente con token pueda restaurarla.</summary>
    Task<AuthenticatedUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
