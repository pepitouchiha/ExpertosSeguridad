using ExpertosSeguridad.Domain.ValueObjects;

namespace ExpertosSeguridad.Application.Abstractions;

/// <summary>
/// Supplies the user performing the current operation. The API resolves it from a request
/// header against the test-user catalogue; a real deployment would resolve it from the
/// authenticated principal without changing any use case.
/// </summary>
public interface ICurrentActorProvider
{
    Actor GetCurrentActor();
}
