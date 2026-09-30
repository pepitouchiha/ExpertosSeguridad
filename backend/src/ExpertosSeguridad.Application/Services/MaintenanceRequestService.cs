using ExpertosSeguridad.Application.Abstractions;
using ExpertosSeguridad.Application.Contracts;
using ExpertosSeguridad.Application.Exceptions;
using ExpertosSeguridad.Application.Mapping;
using ExpertosSeguridad.Domain.Entities;
using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Domain.ValueObjects;
using FluentValidation;
using FluentValidation.Results;

namespace ExpertosSeguridad.Application.Services;

/// <summary>
/// Orchestrates the use cases: it resolves collaborators, delegates the rules to the
/// aggregate and commits once. It deliberately contains no lifecycle rule of its own —
/// those live in the domain — and no persistence detail — those live in infrastructure.
/// </summary>
public sealed class MaintenanceRequestService : IMaintenanceRequestService
{
    private readonly IMaintenanceRequestRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserDirectory _userDirectory;
    private readonly ICurrentActorProvider _currentActorProvider;
    private readonly IClock _clock;
    private readonly IValidator<CreateMaintenanceRequestCommand> _createValidator;

    public MaintenanceRequestService(
        IMaintenanceRequestRepository repository,
        IUnitOfWork unitOfWork,
        IUserDirectory userDirectory,
        ICurrentActorProvider currentActorProvider,
        IClock clock,
        IValidator<CreateMaintenanceRequestCommand> createValidator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _userDirectory = userDirectory;
        _currentActorProvider = currentActorProvider;
        _clock = clock;
        _createValidator = createValidator;
    }

    public async Task<MaintenanceRequestDetailDto> CreateAsync(
        CreateMaintenanceRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(command, cancellationToken);

        var requester = _userDirectory.Find(command.RequesterId)
            ?? throw ValidationFailureFor(nameof(command.RequesterId), "El solicitante indicado no existe.");

        var request = MaintenanceRequest.Create(
            command.Title,
            command.Description,
            command.Category,
            command.Priority,
            requester,
            _clock.UtcNow);

        await _repository.AddAsync(request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return request.ToDetail();
    }

    public async Task<PagedResult<MaintenanceRequestListItemDto>> SearchAsync(
        MaintenanceRequestQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = await _repository.SearchAsync(query.Normalise(), cancellationToken);

        return new PagedResult<MaintenanceRequestListItemDto>(
            page.Items.Select(item => item.ToListItem()).ToList(),
            page.Page,
            page.PageSize,
            page.TotalItems);
    }

    public async Task<MaintenanceRequestDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var request = await _repository.GetByIdWithHistoryAsync(id, cancellationToken)
            ?? throw new NotFoundException("la solicitud", id);

        return request.ToDetail();
    }

    public async Task<MaintenanceRequestDetailDto> ChangeStatusAsync(
        Guid id,
        ChangeStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var request = await _repository.GetByIdWithHistoryAsync(id, cancellationToken)
            ?? throw new NotFoundException("la solicitud", id);

        request.ChangeStatus(command.NewStatus, _currentActorProvider.GetCurrentActor(), _clock.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return request.ToDetail();
    }

    public async Task<MaintenanceRequestDetailDto> AssignResponsibleAsync(
        Guid id,
        AssignResponsibleCommand command,
        CancellationToken cancellationToken = default)
    {
        var request = await _repository.GetByIdWithHistoryAsync(id, cancellationToken)
            ?? throw new NotFoundException("la solicitud", id);

        Actor? responsible = null;
        if (command.ResponsibleId.HasValue)
        {
            responsible = _userDirectory.Find(command.ResponsibleId.Value)
                ?? throw ValidationFailureFor(
                    nameof(command.ResponsibleId),
                    "El responsable indicado no existe en el catálogo de usuarios.");
        }

        request.AssignResponsible(responsible, _currentActorProvider.GetCurrentActor(), _clock.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return request.ToDetail();
    }

    public async Task<RequestSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var counts = await _repository.CountByStatusAsync(cancellationToken);

        int CountOf(RequestStatus status) => counts.TryGetValue(status, out var value) ? value : 0;

        return new RequestSummaryDto(
            counts.Values.Sum(),
            CountOf(RequestStatus.Pending),
            CountOf(RequestStatus.InProgress),
            CountOf(RequestStatus.OnHold),
            CountOf(RequestStatus.Resolved),
            CountOf(RequestStatus.Cancelled));
    }

    private static ValidationException ValidationFailureFor(string field, string message) =>
        new(new[] { new ValidationFailure(field, message) });
}
