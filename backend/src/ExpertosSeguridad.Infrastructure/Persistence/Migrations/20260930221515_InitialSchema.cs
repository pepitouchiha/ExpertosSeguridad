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
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

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
                    table.ForeignKey(
                        name: "FK_maintenance_requests_users_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_requests_users_ResponsibleId",
                        column: x => x.ResponsibleId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    table.ForeignKey(
                        name: "FK_request_history_entries_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "ix_maintenance_requests_requester_id_created_at",
                table: "maintenance_requests",
                columns: new[] { "RequesterId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_requests_responsible_id",
                table: "maintenance_requests",
                column: "ResponsibleId");

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_requests_status_created_at",
                table: "maintenance_requests",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_request_history_entries_actor_id",
                table: "request_history_entries",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "ix_request_history_entries_request_id_occurred_at",
                table: "request_history_entries",
                columns: new[] { "RequestId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_role",
                table: "users",
                column: "Role");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "request_history_entries");

            migrationBuilder.DropTable(
                name: "maintenance_requests");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
