namespace ExpertosSeguridad.Application.Exceptions;

/// <summary>
/// La operación crearía algo que ya existe; hoy, una cuenta con un correo ya registrado.
/// Corresponde a HTTP 409. Puede indicar el campo causante para que el frontend muestre el
/// mensaje junto al campo que lo provocó.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message, string? field = null) : base(message)
    {
        Field = field;
    }

    public string? Field { get; }
}
