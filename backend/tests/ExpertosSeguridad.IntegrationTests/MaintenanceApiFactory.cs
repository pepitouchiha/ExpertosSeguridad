using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace ExpertosSeguridad.IntegrationTests;

/// <summary>
/// Levanta el pipeline real de la API contra un contenedor desechable de PostgreSQL, así que las
/// pruebas de integración ejercitan los mismos controladores, mapeos de EF, migraciones y
/// autenticación que corren en producción. Requiere Docker disponible en la máquina.
/// </summary>
public sealed class MaintenanceApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Contraseña de todas las cuentas de prueba en esta ejecución.</summary>
    public const string DemoPassword = "Prueba*Integracion*2026";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("maintenance_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync() => await _database.StartAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:Default", _database.GetConnectionString());
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");
        builder.UseSetting("Swagger:Enabled", "false");

        // Durante las pruebas se emiten y validan tokens reales; solo la clave es de prueba.
        builder.UseSetting("Jwt:SigningKey", "clave-de-firma-exclusiva-para-pruebas-de-integracion");
    }

    /// <summary>
    /// Aplica las migraciones versionadas al contenedor y vuelve a crear las cuentas de prueba, así
    /// que el esquema bajo prueba es el que trae el repositorio y no uno creado con EnsureCreated, y
    /// cada prueba arranca con el mismo conjunto conocido de usuarios.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MaintenanceDbContext>();

            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();
        }

        await TestAccounts.CreateAsync(Services, DemoPassword);
    }

    /// <summary>
    /// Inicia sesión y devuelve un cliente que lleva el token Bearer resultante, que es como se hace
    /// cada llamada autenticada de estas pruebas.
    /// </summary>
    public async Task<HttpClient> CreateSignedInClientAsync(string email)
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginCommand(email, DemoPassword),
            JsonOptions);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AuthResultDto>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result!.AccessToken);

        return client;
    }
}
