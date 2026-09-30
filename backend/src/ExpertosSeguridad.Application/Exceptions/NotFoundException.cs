namespace ExpertosSeguridad.Application.Exceptions;

/// <summary>
/// Raised when a use case targets an entity that does not exist. Maps to HTTP 404.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string resource, Guid id)
        : base($"No se encontró {resource} con identificador '{id}'.")
    {
        Resource = resource;
        ResourceId = id;
    }

    public string Resource { get; }

    public Guid ResourceId { get; }
}
