using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_006_AddWorkerEarningsLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "worker_earnings_ledger",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attendance_id = table.Column<Guid>(type: "uuid", nullable: true),
                    earnings_date = table.Column<DateOnly>(type: "date", nullable: false),
                    wage_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    wage_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    entry_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reference_ledger_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    finalized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finalized_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worker_earnings_ledger", x => x.id);
                    table.CheckConstraint("ck_worker_earnings_ledger_earning_amount", "entry_type != 'EARNING' OR gross_amount > 0");
                    table.CheckConstraint("ck_worker_earnings_ledger_entry_type", "entry_type IN ('EARNING', 'REVERSAL', 'ADJUSTMENT')");
                    table.CheckConstraint("ck_worker_earnings_ledger_quantity", "quantity > 0");
                    table.CheckConstraint("ck_worker_earnings_ledger_reversal_ref", "entry_type != 'REVERSAL' OR reference_ledger_id IS NOT NULL");
                    table.CheckConstraint("ck_worker_earnings_ledger_status", "status IN ('CALCULATED', 'APPROVED', 'REVERSED')");
                    table.CheckConstraint("ck_worker_earnings_ledger_wage_rate", "wage_rate > 0");
                    table.CheckConstraint("ck_worker_earnings_ledger_wage_type", "wage_type IN ('FULL_DAY', 'HALF_DAY', 'HOURLY', 'MONTHLY')");
                    table.ForeignKey(
                        name: "fk_worker_earnings_ledger_currency",
                        column: x => x.currency_id,
                        principalTable: "currencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_worker_earnings_ledger_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_worker_earnings_ledger_reference",
                        column: x => x.reference_ledger_id,
                        principalTable: "worker_earnings_ledger",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_worker_earnings_ledger_worker",
                        column: x => x.worker_id,
                        principalTable: "workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_worker_earnings_ledger_currency_id",
                table: "worker_earnings_ledger",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "ix_worker_earnings_ledger_org_attendance",
                table: "worker_earnings_ledger",
                columns: new[] { "organization_id", "attendance_id" });

            migrationBuilder.CreateIndex(
                name: "ix_worker_earnings_ledger_org_reference",
                table: "worker_earnings_ledger",
                columns: new[] { "organization_id", "reference_ledger_id" });

            migrationBuilder.CreateIndex(
                name: "ix_worker_earnings_ledger_org_worker_date",
                table: "worker_earnings_ledger",
                columns: new[] { "organization_id", "worker_id", "earnings_date" });

            migrationBuilder.CreateIndex(
                name: "ix_worker_earnings_ledger_org_worker_status",
                table: "worker_earnings_ledger",
                columns: new[] { "organization_id", "worker_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_worker_earnings_ledger_organization_id",
                table: "worker_earnings_ledger",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_worker_earnings_ledger_reference_ledger_id",
                table: "worker_earnings_ledger",
                column: "reference_ledger_id");

            migrationBuilder.CreateIndex(
                name: "IX_worker_earnings_ledger_worker_id",
                table: "worker_earnings_ledger",
                column: "worker_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "worker_earnings_ledger");
        }
    }
}
