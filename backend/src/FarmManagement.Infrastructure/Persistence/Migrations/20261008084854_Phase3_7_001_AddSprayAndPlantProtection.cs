using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_7_001_AddSprayAndPlantProtection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "spray_id",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "application_methods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_methods", x => x.id);
                    table.ForeignKey(
                        name: "fk_application_methods_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_types_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "targets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    target_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_targets", x => x.id);
                    table.ForeignKey(
                        name: "fk_targets_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "plant_protection_products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    active_ingredient = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    manufacturer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plant_protection_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_plant_protection_products_inventory_item",
                        column: x => x.inventory_item_id,
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_plant_protection_products_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_plant_protection_products_product_type",
                        column: x => x.product_type_id,
                        principalTable: "product_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sprays",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    farm_id = table.Column<Guid>(type: "uuid", nullable: false),
                    farm_area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    plantation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    crop_cycle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    crop_cycle_stage_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    planned_date = table.Column<DateOnly>(type: "date", nullable: true),
                    scheduled_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actual_application_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    planned_area = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    planned_area_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actual_treated_area = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    actual_treated_area_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    water_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    water_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    application_method_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purpose_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sprays", x => x.id);
                    table.ForeignKey(
                        name: "fk_sprays_actual_treated_area_unit",
                        column: x => x.actual_treated_area_unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sprays_application_method",
                        column: x => x.application_method_id,
                        principalTable: "application_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sprays_crop_cycle",
                        column: x => x.crop_cycle_id,
                        principalTable: "crop_cycles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sprays_crop_cycle_stage",
                        column: x => x.crop_cycle_stage_id,
                        principalTable: "crop_cycle_stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sprays_farm",
                        column: x => x.farm_id,
                        principalTable: "farms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sprays_farm_area",
                        column: x => x.farm_area_id,
                        principalTable: "farm_areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sprays_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sprays_planned_area_unit",
                        column: x => x.planned_area_unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sprays_plantation",
                        column: x => x.plantation_id,
                        principalTable: "crop_plantations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sprays_target",
                        column: x => x.target_id,
                        principalTable: "targets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sprays_water_unit",
                        column: x => x.water_unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "spray_products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    spray_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    planned_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    actual_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    dosage = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spray_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_spray_products_inventory_item",
                        column: x => x.inventory_item_id,
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_spray_products_spray",
                        column: x => x.spray_id,
                        principalTable: "sprays",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_spray_products_storage_location",
                        column: x => x.storage_location_id,
                        principalTable: "storage_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_spray",
                table: "stock_movements",
                column: "spray_id",
                filter: "spray_id IS NOT NULL");

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

            migrationBuilder.CreateIndex(
                name: "ix_plant_protection_products_organization_id",
                table: "plant_protection_products",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_plant_protection_products_product_type_id",
                table: "plant_protection_products",
                column: "product_type_id");

            migrationBuilder.CreateIndex(
                name: "ux_plant_protection_products_inventory_item",
                table: "plant_protection_products",
                column: "inventory_item_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_product_types_organization_code",
                table: "product_types",
                columns: new[] { "organization_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_product_types_system_code",
                table: "product_types",
                column: "code",
                unique: true,
                filter: "organization_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_spray_products_inventory_item_id",
                table: "spray_products",
                column: "inventory_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_spray_products_storage_location_id",
                table: "spray_products",
                column: "storage_location_id");

            migrationBuilder.CreateIndex(
                name: "ux_spray_products_spray_item",
                table: "spray_products",
                columns: new[] { "spray_id", "inventory_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sprays_actual_treated_area_unit_id",
                table: "sprays",
                column: "actual_treated_area_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_sprays_application_method_id",
                table: "sprays",
                column: "application_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_sprays_crop_cycle_id",
                table: "sprays",
                column: "crop_cycle_id");

            migrationBuilder.CreateIndex(
                name: "IX_sprays_crop_cycle_stage_id",
                table: "sprays",
                column: "crop_cycle_stage_id");

            migrationBuilder.CreateIndex(
                name: "IX_sprays_farm_area_id",
                table: "sprays",
                column: "farm_area_id");

            migrationBuilder.CreateIndex(
                name: "ix_sprays_farm_id",
                table: "sprays",
                column: "farm_id");

            migrationBuilder.CreateIndex(
                name: "ix_sprays_farm_status",
                table: "sprays",
                columns: new[] { "farm_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_sprays_organization_id",
                table: "sprays",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_sprays_planned_area_unit_id",
                table: "sprays",
                column: "planned_area_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_sprays_plantation_id",
                table: "sprays",
                column: "plantation_id");

            migrationBuilder.CreateIndex(
                name: "ix_sprays_scheduled_date_time",
                table: "sprays",
                column: "scheduled_date_time");

            migrationBuilder.CreateIndex(
                name: "IX_sprays_target_id",
                table: "sprays",
                column: "target_id");

            migrationBuilder.CreateIndex(
                name: "IX_sprays_water_unit_id",
                table: "sprays",
                column: "water_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_targets_organization_type",
                table: "targets",
                columns: new[] { "organization_id", "target_type" });

            migrationBuilder.CreateIndex(
                name: "ux_targets_organization_code",
                table: "targets",
                columns: new[] { "organization_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_targets_system_code",
                table: "targets",
                column: "code",
                unique: true,
                filter: "organization_id IS NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movements_spray",
                table: "stock_movements",
                column: "spray_id",
                principalTable: "sprays",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_stock_movements_spray",
                table: "stock_movements");

            migrationBuilder.DropTable(
                name: "plant_protection_products");

            migrationBuilder.DropTable(
                name: "spray_products");

            migrationBuilder.DropTable(
                name: "product_types");

            migrationBuilder.DropTable(
                name: "sprays");

            migrationBuilder.DropTable(
                name: "application_methods");

            migrationBuilder.DropTable(
                name: "targets");

            migrationBuilder.DropIndex(
                name: "ix_stock_movements_spray",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "spray_id",
                table: "stock_movements");
        }
    }
}
