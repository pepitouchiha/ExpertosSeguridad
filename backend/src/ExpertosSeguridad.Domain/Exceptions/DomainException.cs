namespace ExpertosSeguridad.Domain.Exceptions;

/// <summary>
/// Base type for every rule violation raised by the domain model. The API layer maps
/// these to HTTP responses, so infrastructure concerns never leak into the domain.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}
