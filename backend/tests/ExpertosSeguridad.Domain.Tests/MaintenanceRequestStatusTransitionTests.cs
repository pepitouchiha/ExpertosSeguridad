using ExpertosSeguridad.Domain.Entities;
using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Domain.Exceptions;
using ExpertosSeguridad.Domain.Policies;
using ExpertosSeguridad.Domain.ValueObjects;
using FluentAssertions;

namespace ExpertosSeguridad.Domain.Tests;

public class MaintenanceRequestStatusTransitionTests
{
    private static readonly Actor Requester = new(Guid.NewGuid(), "Ana Torres");
    /// <summary>El responsable asignado: el único que puede pausar, reanudar o resolver.</summary>
    private static readonly Actor Operator = new(Guid.NewGuid(), "Diana López");

    /// <summary>Otro miembro del personal: puede asignar y cancelar, no ejecutar.</summary>
    private static readonly Actor Coordinator = new(Guid.NewGuid(), "Esteban Mora");
    private static readonly DateTimeOffset Now = new(2026, 3, 5, 10, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Cada transición de la tabla que se toma a pedido. Pending → InProgress no está porque ya no
    /// se pide: la hace la asignación de un responsable (ver MaintenanceRequestAssignmentTests).
    /// </summary>
    public static TheoryData<RequestStatus, RequestStatus> AllowedTransitions() => new()
    {
        { RequestStatus.Pending, RequestStatus.Cancelled },
        { RequestStatus.InProgress, RequestStatus.OnHold },
        { RequestStatus.InProgress, RequestStatus.Cancelled },
        { RequestStatus.OnHold, RequestStatus.InProgress },
        { RequestStatus.OnHold, RequestStatus.Cancelled }
    };

    public static TheoryData<RequestStatus, RequestStatus> ForbiddenTransitions() => new()
    {
        { RequestStatus.Pending, RequestStatus.OnHold },
        { RequestStatus.Pending, RequestStatus.Resolved },
        { RequestStatus.Pending, RequestStatus.Pending },
        { RequestStatus.InProgress, RequestStatus.Pending },
        { RequestStatus.InProgress, RequestStatus.InProgress },
        { RequestStatus.OnHold, RequestStatus.Pending },
        { RequestStatus.OnHold, RequestStatus.Resolved },
        { RequestStatus.Resolved, RequestStatus.InProgress },
        { RequestStatus.Resolved, RequestStatus.Cancelled },
        { RequestStatus.Cancelled, RequestStatus.InProgress },
        { RequestStatus.Cancelled, RequestStatus.Pending }
    };

    [Theory]
    [MemberData(nameof(AllowedTransitions))]
    public void ChangeStatus_WithAllowedTransition_AppliesItAndRecordsHistory(RequestStatus from, RequestStatus to)
    {
        var request = RequestInStatus(from);
        var historyBefore = request.History.Count;
        var occurredAt = Now.AddHours(5);

        request.ChangeStatus(to, Operator, occurredAt);

        request.Status.Should().Be(to);
        request.UpdatedAt.Should().Be(occurredAt);

        request.History.Should().HaveCount(historyBefore + 1);
        request.History.Last().Should().BeEquivalentTo(new
        {
            EventType = HistoryEventType.StatusChanged,
            PreviousValue = from.ToString(),
            NewValue = to.ToString(),
            ActorId = Operator.Id,
            ActorName = Operator.Name,
            OccurredAt = occurredAt
        });
    }

    [Theory]
    [MemberData(nameof(ForbiddenTransitions))]
    public void ChangeStatus_WithForbiddenTransition_IsRejectedAndLeavesStateUntouched(
        RequestStatus from,
        RequestStatus to)
    {
        var request = RequestInStatus(from);
        var historyBefore = request.History.Count;

        var act = () => request.ChangeStatus(to, Operator, Now.AddHours(5));

        act.Should().Throw<InvalidStatusTransitionException>();
        request.Status.Should().Be(from);
        request.History.Should().HaveCount(historyBefore);
    }

    [Fact]
    public void ChangeStatus_ToUndefinedStatus_IsRejected()
    {
        var request = RequestInStatus(RequestStatus.Pending);

        var act = () => request.ChangeStatus((RequestStatus)42, Operator, Now);

        act.Should().Throw<DomainValidationException>();
    }

    [Theory]
    [InlineData(RequestStatus.Resolved)]
    [InlineData(RequestStatus.Cancelled)]
    public void TerminalStatuses_HaveNoOutgoingTransitions(RequestStatus status)
    {
        RequestStatusTransitionPolicy.AllowedFrom(status).Should().BeEmpty();
        RequestStatusTransitionPolicy.IsTerminal(status).Should().BeTrue();
    }

    [Fact]
    public void AllowedNextStatuses_ReflectsThePolicyForTheCurrentStatus()
    {
        var request = RequestInStatus(RequestStatus.InProgress);

        request.AllowedNextStatuses.Should().BeEquivalentTo(new[]
        {
            RequestStatus.OnHold,
            RequestStatus.Resolved,
            RequestStatus.Cancelled
        });
    }

    [Fact]
    public void AllowedNextStatuses_OfAPendingRequest_IsOnlyCancellation_BecauseAssigningStartsIt()
    {
        var request = RequestInStatus(RequestStatus.Pending);

        request.AllowedNextStatuses.Should().Equal(RequestStatus.Cancelled);
    }

    [Fact]
    public void StartingARequestWithoutResponsible_IsRejectedAsAConflict()
    {
        var request = RequestInStatus(RequestStatus.Pending);

        var act = () => request.ChangeStatus(RequestStatus.InProgress, Operator, Now.AddHours(1));

        act.Should().Throw<InvalidStatusTransitionException>();
        request.Status.Should().Be(RequestStatus.Pending);
        request.History.Should().ContainSingle();
    }

    [Theory]
    [InlineData(RequestStatus.InProgress, RequestStatus.Resolved)]
    [InlineData(RequestStatus.InProgress, RequestStatus.OnHold)]
    [InlineData(RequestStatus.OnHold, RequestStatus.InProgress)]
    public void ExecutionTransitions_BySomeoneOtherThanTheResponsible_AreForbidden(
        RequestStatus from,
        RequestStatus to)
    {
        var request = RequestInStatus(from);
        var historyBefore = request.History.Count;

        var act = () => request.ChangeStatus(to, Coordinator, Now.AddHours(5));

        act.Should().Throw<ForbiddenOperationException>();
        request.Status.Should().Be(from);
        request.History.Should().HaveCount(historyBefore);
    }

    [Theory]
    [InlineData(RequestStatus.Pending)]
    [InlineData(RequestStatus.InProgress)]
    [InlineData(RequestStatus.OnHold)]
    public void Cancelling_IsACoordinationDecision_AnyStaffMemberCanTakeIt(RequestStatus from)
    {
        var request = RequestInStatus(from);

        request.ChangeStatus(RequestStatus.Cancelled, Coordinator, Now.AddHours(5));

        request.Status.Should().Be(RequestStatus.Cancelled);
        request.History.Last().ActorId.Should().Be(Coordinator.Id);
    }

    [Fact]
    public void Resolve_ByTheResponsible_ClosesTheRequestWithTheAnswer()
    {
        var request = RequestInStatus(RequestStatus.InProgress);
        var occurredAt = Now.AddHours(5);

        request.Resolve("  Compresor reemplazado  ", "Se cambió el compresor y la sala volvió a 21 °C.", Operator, occurredAt);

        request.Status.Should().Be(RequestStatus.Resolved);
        request.Resolution.Should().BeEquivalentTo(new
        {
            Title = "Compresor reemplazado",
            Description = "Se cambió el compresor y la sala volvió a 21 °C.",
            RespondedById = Operator.Id,
            RespondedByName = Operator.Name,
            RespondedAt = occurredAt
        });
        request.History.Last().NewValue.Should().Be(nameof(RequestStatus.Resolved));
    }

    [Fact]
    public void ResolvingThroughAPlainStatusChange_IsRejected_BecauseItNeedsAnAnswer()
    {
        var request = RequestInStatus(RequestStatus.InProgress);
        var historyBefore = request.History.Count;

        var act = () => request.ChangeStatus(RequestStatus.Resolved, Operator, Now.AddHours(5));

        act.Should().Throw<DomainValidationException>();
        request.Status.Should().Be(RequestStatus.InProgress);
        request.Resolution.Should().BeNull();
        request.History.Should().HaveCount(historyBefore);
    }

    [Theory]
    [InlineData("", "Descripción suficientemente larga.")]
    [InlineData("Ok", "Descripción suficientemente larga.")]
    [InlineData("Título válido", "corta")]
    public void Resolve_WithAnInvalidAnswer_IsRejectedAndLeavesTheRequestOpen(string title, string description)
    {
        var request = RequestInStatus(RequestStatus.InProgress);

        var act = () => request.Resolve(title, description, Operator, Now.AddHours(5));

        act.Should().Throw<DomainValidationException>();
        request.Status.Should().Be(RequestStatus.InProgress);
        request.Resolution.Should().BeNull();
    }

    [Fact]
    public void Resolve_BySomeoneOtherThanTheResponsible_IsForbidden()
    {
        var request = RequestInStatus(RequestStatus.InProgress);

        var act = () => request.Resolve("Trabajo terminado", "Se completó la reparación solicitada.", Coordinator, Now.AddHours(5));

        act.Should().Throw<ForbiddenOperationException>();
        request.Resolution.Should().BeNull();
    }

    [Fact]
    public void Resolve_FromOnHold_IsNotAContemplatedTransition()
    {
        var request = RequestInStatus(RequestStatus.OnHold);

        var act = () => request.Resolve("Trabajo terminado", "Se completó la reparación solicitada.", Operator, Now.AddHours(5));

        act.Should().Throw<InvalidStatusTransitionException>();
    }

    [Fact]
    public void AnImpossibleTransition_IsAConflictEvenForTheResponsible()
    {
        // La tabla se verifica antes que el actor: un movimiento que no existe es 409 para todos, y
        // solo los movimientos posibles llegan a la pregunta «¿es usted el responsable?».
        var request = RequestInStatus(RequestStatus.Resolved);

        var act = () => request.ChangeStatus(RequestStatus.InProgress, Coordinator, Now.AddHours(5));

        act.Should().Throw<InvalidStatusTransitionException>();
    }

    /// <summary>
    /// Construye una solicitud en el estado pedido recorriendo el flujo real —un coordinador asigna a
    /// <see cref="Operator"/>, lo que la inicia, y el operador hace el resto—, así que el fixture
    /// nunca puede llegar a un estado que el dominio no permitiría.
    /// </summary>
    private static MaintenanceRequest RequestInStatus(RequestStatus status)
    {
        var request = MaintenanceRequest.Create(
            "Título de prueba",
            "Descripción de prueba suficientemente larga.",
            RequestCategory.Equipment,
            RequestPriority.Medium,
            Requester,
            Now);

        switch (status)
        {
            case RequestStatus.Pending:
                break;
            case RequestStatus.InProgress:
                request.AssignResponsible(Operator, Coordinator, Now.AddHours(1));
                break;
            case RequestStatus.OnHold:
                request.AssignResponsible(Operator, Coordinator, Now.AddHours(1));
                request.ChangeStatus(RequestStatus.OnHold, Operator, Now.AddHours(2));
                break;
            case RequestStatus.Resolved:
                request.AssignResponsible(Operator, Coordinator, Now.AddHours(1));
                request.Resolve("Trabajo terminado", "Se completó la reparación solicitada.", Operator, Now.AddHours(2));
                break;
            case RequestStatus.Cancelled:
                request.ChangeStatus(RequestStatus.Cancelled, Operator, Now.AddHours(1));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Estado no contemplado en el fixture.");
        }

        return request;
    }
}
