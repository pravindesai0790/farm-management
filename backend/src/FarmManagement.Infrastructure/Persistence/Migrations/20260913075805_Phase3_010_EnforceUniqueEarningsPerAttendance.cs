using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_010_EnforceUniqueEarningsPerAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_worker_earnings_ledger_org_attendance",
                table: "worker_earnings_ledger");

            migrationBuilder.CreateIndex(
                name: "ux_worker_earnings_ledger_org_attendance",
                table: "worker_earnings_ledger",
                columns: new[] { "organization_id", "attendance_id" },
                unique: true,
                filter: "attendance_id IS NOT NULL AND entry_type = 'EARNING'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_worker_earnings_ledger_org_attendance",
                table: "worker_earnings_ledger");

            migrationBuilder.CreateIndex(
                name: "ix_worker_earnings_ledger_org_attendance",
                table: "worker_earnings_ledger",
                columns: new[] { "organization_id", "attendance_id" });
        }
    }
}
