namespace ExpertosSeguridad.Domain.Exceptions;

/// <summary>
/// Raised when an invariant of the model is violated (mandatory field, length, range).
/// Maps to HTTP 400.
/// </summary>
public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string field, string message) : base(message)
    {
        Field = field;
    }

    public string Field { get; }
}
