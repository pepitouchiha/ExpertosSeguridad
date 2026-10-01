using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Application.Exceptions;
using ExpertosSeguridad.Application.Mapping;
using ExpertosSeguridad.Domain.Entities;
using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Domain.Policies;
using FluentValidation;
using FluentValidation.Results;

namespace ExpertosSeguridad.Application.Services;

/// <summary>
/// Orquesta los casos de uso: resuelve los colaboradores, verifica al llamante contra
/// <see cref="RequestAccessPolicy"/>, delega las reglas del ciclo de vida en el agregado y
/// confirma una sola vez. A propósito no declara reglas propias —la tabla de transiciones y las
/// reglas de acceso viven en el dominio— ni detalles de persistencia, que viven en infraestructura.
/// </summary>
public sealed class MaintenanceRequestService : IMaintenanceRequestService
{
    private readonly IMaintenanceRequestRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _users;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IClock _clock;
    private readonly IValidator<CreateMaintenanceRequestCommand> _createValidator;
    private readonly IValidator<ResolveRequestCommand> _resolveValidator;

    public MaintenanceRequestService(
        IMaintenanceRequestRepository repository,
        IUnitOfWork unitOfWork,
        IUserRepository users,
        ICurrentUserProvider currentUserProvider,
        IClock clock,
        IValidator<CreateMaintenanceRequestCommand> createValidator,
        IValidator<ResolveRequestCommand> resolveValidator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _users = users;
        _currentUserProvider = currentUserProvider;
        _clock = clock;
        _createValidator = createValidator;
        _resolveValidator = resolveValidator;
    }

    public async Task<MaintenanceRequestDetailDto> CreateAsync(
        CreateMaintenanceRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(command, cancellationToken);

        var currentUser = _currentUserProvider.GetCurrentUser();
        RequestAccessPolicy.EnsureCanCreateRequests(currentUser.Role);

        // El solicitante es el usuario autenticado, nunca un valor tomado del cuerpo de la petición.
        var request = MaintenanceRequest.Create(
            command.Title,
            command.Description,
            command.Category,
            command.Priority,
            currentUser.ToActor(),
            _clock.UtcNow);

        await _repository.AddAsync(request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return request.ToDetail(currentUser);
    }

    public async Task<PagedResult<MaintenanceRequestListItemDto>> SearchAsync(
        MaintenanceRequestQuery query,
        CancellationToken cancellationToken = default)
    {
        var currentUser = _currentUserProvider.GetCurrentUser();

        // Ambos valores se deciden aquí y sobrescriben lo que llegue en la query string: el alcance
        // (un solicitante ve solo lo suyo) y la prioridad (alguien del personal ve primero lo que tiene
        // asignado). Solo el personal atiende solicitudes, así que solo ellos reciben prioridad.
        var scoped = query with
        {
            RequesterId = ScopeFor(currentUser),
            PrioritiseResponsibleId = RequestAccessPolicy.CanManageLifecycle(currentUser.Role) ? currentUser.Id : null
        };

        var page = await _repository.SearchAsync(scoped.Normalise(), cancellationToken);

        return new PagedResult<MaintenanceRequestListItemDto>(
            page.Items.Select(item => item.ToListItem()).ToList(),
            page.Page,
            page.PageSize,
            page.TotalItems);
    }

    public async Task<MaintenanceRequestDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUser = _currentUserProvider.GetCurrentUser();

        var request = await _repository.GetByIdWithHistoryAsync(id, cancellationToken)
            ?? throw new NotFoundException("la solicitud", id);

        // Una solicitud de otra persona se reporta como inexistente y no como prohibida: un 403 aquí
        // confirmaría que el identificador existe.
        if (!RequestAccessPolicy.CanViewRequest(currentUser.Role, currentUser.Id, request.RequesterId))
        {
            throw new NotFoundException("la solicitud", id);
        }

        return request.ToDetail(currentUser);
    }

