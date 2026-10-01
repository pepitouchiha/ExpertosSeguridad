using ExpertosSeguridad.Application.Contracts;
using FluentValidation;

namespace ExpertosSeguridad.Application.Validation;

/// <summary>
/// Validación solo de forma del cuerpo del login. Comprueba que lleguen ambos campos, nunca si
/// son correctos: esa respuesta le corresponde a <see cref="Services.AuthService"/>, que devuelve
/// el mismo 401 en cualquier fallo para que el endpoint no permita enumerar cuentas.
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("El correo es obligatorio.");

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.");
    }
}
