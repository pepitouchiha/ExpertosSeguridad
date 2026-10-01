namespace ExpertosSeguridad.Application.Abstractions;

/// <summary>
/// El tiempo es una dependencia externa: abstraerlo mantiene deterministas los casos de uso en
/// las pruebas y garantiza que las fechas de creación las pone el servidor, nunca el cliente.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
