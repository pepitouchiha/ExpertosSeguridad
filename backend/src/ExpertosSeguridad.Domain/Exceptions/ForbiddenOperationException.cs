namespace ExpertosSeguridad.Domain.Exceptions;

/// <summary>
/// Quien llama está autenticado, pero su rol no permite la operación. Distinta de
/// <see cref="DomainValidationException"/> a propósito: el cuerpo está bien, el actor no, así
/// que la API responde 403 en lugar de 400.
/// </summary>
public sealed class ForbiddenOperationException : DomainException
{
    public ForbiddenOperationException(string message) : base(message)
    {
    }
}
