using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Application.Exceptions;
using ExpertosSeguridad.Domain.Entities;
using FluentValidation;

namespace ExpertosSeguridad.Application.Services;

/// <summary>
/// Casos de uso de autenticación. Solo orquestan: la búsqueda en el catálogo, la verificación de
/// la credencial y la emisión del token viven cada una tras su propia abstracción, así que cambiar
/// el algoritmo de hashing o el formato del token solo toca infraestructura.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenIssuer _tokenIssuer;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IClock _clock;
    private readonly IValidator<LoginCommand> _loginValidator;
    private readonly IValidator<RegisterCommand> _registerValidator;

    public AuthService(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IAccessTokenIssuer tokenIssuer,
        ICurrentUserProvider currentUserProvider,
        IClock clock,
        IValidator<LoginCommand> loginValidator,
        IValidator<RegisterCommand> registerValidator)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
        _currentUserProvider = currentUserProvider;
        _clock = clock;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
    }

    public async Task<AuthResultDto> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        await _loginValidator.ValidateAndThrowAsync(command, cancellationToken);

        var email = (command.Email ?? string.Empty).Trim().ToLowerInvariant();
        var user = await _users.FindByEmailAsync(email, cancellationToken);

        // Un correo desconocido y una contraseña incorrecta producen el mismo fallo a propósito. La
        // verificación se ejecuta aunque no haya usuario (el hasher absorbe el nulo), para que el tiempo
        // de respuesta no revele cuál de los dos ocurrió.
        var passwordMatches = _passwordHasher.Verify(command.Password ?? string.Empty, user?.PasswordHash);

        if (user is null || !passwordMatches)
        {
            throw new InvalidCredentialsException();
        }

        // Solo después de que la contraseña coincidió: decir «desactivada» a quien demostró tener la
        // credencial no filtra nada, y le ahorra a un usuario legítimo adivinar por qué no puede entrar.
        user.EnsureCanSignIn();

        return Issue(user);
    }

    public async Task<AuthResultDto> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken = default)
    {
        await _registerValidator.ValidateAndThrowAsync(command, cancellationToken);

        var email = User.NormaliseEmail(command.Email);

        // Respuesta amable, por campo, para el caso común. Dos registros simultáneos con el mismo
        // correo pueden pasar esta verificación; el índice único atrapa al segundo y la unidad de
        // trabajo lo reporta como el mismo 409, así que la regla se cumple de cualquier forma.
        if (await _users.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new ConflictException("Ya existe una cuenta registrada con ese correo.", nameof(command.Email));
        }

        var user = User.Register(command.FullName, email, _passwordHasher.Hash(command.Password), _clock.UtcNow);

        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Con la sesión iniciada de inmediato: la cuenta se puede usar en cuanto existe.
        return Issue(user);
    }

    public async Task<AuthenticatedUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var current = _currentUserProvider.GetCurrentUser();

        var user = await _users.FindByIdAsync(current.Id, cancellationToken)
            ?? throw new NotFoundException("el usuario", current.Id);

        return ToDto(user);
    }

    private AuthResultDto Issue(User user)
    {
        var token = _tokenIssuer.Issue(user);

        return new AuthResultDto(token.Value, token.ExpiresAt, ToDto(user));
    }

    private static AuthenticatedUserDto ToDto(User user) => new(user.Id, user.FullName, user.Email, user.Role);
}
