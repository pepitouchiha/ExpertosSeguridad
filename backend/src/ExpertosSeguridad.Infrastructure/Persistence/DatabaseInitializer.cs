using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpertosSeguridad.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// Aplica las migraciones pendientes para que <c>docker compose up</c> deje la base de datos
    /// lista sin pasos manuales, como exige el enunciado para la reproducibilidad.
    ///
    /// No inserta datos: la aplicación arranca vacía. Las cuentas se crean registrándose, y la
    /// primera se promueve a administrador con el paso documentado en el README.
    /// </summary>
    public static async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MaintenanceDbContext>();

        await context.Database.MigrateAsync(cancellationToken);
    }
}
