using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_9_002_AddFertilizerOperationIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fertilizer_operation_idempotency_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fertilizer_application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fertilizer_operation_idempotency_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_fertilizer_operation_idempotency_records_fertilizer_applica~",
                        column: x => x.fertilizer_application_id,
                        principalTable: "fertilizer_applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_fertilizer_operation_idempotency_records_organizations_orga~",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fertilizer_operation_idempotency",
                table: "fertilizer_operation_idempotency_records",
                columns: new[] { "organization_id", "operation_type", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fertilizer_operation_idempotency_app_id",
                table: "fertilizer_operation_idempotency_records",
                column: "fertilizer_application_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fertilizer_operation_idempotency_records");
        }
    }
}
