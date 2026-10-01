using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Domain.Entities;
using FluentValidation;

namespace ExpertosSeguridad.Application.Validation;

/// <summary>
/// Valida el cuerpo del registro. Las reglas de contraseña viven aquí y no en el dominio porque
/// el dominio nunca ve la contraseña en claro, solo su hash. Los límites de nombre y correo se leen
/// de las constantes de la entidad, así que esas reglas conservan un solo dueño.
/// </summary>
public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;

    public RegisterCommandValidator()
    {
        RuleFor(command => command.FullName)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .Must(name => (name ?? string.Empty).Trim().Length <= User.NameMaxLength)
            .WithMessage($"El nombre no puede superar {User.NameMaxLength} caracteres.");

        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .MaximumLength(User.EmailMaxLength)
            .WithMessage($"El correo no puede superar {User.EmailMaxLength} caracteres.")
            .EmailAddress().WithMessage("El correo no tiene un formato válido.");

        // La longitud es lo que de verdad resiste la adivinación; la regla de letra y número solo
        // descarta las elecciones triviales más comunes. No se exigen símbolos: empujan a sustituciones
        // predecibles sin aportar mucho.
        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .Length(PasswordMinLength, PasswordMaxLength)
            .WithMessage($"La contraseña debe tener entre {PasswordMinLength} y {PasswordMaxLength} caracteres.")
            .Must(password => password is not null && password.Any(char.IsLetter) && password.Any(char.IsDigit))
            .WithMessage("La contraseña debe incluir al menos una letra y un número.");
    }
}
