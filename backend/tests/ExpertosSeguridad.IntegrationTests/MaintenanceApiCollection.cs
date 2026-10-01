namespace ExpertosSeguridad.IntegrationTests;

/// <summary>
/// Todas las suites de integración comparten un host de la API y un contenedor de PostgreSQL. Ser
/// una sola colección de xUnit además hace que se ejecuten una tras otra, lo cual importa porque
/// cada prueba reinicia la misma base de datos antes de empezar.
/// </summary>
[CollectionDefinition(Name)]
public sealed class MaintenanceApiCollection : ICollectionFixture<MaintenanceApiFactory>
{
    public const string Name = "maintenance-api";
}
