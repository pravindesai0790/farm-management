using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_6_003_AddCropCycleStageAndNullableDescriptionToPurchaseInvoiceLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "purchase_invoice_lines",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<Guid>(
                name: "crop_cycle_stage_id",
                table: "purchase_invoice_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_invoice_lines_crop_cycle_stage_id",
                table: "purchase_invoice_lines",
                column: "crop_cycle_stage_id");

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_invoice_lines_crop_cycle_stage",
                table: "purchase_invoice_lines",
                column: "crop_cycle_stage_id",
                principalTable: "crop_cycle_stages",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_purchase_invoice_lines_crop_cycle_stage",
                table: "purchase_invoice_lines");

            migrationBuilder.DropIndex(
                name: "IX_purchase_invoice_lines_crop_cycle_stage_id",
                table: "purchase_invoice_lines");

            migrationBuilder.DropColumn(
                name: "crop_cycle_stage_id",
                table: "purchase_invoice_lines");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "purchase_invoice_lines",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
