using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Domain.Exceptions;
using ExpertosSeguridad.Domain.ValueObjects;

namespace ExpertosSeguridad.Domain.Entities;

/// <summary>
/// Una persona que puede iniciar sesión. La entidad es dueña de las invariantes de identidad (un
/// nombre, un correo normalizado, un rol, si la cuenta está activa) pero, a propósito, no del
/// algoritmo de hashing: recibe una credencial ya procesada, así que el dominio queda libre de
/// infraestructura criptográfica y se puede probar sin ella.
/// </summary>
public sealed class User
{
    public const int NameMaxLength = 120;
    public const int EmailMaxLength = 160;

    private User()
    {
        FullName = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
    }

    public Guid Id { get; private set; }

    public string FullName { get; private set; }

    /// <summary>Se guarda en minúsculas, para que el índice único y la búsqueda del login coincidan.</summary>
    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public UserRole Role { get; private set; }

    /// <summary>
    /// Una cuenta desactivada no puede iniciar sesión, y sus tokens vigentes dejan de aceptarse. Nunca
    /// se borra: el historial de solicitudes la referencia y debe quedar intacto.
    /// </summary>
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Crea una cuenta con un rol explícito. Lo usan las pruebas; la entrada pública es
    /// <see cref="Register"/>, que no permite elegir rol.
    /// </summary>
    public static User Create(
        string fullName,
        string email,
        string passwordHash,
        UserRole role,
        DateTimeOffset createdAt)
    {
        EnsureDefinedRole(role);

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainValidationException(nameof(PasswordHash), "La credencial del usuario es obligatoria.");
        }

        return new User
        {
            Id = Guid.NewGuid(),
            FullName = NormaliseName(fullName),
            Email = NormaliseEmail(email),
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            CreatedAt = createdAt
        };
    }

    /// <summary>
    /// Registro de autoservicio. El rol no es un parámetro: quien se registra es solicitante, y solo
    /// un administrador puede convertirlo en otra cosa. Declararlo aquí, y no en el controlador o en
    /// el formulario, garantiza que ningún llamador futuro pueda registrar un administrador pasando
    /// el valor equivocado.
    /// </summary>
    public static User Register(string fullName, string email, string passwordHash, DateTimeOffset createdAt) =>
        Create(fullName, email, passwordHash, UserRole.Requester, createdAt);

    /// <summary>
    /// Cambia el rol en nombre de un administrador. Un administrador no puede cambiar su propio rol:
    /// además de evitar que se degrade por accidente, esto garantiza que el sistema nunca quede sin
    /// administrador: el último no puede quitarse a sí mismo, y nadie más puede quitárselo.
    /// </summary>
    public void ChangeRole(UserRole newRole, Guid actingUserId)
    {
        EnsureDefinedRole(newRole);
        EnsureNotSelf(actingUserId, "No puede cambiar su propio rol.");

        if (Role == newRole)
        {
            throw new DomainValidationException(nameof(Role), "El usuario ya tiene ese rol.");
        }

        Role = newRole;
    }

    /// <summary>Desactiva la cuenta. La misma restricción sobre uno mismo que <see cref="ChangeRole"/>.</summary>
    public void Deactivate(Guid actingUserId)
    {
        EnsureNotSelf(actingUserId, "No puede desactivar su propia cuenta.");

        if (!IsActive)
        {
            throw new DomainValidationException(nameof(IsActive), "La cuenta ya está desactivada.");
        }

        IsActive = false;
    }

    public void Activate()
    {
        if (IsActive)
        {
            throw new DomainValidationException(nameof(IsActive), "La cuenta ya está activa.");
        }

        IsActive = true;
    }

    /// <summary>
    /// Se comprueba después de verificar la contraseña, así que revelar que la cuenta está
    /// desactivada no le dice nada a quien no tenga ya la credencial.
    /// </summary>
    public void EnsureCanSignIn()
    {
        if (!IsActive)
        {
            throw new ForbiddenOperationException(
                "La cuenta está desactivada. Comuníquese con el administrador de la plataforma.");
        }
    }

    /// <summary>
    /// Proyecta el usuario al objeto de valor que guarda el agregado de la solicitud. El agregado
    /// conserva una copia del nombre en vez de una navegación, así que renombrar a un usuario nunca
    /// reescribe el historial ya registrado.
    /// </summary>
    public Actor ToActor() => new(Id, FullName);

    /// <summary>Normaliza un correo igual que <see cref="Create"/>, para las búsquedas.</summary>
    public static string NormaliseEmail(string email)
    {
        var value = email?.Trim().ToLowerInvariant() ?? string.Empty;

        if (value.Length == 0 || value.Length > EmailMaxLength)
        {
            throw new DomainValidationException(
                nameof(Email),
                $"El correo es obligatorio y no puede superar {EmailMaxLength} caracteres.");
        }

        // Superficial a propósito: una sola '@' con texto a ambos lados. La validación completa del
        // RFC le corresponde a un correo de confirmación, que este alcance no incluye.
        var at = value.IndexOf('@');
        if (at <= 0 || at == value.Length - 1 || value.IndexOf('@', at + 1) >= 0)
        {
            throw new DomainValidationException(nameof(Email), "El correo no tiene un formato válido.");
        }

        return value;
    }

    private void EnsureNotSelf(Guid actingUserId, string message)
    {
        if (actingUserId == Id)
        {
            throw new ForbiddenOperationException(message);
        }
    }

    private static void EnsureDefinedRole(UserRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new DomainValidationException(nameof(Role), $"El rol '{role}' no es válido.");
        }
    }

    private static string NormaliseName(string fullName)
    {
        var value = fullName?.Trim() ?? string.Empty;

        if (value.Length == 0 || value.Length > NameMaxLength)
        {
            throw new DomainValidationException(
                nameof(FullName),
                $"El nombre es obligatorio y no puede superar {NameMaxLength} caracteres.");
        }

        return value;
    }
}
