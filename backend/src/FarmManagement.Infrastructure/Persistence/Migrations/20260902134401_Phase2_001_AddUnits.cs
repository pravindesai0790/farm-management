using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_001_AddUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    unit_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    base_unit_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    conversion_factor = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_units", x => x.id);
                    table.ForeignKey(
                        name: "fk_units_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_units_organization_category",
                table: "units",
                columns: new[] { "organization_id", "unit_category" });

            migrationBuilder.CreateIndex(
                name: "ux_units_organization_code",
                table: "units",
                columns: new[] { "organization_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_units_system_code",
                table: "units",
                column: "code",
                unique: true,
                filter: "organization_id IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "units");
        }
    }
}
