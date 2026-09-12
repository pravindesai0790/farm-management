using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_007_AddWorkerPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "worker_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    payment_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    payment_period_from = table.Column<DateOnly>(type: "date", nullable: true),
                    payment_period_to = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    cancellation_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worker_payments", x => x.id);
                    table.CheckConstraint("ck_worker_payments_amount", "amount > 0");
                    table.CheckConstraint("ck_worker_payments_payment_method", "payment_method IN ('CASH', 'BANK_TRANSFER', 'UPI', 'CHEQUE', 'OTHER')");
                    table.CheckConstraint("ck_worker_payments_payment_type", "payment_type IN ('ADVANCE', 'PAYOUT', 'ADJUSTMENT')");
                    table.CheckConstraint("ck_worker_payments_period", "payment_period_to IS NULL OR payment_period_from IS NULL OR payment_period_to >= payment_period_from");
                    table.CheckConstraint("ck_worker_payments_status", "status IN ('PENDING', 'COMPLETED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "fk_worker_payments_currency",
                        column: x => x.currency_id,
                        principalTable: "currencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_worker_payments_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_worker_payments_worker",
                        column: x => x.worker_id,
                        principalTable: "workers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_worker_payments_currency_id",
                table: "worker_payments",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "ix_worker_payments_org_worker_date",
                table: "worker_payments",
                columns: new[] { "organization_id", "worker_id", "payment_date" });

            migrationBuilder.CreateIndex(
                name: "ix_worker_payments_org_worker_status",
                table: "worker_payments",
                columns: new[] { "organization_id", "worker_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_worker_payments_organization_id",
                table: "worker_payments",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_worker_payments_worker_id",
                table: "worker_payments",
                column: "worker_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "worker_payments");
        }
    }
}
