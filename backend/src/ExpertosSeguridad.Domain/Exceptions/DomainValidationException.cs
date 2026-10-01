namespace ExpertosSeguridad.Domain.Exceptions;

/// <summary>
/// Se lanza cuando se viola una invariante del modelo (campo obligatorio, longitud, rango).
/// Corresponde a HTTP 400.
/// </summary>
public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string field, string message) : base(message)
    {
        Field = field;
    }

    public string Field { get; }
}
