using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Infrastructure.Identity;
using ExpertosSeguridad.Infrastructure.Persistence;
using ExpertosSeguridad.Infrastructure.Persistence.Repositories;
using ExpertosSeguridad.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpertosSeguridad.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<MaintenanceDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IMaintenanceRequestRepository, MaintenanceRequestRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IUserDirectory, InMemoryUserDirectory>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
