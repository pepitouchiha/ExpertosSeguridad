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

        // El identificador lo asigna el dominio, no la base de datos. Declararlo explícitamente es lo
        // que hace que EF trate una entrada nueva como inserción y no como actualización: con la
        // convención por defecto, una clave Guid no vacía se interpreta como «esta fila ya existe».
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

        // El actor de cada evento registrado debe ser un usuario real. Restrict, no cascada: borrar un
        // usuario nunca puede eliminar en silencio el rastro de auditoría que produjo.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entry => entry.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        // La vista de detalle siempre lee el historial de una sola solicitud en orden cronológico.
        builder.HasIndex(entry => new { entry.RequestId, entry.OccurredAt })
            .HasDatabaseName("ix_request_history_entries_request_id_occurred_at");

        // Se declara explícitamente solo para mantener la convención de nombres: la clave foránea del
        // actor necesita un índice de apoyo y EF crearía uno con su propio nombre por defecto.
        builder.HasIndex(entry => entry.ActorId)
            .HasDatabaseName("ix_request_history_entries_actor_id");
    }
}
