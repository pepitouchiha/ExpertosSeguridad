using ExpertosSeguridad.Application.Exceptions;
using ExpertosSeguridad.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FluentValidationException = FluentValidation.ValidationException;

namespace ExpertosSeguridad.Api.Errors;

/// <summary>
/// Single place where exceptions become HTTP responses. Controllers stay free of try/catch
/// and the mapping rule (which failure means which status code) is stated once.
/// Unexpected exceptions are logged in full but answered with a generic message so internal
/// details never reach the client.
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

        // Serialised against the runtime type: with the declared base type the `errors`
        // dictionary of ValidationProblemDetails would be dropped from the payload.
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

    private static ProblemDetails BuildProblem(int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail
    };

    private static string ToCamelCase(string value) =>
        string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];
}
