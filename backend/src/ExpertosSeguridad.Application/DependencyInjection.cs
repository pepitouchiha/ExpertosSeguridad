using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Application.Services;
using ExpertosSeguridad.Application.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ExpertosSeguridad.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the use cases owned by this layer. Each layer exposes its own composition
    /// entry point so the host only wires modules, not individual classes.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IMaintenanceRequestService, MaintenanceRequestService>();
        services.AddScoped<IValidator<CreateMaintenanceRequestCommand>, CreateMaintenanceRequestCommandValidator>();

        return services;
    }
}
