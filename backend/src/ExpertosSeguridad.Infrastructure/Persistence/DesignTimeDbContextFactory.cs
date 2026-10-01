using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ExpertosSeguridad.Infrastructure.Persistence;

/// <summary>
/// Solo lo usa <c>dotnet ef</c> para construir el modelo al generar migraciones. Mantiene las
/// herramientas independientes del host de la API, así que crear una migración nunca necesita
/// una base de datos en marcha ni la configuración de la aplicación.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MaintenanceDbContext>
{
    public MaintenanceDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=maintenance;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<MaintenanceDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new MaintenanceDbContext(options);
    }
}
