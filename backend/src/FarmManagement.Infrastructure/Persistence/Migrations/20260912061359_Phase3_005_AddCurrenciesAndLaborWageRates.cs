using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_005_AddCurrenciesAndLaborWageRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "currencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "labor_wage_rates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    wage_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    wage_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_labor_wage_rates", x => x.id);
                    table.CheckConstraint("ck_labor_wage_rates_dates", "effective_to IS NULL OR effective_to >= effective_from");
                    table.CheckConstraint("ck_labor_wage_rates_gender", "gender IN ('MALE', 'FEMALE', 'OTHER')");
                    table.CheckConstraint("ck_labor_wage_rates_wage_rate", "wage_rate > 0");
                    table.CheckConstraint("ck_labor_wage_rates_wage_type", "wage_type IN ('FULL_DAY', 'HALF_DAY', 'HOURLY', 'MONTHLY')");
                    table.ForeignKey(
                        name: "fk_labor_wage_rates_currency",
                        column: x => x.currency_id,
                        principalTable: "currencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_labor_wage_rates_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_currencies_code",
                table: "currencies",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_labor_wage_rates_currency_id",
                table: "labor_wage_rates",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "ix_labor_wage_rates_org_effective_dates",
                table: "labor_wage_rates",
                columns: new[] { "organization_id", "effective_from", "effective_to" });

            migrationBuilder.CreateIndex(
                name: "ix_labor_wage_rates_org_gender_type",
                table: "labor_wage_rates",
                columns: new[] { "organization_id", "gender", "wage_type" });

            migrationBuilder.CreateIndex(
                name: "ix_labor_wage_rates_org_gender_type_active",
                table: "labor_wage_rates",
                columns: new[] { "organization_id", "gender", "wage_type", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_labor_wage_rates_organization_id",
                table: "labor_wage_rates",
                column: "organization_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "labor_wage_rates");

            migrationBuilder.DropTable(
                name: "currencies");
        }
    }
}
