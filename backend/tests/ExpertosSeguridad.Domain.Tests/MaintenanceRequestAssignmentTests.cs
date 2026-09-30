using ExpertosSeguridad.Domain.Entities;
using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Domain.Exceptions;
using ExpertosSeguridad.Domain.ValueObjects;
using FluentAssertions;

namespace ExpertosSeguridad.Domain.Tests;

public class MaintenanceRequestAssignmentTests
{
    private static readonly Actor Requester = new(Guid.NewGuid(), "Ana Torres");
    private static readonly Actor Supervisor = new(Guid.NewGuid(), "Carlos Ruiz");
    private static readonly Actor Technician = new(Guid.NewGuid(), "Diana López");
    private static readonly Actor OtherTechnician = new(Guid.NewGuid(), "Esteban Mora");
    private static readonly DateTimeOffset Now = new(2026, 3, 5, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AssignResponsible_OnUnassignedRequest_RecordsHistoryWithNoPreviousValue()
    {
        var request = NewRequest();
        var occurredAt = Now.AddHours(1);

        request.AssignResponsible(Technician, Supervisor, occurredAt);

        request.ResponsibleId.Should().Be(Technician.Id);
        request.ResponsibleName.Should().Be(Technician.Name);
        request.UpdatedAt.Should().Be(occurredAt);

        request.History.Last().Should().BeEquivalentTo(new
        {
            EventType = HistoryEventType.ResponsibleChanged,
            PreviousValue = (string?)null,
            NewValue = Technician.Name,
            ActorId = Supervisor.Id,
            OccurredAt = occurredAt
        });
    }

    [Fact]
    public void AssignResponsible_Reassigning_RecordsPreviousAndNewResponsible()
    {
        var request = NewRequest();
        request.AssignResponsible(Technician, Supervisor, Now.AddHours(1));

        request.AssignResponsible(OtherTechnician, Supervisor, Now.AddHours(2));

        request.ResponsibleId.Should().Be(OtherTechnician.Id);
        request.History.Last().Should().BeEquivalentTo(new
        {
            EventType = HistoryEventType.ResponsibleChanged,
            PreviousValue = Technician.Name,
            NewValue = OtherTechnician.Name,
            ActorId = Supervisor.Id
        });
    }

    [Fact]
    public void AssignResponsible_WithNull_ClearsTheAssignmentAndRecordsIt()
    {
        var request = NewRequest();
        request.AssignResponsible(Technician, Supervisor, Now.AddHours(1));

        request.AssignResponsible(null, Supervisor, Now.AddHours(2));

        request.ResponsibleId.Should().BeNull();
        request.ResponsibleName.Should().BeNull();
        request.History.Last().NewValue.Should().BeNull();
        request.History.Last().PreviousValue.Should().Be(Technician.Name);
    }

    [Fact]
    public void AssignResponsible_WithTheSameResponsible_IsRejectedAndAddsNoHistory()
    {
        var request = NewRequest();
        request.AssignResponsible(Technician, Supervisor, Now.AddHours(1));
        var historyBefore = request.History.Count;

        var act = () => request.AssignResponsible(Technician, Supervisor, Now.AddHours(2));

        act.Should().Throw<DomainValidationException>();
        request.History.Should().HaveCount(historyBefore);
    }

    [Theory]
    [InlineData(RequestStatus.Resolved)]
    [InlineData(RequestStatus.Cancelled)]
    public void AssignResponsible_OnTerminalRequest_IsRejected(RequestStatus terminalStatus)
    {
        var request = NewRequest();

        if (terminalStatus == RequestStatus.Resolved)
        {
            request.ChangeStatus(RequestStatus.InProgress, Supervisor, Now.AddHours(1));
            request.ChangeStatus(RequestStatus.Resolved, Supervisor, Now.AddHours(2));
        }
        else
        {
            request.ChangeStatus(RequestStatus.Cancelled, Supervisor, Now.AddHours(1));
        }

        var act = () => request.AssignResponsible(Technician, Supervisor, Now.AddHours(3));

        act.Should().Throw<DomainValidationException>();
    }

    private static MaintenanceRequest NewRequest() => MaintenanceRequest.Create(
        "Título de prueba",
        "Descripción de prueba suficientemente larga.",
        RequestCategory.Equipment,
        RequestPriority.Medium,
        Requester,
        Now);
}
