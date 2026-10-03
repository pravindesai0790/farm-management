using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_6_004_AddReceiptGroupAndIdempotencyToReceiptLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "purchase_invoice_receipt_lines",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "receipt_group_id",
                table: "purchase_invoice_receipt_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoice_receipt_lines_group_id",
                table: "purchase_invoice_receipt_lines",
                column: "receipt_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoice_receipt_lines_idempotency",
                table: "purchase_invoice_receipt_lines",
                columns: new[] { "organization_id", "purchase_invoice_id", "idempotency_key" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_purchase_invoice_receipt_lines_group_id",
                table: "purchase_invoice_receipt_lines");

            migrationBuilder.DropIndex(
                name: "ix_purchase_invoice_receipt_lines_idempotency",
                table: "purchase_invoice_receipt_lines");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "purchase_invoice_receipt_lines");

            migrationBuilder.DropColumn(
                name: "receipt_group_id",
                table: "purchase_invoice_receipt_lines");
        }
    }
}
