using FarmManagement.Domain.Constants;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FarmManagement.Infrastructure.Persistence.Seed;

public sealed class IdentityDataSeeder(
    ApplicationDbContext dbContext,
    IConfiguration configuration,
    ILogger<IdentityDataSeeder> logger)
{
    private const string DevelopmentOrganizationName = "Demo Farm Organization";
    private const string DevelopmentOrganizationCode = "DEMO";

    private static readonly IReadOnlyList<SeedRole> SeedRoles =
    [
        new("SuperAdmin", "Platform-wide administrator."),
        new("OrganizationAdmin", "Administrator for an organization."),
        new("FarmManager", "Manager of farm operations."),
        new("Supervisor", "Supervisor of farm work."),
        new("Worker", "Farm worker.")
    ];

    private static readonly IReadOnlyList<SeedPermission> SeedPermissions =
    [
        new("Users.View", "View users.", "Administration"),
        new("Users.Create", "Create users.", "Administration"),
        new("Users.Update", "Update users.", "Administration"),
        new("Users.Activate", "Activate users.", "Administration"),
        new("Users.Deactivate", "Deactivate users.", "Administration"),
        new("Users.Unlock", "Unlock users.", "Administration"),
        new("Users.ManageRoles", "Manage user roles.", "Administration"),
        new("Roles.View", "View roles.", "Roles"),
        new("Roles.Create", "Create roles.", "Roles"),
        new("Roles.Update", "Update roles.", "Roles"),
        new("Roles.Activate", "Activate roles.", "Roles"),
        new("Roles.Deactivate", "Deactivate roles.", "Roles"),
        new("Roles.ManagePermissions", "Manage role permissions.", "Roles"),
        new("Permissions.View", "View permissions.", "Permissions"),
        new("Organization.Create", "Create organizations.", "Organization"),
        new("Organization.View", "View the current organization.", "Organization"),
        new("Organization.Update", "Update the current organization.", "Organization"),
        new("Organization.Activate", "Activate the current organization.", "Organization"),
        new("Organization.Deactivate", "Deactivate the current organization.", "Organization"),
        new("Farm.View", "View farms.", "Farms"),
        new("Farm.Create", "Create farms.", "Farms"),
        new("Farm.Update", "Update farms.", "Farms"),
        new("Farm.Activate", "Activate farms.", "Farms"),
        new("Farm.Deactivate", "Deactivate farms.", "Farms"),
        new("FarmArea.View", "View farm areas.", "Farm Areas"),
        new("FarmArea.Create", "Create farm areas.", "Farm Areas"),
        new("FarmArea.Update", "Update farm areas.", "Farm Areas"),
        new("FarmArea.Activate", "Activate farm areas.", "Farm Areas"),
        new("FarmArea.Deactivate", "Deactivate farm areas.", "Farm Areas"),
        new("Crop.View", "View crops.", "Crops"),
        new("Crop.Create", "Create crops.", "Crops"),
        new("Crop.Update", "Update crops.", "Crops"),
        new("Crop.Activate", "Activate crops.", "Crops"),
        new("Crop.Deactivate", "Deactivate crops.", "Crops"),
        new("CropVariety.View", "View crop varieties.", "Crop Varieties"),
        new("CropVariety.Create", "Create crop varieties.", "Crop Varieties"),
        new("CropVariety.Update", "Update crop varieties.", "Crop Varieties"),
        new("CropVariety.Activate", "Activate crop varieties.", "Crop Varieties"),
        new("CropVariety.Deactivate", "Deactivate crop varieties.", "Crop Varieties"),
        new("CropLifecycleTemplate.View", "View crop lifecycle templates.", "Crop Lifecycle Templates"),
        new("CropLifecycleTemplate.Create", "Create crop lifecycle templates.", "Crop Lifecycle Templates"),
        new("CropLifecycleTemplate.Update", "Update crop lifecycle templates and stages.", "Crop Lifecycle Templates"),
        new("CropLifecycleTemplate.Activate", "Activate crop lifecycle templates and stages.", "Crop Lifecycle Templates"),
        new("CropLifecycleTemplate.Deactivate", "Deactivate crop lifecycle templates and stages.", "Crop Lifecycle Templates"),
        new("Plantation.View", "View crop plantations.", "Plantations"),
        new("Plantation.Create", "Create crop plantations.", "Plantations"),
        new("Plantation.Update", "Update crop plantations.", "Plantations"),
        new("Plantation.Activate", "Activate crop plantations.", "Plantations"),
        new("Plantation.Terminate", "Terminate crop plantations.", "Plantations"),
        new("CropCycle.View", "View crop cycles.", "Crop Cycles"),
        new("CropCycle.Create", "Create crop cycles.", "Crop Cycles"),
        new("CropCycle.Update", "Update crop cycles.", "Crop Cycles"),
        new("CropCycle.Start", "Start crop cycles.", "Crop Cycles"),
        new("CropCycle.Complete", "Harvest and complete crop cycles.", "Crop Cycles"),
        new("CropCycle.Cancel", "Cancel crop cycles.", "Crop Cycles"),
        new("CropCycleLifecycle.View", "View crop cycle lifecycle progression and stages.", "Crop Cycles"),
        new("CropCycleLifecycle.Start", "Start crop cycle lifecycle.", "Crop Cycles"),
        new("CropCycleLifecycle.UpdateStage", "Complete and update crop cycle stages.", "Crop Cycles"),
        new("CropCycleLifecycle.SkipStage", "Skip crop cycle stages.", "Crop Cycles"),
        new("CropCycleLifecycle.ReopenStage", "Reopen previously completed or skipped crop cycle stages.", "Crop Cycles"),
        new("CropCycleLifecycle.OverrideStage", "Override crop cycle stage status and dates.", "Crop Cycles"),
        new("Unit.View", "View units of measurement.", "Units"),
        new("Unit.Create", "Create units of measurement.", "Units"),
        new("Unit.Update", "Update units of measurement.", "Units"),
        new("Unit.Activate", "Activate units of measurement.", "Units"),
        new("Unit.Deactivate", "Deactivate units of measurement.", "Units"),
        new("PlantationEndReason.View", "View plantation end reasons.", "Plantation End Reasons"),
        new("PlantationEndReason.Create", "Create plantation end reasons.", "Plantation End Reasons"),
        new("PlantationEndReason.Update", "Update plantation end reasons.", "Plantation End Reasons"),
        new("PlantationEndReason.Activate", "Activate plantation end reasons.", "Plantation End Reasons"),
        new("PlantationEndReason.Deactivate", "Deactivate plantation end reasons.", "Plantation End Reasons"),
        new("LaborActivity.View", "View labor activities.", "Labor Activities"),
        new("LaborActivity.Create", "Create labor activities.", "Labor Activities"),
        new("LaborActivity.Update", "Update labor activities.", "Labor Activities"),
        new("LaborActivity.Cancel", "Cancel labor activities.", "Labor Activities"),
        new("LaborActivityType.View", "View labor activity types.", "Labor Activity Types"),
        new("LaborActivityType.Create", "Create labor activity types.", "Labor Activity Types"),
        new("LaborActivityType.Update", "Update labor activity types.", "Labor Activity Types"),
        new("LaborActivityType.Activate", "Activate labor activity types.", "Labor Activity Types"),
        new("LaborActivityType.Deactivate", "Deactivate labor activity types.", "Labor Activity Types"),
        new("Worker.View", "View workers.", "Workers"),
        new("Worker.Create", "Create workers.", "Workers"),
        new("Worker.Update", "Update workers.", "Workers"),
        new("Worker.Activate", "Activate workers.", "Workers"),
        new("Worker.Deactivate", "Deactivate workers.", "Workers"),
        new("Contractor.View", "View contractors.", "Contractors"),
        new("Contractor.Create", "Create contractors.", "Contractors"),
        new("Contractor.Update", "Update contractors.", "Contractors"),
        new("Contractor.Activate", "Activate contractors.", "Contractors"),
        new("Contractor.Deactivate", "Deactivate contractors.", "Contractors"),
        new("LaborCategory.View", "View labor categories.", "Labor Categories"),
        new("LaborCategory.Create", "Create labor categories.", "Labor Categories"),
        new("LaborCategory.Update", "Update labor categories.", "Labor Categories"),
        new("LaborCategory.Activate", "Activate labor categories.", "Labor Categories"),
        new("LaborCategory.Deactivate", "Deactivate labor categories.", "Labor Categories"),
        new("WorkerWage.View", "View labor wage rates.", "Labor Wage Rates"),
        new("WorkerWage.Create", "Create labor wage rates.", "Labor Wage Rates"),
        new("WorkerWage.Update", "Update labor wage rates.", "Labor Wage Rates"),
        new("WorkerPayment.View", "View worker payments.", "Worker Payments"),
        new("WorkerPayment.Create", "Create worker payments.", "Worker Payments"),
        new("WorkerPayment.Cancel", "Cancel worker payments.", "Worker Payments"),
        new("WorkerEarnings.View", "View worker earnings.", "Worker Earnings"),
        new("WorkerEarnings.Approve", "Approve worker earnings.", "Worker Earnings"),
        new("WorkerEarnings.Reverse", "Reverse worker earnings.", "Worker Earnings"),
        new("Attendance.View", "View labor attendance.", "Attendance"),
        new("Attendance.Create", "Create labor attendance.", "Attendance"),
        new("Attendance.Update", "Update labor attendance.", "Attendance"),
        new("Attendance.Finalize", "Finalize labor attendance.", "Attendance"),
        new("InventoryItem.View", "View inventory items.", "Inventory Items"),
        new("InventoryItem.Create", "Create inventory items.", "Inventory Items"),
        new("InventoryItem.Update", "Update inventory items.", "Inventory Items"),
        new("InventoryItem.Activate", "Activate inventory items.", "Inventory Items"),
        new("InventoryItem.Deactivate", "Deactivate inventory items.", "Inventory Items"),
        new("StorageLocation.View", "View storage locations.", "Storage Locations"),
        new("StorageLocation.Create", "Create storage locations.", "Storage Locations"),
        new("StorageLocation.Update", "Update storage locations.", "Storage Locations"),
        new("StorageLocation.Activate", "Activate storage locations.", "Storage Locations"),
        new("StorageLocation.Deactivate", "Deactivate storage locations.", "Storage Locations"),
        new("InventoryStock.View", "View stock overview and ledger.", "Inventory Stock"),
        new("InventoryTransaction.Create", "Post inventory stock transactions.", "Inventory Stock"),
        new("InventoryTransaction.Reverse", "Reverse / void inventory stock transactions.", "Inventory Stock"),
        new("Supplier.View", "View suppliers.", "Suppliers"),
        new("Supplier.Create", "Create suppliers.", "Suppliers"),
        new("Supplier.Update", "Update suppliers.", "Suppliers"),
        new("Supplier.Deactivate", "Deactivate suppliers.", "Suppliers"),
        new("ExpenseCategory.View", "View expense categories.", "Expense Categories"),
        new("ExpenseCategory.Manage", "Manage expense categories.", "Expense Categories"),
        new("Expense.View", "View direct expenses.", "Expenses"),
        new("Expense.Create", "Create direct expenses.", "Expenses"),
        new("Expense.UpdateDraft", "Update draft direct expenses.", "Expenses"),
        new("Expense.Post", "Post direct expenses.", "Expenses"),
        new("Expense.Reverse", "Reverse direct expenses.", "Expenses"),
        new("Expense.Report.View", "View expense reports and summaries.", "Expenses"),
        new("PurchaseInvoice.View", "View purchase invoices.", "Purchase Invoices"),
        new("PurchaseInvoice.Create", "Create purchase invoices.", "Purchase Invoices"),
        new("PurchaseInvoice.UpdateDraft", "Update draft purchase invoices.", "Purchase Invoices"),
        new("PurchaseInvoice.Post", "Post purchase invoices.", "Purchase Invoices"),
        new("PurchaseInvoice.Reverse", "Reverse purchase invoices.", "Purchase Invoices"),
        new("PurchaseInvoice.ReceiveItems", "Receive inventory items from purchase invoices.", "Purchase Invoices"),
        new("SupplierPayment.View", "View supplier payments.", "Supplier Payments"),
        new("SupplierPayment.Create", "Record supplier payments.", "Supplier Payments"),
        new("SupplierPayment.Reverse", "Reverse supplier payments.", "Supplier Payments"),
        new("SupplierBalance.View", "View supplier balances and payables.", "Supplier Payments"),
        new("PlantProtectionProduct.View", "View plant protection products.", "Plant Protection Products"),
        new("PlantProtectionProduct.Create", "Create plant protection products.", "Plant Protection Products"),
        new("PlantProtectionProduct.Update", "Update plant protection products.", "Plant Protection Products"),
        new("PlantProtectionProduct.Activate", "Activate plant protection products.", "Plant Protection Products"),
        new("PlantProtectionProduct.Deactivate", "Deactivate plant protection products.", "Plant Protection Products"),
        new("ProductType.View", "View product types.", "Product Types"),
        new("ProductType.Create", "Create product types.", "Product Types"),
        new("ProductType.Update", "Update product types.", "Product Types"),
        new("ProductType.Activate", "Activate product types.", "Product Types"),
        new("ProductType.Deactivate", "Deactivate product types.", "Product Types"),
        new("Target.View", "View targets.", "Targets"),
        new("Target.Create", "Create targets.", "Targets"),
        new("Target.Update", "Update targets.", "Targets"),
        new("Target.Activate", "Activate targets.", "Targets"),
        new("Target.Deactivate", "Deactivate targets.", "Targets"),
        new("ApplicationMethod.View", "View application methods.", "Application Methods"),
        new("ApplicationMethod.Create", "Create application methods.", "Application Methods"),
        new("ApplicationMethod.Update", "Update application methods.", "Application Methods"),
        new("ApplicationMethod.Activate", "Activate application methods.", "Application Methods"),
        new("ApplicationMethod.Deactivate", "Deactivate application methods.", "Application Methods"),
        new("Spray.View", "View spray applications.", "Sprays"),
        new("Spray.Create", "Create spray applications.", "Sprays"),
        new("Spray.Update", "Update draft spray applications.", "Sprays"),
        new("Spray.Schedule", "Schedule and reschedule spray applications.", "Sprays"),
        new("Spray.Start", "Start spray application executions.", "Sprays"),
        new("Spray.Complete", "Complete spray applications and consume stock.", "Sprays"),
        new("Spray.Cancel", "Cancel spray applications.", "Sprays")
    ];

    private static readonly IReadOnlySet<string> OrganizationAdminPermissions =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Users.View",
            "Users.Create",
            "Users.Update",
            "Users.Activate",
            "Users.Deactivate",
            "Users.Unlock",
            "Users.ManageRoles",
            "Roles.View",
            "Permissions.View",
            "Organization.View",
            "Organization.Update",
            "Organization.Activate",
            "Organization.Deactivate",
            "Farm.View",
            "Farm.Create",
            "Farm.Update",
            "Farm.Activate",
            "Farm.Deactivate",
            "FarmArea.View",
            "FarmArea.Create",
            "FarmArea.Update",
            "FarmArea.Activate",
            "FarmArea.Deactivate",
            "Crop.View",
            "Crop.Create",
            "Crop.Update",
            "Crop.Activate",
            "Crop.Deactivate",
            "CropVariety.View",
            "CropVariety.Create",
            "CropVariety.Update",
            "CropVariety.Activate",
            "CropVariety.Deactivate",
            "CropLifecycleTemplate.View",
            "CropLifecycleTemplate.Create",
            "CropLifecycleTemplate.Update",
            "CropLifecycleTemplate.Activate",
            "CropLifecycleTemplate.Deactivate",
            "Plantation.View",
            "Plantation.Create",
            "Plantation.Update",
            "Plantation.Activate",
            "Plantation.Terminate",
            "CropCycle.View",
            "CropCycle.Create",
            "CropCycle.Update",
            "CropCycle.Start",
            "CropCycle.Complete",
            "CropCycle.Cancel",
            "CropCycleLifecycle.View",
            "CropCycleLifecycle.Start",
            "CropCycleLifecycle.UpdateStage",
            "CropCycleLifecycle.SkipStage",
            "CropCycleLifecycle.ReopenStage",
            "CropCycleLifecycle.OverrideStage",
            "Unit.View",
            "Unit.Create",
            "Unit.Update",
            "Unit.Activate",
            "Unit.Deactivate",
            "PlantationEndReason.View",
            "PlantationEndReason.Create",
            "PlantationEndReason.Update",
            "PlantationEndReason.Activate",
            "PlantationEndReason.Deactivate",
            "LaborActivity.View",
            "LaborActivity.Create",
            "LaborActivity.Update",
            "LaborActivity.Cancel",
            "LaborActivityType.View",
            "LaborActivityType.Create",
            "LaborActivityType.Update",
            "LaborActivityType.Activate",
            "LaborActivityType.Deactivate",
            "Worker.View",
            "Worker.Create",
            "Worker.Update",
            "Worker.Activate",
            "Worker.Deactivate",
            "Contractor.View",
            "Contractor.Create",
            "Contractor.Update",
            "Contractor.Activate",
            "Contractor.Deactivate",
            "LaborCategory.View",
            "LaborCategory.Create",
            "LaborCategory.Update",
            "LaborCategory.Activate",
            "LaborCategory.Deactivate",
            "WorkerWage.View",
            "WorkerWage.Create",
            "WorkerWage.Update",
            "WorkerPayment.View",
            "WorkerPayment.Create",
            "WorkerPayment.Cancel",
            "WorkerEarnings.View",
            "WorkerEarnings.Approve",
            "WorkerEarnings.Reverse",
            "Attendance.View",
            "Attendance.Create",
            "Attendance.Update",
            "Attendance.Finalize",
            "InventoryItem.View",
            "InventoryItem.Create",
            "InventoryItem.Update",
            "InventoryItem.Activate",
            "InventoryItem.Deactivate",
            "StorageLocation.View",
            "StorageLocation.Create",
            "StorageLocation.Update",
            "StorageLocation.Activate",
            "StorageLocation.Deactivate",
            "InventoryStock.View",
            "InventoryTransaction.Create",
            "InventoryTransaction.Reverse",
            "Supplier.View",
            "Supplier.Create",
            "Supplier.Update",
            "Supplier.Deactivate",
            "ExpenseCategory.View",
            "ExpenseCategory.Manage",
            "Expense.View",
            "Expense.Create",
            "Expense.UpdateDraft",
            "Expense.Post",
            "Expense.Reverse",
            "Expense.Report.View",
            "PurchaseInvoice.View",
            "PurchaseInvoice.Create",
            "PurchaseInvoice.UpdateDraft",
            "PurchaseInvoice.Post",
            "PurchaseInvoice.Reverse",
            "PurchaseInvoice.ReceiveItems",
            "SupplierPayment.View",
            "SupplierPayment.Create",
            "SupplierPayment.Reverse",
            "SupplierBalance.View",
            "PlantProtectionProduct.View",
            "PlantProtectionProduct.Create",
            "PlantProtectionProduct.Update",
            "PlantProtectionProduct.Activate",
            "PlantProtectionProduct.Deactivate",
            "ProductType.View",
            "ProductType.Create",
            "ProductType.Update",
            "ProductType.Activate",
            "ProductType.Deactivate",
            "Target.View",
            "Target.Create",
            "Target.Update",
            "Target.Activate",
            "Target.Deactivate",
            "ApplicationMethod.View",
            "ApplicationMethod.Create",
            "ApplicationMethod.Update",
            "ApplicationMethod.Activate",
            "ApplicationMethod.Deactivate",
            "Spray.View",
            "Spray.Create",
            "Spray.Update",
            "Spray.Schedule",
            "Spray.Start",
            "Spray.Complete",
            "Spray.Cancel"
        };

    /// <summary>
    /// Default role permissions seeded for the FarmManager role.
    /// Grants operational management for farm areas, crop cycles, labor activities, and inventory operations.
    /// </summary>
    private static readonly IReadOnlySet<string> FarmManagerPermissions =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Farm.View",
            "FarmArea.View",
            "Crop.View",
            "CropVariety.View",
            "CropLifecycleTemplate.View",
            "Plantation.View",
            "Plantation.Create",
            "Plantation.Update",
            "Plantation.Activate",
            "Plantation.Terminate",
            "CropCycle.View",
            "CropCycle.Create",
            "CropCycle.Update",
            "CropCycle.Start",
            "CropCycle.Complete",
            "CropCycleLifecycle.View",
            "CropCycleLifecycle.Start",
            "CropCycleLifecycle.UpdateStage",
            "CropCycleLifecycle.SkipStage",
            "Unit.View",
            "PlantationEndReason.View",
            "LaborActivity.View",
            "LaborActivity.Create",
            "LaborActivity.Update",
            "LaborActivity.Cancel",
            "LaborActivityType.View",
            "Worker.View",
            "Contractor.View",
            "LaborCategory.View",
            "WorkerWage.View",
            "Attendance.View",
            "Attendance.Create",
            "Attendance.Update",
            "Attendance.Finalize",
            "InventoryItem.View",
            "StorageLocation.View",
            "StorageLocation.Create",
            "StorageLocation.Update",
            "InventoryStock.View",
            "InventoryTransaction.Create",
            "InventoryTransaction.Reverse",
            "Supplier.View",
            "Supplier.Create",
            "Supplier.Update",
            "ExpenseCategory.View",
            "Expense.View",
            "Expense.Create",
            "Expense.UpdateDraft",
            "Expense.Post",
            "Expense.Report.View",
            "PurchaseInvoice.View",
            "PurchaseInvoice.Create",
            "PurchaseInvoice.UpdateDraft",
            "PurchaseInvoice.Post",
            "PurchaseInvoice.ReceiveItems",
            "SupplierPayment.View",
            "SupplierPayment.Create",
            "SupplierBalance.View",
            "PlantProtectionProduct.View",
            "ProductType.View",
            "Target.View",
            "ApplicationMethod.View",
            "Spray.View",
            "Spray.Create",
            "Spray.Update",
            "Spray.Schedule",
            "Spray.Start",
            "Spray.Complete",
            "Spray.Cancel"
        };

    /// <summary>
    /// Default role permissions seeded for the Supervisor role.
    /// Grants field-level execution access for labor activities, attendance logging, inventory viewing, and posting stock transactions.
    /// </summary>
    private static readonly IReadOnlySet<string> SupervisorPermissions =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Farm.View",
            "FarmArea.View",
            "Crop.View",
            "CropVariety.View",
            "Plantation.View",
            "CropCycle.View",
            "CropCycleLifecycle.View",
            "CropCycleLifecycle.UpdateStage",
            "Unit.View",
            "LaborActivity.View",
            "LaborActivity.Create",
            "LaborActivity.Update",
            "LaborActivityType.View",
            "Worker.View",
            "Attendance.View",
            "Attendance.Create",
            "Attendance.Update",
            "InventoryItem.View",
            "StorageLocation.View",
            "InventoryStock.View",
            "InventoryTransaction.Create",
            "PlantProtectionProduct.View",
            "ProductType.View",
            "Target.View",
            "ApplicationMethod.View",
            "Spray.View",
            "Spray.Start",
            "Spray.Complete"
        };

    private static readonly IReadOnlyList<SeedFarmOwnershipType> SeedFarmOwnershipTypes =
    [
        new("OWNED", "Owned"),
        new("LEASED", "Leased"),
        new("RENTED", "Rented"),
        new("MANAGED", "Managed"),
        new("OTHER", "Other")
    ];

    private static readonly IReadOnlyList<SeedUnit> SeedUnits =
    [
        new("ACRE", "Acre", "ac", UnitCategory.Area, "SQUARE_METER", 4046.8564224m, 10),
        new("HECTARE", "Hectare", "ha", UnitCategory.Area, "SQUARE_METER", 10000m, 20),
        new("SQUARE_METER", "Square Meter", "m²", UnitCategory.Area, "SQUARE_METER", 1m, 30),
        new("SQUARE_FEET", "Square Feet", "ft²", UnitCategory.Area, "SQUARE_METER", 0.09290304m, 40),
        new("KILOGRAM", "Kilogram", "kg", UnitCategory.Weight, "KILOGRAM", 1m, 10),
        new("GRAM", "Gram", "g", UnitCategory.Weight, "KILOGRAM", 0.001m, 20),
        new("TON", "Ton", "t", UnitCategory.Weight, "KILOGRAM", 1000m, 30),
        new("QUINTAL", "Quintal", "q", UnitCategory.Weight, "KILOGRAM", 100m, 40),
        new("LITER", "Liter", "L", UnitCategory.Volume, "LITER", 1m, 10),
        new("MILLILITER", "Milliliter", "mL", UnitCategory.Volume, "LITER", 0.001m, 20),
        new("METER", "Meter", "m", UnitCategory.Length, "METER", 1m, 10),
        new("CENTIMETER", "Centimeter", "cm", UnitCategory.Length, "METER", 0.01m, 20),
        new("FOOT", "Foot", "ft", UnitCategory.Length, "METER", 0.3048m, 30),
        new("NUMBER", "Number", "#", UnitCategory.Count, "NUMBER", 1m, 10),
        new("PIECE", "Piece", "pc", UnitCategory.Count, "NUMBER", 1m, 20),
        new("PLANT", "Plant", "plant", UnitCategory.Count, "NUMBER", 1m, 30)
    ];

    private static readonly IReadOnlyList<SeedCurrency> SeedCurrencies =
    [
        new("INR", "Indian Rupee", "₹", 1, Guid.Parse("10000000-0000-0000-0000-000000000001")),
        new("USD", "US Dollar", "$", 2, Guid.Parse("10000000-0000-0000-0000-000000000002")),
        new("EUR", "Euro", "€", 3, Guid.Parse("10000000-0000-0000-0000-000000000003")),
        new("GBP", "British Pound", "£", 4, Guid.Parse("10000000-0000-0000-0000-000000000004"))
    ];

    private static readonly IReadOnlyList<SeedCrop> SeedCrops =
    [
        new("Grapes", "FRUIT", "PERENNIAL"),
        new("Tomato", "VEGETABLE", "ANNUAL"),
        new("Chili", "VEGETABLE", "SEASONAL"),
        new("Mango", "FRUIT", "PERENNIAL"),
        new("Banana", "FRUIT", "PERENNIAL")
    ];

    private static readonly IReadOnlyList<SeedCropVariety> SeedCropVarieties =
    [
        new("Grapes", "THOMPSON_SEEDLESS", "Thompson Seedless"),
        new("Grapes", "SHARAD_SEEDLESS", "Sharad Seedless"),
        new("Grapes", "SONAKA", "Sonaka"),
        new("Grapes", "MANIK_CHAMAN", "Manik Chaman")
    ];

    private static readonly IReadOnlyList<SeedLifecycleTemplate> SeedLifecycleTemplates =
    [
        new(
            CropName: "Grapes",
            Name: "Grape Standard Lifecycle",
            Description: "Standard lifecycle for grape production.",
            IsDefault: true,
            Stages:
            [
                new("Dormancy", 1, 30, "Vine dormancy period."),
                new("Pruning", 2, 15, "Vine pruning and canopy preparation."),
                new("Bud Break", 3, 10, "Initial vine bud emergence."),
                new("Shoot Development", 4, 20, "Shoot elongation and foliage development."),
                new("Flowering", 5, 7, "Vine flowering and bloom stage."),
                new("Fruit Set", 6, 10, "Berry formation and initial fruit set."),
                new("Berry Development", 7, 30, "Berry growth and cluster development."),
                new("Ripening", 8, 25, "Berry veraison and sugar accumulation."),
                new("Harvest", 9, 15, "Fruit harvesting stage.")
            ])
    ];

    private static readonly IReadOnlyList<SeedPlantationEndReason> SeedPlantationEndReasons =
    [
        new("HARVEST_COMPLETED", "Harvest Completed"),
        new("WEATHER_DISASTER", "Weather Disaster"),
        new("FLOOD", "Flood"),
        new("DROUGHT", "Drought"),
        new("CYCLONE", "Cyclone"),
        new("PEST_INFESTATION", "Pest Infestation"),
        new("DISEASE", "Disease"),
        new("CROP_FAILURE", "Crop Failure"),
        new("POOR_CROP_HEALTH", "Poor Crop Health"),
        new("SOIL_PROBLEM", "Soil Problem"),
        new("REPLANT_REQUIRED", "Replant Required"),
        new("FARMER_DECISION", "Farmer Decision"),
        new("OTHER", "Other")
    ];

    private static readonly IReadOnlyList<SeedLaborActivityType> SeedLaborActivityTypes =
    [
        new("PLANTING", "Planting", 1, "Planting crops or saplings."),
        new("TRANSPLANTING", "Transplanting", 2, "Transplanting seedlings."),
        new("PRUNING", "Pruning", 3, "Pruning plants or trees."),
        new("WEEDING", "Weeding", 4, "Weeding and weed removal."),
        new("SPRAYING_ASSISTANCE", "Spraying Assistance", 5, "Assisting in spraying operations."),
        new("FERTILIZER_APPLICATION", "Fertilizer Application", 6, "Applying fertilizers to crops."),
        new("IRRIGATION_WORK", "Irrigation Work", 7, "Irrigation operations and maintenance."),
        new("CLEANING", "Cleaning", 8, "Cleaning and clearing plots."),
        new("SOIL_PREPARATION", "Soil Preparation", 9, "Preparing soil and beds."),
        new("GENERAL_MAINTENANCE", "General Maintenance", 10, "General farm and field maintenance."),
        new("TYING", "Tying", 11, "Tying and supporting vines or plants."),
        new("HARVESTING", "Harvesting", 12, "Harvesting produce."),
        new("OTHER", "Other", 13, "Other labor activities.")
    ];

    private static readonly IReadOnlyList<SeedLaborCategory> SeedLaborCategories =
    [
        new("Skilled", "Skilled labor"),
        new("Semi-Skilled", "Semi-skilled labor"),
        new("Unskilled", "General and unskilled labor"),
        new("Supervisor", "Field or crew supervisor"),
        new("Operator", "Equipment or machinery operator"),
        new("Specialized", "Specialized farming or technical labor")
    ];

    private static readonly IReadOnlyList<SeedProductType> SeedProductTypes =
    [
        new("FUNGICIDE", "Fungicide", 10, "Controls fungal infections and diseases."),
        new("INSECTICIDE", "Insecticide", 20, "Controls insect pests."),
        new("MITICIDE", "Miticide", 30, "Controls mites and ticks (acaricide)."),
        new("BACTERICIDE", "Bactericide", 40, "Controls bacterial infections."),
        new("HERBICIDE", "Herbicide", 50, "Controls weeds and unwanted vegetation."),
        new("NEMATICIDE", "Nematicide", 60, "Controls parasitic plant nematodes."),
        new("BIOLOGICAL", "Biological", 70, "Biological control agents and bio-pesticides."),
        new("ADJUVANT", "Adjuvant", 80, "Spreading agents, stickers, and tank-mix adjuvants."),
        new("OTHER", "Other", 90, "Other plant protection and spray products.")
    ];

    private static readonly IReadOnlyList<SeedTarget> SeedTargets =
    [
        new("POWDERY_MILDEW", "Powdery Mildew", TargetType.Disease, 10, "Fungal disease causing white powdery spots on leaves and stems."),
        new("DOWNY_MILDEW", "Downy Mildew", TargetType.Disease, 20, "Fungal-like organism causing yellow patches and mold underneath leaves."),
        new("ANTHRACNOSE", "Anthracnose", TargetType.Disease, 30, "Fungal disease causing dark, sunken lesions on leaves, stems, or fruit."),
        new("BACTERIAL_DISEASE", "Bacterial Disease", TargetType.Disease, 40, "General bacterial blight, spot, or wilt."),
        new("OTHER_DISEASE", "Other Disease", TargetType.Disease, 50, "Other plant diseases."),
        new("THRIPS", "Thrips", TargetType.Insect, 60, "Minute insects feeding on sap causing curled leaves and silvering."),
        new("MEALYBUG", "Mealybug", TargetType.Insect, 70, "Unarmored scale insects secreting honeydew and causing sooty mold."),
        new("APHIDS", "Aphids", TargetType.Insect, 80, "Sap-sucking insects causing stunted growth and leaf distortion."),
        new("FRUIT_FLY", "Fruit Fly", TargetType.Insect, 90, "Pest larvae infesting fruit flesh."),
        new("OTHER_INSECT", "Other Insect", TargetType.Insect, 100, "Other insect pests."),
        new("RED_SPIDER_MITE", "Red Spider Mite", TargetType.Mite, 110, "Tetranychid mite causing yellow stippling and webbing on leaves."),
        new("OTHER_MITE", "Other Mite", TargetType.Mite, 120, "Other mite pests."),
        new("GENERAL_WEED", "General Weed", TargetType.Weed, 130, "General annual and perennial weed control."),
        new("OTHER_WEED", "Other Weed", TargetType.Weed, 140, "Other broadleaf or grassy weeds."),
        new("GENERAL_PREVENTIVE", "General Preventive", TargetType.Other, 150, "Preventive, prophylactic spray application."),
        new("OTHER", "Other", TargetType.Other, 160, "Other targets.")
    ];

    private static readonly IReadOnlyList<SeedApplicationMethod> SeedApplicationMethods =
    [
        new("KNAPSACK_SPRAYER", "Knapsack Sprayer", 10, "Manual or battery-operated backpack knapsack sprayer."),
        new("POWER_SPRAYER", "Power Sprayer", 20, "Engine or motor-driven high-pressure power sprayer."),
        new("TRACTOR_SPRAYER", "Tractor Sprayer", 30, "Tractor-mounted boom or air-blast sprayer."),
        new("DRONE_SPRAYER", "Drone Sprayer", 40, "Agricultural spraying drone (UAV)."),
        new("MANUAL", "Manual", 50, "Manual drenching, brush, or hand application."),
        new("OTHER", "Other", 60, "Other application equipment or methods.")
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var initialAdmin = ReadInitialAdminConfiguration();

        var isRelational = dbContext.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) != true;

        if (isRelational)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await SeedCoreAsync(initialAdmin, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        else
        {
            await SeedCoreAsync(initialAdmin, cancellationToken);
        }

        logger.LogInformation("Identity data seeding completed.");
    }

    private async Task SeedCoreAsync(InitialAdminConfiguration? initialAdmin, CancellationToken cancellationToken)
    {
        var organization = await SeedOrganizationAsync(cancellationToken);
        var roles = await SeedRolesAsync(cancellationToken);
        var permissions = await SeedPermissionsAsync(cancellationToken);

        await SeedRolePermissionsAsync(roles, permissions, cancellationToken);
        await SeedCurrenciesAsync(cancellationToken);
        await SeedUnitsAsync(cancellationToken);
        await SeedFarmOwnershipTypesAsync(cancellationToken);
        await SeedCropsAsync(cancellationToken);
        await SeedCropVarietiesAsync(cancellationToken);
        await SeedCropLifecycleTemplatesAsync(cancellationToken);
        await SeedPlantationEndReasonsAsync(cancellationToken);
        await SeedLaborActivityTypesAsync(cancellationToken);
        await SeedLaborCategoriesAsync(cancellationToken);
        await SeedExpenseCategoriesAsync(cancellationToken);
        await SeedInventoryItemCategoriesAsync(cancellationToken);
        await SeedProductTypesAsync(cancellationToken);
        await SeedTargetsAsync(cancellationToken);
        await SeedApplicationMethodsAsync(cancellationToken);
        await SeedInitialSuperAdminAsync(organization, roles["SuperAdmin"], initialAdmin, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedCurrenciesAsync(CancellationToken cancellationToken)
    {
        foreach (var seedCurrency in SeedCurrencies)
        {
            var exists = await dbContext.Currencies
                .AnyAsync(currency => currency.Code == seedCurrency.Code, cancellationToken);

            if (exists)
            {
                continue;
            }

            dbContext.Currencies.Add(new Currency(
                code: seedCurrency.Code,
                name: seedCurrency.Name,
                symbol: seedCurrency.Symbol,
                isSystem: true,
                displayOrder: seedCurrency.DisplayOrder,
                id: seedCurrency.Id));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedUnitsAsync(CancellationToken cancellationToken)
    {
        foreach (var seedUnit in SeedUnits)
        {
            var unitExists = await dbContext.Units
                .AnyAsync(
                    unit => unit.OrganizationId == null && unit.Code == seedUnit.Code,
                    cancellationToken);

            if (unitExists)
            {
                continue;
            }

            dbContext.Units.Add(new Unit(
                organizationId: null,
                code: seedUnit.Code,
                name: seedUnit.Name,
                symbol: seedUnit.Symbol,
                unitCategory: seedUnit.Category,
                baseUnitCode: seedUnit.BaseUnitCode,
                conversionFactor: seedUnit.ConversionFactor,
                isSystem: true,
                displayOrder: seedUnit.DisplayOrder));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedFarmOwnershipTypesAsync(CancellationToken cancellationToken)
    {
        foreach (var seedType in SeedFarmOwnershipTypes)
        {
            var exists = await dbContext.FarmOwnershipTypes
                .AnyAsync(item => item.Code == seedType.Code, cancellationToken);
            if (!exists)
            {
                dbContext.FarmOwnershipTypes.Add(new FarmOwnershipType(seedType.Code, seedType.Name));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedCropsAsync(CancellationToken cancellationToken)
    {
        foreach (var seedCrop in SeedCrops)
        {
            var exists = await dbContext.Crops.AnyAsync(
                crop => crop.IsSystem && crop.OrganizationId == null && crop.Name == seedCrop.Name,
                cancellationToken);
            if (!exists)
            {
                dbContext.Crops.Add(new Crop(
                    organizationId: null,
                    name: seedCrop.Name,
                    cropType: seedCrop.CropType,
                    cropDurationType: seedCrop.DurationType,
                    isSystem: true));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedCropVarietiesAsync(CancellationToken cancellationToken)
    {
        foreach (var seedVariety in SeedCropVarieties)
        {
            var crop = await dbContext.Crops.SingleAsync(
                item => item.IsSystem && item.OrganizationId == null && item.Name == seedVariety.CropName,
                cancellationToken);
            var exists = await dbContext.CropVarieties.AnyAsync(
                variety => variety.IsSystem && variety.OrganizationId == null &&
                           variety.CropId == crop.Id && variety.Code == seedVariety.Code,
                cancellationToken);
            if (!exists)
            {
                dbContext.CropVarieties.Add(new CropVariety(
                    organizationId: null,
                    cropId: crop.Id,
                    code: seedVariety.Code,
                    name: seedVariety.Name,
                    isSystem: true));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedCropLifecycleTemplatesAsync(CancellationToken cancellationToken)
    {
        foreach (var seedTemplate in SeedLifecycleTemplates)
        {
            var crop = await dbContext.Crops.FirstOrDefaultAsync(
                item => item.IsSystem && item.OrganizationId == null && (item.Name == seedTemplate.CropName || item.Name == "Grape"),
                cancellationToken);

            if (crop is null)
            {
                crop = new Crop(
                    organizationId: null,
                    name: seedTemplate.CropName,
                    cropType: "FRUIT",
                    cropDurationType: "PERENNIAL",
                    isSystem: true);

                dbContext.Crops.Add(crop);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var existingTemplate = await dbContext.CropLifecycleTemplates
                .Include(t => t.Stages)
                .FirstOrDefaultAsync(
                    t => t.IsSystem &&
                         t.OrganizationId == null &&
                         t.CropId == crop.Id &&
                         t.Name == seedTemplate.Name,
                    cancellationToken);

            if (existingTemplate is null)
            {
                var hasDefault = await dbContext.CropLifecycleTemplates
                    .AnyAsync(
                        t => t.CropId == crop.Id &&
                             t.OrganizationId == null &&
                             t.IsDefault,
                        cancellationToken);

                var template = new CropLifecycleTemplate(
                    organizationId: null,
                    cropId: crop.Id,
                    name: seedTemplate.Name,
                    isDefault: !hasDefault && seedTemplate.IsDefault,
                    isSystem: true,
                    description: seedTemplate.Description);

                dbContext.CropLifecycleTemplates.Add(template);

                foreach (var seedStage in seedTemplate.Stages)
                {
                    var stage = new CropLifecycleStage(
                        lifecycleTemplateId: template.Id,
                        stageName: seedStage.Name,
                        sequenceNumber: seedStage.SequenceNumber,
                        expectedDurationDays: seedStage.ExpectedDurationDays,
                        description: seedStage.Description);

                    dbContext.CropLifecycleStages.Add(stage);
                }
            }
            else
            {
                var existingSequences = existingTemplate.Stages
                    .Select(s => s.SequenceNumber)
                    .ToHashSet();

                foreach (var seedStage in seedTemplate.Stages)
                {
                    if (!existingSequences.Contains(seedStage.SequenceNumber))
                    {
                        var stage = new CropLifecycleStage(
                            lifecycleTemplateId: existingTemplate.Id,
                            stageName: seedStage.Name,
                            sequenceNumber: seedStage.SequenceNumber,
                            expectedDurationDays: seedStage.ExpectedDurationDays,
                            description: seedStage.Description);

                        dbContext.CropLifecycleStages.Add(stage);
                    }
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedPlantationEndReasonsAsync(CancellationToken cancellationToken)
    {
        foreach (var seedReason in SeedPlantationEndReasons)
        {
            var exists = await dbContext.PlantationEndReasons.AnyAsync(
                reason => reason.IsSystem && reason.OrganizationId == null && reason.Code == seedReason.Code,
                cancellationToken);
            if (!exists)
            {
                dbContext.PlantationEndReasons.Add(new PlantationEndReason(
                    organizationId: null,
                    code: seedReason.Code,
                    name: seedReason.Name,
                    isSystem: true));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedLaborActivityTypesAsync(CancellationToken cancellationToken)
    {
        foreach (var seedType in SeedLaborActivityTypes)
        {
            var exists = await dbContext.LaborActivityTypes.AnyAsync(
                type => type.IsSystem && type.OrganizationId == null && type.Code == seedType.Code,
                cancellationToken);

            if (!exists)
            {
                dbContext.LaborActivityTypes.Add(new LaborActivityType(
                    organizationId: null,
                    code: seedType.Code,
                    name: seedType.Name,
                    isSystem: true,
                    description: seedType.Description,
                    displayOrder: seedType.DisplayOrder));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedLaborCategoriesAsync(CancellationToken cancellationToken)
    {
        foreach (var seedCategory in SeedLaborCategories)
        {
            var exists = await dbContext.LaborCategories.AnyAsync(
                category => category.IsSystem && category.OrganizationId == null && category.Name == seedCategory.Name,
                cancellationToken);

            if (!exists)
            {
                dbContext.LaborCategories.Add(new LaborCategory(
                    organizationId: null,
                    name: seedCategory.Name,
                    isSystem: true,
                    description: seedCategory.Description));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedExpenseCategoriesAsync(CancellationToken cancellationToken)
    {
        foreach (var seedCategory in SeedExpenseCategories)
        {
            var exists = await dbContext.ExpenseCategories.AnyAsync(
                category => category.IsSystemDefault && category.OrganizationId == null && category.Name == seedCategory.Name,
                cancellationToken);

            if (!exists)
            {
                dbContext.ExpenseCategories.Add(new ExpenseCategory(
                    organizationId: null,
                    name: seedCategory.Name,
                    code: seedCategory.Code,
                    isSystemDefault: true,
                    description: seedCategory.Description));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedInventoryItemCategoriesAsync(CancellationToken cancellationToken)
    {
        foreach (var def in FarmInventoryCategories.All)
        {
            var exists = await dbContext.InventoryItemCategories.AnyAsync(
                cat => cat.Id == def.Id || (cat.IsSystem && cat.OrganizationId == null && cat.Code == def.Code),
                cancellationToken);

            if (!exists)
            {
                dbContext.InventoryItemCategories.Add(new InventoryItemCategory(
                    organizationId: null,
                    name: def.Name,
                    code: def.Code,
                    description: def.Description,
                    examples: def.Examples,
                    icon: def.Icon,
                    displayOrder: def.DisplayOrder,
                    isSystem: true,
                    createdBy: null,
                    id: def.Id));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedProductTypesAsync(CancellationToken cancellationToken)
    {
        foreach (var seedType in SeedProductTypes)
        {
            var exists = await dbContext.ProductTypes.AnyAsync(
                type => type.IsSystem && type.OrganizationId == null && type.Code == seedType.Code,
                cancellationToken);

            if (!exists)
            {
                dbContext.ProductTypes.Add(new ProductType(
                    organizationId: null,
                    code: seedType.Code,
                    name: seedType.Name,
                    isSystem: true,
                    description: seedType.Description,
                    displayOrder: seedType.DisplayOrder));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedTargetsAsync(CancellationToken cancellationToken)
    {
        foreach (var seedTarget in SeedTargets)
        {
            var exists = await dbContext.Targets.AnyAsync(
                target => target.IsSystem && target.OrganizationId == null && target.Code == seedTarget.Code,
                cancellationToken);

            if (!exists)
            {
                dbContext.Targets.Add(new Target(
                    organizationId: null,
                    code: seedTarget.Code,
                    name: seedTarget.Name,
                    targetType: seedTarget.TargetType,
                    isSystem: true,
                    description: seedTarget.Description,
                    displayOrder: seedTarget.DisplayOrder));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedApplicationMethodsAsync(CancellationToken cancellationToken)
    {
        foreach (var seedMethod in SeedApplicationMethods)
        {
            var exists = await dbContext.ApplicationMethods.AnyAsync(
                method => method.IsSystem && method.OrganizationId == null && method.Code == seedMethod.Code,
                cancellationToken);

            if (!exists)
            {
                dbContext.ApplicationMethods.Add(new ApplicationMethod(
                    organizationId: null,
                    code: seedMethod.Code,
                    name: seedMethod.Name,
                    isSystem: true,
                    description: seedMethod.Description,
                    displayOrder: seedMethod.DisplayOrder));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Organization> SeedOrganizationAsync(CancellationToken cancellationToken)
    {
        var organization = await dbContext.Organizations
            .SingleOrDefaultAsync(
                item => item.Code == DevelopmentOrganizationCode,
                cancellationToken);

        if (organization is not null)
        {
            return organization;
        }

        organization = new Organization(
            DevelopmentOrganizationName,
            DevelopmentOrganizationCode);

        dbContext.Organizations.Add(organization);
        await dbContext.SaveChangesAsync(cancellationToken);
        return organization;
    }

    private async Task<Dictionary<string, Role>> SeedRolesAsync(CancellationToken cancellationToken)
    {
        var roles = new Dictionary<string, Role>(StringComparer.Ordinal);

        foreach (var seedRole in SeedRoles)
        {
            var role = await dbContext.Roles
                .SingleOrDefaultAsync(item => item.Name == seedRole.Name, cancellationToken);

            if (role is null)
            {
                role = new Role(seedRole.Name, seedRole.Description, isSystemRole: true);
                dbContext.Roles.Add(role);
            }
            else
            {
                role.MarkAsSystemRole();
            }

            roles.Add(seedRole.Name, role);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return roles;
    }

    private async Task<Dictionary<string, Permission>> SeedPermissionsAsync(
        CancellationToken cancellationToken)
    {
        var permissions = new Dictionary<string, Permission>(StringComparer.Ordinal);

        foreach (var seedPermission in SeedPermissions)
        {
            var permission = await dbContext.Permissions
                .SingleOrDefaultAsync(item => item.Name == seedPermission.Name, cancellationToken);

            if (permission is null)
            {
                permission = new Permission(
                    seedPermission.Name,
                    seedPermission.Module,
                    seedPermission.Description);

                dbContext.Permissions.Add(permission);
            }

            permissions.Add(seedPermission.Name, permission);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return permissions;
    }

    private async Task SeedRolePermissionsAsync(
        IReadOnlyDictionary<string, Role> roles,
        IReadOnlyDictionary<string, Permission> permissions,
        CancellationToken cancellationToken)
    {
        var existingAssignments = await dbContext.RolePermissions
            .AsNoTracking()
            .Select(rolePermission => new
            {
                rolePermission.RoleId,
                rolePermission.PermissionId
            })
            .ToListAsync(cancellationToken);

        var existingAssignmentKeys = existingAssignments
            .Select(item => (item.RoleId, item.PermissionId))
            .ToHashSet();

        foreach (var permission in permissions.Values)
        {
            AddRolePermissionIfMissing(
                roles["SuperAdmin"],
                permission,
                existingAssignmentKeys);
        }

        foreach (var permissionName in OrganizationAdminPermissions)
        {
            AddRolePermissionIfMissing(
                roles["OrganizationAdmin"],
                permissions[permissionName],
                existingAssignmentKeys);
        }

        // Seed default permissions for FarmManager role
        if (roles.TryGetValue("FarmManager", out var farmManagerRole))
        {
            foreach (var permissionName in FarmManagerPermissions)
            {
                if (permissions.TryGetValue(permissionName, out var perm))
                {
                    AddRolePermissionIfMissing(farmManagerRole, perm, existingAssignmentKeys);
                }
            }
        }

        // Seed default permissions for Supervisor role
        if (roles.TryGetValue("Supervisor", out var supervisorRole))
        {
            foreach (var permissionName in SupervisorPermissions)
            {
                if (permissions.TryGetValue(permissionName, out var perm))
                {
                    AddRolePermissionIfMissing(supervisorRole, perm, existingAssignmentKeys);
                }
            }
        }
    }

    private void AddRolePermissionIfMissing(
        Role role,
        Permission permission,
        ISet<(Guid RoleId, Guid PermissionId)> existingAssignmentKeys)
    {
        if (!existingAssignmentKeys.Add((role.Id, permission.Id)))
        {
            return;
        }

        dbContext.RolePermissions.Add(new RolePermission(role.Id, permission.Id));
    }

    private async Task SeedInitialSuperAdminAsync(
        Organization organization,
        Role superAdminRole,
        InitialAdminConfiguration? initialAdmin,
        CancellationToken cancellationToken)
    {
        if (initialAdmin is null)
        {
            logger.LogWarning(
                "Initial admin credentials are not configured; the initial SuperAdmin was not created.");
            return;
        }

        var user = await dbContext.Users
            .SingleOrDefaultAsync(
                item => item.Email.ToLower() == initialAdmin.Email,
                cancellationToken);

        if (user is null)
        {
            var existingSuperAdmin = await dbContext.UserRoles
                .AnyAsync(item => item.RoleId == superAdminRole.Id, cancellationToken);

            if (existingSuperAdmin)
            {
                logger.LogInformation(
                    "A SuperAdmin already exists; the configured initial admin was not created.");
                return;
            }

            var passwordHasher = new PasswordHasher<User>();
            var passwordHash = passwordHasher.HashPassword(null!, initialAdmin.Password);

            user = new User(
                organization.Id,
                "Initial",
                "Administrator",
                initialAdmin.Email,
                passwordHash);

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Initial SuperAdmin created.");
        }

        var hasSuperAdminRole = await dbContext.UserRoles
            .AnyAsync(
                item => item.UserId == user.Id && item.RoleId == superAdminRole.Id,
                cancellationToken);

        if (!hasSuperAdminRole)
        {
            dbContext.UserRoles.Add(new UserRole(user.Id, superAdminRole.Id, DateTimeOffset.UtcNow));
        }
    }

    private InitialAdminConfiguration? ReadInitialAdminConfiguration()
    {
        var email = ReadConfigurationValue("InitialAdmin:Email", "INITIAL_ADMIN_EMAIL");
        var password = ReadConfigurationValue("InitialAdmin:Password", "INITIAL_ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Both INITIAL_ADMIN_EMAIL and INITIAL_ADMIN_PASSWORD must be configured together.");
        }

        return new InitialAdminConfiguration(email.Trim().ToLowerInvariant(), password);
    }

    private string? ReadConfigurationValue(string configurationKey, string environmentVariable)
    {
        return configuration[configurationKey]
            ?? configuration[environmentVariable]
            ?? Environment.GetEnvironmentVariable(environmentVariable);
    }

    public static IReadOnlyList<(string Name, string Description, string Module)> SeedPermissionDefinitions =>
        SeedPermissions.Select(p => (p.Name, p.Description, p.Module)).ToArray();

    public static IReadOnlySet<string> OrganizationAdminPermissionNames =>
        OrganizationAdminPermissions;

    private sealed record SeedRole(string Name, string Description);

    private sealed record SeedPermission(string Name, string Description, string Module);

    private sealed record SeedUnit(
        string Code,
        string Name,
        string Symbol,
        UnitCategory Category,
        string BaseUnitCode,
        decimal ConversionFactor,
        int DisplayOrder);

    private sealed record SeedFarmOwnershipType(string Code, string Name);

    private sealed record SeedCrop(string Name, string CropType, string DurationType);

    private sealed record SeedCropVariety(string CropName, string Code, string Name);

    private sealed record SeedPlantationEndReason(string Code, string Name);

    private sealed record SeedLaborActivityType(
        string Code,
        string Name,
        int DisplayOrder = 0,
        string? Description = null);

    private sealed record SeedLaborCategory(string Name, string? Description = null);

    private sealed record SeedCurrency(string Code, string Name, string Symbol, int DisplayOrder, Guid Id);

    private sealed record SeedLifecycleTemplate(
        string CropName,
        string Name,
        string Description,
        bool IsDefault,
        IReadOnlyList<SeedLifecycleStage> Stages);

    private sealed record SeedLifecycleStage(
        string Name,
        int SequenceNumber,
        int? ExpectedDurationDays,
        string? Description = null);

    private static readonly IReadOnlyList<SeedExpenseCategory> SeedExpenseCategories =
    [
        new("Fertilizer", "FERT", "Fertilizers and soil enrichment inputs"),
        new("Pesticides", "PEST", "Pesticides, insecticides, and fungicides"),
        new("Seeds", "SEED", "Seeds and planting material"),
        new("Irrigation", "IRRI", "Irrigation supplies, water, and system maintenance"),
        new("Electricity", "ELEC", "Electricity and power utility costs"),
        new("Fuel", "FUEL", "Diesel, petrol, and fuel for farm machinery"),
        new("Equipment Repair", "REP", "Repairs and maintenance of farm equipment and implements"),
        new("Machinery Rental", "RENT_MACH", "Rental and hire charges for machinery and tractors"),
        new("Transport", "TRN", "Freight, haulage, and transportation costs"),
        new("Rent/Lease", "LEASE", "Land, facility, and storage lease or rent"),
        new("Utilities", "UTIL", "Water, waste, and general utility charges"),
        new("Packaging", "PKG", "Harvest crates, sacks, boxes, and packaging supplies"),
        new("Professional Services", "PROF", "Agronomy consulting, soil testing, legal, and accounting"),
        new("Other", "OTHR", "Miscellaneous operational farm expenses")
    ];

    private sealed record SeedExpenseCategory(string Name, string Code, string? Description = null);

    private sealed record SeedProductType(string Code, string Name, int DisplayOrder, string? Description = null);

    private sealed record SeedTarget(string Code, string Name, TargetType TargetType, int DisplayOrder, string? Description = null);

    private sealed record SeedApplicationMethod(string Code, string Name, int DisplayOrder, string? Description = null);

    private sealed record InitialAdminConfiguration(string Email, string Password);
}
