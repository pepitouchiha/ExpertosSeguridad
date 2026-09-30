using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Domain.Entities;
using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExpertosSeguridad.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies pending migrations so <c>docker compose up</c> yields a working database with
    /// no manual step, as the brief requires for reproducibility.
    /// </summary>
    public static async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MaintenanceDbContext>();

        await context.Database.MigrateAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts a small, varied dataset the first time the database is empty, so the list,
    /// the filters and the dashboard have something to show on a fresh checkout.
    /// Sample rows are built through the domain API, so their history is real, not fabricated.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MaintenanceDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseInitializer));

        if (await context.MaintenanceRequests.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = clock.UtcNow;
        var ana = InMemoryUserDirectory.AnaTorres;
        var carlos = InMemoryUserDirectory.CarlosRuiz;
        var diana = InMemoryUserDirectory.DianaLopez;
        var esteban = InMemoryUserDirectory.EstebanMora;

        var pending = MaintenanceRequest.Create(
            "Fuga de agua en el baño del segundo piso",
            "Se detectó una fuga constante bajo el lavamanos que ya humedeció la pared contigua.",
            RequestCategory.Infrastructure,
            RequestPriority.High,
            ana,
            now.AddDays(-1));

        var inProgress = MaintenanceRequest.Create(
            "Aire acondicionado de la sala de servidores no enfría",
            "La temperatura del rack subió a 31 °C. Se requiere revisión del compresor lo antes posible.",
            RequestCategory.Equipment,
            RequestPriority.Critical,
            carlos,
            now.AddDays(-3));
        inProgress.AssignResponsible(diana, carlos, now.AddDays(-3).AddHours(2));
        inProgress.ChangeStatus(RequestStatus.InProgress, diana, now.AddDays(-2));

        var onHold = MaintenanceRequest.Create(
            "Licencia del antivirus corporativo por vencer",
            "La consola reporta 12 días restantes de licencia para 140 equipos administrados.",
            RequestCategory.Software,
            RequestPriority.Medium,
            diana,
            now.AddDays(-6));
        onHold.AssignResponsible(esteban, diana, now.AddDays(-6).AddHours(1));
        onHold.ChangeStatus(RequestStatus.InProgress, esteban, now.AddDays(-5));
        onHold.ChangeStatus(RequestStatus.OnHold, esteban, now.AddDays(-4));

        var resolved = MaintenanceRequest.Create(
            "Cambio de luminarias en el parqueadero",
            "Ocho luminarias del parqueadero visitantes están fundidas y reducen la visibilidad nocturna.",
            RequestCategory.Infrastructure,
            RequestPriority.Low,
            esteban,
            now.AddDays(-12));
        resolved.AssignResponsible(ana, esteban, now.AddDays(-12).AddHours(3));
        resolved.ChangeStatus(RequestStatus.InProgress, ana, now.AddDays(-11));
        resolved.ChangeStatus(RequestStatus.Resolved, ana, now.AddDays(-9));

        var cancelled = MaintenanceRequest.Create(
            "Traslado de impresora al área de recepción",
            "Se solicitó mover la impresora multifuncional; el área decidió mantener el equipo en su sitio actual.",
            RequestCategory.Other,
            RequestPriority.Low,
            ana,
            now.AddDays(-15));
        cancelled.ChangeStatus(RequestStatus.Cancelled, ana, now.AddDays(-14));

        context.MaintenanceRequests.AddRange(pending, inProgress, onHold, resolved, cancelled);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Se insertaron datos de ejemplo iniciales.");
    }
}
