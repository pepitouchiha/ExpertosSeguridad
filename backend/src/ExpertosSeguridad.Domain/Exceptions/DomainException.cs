namespace ExpertosSeguridad.Domain.Exceptions;

/// <summary>
/// Tipo base de toda violación de reglas que lanza el modelo de dominio. La capa de API las
/// traduce a respuestas HTTP, así que las preocupaciones de infraestructura nunca entran al dominio.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}
