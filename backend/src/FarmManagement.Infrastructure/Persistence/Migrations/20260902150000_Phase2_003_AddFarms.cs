using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations;

public partial class Phase2_003_AddFarms : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "farm_ownership_types",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
            },
            constraints: table => table.PrimaryKey("PK_farm_ownership_types", x => x.id));

        migrationBuilder.CreateTable(
            name: "farms",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                ownership_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                total_area = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                area_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                address_line1 = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                address_line2 = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                postal_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_farms", x => x.id);
                table.ForeignKey("fk_farm_area_unit", x => x.area_unit_id, "units", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_farm_organization", x => x.organization_id, "organizations", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_farm_ownership_type", x => x.ownership_type_id, "farm_ownership_types", "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ux_farm_ownership_types_code",
            table: "farm_ownership_types",
            column: "code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_farm_organization_code",
            table: "farms",
            columns: new[] { "organization_id", "code" },
            unique: true);

        migrationBuilder.CreateIndex(name: "IX_farms_area_unit_id", table: "farms", column: "area_unit_id");
        migrationBuilder.CreateIndex(name: "IX_farms_ownership_type_id", table: "farms", column: "ownership_type_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "farms");
        migrationBuilder.DropTable(name: "farm_ownership_types");
    }
}