    public async Task<MaintenanceRequestDetailDto> ChangeStatusAsync(
        Guid id,
        ChangeStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var currentUser = _currentUserProvider.GetCurrentUser();
        RequestAccessPolicy.EnsureCanManageLifecycle(currentUser.Role);

        var request = await _repository.GetByIdWithHistoryAsync(id, cancellationToken)
            ?? throw new NotFoundException("la solicitud", id);

        // El agregado verifica la tabla de transiciones, que haya responsable y que quien llama sea
        // ese responsable en las transiciones de ejecución (pausar, reanudar, resolver).
        request.ChangeStatus(command.NewStatus, currentUser.ToActor(), _clock.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return request.ToDetail(currentUser);
    }

    public async Task<MaintenanceRequestDetailDto> ResolveAsync(
        Guid id,
        ResolveRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        await _resolveValidator.ValidateAndThrowAsync(command, cancellationToken);

        var currentUser = _currentUserProvider.GetCurrentUser();
        RequestAccessPolicy.EnsureCanManageLifecycle(currentUser.Role);

        var request = await _repository.GetByIdWithHistoryAsync(id, cancellationToken)
            ?? throw new NotFoundException("la solicitud", id);

        // El agregado verifica que se pueda resolver y que lo haga este actor, y registra la respuesta
        // y el cambio de estado juntos; una sola confirmación guarda ambos o ninguno.
        request.Resolve(command.Title, command.Description, currentUser.ToActor(), _clock.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return request.ToDetail(currentUser);
    }

    public async Task<MaintenanceRequestDetailDto> AssignResponsibleAsync(
        Guid id,
        AssignResponsibleCommand command,
        CancellationToken cancellationToken = default)
    {
        var currentUser = _currentUserProvider.GetCurrentUser();
        RequestAccessPolicy.EnsureCanManageLifecycle(currentUser.Role);

        var request = await _repository.GetByIdWithHistoryAsync(id, cancellationToken)
            ?? throw new NotFoundException("la solicitud", id);

        if (!command.ResponsibleId.HasValue)
        {
            throw ValidationFailureFor(
                nameof(command.ResponsibleId),
                "Indique el responsable. Una solicitud puede reasignarse, pero no quedar sin responsable.");
        }

        var candidate = await _users.FindByIdAsync(command.ResponsibleId.Value, cancellationToken);

        // Solo personal activo puede llevar una solicitud: un solicitante, o una cuenta desactivada,
        // sería un responsable incapaz de avanzar la solicitud que se le asignó.
        if (candidate is null || candidate.Role != UserRole.Staff || !candidate.IsActive)
        {
            throw ValidationFailureFor(
                nameof(command.ResponsibleId),
                "El responsable indicado no existe o no pertenece al personal activo de la empresa.");
        }

        // Asignar una solicitud pendiente también la inicia; el agregado registra ambos eventos.
        request.AssignResponsible(candidate.ToActor(), currentUser.ToActor(), _clock.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return request.ToDetail(currentUser);
    }

    public async Task<RequestSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var counts = await _repository.CountByStatusAsync(
            ScopeFor(_currentUserProvider.GetCurrentUser()),
            cancellationToken);

        int CountOf(RequestStatus status) => counts.TryGetValue(status, out var value) ? value : 0;

        return new RequestSummaryDto(
            counts.Values.Sum(),
            CountOf(RequestStatus.Pending),
            CountOf(RequestStatus.InProgress),
            CountOf(RequestStatus.OnHold),
            CountOf(RequestStatus.Resolved),
            CountOf(RequestStatus.Cancelled));
    }

    /// <summary>
    /// Nulo para el personal (todas las solicitudes); el identificador del propio llamante para un
    /// solicitante. El listado y el panel pasan por aquí, así que nunca pueden discrepar.
    /// </summary>
    private static Guid? ScopeFor(CurrentUser user) =>
        RequestAccessPolicy.CanViewEveryRequest(user.Role) ? null : user.Id;

    private static ValidationException ValidationFailureFor(string field, string message) =>
        new(new[] { new ValidationFailure(field, message) });
}
