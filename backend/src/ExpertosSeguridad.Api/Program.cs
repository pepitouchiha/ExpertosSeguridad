using System.Text;
using System.Text.Json.Serialization;
using ExpertosSeguridad.Api.Errors;
using ExpertosSeguridad.Api.Identity;
using ExpertosSeguridad.Application;
using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Infrastructure;
using ExpertosSeguridad.Infrastructure.Identity;
using ExpertosSeguridad.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// La configuración viene de variables de entorno (docker compose, .env); appsettings solo
// guarda valores por defecto que no son secretos.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión. Defina ConnectionStrings__Default como variable de entorno.");

const string CorsPolicyName = "frontend";
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, connectionString);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserProvider, ClaimsCurrentUserProvider>();

// --- Autenticación ---
// Se valida con el mismo objeto de opciones con el que firma el emisor, así que es imposible,
// por construcción, que emitir y validar queden desalineados.
var jwtOptions = JwtOptions.FromConfiguration(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            // Ninguna tolerancia para tokens vencidos: los cinco minutos por defecto extenderían en
            // silencio cada sesión más allá de su vigencia declarada.
            ClockSkew = TimeSpan.Zero
        };

        // Una firma válida prueba quién emitió el token, no que la cuenta lo siga mereciendo. Esto
        // vuelve a comprobar en la base de datos si la cuenta está activa y su rol.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = ActiveSessionValidator.ValidateAsync
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Los enums viajan como nombres para que el contrato de la API sea legible y estable para el frontend.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Los errores de enlace (JSON mal formado, un nombre de enum desconocido) nunca llegan al
        // caso de uso, así que GlobalExceptionHandler no los ve. La respuesta por defecto del framework
        // viene en inglés y cita nombres de tipos internos ("could not be converted to ...RegisterCommand");
        // esto la reemplaza por el mismo formato que usa el resto de la API.
        options.InvalidModelStateResponseFactory = BindingErrorResponse.Create;
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

    // Permite que la interfaz de Swagger envíe el token, para que la API siga siendo explorable
    // ahora que la autenticación es obligatoria.
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pegue aquí el accessToken devuelto por POST /api/auth/login.",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };

    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });

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

// El orden importa: la autenticación construye el principal que luego inspecciona la autorización.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).ExcludeFromDescription();

if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
{
    await DatabaseInitializer.MigrateAsync(app.Services);
}

app.Run();

// Expuesto para que las pruebas de integración levanten el pipeline real con WebApplicationFactory.
public partial class Program;
