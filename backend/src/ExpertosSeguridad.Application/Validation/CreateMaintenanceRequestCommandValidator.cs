using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Domain.Entities;
using FluentValidation;

namespace ExpertosSeguridad.Application.Validation;

/// <summary>
/// Valida el contrato de entrada antes de que llegue al agregado, para que la API pueda responder
/// un 400 por campo en lugar de un único mensaje de excepción. Los límites se leen de las
/// constantes del dominio: la regla tiene un solo dueño y el validador solo la adelanta.
/// </summary>
public sealed class CreateMaintenanceRequestCommandValidator : AbstractValidator<CreateMaintenanceRequestCommand>
{
    public CreateMaintenanceRequestCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty().WithMessage("El título es obligatorio.")
            .Must(title => (title ?? string.Empty).Trim().Length
                is >= MaintenanceRequest.TitleMinLength and <= MaintenanceRequest.TitleMaxLength)
            .WithMessage($"El título debe tener entre {MaintenanceRequest.TitleMinLength} y {MaintenanceRequest.TitleMaxLength} caracteres.");

        RuleFor(command => command.Description)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .Must(description => (description ?? string.Empty).Trim().Length
                is >= MaintenanceRequest.DescriptionMinLength and <= MaintenanceRequest.DescriptionMaxLength)
            .WithMessage($"La descripción debe tener entre {MaintenanceRequest.DescriptionMinLength} y {MaintenanceRequest.DescriptionMaxLength} caracteres.");

        RuleFor(command => command.Category)
            .IsInEnum().WithMessage("La categoría indicada no es válida.");

        RuleFor(command => command.Priority)
            .IsInEnum().WithMessage("La prioridad indicada no es válida.");
    }
}
