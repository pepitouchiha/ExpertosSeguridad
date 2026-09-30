using ExpertosSeguridad.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpertosSeguridad.Infrastructure.Persistence.Configurations;

public sealed class RequestHistoryEntryConfiguration : IEntityTypeConfiguration<RequestHistoryEntry>
{
    public void Configure(EntityTypeBuilder<RequestHistoryEntry> builder)
    {
        builder.ToTable("request_history_entries");

        builder.HasKey(entry => entry.Id);

        // The identifier is assigned by the domain, not by the database. Saying so explicitly
        // is what makes EF track a new entry as an insert instead of an update: with the
        // default convention a non-empty Guid key reads as "this row already exists".
        builder.Property(entry => entry.Id).ValueGeneratedNever();

        builder.Property(entry => entry.EventType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(entry => entry.PreviousValue).HasMaxLength(120);
        builder.Property(entry => entry.NewValue).HasMaxLength(120);

        builder.Property(entry => entry.ActorId).IsRequired();

        builder.Property(entry => entry.ActorName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(entry => entry.OccurredAt).IsRequired();

        // The detail view always reads a single request's history in chronological order.
        builder.HasIndex(entry => new { entry.RequestId, entry.OccurredAt })
            .HasDatabaseName("ix_request_history_entries_request_id_occurred_at");
    }
}
