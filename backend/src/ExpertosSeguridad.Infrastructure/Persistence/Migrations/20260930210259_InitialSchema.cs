using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpertosSeguridad.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "maintenance_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ResponsibleId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResponsibleName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "request_history_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PreviousValue = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    NewValue = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_history_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_request_history_entries_maintenance_requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "maintenance_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_requests_category_created_at",
                table: "maintenance_requests",
                columns: new[] { "Category", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_requests_created_at",
                table: "maintenance_requests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_requests_priority_created_at",
                table: "maintenance_requests",
                columns: new[] { "Priority", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_requests_responsible_id",
                table: "maintenance_requests",
                column: "ResponsibleId");

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_requests_status_created_at",
                table: "maintenance_requests",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_request_history_entries_request_id_occurred_at",
                table: "request_history_entries",
                columns: new[] { "RequestId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "request_history_entries");

            migrationBuilder.DropTable(
                name: "maintenance_requests");
        }
    }
}
