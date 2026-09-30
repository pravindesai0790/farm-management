using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_5_004_AddStockMovementReversals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_stock_movements_opening_stock_location_item",
                table: "stock_movements");

            migrationBuilder.AddColumn<bool>(
                name: "is_reversed",
                table: "stock_movements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "reversal_movement_id",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reversal_reason",
                table: "stock_movements",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reversed_movement_id",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_organization_is_reversed",
                table: "stock_movements",
                columns: new[] { "organization_id", "is_reversed" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_reversal_movement",
                table: "stock_movements",
                column: "reversal_movement_id",
                filter: "reversal_movement_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_reversed_movement",
                table: "stock_movements",
                column: "reversed_movement_id",
                filter: "reversed_movement_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_stock_movements_opening_stock_location_item",
                table: "stock_movements",
                columns: new[] { "storage_location_id", "inventory_item_id" },
                unique: true,
                filter: "movement_type = 'OPENINGSTOCK' AND is_reversed = FALSE");

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movements_reversal_movement",
                table: "stock_movements",
                column: "reversal_movement_id",
                principalTable: "stock_movements",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movements_reversed_movement",
                table: "stock_movements",
                column: "reversed_movement_id",
                principalTable: "stock_movements",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_stock_movements_reversal_movement",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movements_reversed_movement",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ix_stock_movements_organization_is_reversed",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ix_stock_movements_reversal_movement",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ix_stock_movements_reversed_movement",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ux_stock_movements_opening_stock_location_item",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "is_reversed",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "reversal_movement_id",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "reversal_reason",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "reversed_movement_id",
                table: "stock_movements");

            migrationBuilder.CreateIndex(
                name: "ux_stock_movements_opening_stock_location_item",
                table: "stock_movements",
                columns: new[] { "storage_location_id", "inventory_item_id" },
                unique: true,
                filter: "movement_type = 'OPENINGSTOCK'");
        }
    }
}
