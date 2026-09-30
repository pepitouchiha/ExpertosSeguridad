using ExpertosSeguridad.Domain.Entities;
using ExpertosSeguridad.Domain.Enums;
using ExpertosSeguridad.Domain.Exceptions;
using ExpertosSeguridad.Domain.ValueObjects;
using FluentAssertions;

namespace ExpertosSeguridad.Domain.Tests;

public class MaintenanceRequestCreationTests
{
    private static readonly Actor Requester = new(Guid.NewGuid(), "Ana Torres");
    private static readonly DateTimeOffset Now = new(2026, 3, 5, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidData_StartsAsPendingAndRecordsHistory()
    {
        var request = MaintenanceRequest.Create(
            "Fuga de agua en el baño",
            "Se detectó una fuga bajo el lavamanos que humedeció la pared.",
            RequestCategory.Infrastructure,
            RequestPriority.High,
            Requester,
            Now);

        request.Status.Should().Be(RequestStatus.Pending);
        request.CreatedAt.Should().Be(Now);
        request.UpdatedAt.Should().Be(Now);
        request.ResponsibleId.Should().BeNull();
        request.RequesterId.Should().Be(Requester.Id);

        request.History.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                EventType = HistoryEventType.Created,
                PreviousValue = (string?)null,
                NewValue = nameof(RequestStatus.Pending),
                ActorId = Requester.Id,
                OccurredAt = Now
            });
    }

    [Fact]
    public void Create_TrimsSurroundingWhitespace()
    {
        var request = MaintenanceRequest.Create(
            "   Cambio de luminarias   ",
            "   Ocho luminarias del parqueadero están fundidas.   ",
            RequestCategory.Infrastructure,
            RequestPriority.Low,
            Requester,
            Now);

        request.Title.Should().Be("Cambio de luminarias");
        request.Description.Should().Be("Ocho luminarias del parqueadero están fundidas.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abcd")]
    public void Create_WithInvalidTitle_IsRejected(string title)
    {
        var act = () => MaintenanceRequest.Create(
            title,
            "Descripción suficientemente larga para ser válida.",
            RequestCategory.Software,
            RequestPriority.Medium,
            Requester,
            Now);

        act.Should().Throw<DomainValidationException>()
            .Which.Field.Should().Be("Title");
    }

    [Fact]
    public void Create_WithTitleOverMaxLength_IsRejected()
    {
        var act = () => MaintenanceRequest.Create(
            new string('a', MaintenanceRequest.TitleMaxLength + 1),
            "Descripción suficientemente larga para ser válida.",
            RequestCategory.Software,
            RequestPriority.Medium,
            Requester,
            Now);

        act.Should().Throw<DomainValidationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("corta")]
    public void Create_WithInvalidDescription_IsRejected(string description)
    {
        var act = () => MaintenanceRequest.Create(
            "Título válido",
            description,
            RequestCategory.Equipment,
            RequestPriority.Critical,
            Requester,
            Now);

        act.Should().Throw<DomainValidationException>()
            .Which.Field.Should().Be("Description");
    }

    [Fact]
    public void Create_WithUndefinedCategory_IsRejected()
    {
        var act = () => MaintenanceRequest.Create(
            "Título válido",
            "Descripción suficientemente larga para ser válida.",
            (RequestCategory)99,
            RequestPriority.Low,
            Requester,
            Now);

        act.Should().Throw<DomainValidationException>();
    }
}
