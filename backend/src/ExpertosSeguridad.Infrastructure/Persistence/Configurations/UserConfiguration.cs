using ExpertosSeguridad.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpertosSeguridad.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        // Como en los otros agregados, el identificador se asigna en la fábrica.
        builder.Property(user => user.Id).ValueGeneratedNever();

        builder.Property(user => user.FullName)
            .HasMaxLength(User.NameMaxLength)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasMaxLength(User.EmailMaxLength)
            .IsRequired();

        // El hash es largo y variable; la columna está dimensionada para el formato PBKDF2
        // ("iteraciones.sal.clave" en base64), con margen para un cambio futuro de algoritmo.
        builder.Property(user => user.PasswordHash)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(user => user.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(user => user.CreatedAt).IsRequired();

        // Aquí no hay HasDefaultValue a propósito: en un bool, EF interpreta el valor por defecto de
        // C# (false) como «sin asignar» e insertaría el valor por defecto de la base, así que una cuenta
        // creada desactivada aparecería activa. La migración que agrega la columna fija el valor por
        // defecto para las filas que ya existían, que es el único lugar donde hace falta.
        builder.Property(user => user.IsActive).IsRequired();

        // El login busca al usuario por correo en cada inicio de sesión, y dos cuentas nunca deben
        // compartirlo. La entidad guarda el valor en minúsculas, así que basta un índice único simple
        // y la búsqueda puede usarlo.
        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("ix_users_email");

        // El selector de responsable lista solo al personal, y el panel de administración filtra por rol.
        builder.HasIndex(user => user.Role)
            .HasDatabaseName("ix_users_role");
    }
}
