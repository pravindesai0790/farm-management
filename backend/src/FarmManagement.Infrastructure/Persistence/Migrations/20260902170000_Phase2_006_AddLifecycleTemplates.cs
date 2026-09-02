using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_006_AddLifecycleTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "crop_lifecycle_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    crop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crop_lifecycle_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_lifecycle_template_crop",
                        column: x => x.crop_id,
                        principalTable: "crops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lifecycle_template_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "crop_lifecycle_stages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lifecycle_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stage_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    stage_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crop_lifecycle_stages", x => x.id);
                    table.ForeignKey(
                        name: "fk_lifecycle_stage_template",
                        column: x => x.lifecycle_template_id,
                        principalTable: "crop_lifecycle_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_lifecycle_stages_template_id",
                table: "crop_lifecycle_stages",
                column: "lifecycle_template_id");

            migrationBuilder.CreateIndex(
                name: "ux_lifecycle_stage_sequence",
                table: "crop_lifecycle_stages",
                columns: new[] { "lifecycle_template_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lifecycle_templates_crop_organization",
                table: "crop_lifecycle_templates",
                columns: new[] { "crop_id", "organization_id" });

            migrationBuilder.CreateIndex(
                name: "ix_lifecycle_templates_organization_id",
                table: "crop_lifecycle_templates",
                column: "organization_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "crop_lifecycle_stages");

            migrationBuilder.DropTable(
                name: "crop_lifecycle_templates");
        }
    }
}
