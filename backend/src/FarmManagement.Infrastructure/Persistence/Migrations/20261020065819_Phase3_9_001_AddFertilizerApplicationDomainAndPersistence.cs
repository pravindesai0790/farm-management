using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_9_001_AddFertilizerApplicationDomainAndPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_application_methods_organization_code",
                table: "application_methods");

            migrationBuilder.DropIndex(
                name: "ux_application_methods_system_code",
                table: "application_methods");

            migrationBuilder.AddColumn<Guid>(
                name: "fertilizer_application_id",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "module",
                table: "application_methods",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "SPRAY");

            migrationBuilder.CreateTable(
                name: "fertilizer_applications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    farm_id = table.Column<Guid>(type: "uuid", nullable: true),
                    farm_area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    plantation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    crop_cycle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    crop_cycle_stage_id = table.Column<Guid>(type: "uuid", nullable: true),
                    application_method_id = table.Column<Guid>(type: "uuid", nullable: true),
                    other_method_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    irrigation_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purpose_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    planned_date = table.Column<DateOnly>(type: "date", nullable: true),
                    scheduled_date = table.Column<DateOnly>(type: "date", nullable: true),
                    actual_application_date = table.Column<DateOnly>(type: "date", nullable: true),
                    planned_area = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    planned_area_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actual_area = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    actual_area_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    started_by = table.Column<Guid>(type: "uuid", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fertilizer_applications", x => x.id);
                    table.ForeignKey(
                        name: "fk_fertilizer_applications_actual_area_unit",
                        column: x => x.actual_area_unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fertilizer_applications_application_method",
                        column: x => x.application_method_id,
                        principalTable: "application_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fertilizer_applications_crop_cycle",
                        column: x => x.crop_cycle_id,
                        principalTable: "crop_cycles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fertilizer_applications_crop_cycle_stage",
                        column: x => x.crop_cycle_stage_id,
                        principalTable: "crop_cycle_stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fertilizer_applications_farm",
                        column: x => x.farm_id,
                        principalTable: "farms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fertilizer_applications_farm_area",
                        column: x => x.farm_area_id,
                        principalTable: "farm_areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fertilizer_applications_irrigation_event",
                        column: x => x.irrigation_event_id,
                        principalTable: "irrigation_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fertilizer_applications_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fertilizer_applications_planned_area_unit",
                        column: x => x.planned_area_unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fertilizer_applications_plantation",
                        column: x => x.plantation_id,
                        principalTable: "crop_plantations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fertilizer_reference_sequences",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    last_number = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fertilizer_reference_sequences", x => new { x.organization_id, x.year });
                    table.ForeignKey(
                        name: "fk_fertilizer_reference_sequences_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fertilizer_application_products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fertilizer_application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    planned_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    actual_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    stock_movement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fertilizer_application_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_fertilizer_application_products_fertilizer_application",
                        column: x => x.fertilizer_application_id,
                        principalTable: "fertilizer_applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fertilizer_application_products_inventory_item",
                        column: x => x.inventory_item_id,
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fertilizer_application_products_stock_movement",
                        column: x => x.stock_movement_id,
                        principalTable: "stock_movements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_fertilizer_application_products_storage_location",
                        column: x => x.storage_location_id,
                        principalTable: "storage_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_fertilizer_application",
                table: "stock_movements",
                column: "fertilizer_application_id",
                filter: "fertilizer_application_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_application_methods_organization_module_code",
                table: "application_methods",
                columns: new[] { "organization_id", "module", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_application_methods_system_module_code",
                table: "application_methods",
                columns: new[] { "module", "code" },
                unique: true,
                filter: "organization_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_fertilizer_application_products_inventory_item_id",
                table: "fertilizer_application_products",
                column: "inventory_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_fertilizer_application_products_stock_movement_id",
                table: "fertilizer_application_products",
                column: "stock_movement_id");

            migrationBuilder.CreateIndex(
                name: "ix_fertilizer_application_products_storage_location_id",
                table: "fertilizer_application_products",
                column: "storage_location_id");

            migrationBuilder.CreateIndex(
                name: "ux_fertilizer_application_products_application_item",
                table: "fertilizer_application_products",
                columns: new[] { "fertilizer_application_id", "inventory_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fertilizer_applications_actual_app_date",
                table: "fertilizer_applications",
                column: "actual_application_date");

            migrationBuilder.CreateIndex(
                name: "IX_fertilizer_applications_actual_area_unit_id",
                table: "fertilizer_applications",
                column: "actual_area_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_fertilizer_applications_application_method_id",
                table: "fertilizer_applications",
                column: "application_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_fertilizer_applications_created_at",
                table: "fertilizer_applications",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_fertilizer_applications_crop_cycle_id",
                table: "fertilizer_applications",
                column: "crop_cycle_id");

            migrationBuilder.CreateIndex(
                name: "ix_fertilizer_applications_crop_cycle_stage_id",
                table: "fertilizer_applications",
                column: "crop_cycle_stage_id");

            migrationBuilder.CreateIndex(
                name: "ix_fertilizer_applications_farm_area_id",
                table: "fertilizer_applications",
                column: "farm_area_id");

            migrationBuilder.CreateIndex(
                name: "ix_fertilizer_applications_farm_id",
                table: "fertilizer_applications",
                column: "farm_id");

            migrationBuilder.CreateIndex(
                name: "IX_fertilizer_applications_irrigation_event_id",
                table: "fertilizer_applications",
                column: "irrigation_event_id");

            migrationBuilder.CreateIndex(
                name: "ix_fertilizer_applications_org_idempotency",
                table: "fertilizer_applications",
                columns: new[] { "organization_id", "idempotency_key" });

            migrationBuilder.CreateIndex(
                name: "ix_fertilizer_applications_org_status",
                table: "fertilizer_applications",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_fertilizer_applications_planned_area_unit_id",
                table: "fertilizer_applications",
                column: "planned_area_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_fertilizer_applications_planned_date",
                table: "fertilizer_applications",
                column: "planned_date");

            migrationBuilder.CreateIndex(
                name: "ix_fertilizer_applications_plantation_id",
                table: "fertilizer_applications",
                column: "plantation_id");

            migrationBuilder.CreateIndex(
                name: "ix_fertilizer_applications_scheduled_date",
                table: "fertilizer_applications",
                column: "scheduled_date");

            migrationBuilder.CreateIndex(
                name: "ux_fertilizer_applications_org_ref",
                table: "fertilizer_applications",
                columns: new[] { "organization_id", "reference_number" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movements_fertilizer_application",
                table: "stock_movements",
                column: "fertilizer_application_id",
                principalTable: "fertilizer_applications",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_stock_movements_fertilizer_application",
                table: "stock_movements");

            migrationBuilder.DropTable(
                name: "fertilizer_application_products");

            migrationBuilder.DropTable(
                name: "fertilizer_reference_sequences");

            migrationBuilder.DropTable(
                name: "fertilizer_applications");

            migrationBuilder.DropIndex(
                name: "ix_stock_movements_fertilizer_application",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ux_application_methods_organization_module_code",
                table: "application_methods");

            migrationBuilder.DropIndex(
                name: "ux_application_methods_system_module_code",
                table: "application_methods");

            migrationBuilder.DropColumn(
                name: "fertilizer_application_id",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "module",
                table: "application_methods");

            migrationBuilder.CreateIndex(
                name: "ux_application_methods_organization_code",
                table: "application_methods",
                columns: new[] { "organization_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_application_methods_system_code",
                table: "application_methods",
                column: "code",
                unique: true,
                filter: "organization_id IS NULL");
        }
    }
}
