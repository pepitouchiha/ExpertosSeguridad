using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpertosSeguridad.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserActivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Editado a mano: EF genera defaultValue: false para un bool nuevo no anulable, lo que habría
            // desactivado todas las cuentas existentes en el momento de aplicar la migración. Los usuarios
            // existentes deben seguir activos.
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "users");
        }
    }
}
