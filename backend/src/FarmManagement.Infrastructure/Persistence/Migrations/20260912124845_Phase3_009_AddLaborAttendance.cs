using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_009_AddLaborAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "labor_attendance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    farm_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attendance_date = table.Column<DateOnly>(type: "date", nullable: false),
                    attendance_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    working_hours = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    calculated_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    calculated_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    currency_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    finalized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finalized_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_labor_attendance", x => x.id);
                    table.CheckConstraint("ck_labor_attendance_amounts", "(calculated_rate IS NULL OR calculated_rate >= 0) AND (calculated_amount IS NULL OR calculated_amount >= 0)");
                    table.CheckConstraint("ck_labor_attendance_finalized", "status != 'FINALIZED' OR (finalized_at IS NOT NULL AND finalized_by IS NOT NULL)");
                    table.CheckConstraint("ck_labor_attendance_not_worked_earnings", "attendance_type != 'NOT_WORKED' OR calculated_amount = 0 OR calculated_amount IS NULL");
                    table.CheckConstraint("ck_labor_attendance_status", "status IN ('DRAFT', 'FINALIZED')");
                    table.CheckConstraint("ck_labor_attendance_type", "attendance_type IN ('FULL_DAY', 'HALF_DAY', 'HOURLY', 'NOT_WORKED')");
                    table.CheckConstraint("ck_labor_attendance_working_hours", "(attendance_type = 'HOURLY' AND working_hours IS NOT NULL AND working_hours > 0) OR (attendance_type != 'HOURLY' AND working_hours IS NULL)");
                    table.ForeignKey(
                        name: "fk_labor_attendance_currency",
                        column: x => x.currency_id,
                        principalTable: "currencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_labor_attendance_farm",
                        column: x => x.farm_id,
                        principalTable: "farms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_labor_attendance_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_labor_attendance_worker",
                        column: x => x.worker_id,
                        principalTable: "workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_labor_attendance_currency_id",
                table: "labor_attendance",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "ix_labor_attendance_farm_attendance_date",
                table: "labor_attendance",
                columns: new[] { "farm_id", "attendance_date" });

            migrationBuilder.CreateIndex(
                name: "ix_labor_attendance_org_attendance_date",
                table: "labor_attendance",
                columns: new[] { "organization_id", "attendance_date" });

            migrationBuilder.CreateIndex(
                name: "ix_labor_attendance_org_farm_attendance_date",
                table: "labor_attendance",
                columns: new[] { "organization_id", "farm_id", "attendance_date" });

            migrationBuilder.CreateIndex(
                name: "ix_labor_attendance_organization_id",
                table: "labor_attendance",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_labor_attendance_status",
                table: "labor_attendance",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_labor_attendance_worker_id",
                table: "labor_attendance",
                column: "worker_id");

            migrationBuilder.CreateIndex(
                name: "ix_labor_attendance_org_worker_attendance_date",
                table: "labor_attendance",
                columns: new[] { "organization_id", "worker_id", "attendance_date" });

            migrationBuilder.CreateIndex(
                name: "ux_labor_attendance_org_worker_date_payroll",
                table: "labor_attendance",
                columns: new[] { "organization_id", "worker_id", "attendance_date" },
                unique: true,
                filter: "attendance_type != 'NOT_WORKED'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "labor_attendance");
        }
    }
}
