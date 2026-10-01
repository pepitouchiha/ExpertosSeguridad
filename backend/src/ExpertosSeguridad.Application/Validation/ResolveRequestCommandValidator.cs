using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Domain.ValueObjects;
using FluentValidation;

namespace ExpertosSeguridad.Application.Validation;

/// <summary>
/// Errores 400 por campo para el formulario de respuesta. Los límites vienen de
/// <see cref="Resolution"/>, que los vuelve a aplicar: la regla tiene un solo dueño y esto solo
/// la muestra campo por campo.
/// </summary>
public sealed class ResolveRequestCommandValidator : AbstractValidator<ResolveRequestCommand>
{
    public ResolveRequestCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty().WithMessage("El título de la respuesta es obligatorio.")
            .Must(title => (title ?? string.Empty).Trim().Length
                is >= Resolution.TitleMinLength and <= Resolution.TitleMaxLength)
            .WithMessage($"El título debe tener entre {Resolution.TitleMinLength} y {Resolution.TitleMaxLength} caracteres.");

        RuleFor(command => command.Description)
            .NotEmpty().WithMessage("La descripción de la respuesta es obligatoria.")
            .Must(description => (description ?? string.Empty).Trim().Length
                is >= Resolution.DescriptionMinLength and <= Resolution.DescriptionMaxLength)
            .WithMessage($"La descripción debe tener entre {Resolution.DescriptionMinLength} y {Resolution.DescriptionMaxLength} caracteres.");
    }
}
