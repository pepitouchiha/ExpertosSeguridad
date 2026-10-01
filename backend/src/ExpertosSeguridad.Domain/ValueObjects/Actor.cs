using ExpertosSeguridad.Domain.Exceptions;

namespace ExpertosSeguridad.Domain.ValueObjects;

/// <summary>
/// Identifica a una persona que participa en una solicitud: el solicitante, el responsable o
/// quien ejecuta una operación. Es un objeto de valor con una copia del nombre, no una referencia
/// al usuario: el historial debe seguir mostrando el nombre que tenía la persona cuando ocurrió
/// el evento, aunque después cambie.
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
