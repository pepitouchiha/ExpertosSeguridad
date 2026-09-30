using ExpertosSeguridad.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpertosSeguridad.Infrastructure.Persistence;

public sealed class MaintenanceDbContext : DbContext
{
    public MaintenanceDbContext(DbContextOptions<MaintenanceDbContext> options) : base(options)
    {
    }

    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();

    public DbSet<RequestHistoryEntry> RequestHistoryEntries => Set<RequestHistoryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MaintenanceDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
