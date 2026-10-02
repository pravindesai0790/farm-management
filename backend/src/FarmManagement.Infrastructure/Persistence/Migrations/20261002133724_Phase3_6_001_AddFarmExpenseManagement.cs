using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FarmManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_6_001_AddFarmExpenseManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "expense_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_system_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_expense_categories_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_person = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    registration_identifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppliers", x => x.id);
                    table.ForeignKey(
                        name: "fk_suppliers_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "expenses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    farm_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expense_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expense_date = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    farm_area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    plantation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    crop_cycle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    attachment_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    posted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    posted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reversed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reversed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reversal_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expenses", x => x.id);
                    table.CheckConstraint("ck_expenses_amount", "amount > 0");
                    table.CheckConstraint("ck_expenses_status", "status IN ('DRAFT', 'POSTED', 'REVERSED')");
                    table.ForeignKey(
                        name: "fk_expenses_crop_cycle",
                        column: x => x.crop_cycle_id,
                        principalTable: "crop_cycles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expenses_currency",
                        column: x => x.currency_id,
                        principalTable: "currencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expenses_expense_category",
                        column: x => x.expense_category_id,
                        principalTable: "expense_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expenses_farm",
                        column: x => x.farm_id,
                        principalTable: "farms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expenses_farm_area",
                        column: x => x.farm_area_id,
                        principalTable: "farm_areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expenses_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expenses_plantation",
                        column: x => x.plantation_id,
                        principalTable: "crop_plantations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_expenses_supplier",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    farm_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_invoice_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    invoice_date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    currency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_terms = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    other_charges = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    attachment_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    posted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    posted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reversed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reversed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reversal_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_invoices", x => x.id);
                    table.CheckConstraint("ck_purchase_invoices_due_date", "due_date IS NULL OR due_date >= invoice_date");
                    table.CheckConstraint("ck_purchase_invoices_financials", "subtotal >= 0 AND total_amount >= 0 AND tax_amount >= 0 AND other_charges >= 0 AND discount_amount >= 0");
                    table.CheckConstraint("ck_purchase_invoices_status", "status IN ('DRAFT', 'POSTED', 'REVERSED')");
                    table.ForeignKey(
                        name: "fk_purchase_invoices_currency",
                        column: x => x.currency_id,
                        principalTable: "currencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_invoices_farm",
                        column: x => x.farm_id,
                        principalTable: "farms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_invoices_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_invoices_supplier",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reversed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reversed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reversal_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_payments", x => x.id);
                    table.CheckConstraint("ck_supplier_payments_amount", "amount > 0");
                    table.CheckConstraint("ck_supplier_payments_payment_method", "payment_method IN ('CASH', 'BANK_TRANSFER', 'UPI', 'CHEQUE', 'OTHER')");
                    table.CheckConstraint("ck_supplier_payments_status", "status IN ('COMPLETED', 'REVERSED')");
                    table.ForeignKey(
                        name: "fk_supplier_payments_currency",
                        column: x => x.currency_id,
                        principalTable: "currencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_payments_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_payments_supplier",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_invoice_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expense_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    stock_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    farm_area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    plantation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    crop_cycle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_invoice_lines", x => x.id);
                    table.CheckConstraint("ck_purchase_invoice_lines_amount", "line_amount >= 0 AND unit_price >= 0");
                    table.CheckConstraint("ck_purchase_invoice_lines_line_type", "line_type IN ('INVENTORY_ITEM', 'NON_INVENTORY_EXPENSE')");
                    table.CheckConstraint("ck_purchase_invoice_lines_qty", "quantity IS NULL OR quantity > 0");
                    table.ForeignKey(
                        name: "fk_purchase_invoice_lines_crop_cycle",
                        column: x => x.crop_cycle_id,
                        principalTable: "crop_cycles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_invoice_lines_expense_category",
                        column: x => x.expense_category_id,
                        principalTable: "expense_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_invoice_lines_farm_area",
                        column: x => x.farm_area_id,
                        principalTable: "farm_areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_invoice_lines_inventory_item",
                        column: x => x.inventory_item_id,
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_invoice_lines_invoice",
                        column: x => x.purchase_invoice_id,
                        principalTable: "purchase_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_purchase_invoice_lines_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_invoice_lines_plantation",
                        column: x => x.plantation_id,
                        principalTable: "crop_plantations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_invoice_lines_stock_unit",
                        column: x => x.stock_unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_payment_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocated_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_payment_allocations", x => x.id);
                    table.CheckConstraint("ck_supplier_payment_allocations_amount", "allocated_amount > 0");
                    table.ForeignKey(
                        name: "fk_supplier_payment_allocations_invoice",
                        column: x => x.purchase_invoice_id,
                        principalTable: "purchase_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_payment_allocations_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_payment_allocations_payment",
                        column: x => x.supplier_payment_id,
                        principalTable: "supplier_payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_invoice_receipt_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_invoice_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_movement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    received_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_invoice_receipt_lines", x => x.id);
                    table.CheckConstraint("ck_purchase_invoice_receipt_lines_qty", "received_quantity > 0");
                    table.ForeignKey(
                        name: "fk_purchase_invoice_receipt_lines_invoice",
                        column: x => x.purchase_invoice_id,
                        principalTable: "purchase_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_invoice_receipt_lines_line",
                        column: x => x.purchase_invoice_line_id,
                        principalTable: "purchase_invoice_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_invoice_receipt_lines_movement",
                        column: x => x.stock_movement_id,
                        principalTable: "stock_movements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_invoice_receipt_lines_organization",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_expense_categories_organization_id",
                table: "expense_categories",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_expense_categories_organization_name",
                table: "expense_categories",
                columns: new[] { "organization_id", "name" });

            migrationBuilder.CreateIndex(
                name: "IX_expenses_crop_cycle_id",
                table: "expenses",
                column: "crop_cycle_id");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_currency_id",
                table: "expenses",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_expense_category_id",
                table: "expenses",
                column: "expense_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_farm_area_id",
                table: "expenses",
                column: "farm_area_id");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_farm_id",
                table: "expenses",
                column: "farm_id");

            migrationBuilder.CreateIndex(
                name: "ix_expenses_org_category",
                table: "expenses",
                columns: new[] { "organization_id", "expense_category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_expenses_org_farm_date",
                table: "expenses",
                columns: new[] { "organization_id", "farm_id", "expense_date" });

            migrationBuilder.CreateIndex(
                name: "ix_expenses_org_status",
                table: "expenses",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_expenses_organization_id",
                table: "expenses",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_plantation_id",
                table: "expenses",
                column: "plantation_id");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_supplier_id",
                table: "expenses",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_invoice_lines_crop_cycle_id",
                table: "purchase_invoice_lines",
                column: "crop_cycle_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_invoice_lines_expense_category_id",
                table: "purchase_invoice_lines",
                column: "expense_category_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_invoice_lines_farm_area_id",
                table: "purchase_invoice_lines",
                column: "farm_area_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_invoice_lines_inventory_item_id",
                table: "purchase_invoice_lines",
                column: "inventory_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoice_lines_invoice_id",
                table: "purchase_invoice_lines",
                column: "purchase_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoice_lines_organization_id",
                table: "purchase_invoice_lines",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_invoice_lines_plantation_id",
                table: "purchase_invoice_lines",
                column: "plantation_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_invoice_lines_stock_unit_id",
                table: "purchase_invoice_lines",
                column: "stock_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoice_receipt_lines_invoice_id",
                table: "purchase_invoice_receipt_lines",
                column: "purchase_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoice_receipt_lines_line_id",
                table: "purchase_invoice_receipt_lines",
                column: "purchase_invoice_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoice_receipt_lines_movement",
                table: "purchase_invoice_receipt_lines",
                column: "stock_movement_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoice_receipt_lines_org_id",
                table: "purchase_invoice_receipt_lines",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_invoices_currency_id",
                table: "purchase_invoices",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_invoices_farm_id",
                table: "purchase_invoices",
                column: "farm_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoices_org_farm_date",
                table: "purchase_invoices",
                columns: new[] { "organization_id", "farm_id", "invoice_date" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoices_org_status",
                table: "purchase_invoices",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoices_org_supplier_invoice_number",
                table: "purchase_invoices",
                columns: new[] { "organization_id", "supplier_id", "supplier_invoice_number" },
                unique: true,
                filter: "status != 'REVERSED'");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_invoices_organization_id",
                table: "purchase_invoices",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_invoices_supplier_id",
                table: "purchase_invoices",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payment_allocations_invoice_id",
                table: "supplier_payment_allocations",
                column: "purchase_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payment_allocations_org_id",
                table: "supplier_payment_allocations",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payment_allocations_payment_id",
                table: "supplier_payment_allocations",
                column: "supplier_payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_payments_currency_id",
                table: "supplier_payments",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payments_org_idempotency",
                table: "supplier_payments",
                columns: new[] { "organization_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payments_org_supplier_date",
                table: "supplier_payments",
                columns: new[] { "organization_id", "supplier_id", "payment_date" });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payments_organization_id",
                table: "supplier_payments",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_payments_supplier_id",
                table: "supplier_payments",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_organization_id",
                table: "suppliers",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_organization_name",
                table: "suppliers",
                columns: new[] { "organization_id", "name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expenses");

            migrationBuilder.DropTable(
                name: "purchase_invoice_receipt_lines");

            migrationBuilder.DropTable(
                name: "supplier_payment_allocations");

            migrationBuilder.DropTable(
                name: "purchase_invoice_lines");

            migrationBuilder.DropTable(
                name: "supplier_payments");

            migrationBuilder.DropTable(
                name: "expense_categories");

            migrationBuilder.DropTable(
                name: "purchase_invoices");

            migrationBuilder.DropTable(
                name: "suppliers");
        }
    }
}
