using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Infrastructure.Identity;
using ExpertosSeguridad.Infrastructure.Persistence;
using ExpertosSeguridad.Infrastructure.Persistence.Repositories;
using ExpertosSeguridad.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ExpertosSeguridad.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        services.AddDbContext<MaintenanceDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IMaintenanceRequestRepository, MaintenanceRequestRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IClock, SystemClock>();

        // Infraestructura de identidad: el algoritmo y el formato del token son decisiones de
        // infraestructura, así que la capa de aplicación solo ve las dos interfaces.
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

        // Enlazadas y validadas de inmediato: el host debe negarse a arrancar con una clave inservible
        // en vez de descubrirlo cuando el primer usuario intente iniciar sesión.
        services.AddSingleton(Options.Create(JwtOptions.FromConfiguration(configuration)));

        return services;
    }
}
