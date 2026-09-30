using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ExpertosSeguridad.Infrastructure.Persistence;

/// <summary>
/// Used only by <c>dotnet ef</c> to build the model when generating migrations. It keeps the
/// tooling independent from the API host, so creating a migration never needs a running
/// database or the application configuration.
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
