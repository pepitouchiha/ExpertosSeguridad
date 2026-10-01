using ExpertosSeguridad.Domain.Exceptions;

namespace ExpertosSeguridad.Domain.ValueObjects;

/// <summary>
/// La respuesta que el responsable le da al solicitante al cerrar una solicitud: qué se hizo y
/// quién lo hizo. Un objeto de valor: no tiene identidad propia, es inmutable una vez registrado
/// y solo existe como parte de la solicitud que resuelve.
///
/// El nombre de quien responde se guarda como copia por la misma razón que en el historial: la
/// respuesta debe seguir diciendo quién la dio aunque ese usuario cambie de nombre después.
/// </summary>
public sealed class Resolution
{
    public const int TitleMinLength = 5;
    public const int TitleMaxLength = 120;
    public const int DescriptionMinLength = 10;
    public const int DescriptionMaxLength = 2000;

    /// <summary>Para EF Core, que materializa las columnas propias en este tipo.</summary>
    private Resolution()
    {
        Title = string.Empty;
        Description = string.Empty;
        RespondedByName = string.Empty;
    }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public Guid RespondedById { get; private set; }

    public string RespondedByName { get; private set; }

    public DateTimeOffset RespondedAt { get; private set; }

    public static Resolution Create(string title, string description, Actor respondedBy, DateTimeOffset respondedAt)
    {
        ArgumentNullException.ThrowIfNull(respondedBy);

        return new Resolution
        {
            Title = Normalise(title, TitleMinLength, TitleMaxLength, nameof(Title),
                $"El título de la respuesta debe tener entre {TitleMinLength} y {TitleMaxLength} caracteres."),
            Description = Normalise(description, DescriptionMinLength, DescriptionMaxLength, nameof(Description),
                $"La descripción de la respuesta debe tener entre {DescriptionMinLength} y {DescriptionMaxLength} caracteres."),
            RespondedById = respondedBy.Id,
            RespondedByName = respondedBy.Name,
            RespondedAt = respondedAt
        };
    }

    private static string Normalise(string value, int min, int max, string field, string message)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length < min || trimmed.Length > max)
        {
            throw new DomainValidationException(field, message);
        }

        return trimmed;
    }
}
