using System.Security.Cryptography;
using ExpertosSeguridad.Application.Abstractions;

namespace ExpertosSeguridad.Infrastructure.Identity;

/// <summary>
/// PBKDF2-HMAC-SHA256 sobre el primitivo del BCL. No es criptografía casera: la derivación de la
/// clave es <see cref="Rfc2898DeriveBytes"/>, un KDF estándar, y la clase solo se ocupa de la
/// higiene alrededor: una sal aleatoria nueva por contraseña, el número de iteraciones guardado
/// junto al hash para poder subirlo después sin invalidar las filas existentes, y una comparación
/// en tiempo constante.
///
/// Se prefiere a incorporar ASP.NET Core Identity porque el enunciado descarta un sistema de
/// identidad completo: una dependencia menos a cambio de unas treinta líneas fáciles de explicar.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Derive(password, salt, Iterations);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string? storedHash)
    {
        if (!TryParse(storedHash, out var iterations, out var salt, out var expectedKey))
        {
            // No existe el usuario, o la fila no se puede leer. Se deriva de todos modos para que este
            // camino cueste lo mismo que una verificación real y no se pueda distinguir por el tiempo.
            Derive(password, new byte[SaltSize], Iterations);
            return false;
        }

        var actualKey = Derive(password, salt, iterations);

        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }

    private static byte[] Derive(string password, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(password ?? string.Empty, salt, iterations, Algorithm, KeySize);

    private static bool TryParse(string? storedHash, out int iterations, out byte[] salt, out byte[] key)
    {
        iterations = 0;
        salt = Array.Empty<byte>();
        key = Array.Empty<byte>();

        if (string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var parts = storedHash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out iterations) || iterations <= 0)
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[1]);
            key = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        return salt.Length > 0 && key.Length > 0;
    }
}
