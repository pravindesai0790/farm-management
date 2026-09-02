using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_008_AddPlantationEndReasonsAndPlantations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plantation_end_reasons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plantation_end_reasons", x => x.id);
                    table.ForeignKey(
                        name: "fk_plantation_end_reason_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "crop_plantations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    farm_id = table.Column<Guid>(type: "uuid", nullable: false),
                    farm_area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    crop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variety_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lifecycle_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    plantation_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    plantation_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    allocated_area = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    area_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    planting_date = table.Column<DateOnly>(type: "date", nullable: false),
                    expected_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    actual_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    end_reason_id = table.Column<Guid>(type: "uuid", nullable: true),
                    end_notes = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crop_plantations", x => x.id);
                    table.ForeignKey(
                        name: "fk_plantation_area",
                        column: x => x.farm_area_id,
                        principalTable: "farm_areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_plantation_area_unit",
                        column: x => x.area_unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_plantation_crop",
                        column: x => x.crop_id,
                        principalTable: "crops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_plantation_end_reason",
                        column: x => x.end_reason_id,
                        principalTable: "plantation_end_reasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_plantation_farm",
                        column: x => x.farm_id,
                        principalTable: "farms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_plantation_lifecycle",
                        column: x => x.lifecycle_template_id,
                        principalTable: "crop_lifecycle_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_plantation_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_plantation_variety",
                        column: x => x.variety_id,
                        principalTable: "crop_varieties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_crop_plantations_area_unit_id",
                table: "crop_plantations",
                column: "area_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_crop_plantations_crop_id",
                table: "crop_plantations",
                column: "crop_id");

            migrationBuilder.CreateIndex(
                name: "IX_crop_plantations_end_reason_id",
                table: "crop_plantations",
                column: "end_reason_id");

            migrationBuilder.CreateIndex(
                name: "IX_crop_plantations_farm_id",
                table: "crop_plantations",
                column: "farm_id");

            migrationBuilder.CreateIndex(
                name: "IX_crop_plantations_lifecycle_template_id",
                table: "crop_plantations",
                column: "lifecycle_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_crop_plantations_variety_id",
                table: "crop_plantations",
                column: "variety_id");

            migrationBuilder.CreateIndex(
                name: "ix_plantations_area_status",
                table: "crop_plantations",
                columns: new[] { "farm_area_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_plantation_organization_code",
                table: "crop_plantations",
                columns: new[] { "organization_id", "plantation_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_plantation_end_reasons_organization_code",
                table: "plantation_end_reasons",
                columns: new[] { "organization_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "crop_plantations");

            migrationBuilder.DropTable(
                name: "plantation_end_reasons");
        }
    }
}
