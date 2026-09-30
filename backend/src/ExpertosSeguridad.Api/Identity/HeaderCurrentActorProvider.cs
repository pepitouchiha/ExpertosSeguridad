using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Domain.ValueObjects;
using FluentValidation;
using FluentValidation.Results;

namespace ExpertosSeguridad.Api.Identity;

/// <summary>
/// Resolves who is performing the operation from the <c>X-Actor-Id</c> header against the
/// test-user catalogue. This is the documented stand-in for authentication: with a real
/// identity provider only this class changes, because the use cases depend on the interface.
/// </summary>
public sealed class HeaderCurrentActorProvider : ICurrentActorProvider
{
    public const string HeaderName = "X-Actor-Id";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserDirectory _userDirectory;

    public HeaderCurrentActorProvider(IHttpContextAccessor httpContextAccessor, IUserDirectory userDirectory)
    {
        _httpContextAccessor = httpContextAccessor;
        _userDirectory = userDirectory;
    }

    public Actor GetCurrentActor()
    {
        var header = _httpContextAccessor.HttpContext?.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrWhiteSpace(header))
        {
            // No header means the default test user, so the API stays usable from Swagger.
            return _userDirectory.GetAll().First();
        }

        if (!Guid.TryParse(header, out var actorId))
        {
            throw Invalid($"El encabezado {HeaderName} debe ser un identificador válido.");
        }

        return _userDirectory.Find(actorId)
            ?? throw Invalid($"El usuario indicado en {HeaderName} no existe en el catálogo.");
    }

    private static ValidationException Invalid(string message) =>
        new(new[] { new ValidationFailure(HeaderName, message) });
}
