using Microsoft.AspNetCore.Mvc;

namespace ExpertosSeguridad.Api.Errors;

/// <summary>
/// Respuesta para las solicitudes que el framework ni siquiera pudo enlazar: un cuerpo que no es
/// JSON válido, o un valor del tipo equivocado (un estado que no existe, texto donde va un número).
///
/// Los mensajes propios del framework se descartan a propósito. Están escritos para
/// desarrolladores, en inglés, y nombran tipos internos y rutas JSON; el cliente recibe un mensaje
/// genérico y, cuando se puede identificar, el nombre del campo, nada sobre cómo está construido
/// el servidor.
/// </summary>
public static class BindingErrorResponse
{
    private const string FieldMessage = "El valor indicado no tiene un formato válido.";

    public static IActionResult Create(ActionContext context)
    {
        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .Select(entry => FieldName(entry.Key))
            // Un nombre vacío significa que falló todo el cuerpo; eso ya lo cubre Detail.
            .Where(field => field.Length > 0)
            .Distinct()
            .ToDictionary(field => field, _ => new[] { FieldMessage });

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Solicitud inválida",
            Detail = "La solicitud no tiene un formato válido. Revise los datos enviados.",
            Instance = context.HttpContext.Request.Path
        };

        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" }
        };
    }

    /// <summary>
    /// "$.fullName" → "fullName"; "status" se queda igual; el parámetro de la acción ("command"),
    /// que el framework reporta cuando no se puede leer el cuerpo entero, queda vacío.
    /// </summary>
    private static string FieldName(string key)
    {
        var name = key.StartsWith("$", StringComparison.Ordinal) ? key.TrimStart('$', '.') : key;

        if (name.Length == 0 || name.Equals("command", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
