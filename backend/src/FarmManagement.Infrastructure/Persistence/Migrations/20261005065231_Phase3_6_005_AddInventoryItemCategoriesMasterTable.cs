using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_6_005_AddInventoryItemCategoriesMasterTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                table: "inventory_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "inventory_item_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    examples = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "category"),
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
                    table.PrimaryKey("PK_inventory_item_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_item_categories_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Insert system categories
            var seedSql = @"
                INSERT INTO inventory_item_categories (id, organization_id, name, code, description, examples, icon, display_order, is_system, is_active, created_at)
                VALUES
                ('c1000000-0000-0000-0000-000000000001', NULL, 'Seeds & Planting Materials', 'SEEDS_PLANTING', 'Seeds, saplings, tubers, seedlings, and inoculants.', 'Seeds, saplings, tubers, seedlings, inoculants', 'spa', 1, true, true, NOW()),
                ('c1000000-0000-0000-0000-000000000002', NULL, 'Fertilizers & Soil Amendments', 'FERTILIZERS_SOIL', 'Nitrogen, phosphorus, potassium (NPK), organic compost, lime, and micronutrients.', 'NPK, organic compost, lime, micronutrients', 'compost', 2, true, true, NOW()),
                ('c1000000-0000-0000-0000-000000000003', NULL, 'Agrochemicals & Pest Control', 'AGROCHEMICALS_PEST_CONTROL', 'Herbicides, insecticides, fungicides, and rodenticides.', 'Herbicides, insecticides, fungicides, rodenticides', 'pest_control', 3, true, true, NOW()),
                ('c1000000-0000-0000-0000-000000000004', NULL, 'Feed & Animal Nutrition', 'FEED_ANIMAL_NUTRITION', 'Livestock feed, forage, grains, mineral blocks, and supplements.', 'Livestock feed, forage, grains, mineral blocks, supplements', 'pets', 4, true, true, NOW()),
                ('c1000000-0000-0000-0000-000000000005', NULL, 'Veterinary & Animal Health', 'VETERINARY_ANIMAL_HEALTH', 'Vaccines, medicines, dewormers, first-aid supplies, and tagging tools.', 'Vaccines, medicines, dewormers, tagging tools', 'medical_services', 5, true, true, NOW()),
                ('c1000000-0000-0000-0000-000000000006', NULL, 'Fuel, Oil & Lubricants', 'FUEL_OIL_LUBRICANTS', 'Diesel, gasoline, engine oil, and hydraulic fluid.', 'Diesel, gasoline, engine oil, hydraulic fluid', 'local_gas_station', 6, true, true, NOW()),
                ('c1000000-0000-0000-0000-000000000007', NULL, 'Tools & Hand Equipment', 'TOOLS_HAND_EQUIPMENT', 'Shovels, pruners, hoes, forks, and buckets.', 'Shovels, pruners, hoes, forks, buckets', 'handyman', 7, true, true, NOW()),
                ('c1000000-0000-0000-0000-000000000008', NULL, 'Machinery & Heavy Equipment Parts', 'MACHINERY_EQUIPMENT_PARTS', 'Tractor parts, belts, filters, tires, and harvester components.', 'Tractor parts, belts, filters, tires, harvester parts', 'precision_manufacturing', 8, true, true, NOW()),
                ('c1000000-0000-0000-0000-000000000009', NULL, 'Irrigation & Plumbing', 'IRRIGATION_PLUMBING', 'Pipes, valves, sprinklers, drip tape, and fittings.', 'Pipes, valves, sprinklers, drip tape, fittings', 'water_drop', 9, true, true, NOW()),
                ('c1000000-0000-0000-0000-000000000010', NULL, 'Harvesting & Storage Supplies', 'HARVESTING_STORAGE_SUPPLIES', 'Crates, bins, bags, twine, and cold storage items.', 'Crates, bins, bags, twine, storage items', 'inventory_2', 10, true, true, NOW()),
                ('c1000000-0000-0000-0000-000000000011', NULL, 'Safety & Protective Gear', 'SAFETY_PROTECTIVE_GEAR', 'Gloves, respirators, eye protection, and first-aid kits.', 'Gloves, respirators, eye protection, first-aid', 'health_and_safety', 11, true, true, NOW()),
                ('c1000000-0000-0000-0000-000000000012', NULL, 'Building, Fencing & Hardware', 'BUILDING_FENCING', 'Wire fencing, posts, nails, lumber, shade netting, and fasteners.', 'Fencing, posts, nails, lumber, shade net', 'fence', 12, true, true, NOW()),
                ('c1000000-0000-0000-0000-000000000013', NULL, 'General Farm Supplies', 'GENERAL_SUPPLIES', 'General consumable supplies, cleaning agents, packaging, and sundries.', 'Cleaning agents, sundries, general supplies', 'category', 13, true, true, NOW())
                ON CONFLICT (id) DO NOTHING;";
            migrationBuilder.Sql(seedSql);

            // Data patch for existing items based on their previous string category / name
            migrationBuilder.Sql(@"
                UPDATE inventory_items 
                SET category_id = 'c1000000-0000-0000-0000-000000000002' 
                WHERE category_id IS NULL AND (
                    LOWER(category) LIKE '%fertilizer%' OR 
                    LOWER(name) LIKE '%fertilizer%' OR 
                    LOWER(name) LIKE '%urea%'
                );");

            migrationBuilder.Sql(@"
                UPDATE inventory_items 
                SET category_id = 'c1000000-0000-0000-0000-000000000003' 
                WHERE category_id IS NULL AND (
                    LOWER(category) LIKE '%pesticide%' OR 
                    LOWER(name) LIKE '%pesticide%' OR 
                    LOWER(category) LIKE '%chemical%' OR
                    LOWER(category) LIKE '%agrochemical%'
                );");

            // Now drop old category string column
            migrationBuilder.DropColumn(
                name: "category",
                table: "inventory_items");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_category_id",
                table: "inventory_items",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ux_inventory_item_categories_org_code",
                table: "inventory_item_categories",
                columns: new[] { "organization_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_inventory_item_categories_system_code",
                table: "inventory_item_categories",
                column: "code",
                unique: true,
                filter: "organization_id IS NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_items_category",
                table: "inventory_items",
                column: "category_id",
                principalTable: "inventory_item_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventory_items_category",
                table: "inventory_items");

            migrationBuilder.DropTable(
                name: "inventory_item_categories");

            migrationBuilder.DropIndex(
                name: "IX_inventory_items_category_id",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "inventory_items");

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "inventory_items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
