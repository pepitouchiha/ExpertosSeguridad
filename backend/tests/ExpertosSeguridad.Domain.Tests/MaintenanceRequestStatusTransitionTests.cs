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
    private static readonly Actor Operator = new(Guid.NewGuid(), "Carlos Ruiz");
    private static readonly DateTimeOffset Now = new(2026, 3, 5, 10, 0, 0, TimeSpan.Zero);

    public static TheoryData<RequestStatus, RequestStatus> AllowedTransitions() => new()
    {
        { RequestStatus.Pending, RequestStatus.InProgress },
        { RequestStatus.Pending, RequestStatus.Cancelled },
        { RequestStatus.InProgress, RequestStatus.OnHold },
        { RequestStatus.InProgress, RequestStatus.Resolved },
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

    /// <summary>
    /// Builds a request in the requested status by walking the real transitions, so the
    /// fixture can never reach a state the domain would not allow.
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
                request.ChangeStatus(RequestStatus.InProgress, Operator, Now.AddHours(1));
                break;
            case RequestStatus.OnHold:
                request.ChangeStatus(RequestStatus.InProgress, Operator, Now.AddHours(1));
                request.ChangeStatus(RequestStatus.OnHold, Operator, Now.AddHours(2));
                break;
            case RequestStatus.Resolved:
                request.ChangeStatus(RequestStatus.InProgress, Operator, Now.AddHours(1));
                request.ChangeStatus(RequestStatus.Resolved, Operator, Now.AddHours(2));
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
