using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Phase 3.4 - Phase 1 Migration for Labor Activity:
    /// 1. Adds nullable crop_cycle_stage_id referencing crop_cycle_stages(id) with ON DELETE RESTRICT and index ix_labor_activities_crop_cycle_stage.
    /// 2. Drops obsolete columns: worker_count, total_working_hours, cost_amount, currency_id.
    /// 3. Drops associated check constraints: ck_labor_activities_worker_count, ck_labor_activities_working_hours, ck_labor_activities_cost_amount.
    /// NOTE: Rollback (Down) restores the column definitions and check constraints, but cannot restore data previously stored in dropped columns without prior backup/archive data.
    /// </summary>
    public partial class Phase3_017_LaborActivityStageAndDropObsoleteColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_labor_activities_cost_amount",
                table: "labor_activities");

            migrationBuilder.DropCheckConstraint(
                name: "ck_labor_activities_worker_count",
                table: "labor_activities");

            migrationBuilder.DropCheckConstraint(
                name: "ck_labor_activities_working_hours",
                table: "labor_activities");

            migrationBuilder.DropColumn(
                name: "cost_amount",
                table: "labor_activities");

            migrationBuilder.DropColumn(
                name: "currency_id",
                table: "labor_activities");

            migrationBuilder.DropColumn(
                name: "total_working_hours",
                table: "labor_activities");

            migrationBuilder.DropColumn(
                name: "worker_count",
                table: "labor_activities");

            migrationBuilder.AddColumn<Guid>(
                name: "crop_cycle_stage_id",
                table: "labor_activities",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_labor_activities_crop_cycle_stage",
                table: "labor_activities",
                column: "crop_cycle_stage_id");

            migrationBuilder.AddForeignKey(
                name: "fk_labor_activity_stage",
                table: "labor_activities",
                column: "crop_cycle_stage_id",
                principalTable: "crop_cycle_stages",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_labor_activity_stage",
                table: "labor_activities");

            migrationBuilder.DropIndex(
                name: "ix_labor_activities_crop_cycle_stage",
                table: "labor_activities");

            migrationBuilder.DropColumn(
                name: "crop_cycle_stage_id",
                table: "labor_activities");

            migrationBuilder.AddColumn<Guid>(
                name: "currency_id",
                table: "labor_activities",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "cost_amount",
                table: "labor_activities",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "total_working_hours",
                table: "labor_activities",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "worker_count",
                table: "labor_activities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "ck_labor_activities_cost_amount",
                table: "labor_activities",
                sql: "cost_amount IS NULL OR cost_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_labor_activities_worker_count",
                table: "labor_activities",
                sql: "worker_count > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_labor_activities_working_hours",
                table: "labor_activities",
                sql: "total_working_hours IS NULL OR total_working_hours > 0");
        }
    }
}
