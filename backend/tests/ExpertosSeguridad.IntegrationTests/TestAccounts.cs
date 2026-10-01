using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Domain.Entities;
using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace ExpertosSeguridad.IntegrationTests;

/// <summary>
/// Cuentas que las pruebas de integración crean antes de cada caso. Viven aquí y no en la
/// aplicación: la aplicación arranca sin datos, y estos usuarios solo existen para ejercitar
/// los roles. Dos solicitantes, dos miembros del personal y un administrador es el mínimo para
/// probar que cada rol ve una aplicación distinta.
/// </summary>
public static class TestAccounts
{
    public const string AnaEmail = "ana.torres@cliente.com";
    public const string CarlosEmail = "carlos.ruiz@cliente.com";
    public const string DianaEmail = "diana.lopez@expertoseguridad.com";
    public const string EstebanEmail = "esteban.mora@expertoseguridad.com";
    public const string AdminEmail = "admin@expertoseguridad.com";

    public static readonly IReadOnlyList<(string Name, string Email, UserRole Role)> All = new[]
    {
        ("Ana Torres", AnaEmail, UserRole.Requester),
        ("Carlos Ruiz", CarlosEmail, UserRole.Requester),
        ("Diana López", DianaEmail, UserRole.Staff),
        ("Esteban Mora", EstebanEmail, UserRole.Staff),
        ("Marcela Rincón", AdminEmail, UserRole.Admin)
    };

    /// <summary>
    /// Crea las cuentas con el mismo hasher que usa la API, para que el inicio de sesión de las
    /// pruebas recorra el camino real de verificación de contraseñas.
    /// </summary>
    public static async Task CreateAsync(IServiceProvider services, string password)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MaintenanceDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var hash = hasher.Hash(password);

        context.Users.AddRange(All.Select(account =>
            User.Create(account.Name, account.Email, hash, account.Role, DateTimeOffset.UtcNow)));

        await context.SaveChangesAsync();
    }
}
