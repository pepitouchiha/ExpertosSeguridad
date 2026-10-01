namespace ExpertosSeguridad.Application.Exceptions;

/// <summary>
/// Se lanza cuando un caso de uso apunta a una entidad que no existe. Corresponde a HTTP 404.
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
