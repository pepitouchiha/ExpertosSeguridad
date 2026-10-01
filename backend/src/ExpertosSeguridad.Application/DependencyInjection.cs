using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Application.Services;
using ExpertosSeguridad.Application.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ExpertosSeguridad.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra los casos de uso de esta capa. Cada capa expone su propio punto de composición para
    /// que el host solo conecte módulos, no clases individuales.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IMaintenanceRequestService, MaintenanceRequestService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserAdministrationService, UserAdministrationService>();
        services.AddScoped<IValidator<CreateMaintenanceRequestCommand>, CreateMaintenanceRequestCommandValidator>();
        services.AddScoped<IValidator<LoginCommand>, LoginCommandValidator>();
        services.AddScoped<IValidator<ResolveRequestCommand>, ResolveRequestCommandValidator>();
        services.AddScoped<IValidator<RegisterCommand>, RegisterCommandValidator>();

        return services;
    }
}
