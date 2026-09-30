using System.Text.Json.Serialization;
using ExpertosSeguridad.Api.Errors;
using ExpertosSeguridad.Api.Identity;
using ExpertosSeguridad.Application;
using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Infrastructure;
using ExpertosSeguridad.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Configuration comes from environment variables (docker compose, .env) with appsettings
// only holding non-secret defaults.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión. Defina ConnectionStrings__Default como variable de entorno.");

const string CorsPolicyName = "frontend";
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services
    .AddApplication()
    .AddInfrastructure(connectionString);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentActorProvider, HeaderCurrentActorProvider>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Enums travel as names so the API contract stays readable and stable for the frontend.
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddCors(options => options.AddPolicy(CorsPolicyName, policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Gestión de solicitudes de mantenimiento", Version = "v1" });
    var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    if (File.Exists(xmlFile))
    {
        options.IncludeXmlComments(xmlFile);
    }
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Configuration.GetValue("Swagger:Enabled", true))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(CorsPolicyName);
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).ExcludeFromDescription();

if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
{
    await DatabaseInitializer.MigrateAsync(app.Services);

    if (app.Configuration.GetValue("Database:SeedSampleData", false))
    {
        await DatabaseInitializer.SeedAsync(app.Services);
    }
}

app.Run();

// Exposed so the integration tests can boot the real pipeline with WebApplicationFactory.
public partial class Program;
