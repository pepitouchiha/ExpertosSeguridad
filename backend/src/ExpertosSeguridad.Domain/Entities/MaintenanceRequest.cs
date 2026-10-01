using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Domain.Exceptions;
using ExpertosSeguridad.Domain.Policies;
using ExpertosSeguridad.Domain.ValueObjects;

namespace ExpertosSeguridad.Domain.Entities;

/// <summary>
/// Raíz de agregado del ciclo de vida de la solicitud. Toda mutación de estado pasa por un método
/// de comportamiento que valida la regla y agrega la entrada de historial correspondiente, así que
/// las invariantes se cumplen sin importar quién maneje el modelo (API, prueba, un proceso futuro).
/// </summary>
public sealed class MaintenanceRequest
{
    public const int TitleMinLength = 5;
    public const int TitleMaxLength = 120;
    public const int DescriptionMinLength = 10;
    public const int DescriptionMaxLength = 2000;

    private readonly List<RequestHistoryEntry> _history = new();

    private MaintenanceRequest()
    {
        Title = string.Empty;
        Description = string.Empty;
        RequesterName = string.Empty;
    }

    public Guid Id { get; private set; }

    /// <summary>
    /// Número secuencial legible ("SOL-0007"), asignado por la base de datos al insertar. El Guid sigue
    /// siendo la identidad en el código y en las URLs; esto existe solo para que las personas puedan
    /// referirse a una solicitud. Vale 0 hasta que la solicitud se guarda.
    /// </summary>
    public int Number { get; private set; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public RequestCategory Category { get; private set; }

    public RequestPriority Priority { get; private set; }

    public RequestStatus Status { get; private set; }

    public Guid RequesterId { get; private set; }

    public string RequesterName { get; private set; }

    public Guid? ResponsibleId { get; private set; }

    public string? ResponsibleName { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>La respuesta del responsable al solicitante. Existe una vez resuelta la solicitud.</summary>
    public Resolution? Resolution { get; private set; }

    public IReadOnlyCollection<RequestHistoryEntry> History => _history.AsReadOnly();

    /// <summary>
    /// Las transiciones que esta solicitud puede tomar ahora: las de la tabla, menos las que necesitan
    /// un responsable cuando no lo hay. Por eso una solicitud pendiente solo ofrece cancelar: la forma
    /// de iniciarla es asignarla.
    /// </summary>
    public IReadOnlyList<RequestStatus> AllowedNextStatuses => RequestStatusTransitionPolicy.AllowedFrom(Status)
        .Where(target => ResponsibleId.HasValue || !RequestAccessPolicy.IsReservedForResponsible(target))
        .ToList();

    /// <summary>
    /// Fábrica que hace cumplir las invariantes de creación. El estado inicial es siempre
    /// <see cref="RequestStatus.Pending"/> y la fecha la entrega quien llama (la API usa el reloj
    /// del servidor), nunca el cuerpo HTTP.
    /// </summary>
    public static MaintenanceRequest Create(
        string title,
        string description,
        RequestCategory category,
        RequestPriority priority,
        Actor requester,
        DateTimeOffset createdAt)
    {
        var normalisedTitle = NormaliseTitle(title);
        var normalisedDescription = NormaliseDescription(description);
        EnsureDefinedEnum(category, nameof(category));
        EnsureDefinedEnum(priority, nameof(priority));

        var request = new MaintenanceRequest
        {
            Id = Guid.NewGuid(),
            Title = normalisedTitle,
            Description = normalisedDescription,
            Category = category,
            Priority = priority,
            Status = RequestStatus.Pending,
            RequesterId = requester.Id,
            RequesterName = requester.Name,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        request._history.Add(RequestHistoryEntry.ForCreation(request.Id, requester, createdAt));

        return request;
    }

    /// <summary>
    /// Aplica una transición del ciclo de vida pedida por <paramref name="actor"/>. Las
    /// verificaciones van de la más general a la más específica, lo que además fija la respuesta HTTP:
    /// <list type="number">
    /// <item>la transición debe existir en la tabla; si no, 409 para cualquiera;</item>
    /// <item>las transiciones de ejecución necesitan responsable; si no, 409: la solicitud no está lista;</item>
    /// <item>y solo ese responsable puede tomarlas; si no, 403.</item>
    /// </list>
    /// Verificar al actor dentro del agregado, y no solo en el servicio, garantiza que nadie pueda
    /// registrar «resuelta por» alguien que no estaba haciendo el trabajo.
    /// </summary>
    public void ChangeStatus(RequestStatus newStatus, Actor actor, DateTimeOffset occurredAt)
    {
        EnsureCanMoveTo(newStatus, actor);

        // Resolver no es un simple cambio de estado: cierra la solicitud con una respuesta al
        // solicitante, así que tiene su propia operación, que no se puede invocar sin respuesta.
        if (newStatus == RequestStatus.Resolved)
        {
            throw new DomainValidationException(
                nameof(Resolution),
                "Para resolver la solicitud registre la respuesta para el solicitante.");
        }

        ApplyStatus(newStatus, actor, occurredAt);
    }

    /// <summary>
    /// Cierra la solicitud con la respuesta del responsable. Las mismas verificaciones que cualquier
    /// transición de ejecución (debe estar en progreso, tener responsable y resolverla él), y la
    /// respuesta y el cambio de estado se aplican juntos: no puede existir una solicitud resuelta
    /// sin respuesta, ni una respuesta sobre una solicitud que no se resolvió.
    /// </summary>
    public void Resolve(string title, string description, Actor actor, DateTimeOffset occurredAt)
    {
        EnsureCanMoveTo(RequestStatus.Resolved, actor);

        Resolution = Resolution.Create(title, description, actor, occurredAt);
        ApplyStatus(RequestStatus.Resolved, actor, occurredAt);
    }

    private void EnsureCanMoveTo(RequestStatus newStatus, Actor actor)
    {
        EnsureDefinedEnum(newStatus, nameof(newStatus));

        if (!RequestStatusTransitionPolicy.IsAllowed(Status, newStatus))
        {
            throw new InvalidStatusTransitionException(Status, newStatus);
        }

        if (RequestAccessPolicy.IsReservedForResponsible(newStatus))
        {
            if (!ResponsibleId.HasValue)
            {
                throw new InvalidStatusTransitionException(
                    Status,
                    newStatus,
                    "La solicitud no tiene responsable. Asígnela para iniciar su atención.");
            }

            if (ResponsibleId != actor.Id)
            {
                throw new ForbiddenOperationException(
                    $"Solo el responsable asignado ({ResponsibleName}) puede poner en espera, reanudar o resolver esta solicitud.");
            }
        }
    }

    /// <summary>
    /// Asigna o reemplaza al responsable. Asignar una solicitud pendiente también la inicia: con
    /// alguien a cargo ya no hay nada que esperar, así que pasa a
    /// <see cref="RequestStatus.InProgress"/> en la misma operación, y el historial registra ambos
    /// eventos, la asignación y el cambio de estado que provocó, con el mismo actor e instante. Las
    /// dos entradas se guardan en la misma confirmación, o ninguna.
    ///
    /// El responsable se puede reemplazar, pero nunca quitar. Dejar sin responsable una solicitud
    /// iniciada implicaría volver a <see cref="RequestStatus.Pending"/>, una transición que la tabla
    /// del ciclo de vida no contempla; y una solicitud pendiente nunca tiene responsable que quitar.
    /// </summary>
    public void AssignResponsible(Actor responsible, Actor actor, DateTimeOffset occurredAt)
    {
        ArgumentNullException.ThrowIfNull(responsible);

        if (RequestStatusTransitionPolicy.IsTerminal(Status))
        {
            throw new DomainValidationException(
                nameof(ResponsibleId),
                $"No se puede modificar el responsable de una solicitud en estado '{Status}'.");
        }

        if (ResponsibleId == responsible.Id)
        {
            throw new DomainValidationException(
                nameof(ResponsibleId),
                "El responsable indicado ya está asignado a la solicitud.");
        }

        var previous = ResponsibleId.HasValue
            ? new Actor(ResponsibleId.Value, ResponsibleName!)
            : null;

        ResponsibleId = responsible.Id;
        ResponsibleName = responsible.Name;
        UpdatedAt = occurredAt;

        _history.Add(RequestHistoryEntry.ForResponsibleChange(Id, previous, responsible, actor, occurredAt));

        // La tabla permite Pending → InProgress, así que es una transición normal y no un atajo
        // alrededor de la política; solo es automática en lugar de pedida.
        if (Status == RequestStatus.Pending)
        {
            ApplyStatus(RequestStatus.InProgress, actor, occurredAt);
        }
    }

    private void ApplyStatus(RequestStatus newStatus, Actor actor, DateTimeOffset occurredAt)
    {
        var previousStatus = Status;
        Status = newStatus;
        UpdatedAt = occurredAt;

        _history.Add(RequestHistoryEntry.ForStatusChange(Id, previousStatus, newStatus, actor, occurredAt));
    }

    private static string NormaliseTitle(string title)
    {
        var value = title?.Trim() ?? string.Empty;

        if (value.Length is < TitleMinLength or > TitleMaxLength)
        {
            throw new DomainValidationException(
                nameof(Title),
                $"El título debe tener entre {TitleMinLength} y {TitleMaxLength} caracteres.");
        }

        return value;
    }

    private static string NormaliseDescription(string description)
    {
        var value = description?.Trim() ?? string.Empty;

        if (value.Length is < DescriptionMinLength or > DescriptionMaxLength)
        {
            throw new DomainValidationException(
                nameof(Description),
                $"La descripción debe tener entre {DescriptionMinLength} y {DescriptionMaxLength} caracteres.");
        }

        return value;
    }

    private static void EnsureDefinedEnum<TEnum>(TEnum value, string field) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new DomainValidationException(field, $"El valor '{value}' no es válido para {typeof(TEnum).Name}.");
        }
    }
}
