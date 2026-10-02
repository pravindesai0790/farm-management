using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_6_002_AddCropCycleStageToExpense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "crop_cycle_stage_id",
                table: "expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_expenses_crop_cycle_stage_id",
                table: "expenses",
                column: "crop_cycle_stage_id");

            migrationBuilder.AddForeignKey(
                name: "fk_expenses_crop_cycle_stage",
                table: "expenses",
                column: "crop_cycle_stage_id",
                principalTable: "crop_cycle_stages",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_expenses_crop_cycle_stage",
                table: "expenses");

            migrationBuilder.DropIndex(
                name: "IX_expenses_crop_cycle_stage_id",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "crop_cycle_stage_id",
                table: "expenses");
        }
    }
}
