using ExpertosSeguridad.Domain.Exceptions;

namespace ExpertosSeguridad.Domain.ValueObjects;

/// <summary>
/// Identifies a person taking part in a request: requester, responsible or the actor
/// performing an operation. Modelled as an owned value object because the test brief
/// scopes users to a fixed catalogue rather than a managed identity system.
/// </summary>
public sealed record Actor
{
    public const int MaxNameLength = 120;

    public Guid Id { get; }

    public string Name { get; }

    public Actor(Guid id, string name)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("actor.id", "El identificador del actor es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("actor.name", "El nombre del actor es obligatorio.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > MaxNameLength)
        {
            throw new DomainValidationException(
                "actor.name",
                $"El nombre del actor no puede superar {MaxNameLength} caracteres.");
        }

        Id = id;
        Name = trimmed;
    }
}
