using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_5_003_AddStockMovementOperationalLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "crop_cycle_id",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "crop_cycle_stage_id",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "farm_area_id",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "labor_activity_id",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "plantation_id",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_crop_cycle",
                table: "stock_movements",
                column: "crop_cycle_id",
                filter: "crop_cycle_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_crop_cycle_stage",
                table: "stock_movements",
                column: "crop_cycle_stage_id",
                filter: "crop_cycle_stage_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_farm_area",
                table: "stock_movements",
                column: "farm_area_id",
                filter: "farm_area_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_labor_activity",
                table: "stock_movements",
                column: "labor_activity_id",
                filter: "labor_activity_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_plantation",
                table: "stock_movements",
                column: "plantation_id",
                filter: "plantation_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movements_crop_cycle",
                table: "stock_movements",
                column: "crop_cycle_id",
                principalTable: "crop_cycles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movements_crop_cycle_stage",
                table: "stock_movements",
                column: "crop_cycle_stage_id",
                principalTable: "crop_cycle_stages",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movements_farm_area",
                table: "stock_movements",
                column: "farm_area_id",
                principalTable: "farm_areas",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movements_labor_activity",
                table: "stock_movements",
                column: "labor_activity_id",
                principalTable: "labor_activities",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movements_plantation",
                table: "stock_movements",
                column: "plantation_id",
                principalTable: "crop_plantations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_stock_movements_crop_cycle",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movements_crop_cycle_stage",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movements_farm_area",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movements_labor_activity",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movements_plantation",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ix_stock_movements_crop_cycle",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ix_stock_movements_crop_cycle_stage",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ix_stock_movements_farm_area",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ix_stock_movements_labor_activity",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ix_stock_movements_plantation",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "crop_cycle_id",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "crop_cycle_stage_id",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "farm_area_id",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "labor_activity_id",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "plantation_id",
                table: "stock_movements");
        }
    }
}
