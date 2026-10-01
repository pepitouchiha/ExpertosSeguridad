namespace ExpertosSeguridad.Application.Abstractions;

/// <summary>
/// Hashing de credenciales, declarado aquí e implementado en infraestructura para que los casos
/// de uso nunca dependan de un algoritmo en particular. La verificación devuelve un bool en vez de
/// lanzar una excepción: «contraseña incorrecta» es un resultado esperado de un login, no uno
/// excepcional.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>
    /// Compara una contraseña con un hash guardado. Un hash nulo (usuario inexistente) debe hacer el
    /// mismo trabajo y devolver false, para que nadie pueda distinguir una cuenta inexistente de una
    /// contraseña incorrecta midiendo el tiempo de respuesta. Saber qué significa «el mismo trabajo»
    /// le corresponde a la implementación; por eso el caso nulo se resuelve aquí y no en quien llama.
    /// </summary>
    bool Verify(string password, string? storedHash);
}
