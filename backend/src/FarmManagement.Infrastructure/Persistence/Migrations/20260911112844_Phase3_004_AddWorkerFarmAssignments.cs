using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_004_AddWorkerFarmAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "worker_farm_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    farm_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_from = table.Column<DateOnly>(type: "date", nullable: false),
                    assigned_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worker_farm_assignments", x => x.id);
                    table.CheckConstraint("ck_worker_farm_assignments_dates", "assigned_to IS NULL OR assigned_to >= assigned_from");
                    table.ForeignKey(
                        name: "fk_worker_farm_assignments_farm",
                        column: x => x.farm_id,
                        principalTable: "farms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_worker_farm_assignments_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_worker_farm_assignments_worker",
                        column: x => x.worker_id,
                        principalTable: "workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_worker_farm_assignments_farm_id",
                table: "worker_farm_assignments",
                column: "farm_id");

            migrationBuilder.CreateIndex(
                name: "ix_worker_farm_assignments_org_farm",
                table: "worker_farm_assignments",
                columns: new[] { "organization_id", "farm_id" });

            migrationBuilder.CreateIndex(
                name: "ix_worker_farm_assignments_org_farm_active",
                table: "worker_farm_assignments",
                columns: new[] { "organization_id", "farm_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_worker_farm_assignments_org_worker",
                table: "worker_farm_assignments",
                columns: new[] { "organization_id", "worker_id" });

            migrationBuilder.CreateIndex(
                name: "ix_worker_farm_assignments_org_worker_active",
                table: "worker_farm_assignments",
                columns: new[] { "organization_id", "worker_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_worker_farm_assignments_organization_id",
                table: "worker_farm_assignments",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_worker_farm_assignments_worker_id",
                table: "worker_farm_assignments",
                column: "worker_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "worker_farm_assignments");
        }
    }
}
