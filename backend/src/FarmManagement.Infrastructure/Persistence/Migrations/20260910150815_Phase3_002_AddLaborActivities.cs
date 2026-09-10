using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_002_AddLaborActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "labor_activities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activity_date = table.Column<DateOnly>(type: "date", nullable: false),
                    farm_id = table.Column<Guid>(type: "uuid", nullable: false),
                    farm_area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    plantation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    crop_cycle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    labor_activity_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    worker_count = table.Column<int>(type: "integer", nullable: false),
                    total_working_hours = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    cost_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    currency_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "COMPLETED"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_labor_activities", x => x.id);
                    table.CheckConstraint("ck_labor_activities_cost_amount", "cost_amount IS NULL OR cost_amount >= 0");
                    table.CheckConstraint("ck_labor_activities_status", "status IN ('DRAFT', 'COMPLETED', 'CANCELLED')");
                    table.CheckConstraint("ck_labor_activities_worker_count", "worker_count > 0");
                    table.CheckConstraint("ck_labor_activities_working_hours", "total_working_hours IS NULL OR total_working_hours > 0");
                    table.ForeignKey(
                        name: "fk_labor_activity_area",
                        column: x => x.farm_area_id,
                        principalTable: "farm_areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_labor_activity_cycle",
                        column: x => x.crop_cycle_id,
                        principalTable: "crop_cycles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_labor_activity_farm",
                        column: x => x.farm_id,
                        principalTable: "farms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_labor_activity_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_labor_activity_plantation",
                        column: x => x.plantation_id,
                        principalTable: "crop_plantations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_labor_activity_type",
                        column: x => x.labor_activity_type_id,
                        principalTable: "labor_activity_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_labor_activities_activity_date",
                table: "labor_activities",
                column: "activity_date");

            migrationBuilder.CreateIndex(
                name: "ix_labor_activities_crop_cycle",
                table: "labor_activities",
                column: "crop_cycle_id");

            migrationBuilder.CreateIndex(
                name: "ix_labor_activities_farm",
                table: "labor_activities",
                column: "farm_id");

            migrationBuilder.CreateIndex(
                name: "ix_labor_activities_farm_area",
                table: "labor_activities",
                column: "farm_area_id");

            migrationBuilder.CreateIndex(
                name: "ix_labor_activities_organization",
                table: "labor_activities",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_labor_activities_plantation",
                table: "labor_activities",
                column: "plantation_id");

            migrationBuilder.CreateIndex(
                name: "ix_labor_activities_status",
                table: "labor_activities",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_labor_activities_type",
                table: "labor_activities",
                column: "labor_activity_type_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "labor_activities");
        }
    }
}
