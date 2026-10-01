namespace ExpertosSeguridad.Application.Exceptions;

/// <summary>
/// Se lanza cuando un login no coincide. El mensaje es el mismo a propósito tanto si el correo no
/// existe como si la contraseña es incorrecta, para que el endpoint no sirva para averiguar qué
/// cuentas existen.
/// </summary>
public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("El correo o la contraseña no son correctos.")
    {
    }
}
