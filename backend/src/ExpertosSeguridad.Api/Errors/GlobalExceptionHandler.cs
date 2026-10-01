using ExpertosSeguridad.Application.Exceptions;
using ExpertosSeguridad.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FluentValidationException = FluentValidation.ValidationException;

namespace ExpertosSeguridad.Api.Errors;

/// <summary>
/// Único lugar donde las excepciones se convierten en respuestas HTTP. Los controladores quedan
/// libres de try/catch y la regla de correspondencia (qué falla significa qué código) se declara
/// una sola vez. Las excepciones inesperadas se registran completas en el log, pero se responden
/// con un mensaje genérico para que ningún detalle interno llegue al cliente.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            FluentValidationException validation => BuildValidationProblem(validation),
            DomainValidationException domainValidation => BuildProblem(
                StatusCodes.Status400BadRequest,
                "Solicitud inválida",
                domainValidation.Message),
            InvalidStatusTransitionException transition => BuildProblem(
                StatusCodes.Status409Conflict,
                "Transición no permitida",
                transition.Message),
            // Se sabe quién llama, pero su rol no permite la operación: 403, no 400.
            ForbiddenOperationException forbidden => BuildProblem(
                StatusCodes.Status403Forbidden,
                "Operación no permitida",
                forbidden.Message),
            // Credenciales incorrectas y usuario inexistente terminan aquí con el mismo mensaje.
            InvalidCredentialsException credentials => BuildProblem(
                StatusCodes.Status401Unauthorized,
                "Credenciales inválidas",
                credentials.Message),
            UnauthorizedAccessException => BuildProblem(
                StatusCodes.Status401Unauthorized,
                "No autenticado",
                "La operación requiere iniciar sesión."),
            ConflictException conflict => BuildConflictProblem(conflict),
            NotFoundException notFound => BuildProblem(
                StatusCodes.Status404NotFound,
                "Recurso no encontrado",
                notFound.Message),
            _ => null
        };

        if (problem is null)
        {
            _logger.LogError(exception, "Error no controlado al procesar {Path}", httpContext.Request.Path);

            problem = BuildProblem(
                StatusCodes.Status500InternalServerError,
                "Error interno",
                "Ocurrió un error inesperado al procesar la solicitud.");
        }
        else
        {
            _logger.LogInformation(
                "Solicitud rechazada en {Path}: {Detail}",
                httpContext.Request.Path,
                problem.Detail);
        }

        problem.Instance = httpContext.Request.Path;
        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        // Se serializa con el tipo en tiempo de ejecución: con el tipo base declarado se perdería
        // del cuerpo el diccionario `errors` de ValidationProblemDetails.
        await httpContext.Response.WriteAsJsonAsync(
            problem,
            problem.GetType(),
            options: null,
            contentType: "application/problem+json",
            cancellationToken);

        return true;
    }

    private static ProblemDetails BuildValidationProblem(FluentValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(failure => ToCamelCase(failure.PropertyName))
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Solicitud inválida",
            Detail = "Uno o más campos no superaron la validación."
        };
    }

    /// <summary>
    /// Un 409 que, cuando el conflicto corresponde a un campo, lo lleva en <c>errors</c> igual que un
    /// error de validación. Así el formulario puede mostrar «correo ya registrado» bajo el campo de
    /// correo sin un caso especial.
    /// </summary>
    private static ProblemDetails BuildConflictProblem(ConflictException conflict)
    {
        if (conflict.Field is null)
        {
            return BuildProblem(StatusCodes.Status409Conflict, "Conflicto", conflict.Message);
        }

        return new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [ToCamelCase(conflict.Field)] = new[] { conflict.Message }
        })
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflicto",
            Detail = conflict.Message
        };
    }

    private static ProblemDetails BuildProblem(int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail
    };

    private static string ToCamelCase(string value) =>
        string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];
}
