using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_003_AddLaborMasters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contractors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_person = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contractors", x => x.id);
                    table.ForeignKey(
                        name: "fk_contractors_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "labor_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_labor_categories", x => x.id);
                    table.CheckConstraint("ck_labor_categories_system_org", "(is_system = TRUE AND organization_id IS NULL) OR (is_system = FALSE AND organization_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_labor_categories_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    mobile_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    alternate_mobile_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    labor_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    employment_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    contractor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    joining_date = table.Column<DateOnly>(type: "date", nullable: true),
                    leaving_date = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workers", x => x.id);
                    table.CheckConstraint("ck_workers_contractor_required", "employment_type != 'CONTRACT' OR contractor_id IS NOT NULL");
                    table.CheckConstraint("ck_workers_dates", "leaving_date IS NULL OR joining_date IS NULL OR leaving_date >= joining_date");
                    table.CheckConstraint("ck_workers_employment_type", "employment_type IN ('PERMANENT', 'SEASONAL', 'DAILY_WAGE', 'CONTRACT')");
                    table.CheckConstraint("ck_workers_gender", "gender IN ('MALE', 'FEMALE', 'OTHER')");
                    table.ForeignKey(
                        name: "fk_workers_contractor",
                        column: x => x.contractor_id,
                        principalTable: "contractors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_workers_labor_category",
                        column: x => x.labor_category_id,
                        principalTable: "labor_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_workers_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contractors_organization_id",
                table: "contractors",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_contractors_organization_is_active",
                table: "contractors",
                columns: new[] { "organization_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_contractors_organization_name",
                table: "contractors",
                columns: new[] { "organization_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_contractors_organization_phone",
                table: "contractors",
                columns: new[] { "organization_id", "phone_number" });

            migrationBuilder.CreateIndex(
                name: "ix_labor_categories_organization_id",
                table: "labor_categories",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ux_labor_categories_organization_name",
                table: "labor_categories",
                columns: new[] { "organization_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_labor_categories_system_name",
                table: "labor_categories",
                column: "name",
                unique: true,
                filter: "organization_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_workers_contractor_id",
                table: "workers",
                column: "contractor_id");

            migrationBuilder.CreateIndex(
                name: "IX_workers_labor_category_id",
                table: "workers",
                column: "labor_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_workers_organization_contractor",
                table: "workers",
                columns: new[] { "organization_id", "contractor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_workers_organization_display_name",
                table: "workers",
                columns: new[] { "organization_id", "display_name" });

            migrationBuilder.CreateIndex(
                name: "ix_workers_organization_id",
                table: "workers",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_workers_organization_is_active",
                table: "workers",
                columns: new[] { "organization_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_workers_organization_labor_category",
                table: "workers",
                columns: new[] { "organization_id", "labor_category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_workers_organization_mobile",
                table: "workers",
                columns: new[] { "organization_id", "mobile_number" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workers");

            migrationBuilder.DropTable(
                name: "contractors");

            migrationBuilder.DropTable(
                name: "labor_categories");
        }
    }
}
