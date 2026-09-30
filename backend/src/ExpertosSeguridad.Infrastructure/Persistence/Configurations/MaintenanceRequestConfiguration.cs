using ExpertosSeguridad.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpertosSeguridad.Infrastructure.Persistence.Configurations;

public sealed class MaintenanceRequestConfiguration : IEntityTypeConfiguration<MaintenanceRequest>
{
    public void Configure(EntityTypeBuilder<MaintenanceRequest> builder)
    {
        builder.ToTable("maintenance_requests");

        builder.HasKey(request => request.Id);

        // The aggregate assigns its own identifier in the factory, so the key is never
        // store-generated. See the note in RequestHistoryEntryConfiguration.
        builder.Property(request => request.Id).ValueGeneratedNever();

        builder.Property(request => request.Title)
            .HasMaxLength(MaintenanceRequest.TitleMaxLength)
            .IsRequired();

        builder.Property(request => request.Description)
            .HasMaxLength(MaintenanceRequest.DescriptionMaxLength)
            .IsRequired();

        // Enums are persisted as text: the data stays readable in SQL and adding a member
        // later cannot silently re-map existing rows the way ordinal storage would.
        builder.Property(request => request.Category)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(request => request.Priority)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(request => request.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(request => request.RequesterId).IsRequired();

        builder.Property(request => request.RequesterName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(request => request.ResponsibleName).HasMaxLength(120);

        builder.Property(request => request.CreatedAt).IsRequired();
        builder.Property(request => request.UpdatedAt).IsRequired();

        // History is only reachable through the aggregate root, so the collection is mapped
        // to the backing field and the entries cascade with their request.
        builder.HasMany(request => request.History)
            .WithOne()
            .HasForeignKey(entry => entry.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(request => request.History)
            .HasField("_history")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Sorting by creation date is the list default, and every filter narrows that same
        // query, so the sort column leads the composite indexes.
        builder.HasIndex(request => request.CreatedAt)
            .HasDatabaseName("ix_maintenance_requests_created_at");

        builder.HasIndex(request => new { request.Status, request.CreatedAt })
            .HasDatabaseName("ix_maintenance_requests_status_created_at");

        builder.HasIndex(request => new { request.Priority, request.CreatedAt })
            .HasDatabaseName("ix_maintenance_requests_priority_created_at");

        builder.HasIndex(request => new { request.Category, request.CreatedAt })
            .HasDatabaseName("ix_maintenance_requests_category_created_at");

        builder.HasIndex(request => request.ResponsibleId)
            .HasDatabaseName("ix_maintenance_requests_responsible_id");
    }
}
