using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_5_002_InventoryConcurrencyAndIntegrityFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stock_movements_storage_location_id",
                table: "stock_movements");

            migrationBuilder.CreateIndex(
                name: "ux_stock_movements_opening_stock_location_item",
                table: "stock_movements",
                columns: new[] { "storage_location_id", "inventory_item_id" },
                unique: true,
                filter: "movement_type = 'OPENINGSTOCK'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_stock_movements_opening_stock_location_item",
                table: "stock_movements");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_storage_location_id",
                table: "stock_movements",
                column: "storage_location_id");
        }
    }
}
