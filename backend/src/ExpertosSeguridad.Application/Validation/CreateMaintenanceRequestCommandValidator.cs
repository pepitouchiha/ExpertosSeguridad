using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Domain.Entities;
using FluentValidation;

namespace ExpertosSeguridad.Application.Validation;

/// <summary>
/// Validates the incoming contract before it reaches the aggregate, so the API can answer
/// with a per-field 400 instead of a single exception message. Bounds are read from the
/// domain constants: the rule keeps one owner and the validator only surfaces it earlier.
/// </summary>
public sealed class CreateMaintenanceRequestCommandValidator : AbstractValidator<CreateMaintenanceRequestCommand>
{
    public CreateMaintenanceRequestCommandValidator(IUserDirectory userDirectory)
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

        RuleFor(command => command.RequesterId)
            .Must(id => userDirectory.Find(id) is not null)
            .WithMessage("El solicitante indicado no existe en el catálogo de usuarios.");
    }
}
