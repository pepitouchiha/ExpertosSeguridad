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
    public void AssigningAPendingRequest_StartsIt_AndRecordsBothEventsInOrder()
    {
        var request = NewRequest();
        var occurredAt = Now.AddHours(1);

        request.AssignResponsible(Technician, Supervisor, occurredAt);

        request.ResponsibleId.Should().Be(Technician.Id);
        request.ResponsibleName.Should().Be(Technician.Name);
        request.Status.Should().Be(RequestStatus.InProgress);
        request.UpdatedAt.Should().Be(occurredAt);

        // Creada, luego la asignación y luego el inicio que provocó: mismo actor, mismo instante.
        request.History.Should().HaveCount(3);
        request.History.Skip(1).Should().BeEquivalentTo(
            new object[]
            {
                new
                {
                    EventType = HistoryEventType.ResponsibleChanged,
                    PreviousValue = (string?)null,
                    NewValue = Technician.Name,
                    ActorId = Supervisor.Id,
                    OccurredAt = occurredAt
                },
                new
                {
                    EventType = HistoryEventType.StatusChanged,
                    PreviousValue = nameof(RequestStatus.Pending),
                    NewValue = nameof(RequestStatus.InProgress),
                    ActorId = Supervisor.Id,
                    OccurredAt = occurredAt
                }
            },
            options => options.WithStrictOrdering());
    }

    [Fact]
    public void Reassigning_RecordsPreviousAndNewResponsible_WithoutAnotherStatusChange()
    {
        var request = NewRequest();
        request.AssignResponsible(Technician, Supervisor, Now.AddHours(1));
        var historyBefore = request.History.Count;

        request.AssignResponsible(OtherTechnician, Supervisor, Now.AddHours(2));

        request.ResponsibleId.Should().Be(OtherTechnician.Id);
        request.Status.Should().Be(RequestStatus.InProgress);
        request.History.Should().HaveCount(historyBefore + 1);
        request.History.Last().Should().BeEquivalentTo(new
        {
            EventType = HistoryEventType.ResponsibleChanged,
            PreviousValue = Technician.Name,
            NewValue = OtherTechnician.Name,
            ActorId = Supervisor.Id
        });
    }

    [Fact]
    public void ReassigningARequestOnHold_KeepsItOnHold()
    {
        var request = NewRequest();
        request.AssignResponsible(Technician, Supervisor, Now.AddHours(1));
        request.ChangeStatus(RequestStatus.OnHold, Technician, Now.AddHours(2));

        request.AssignResponsible(OtherTechnician, Supervisor, Now.AddHours(3));

        // Solo una solicitud pendiente se inicia al asignarla; pausar es decisión del responsable y un
        // cambio de manos no lo deshace.
        request.Status.Should().Be(RequestStatus.OnHold);
    }

    [Fact]
    public void TheNewResponsible_TakesOverTheExecutionTransitions()
    {
        var request = NewRequest();
        request.AssignResponsible(Technician, Supervisor, Now.AddHours(1));
        request.AssignResponsible(OtherTechnician, Supervisor, Now.AddHours(2));

        var byFormerResponsible = () => request.ChangeStatus(RequestStatus.Resolved, Technician, Now.AddHours(3));
        byFormerResponsible.Should().Throw<ForbiddenOperationException>();

        request.Resolve("Trabajo terminado", "Se completó la reparación solicitada.", OtherTechnician, Now.AddHours(3));
        request.Status.Should().Be(RequestStatus.Resolved);
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

    [Fact]
    public void AssignResponsible_WithoutAResponsible_IsRejected()
    {
        // Una solicitud iniciada no puede volver a Pending, así que nunca puede quedar sin responsable.
        var request = NewRequest();

        var act = () => request.AssignResponsible(null!, Supervisor, Now.AddHours(1));

        act.Should().Throw<ArgumentNullException>();
        request.History.Should().ContainSingle();
    }

    [Theory]
    [InlineData(RequestStatus.Resolved)]
    [InlineData(RequestStatus.Cancelled)]
    public void AssignResponsible_OnTerminalRequest_IsRejected(RequestStatus terminalStatus)
    {
        var request = NewRequest();

        if (terminalStatus == RequestStatus.Resolved)
        {
            request.AssignResponsible(Technician, Supervisor, Now.AddHours(1));
            request.Resolve("Trabajo terminado", "Se completó la reparación solicitada.", Technician, Now.AddHours(2));
        }
        else
        {
            request.ChangeStatus(RequestStatus.Cancelled, Supervisor, Now.AddHours(1));
        }

        var act = () => request.AssignResponsible(OtherTechnician, Supervisor, Now.AddHours(3));

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
