using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_008_AddWorkerPaymentAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "worker_payment_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_earnings_ledger_id = table.Column<Guid>(type: "uuid", nullable: true),
                    allocated_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    allocation_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    allocation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worker_payment_allocations", x => x.id);
                    table.ForeignKey(
                        name: "FK_worker_payment_allocations_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_worker_payment_allocations_worker_earnings_ledger_worker_ea~",
                        column: x => x.worker_earnings_ledger_id,
                        principalTable: "worker_earnings_ledger",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_worker_payment_allocations_worker_payments_worker_payment_id",
                        column: x => x.worker_payment_id,
                        principalTable: "worker_payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_worker_payment_allocations_organization_id_allocation_date",
                table: "worker_payment_allocations",
                columns: new[] { "organization_id", "allocation_date" });

            migrationBuilder.CreateIndex(
                name: "IX_worker_payment_allocations_organization_id_allocation_type",
                table: "worker_payment_allocations",
                columns: new[] { "organization_id", "allocation_type" });

            migrationBuilder.CreateIndex(
                name: "IX_worker_payment_allocations_organization_id_worker_earnings_~",
                table: "worker_payment_allocations",
                columns: new[] { "organization_id", "worker_earnings_ledger_id" });

            migrationBuilder.CreateIndex(
                name: "IX_worker_payment_allocations_organization_id_worker_payment_id",
                table: "worker_payment_allocations",
                columns: new[] { "organization_id", "worker_payment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_worker_payment_allocations_worker_earnings_ledger_id",
                table: "worker_payment_allocations",
                column: "worker_earnings_ledger_id");

            migrationBuilder.CreateIndex(
                name: "IX_worker_payment_allocations_worker_payment_id",
                table: "worker_payment_allocations",
                column: "worker_payment_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "worker_payment_allocations");
        }
    }
}
