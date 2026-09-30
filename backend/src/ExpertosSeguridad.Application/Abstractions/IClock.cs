namespace ExpertosSeguridad.Application.Abstractions;

/// <summary>
/// Time is an external dependency: abstracting it keeps use cases deterministic in tests
/// and guarantees creation timestamps come from the server, never from the client payload.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
