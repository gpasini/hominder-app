using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Hominder.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "household_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_household_members", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Policy = table.Column<string>(type: "jsonb", nullable: false),
                    AssigneeId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_tasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_task_completions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompletedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    CompletedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenanceTaskId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_task_completions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maintenance_task_completions_maintenance_tasks_MaintenanceT~",
                        column: x => x.MaintenanceTaskId,
                        principalTable: "maintenance_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_task_completions_MaintenanceTaskId",
                table: "maintenance_task_completions",
                column: "MaintenanceTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "household_members");

            migrationBuilder.DropTable(
                name: "maintenance_task_completions");

            migrationBuilder.DropTable(
                name: "maintenance_tasks");
        }
    }
}
