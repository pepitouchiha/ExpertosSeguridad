using ExpertosSeguridad.Domain.Entities;
using ExpertosSeguridad.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpertosSeguridad.Infrastructure.Persistence.Configurations;

public sealed class MaintenanceRequestConfiguration : IEntityTypeConfiguration<MaintenanceRequest>
{
    public void Configure(EntityTypeBuilder<MaintenanceRequest> builder)
    {
        builder.ToTable("maintenance_requests");

        builder.HasKey(request => request.Id);

        // El agregado asigna su propio identificador en la fábrica, así que la clave nunca la genera
        // la base de datos. Ver la nota en RequestHistoryEntryConfiguration.
        builder.Property(request => request.Id).ValueGeneratedNever();

        // El número legible es el único valor que asigna la base de datos y no el dominio: una columna
        // de identidad es la única forma de repartir números secuenciales sin duplicados cuando dos
        // inserciones simultáneas leerían por su cuenta «el último número».
        builder.Property(request => request.Number)
            .UseIdentityByDefaultColumn()
            .ValueGeneratedOnAdd();

        builder.HasIndex(request => request.Number)
            .IsUnique()
            .HasDatabaseName("ix_maintenance_requests_number");

        builder.Property(request => request.Title)
            .HasMaxLength(MaintenanceRequest.TitleMaxLength)
            .IsRequired();

        // La respuesta vive en la fila de la propia solicitud: no tiene identidad ni vida fuera de la
        // solicitud que resuelve. Las columnas son anulables porque solo existe una vez resuelta.
        builder.OwnsOne(request => request.Resolution, resolution =>
        {
            resolution.Property(value => value.Title)
                .HasColumnName("ResolutionTitle")
                .HasMaxLength(Resolution.TitleMaxLength);

            resolution.Property(value => value.Description)
                .HasColumnName("ResolutionDescription")
                .HasMaxLength(Resolution.DescriptionMaxLength);

            resolution.Property(value => value.RespondedById).HasColumnName("ResolvedById");

            resolution.Property(value => value.RespondedByName)
                .HasColumnName("ResolvedByName")
                .HasMaxLength(120);

            resolution.Property(value => value.RespondedAt).HasColumnName("ResolvedAt");

            // Quien respondió debe ser un usuario real, y borrarlo no debe borrar la respuesta.
            resolution.HasOne<User>()
                .WithMany()
                .HasForeignKey(value => value.RespondedById)
                .OnDelete(DeleteBehavior.Restrict);

            resolution.HasIndex(value => value.RespondedById)
                .HasDatabaseName("ix_maintenance_requests_resolved_by_id");
        });

        builder.Property(request => request.Description)
            .HasMaxLength(MaintenanceRequest.DescriptionMaxLength)
            .IsRequired();

        // Los enums se guardan como texto: los datos se leen bien en SQL y agregar un miembro después
        // no puede reasignar en silencio las filas existentes, como pasaría guardando el ordinal.
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

        // Claves foráneas declaradas sin propiedades de navegación a propósito: la solicitud es su
        // propia raíz de agregado y no debe tener referencias al agregado de usuario. La base de datos
        // igual garantiza que una solicitud apunte a usuarios existentes, y Restrict convierte borrar un
        // usuario con historial en un acto deliberado en lugar de una cascada silenciosa.
        //
        // RequesterName y ResponsibleName se mantienen desnormalizados: son la copia del nombre en el
        // momento del evento, así que renombrar a un usuario nunca reescribe el historial registrado.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(request => request.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(request => request.ResponsibleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Al historial solo se llega por la raíz del agregado, así que la colección se mapea al campo
        // privado y las entradas se borran en cascada con su solicitud.
        builder.HasMany(request => request.History)
            .WithOne()
            .HasForeignKey(entry => entry.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(request => request.History)
            .HasField("_history")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Ordenar por fecha de creación es el valor por defecto del listado, y cada filtro acota esa
        // misma consulta, así que la columna de orden encabeza los índices compuestos.
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

        // El listado de un solicitante siempre filtra por dueño y ordena por fecha, así que el par se
        // indexa junto en lugar de depender solo de la columna del solicitante.
        builder.HasIndex(request => new { request.RequesterId, request.CreatedAt })
            .HasDatabaseName("ix_maintenance_requests_requester_id_created_at");
    }
}
