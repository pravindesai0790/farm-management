using System.Reflection;
using FarmManagement.Infrastructure.Persistence.Seed;
using Xunit;

namespace FarmManagement.API.Tests.Services;

/// <summary>
/// Verifies default role permission seedings in IdentityDataSeeder for FarmManager and Supervisor roles.
/// </summary>
public class IdentityDataSeederTests
{
    [Fact]
    public void FarmManagerPermissions_ContainsInventoryAndOperationalPermissions()
    {
        // Arrange
        var fieldInfo = typeof(IdentityDataSeeder)
            .GetField("FarmManagerPermissions", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(fieldInfo);
        var permissions = (IReadOnlySet<string>)fieldInfo.GetValue(null)!;

        // Assert Inventory Permissions
        Assert.Contains("InventoryItem.View", permissions);
        Assert.Contains("StorageLocation.View", permissions);
        Assert.Contains("StorageLocation.Create", permissions);
        Assert.Contains("StorageLocation.Update", permissions);
        Assert.Contains("InventoryStock.View", permissions);
        Assert.Contains("InventoryTransaction.Create", permissions);

        // Assert Farm Operational Permissions
        Assert.Contains("Farm.View", permissions);
        Assert.Contains("CropCycle.View", permissions);
        Assert.Contains("Attendance.View", permissions);
        Assert.Contains("Attendance.Finalize", permissions);

        // Assert Phase 3.6 Expense Permissions
        Assert.Contains("Supplier.View", permissions);
        Assert.Contains("Supplier.Create", permissions);
        Assert.Contains("Expense.View", permissions);
        Assert.Contains("Expense.Create", permissions);
        Assert.Contains("Expense.Post", permissions);
        Assert.Contains("PurchaseInvoice.View", permissions);
        Assert.Contains("PurchaseInvoice.Post", permissions);
        Assert.Contains("PurchaseInvoice.ReceiveItems", permissions);
        Assert.Contains("SupplierPayment.View", permissions);
        Assert.Contains("SupplierBalance.View", permissions);

        // Assert Phase 3.7 Spray Permissions
        Assert.Contains("Spray.View", permissions);
        Assert.Contains("Spray.Create", permissions);
        Assert.Contains("Spray.Update", permissions);
        Assert.Contains("Spray.Schedule", permissions);
        Assert.Contains("Spray.Start", permissions);
        Assert.Contains("Spray.Complete", permissions);
        Assert.Contains("Spray.Cancel", permissions);
        Assert.Contains("PlantProtectionProduct.View", permissions);
        Assert.Contains("ProductType.View", permissions);
        Assert.Contains("Target.View", permissions);
        Assert.Contains("ApplicationMethod.View", permissions);
    }

    [Fact]
    public void OrganizationAdminPermissions_ContainsExpenseManagementPermissions()
    {
        // Arrange
        var fieldInfo = typeof(IdentityDataSeeder)
            .GetField("OrganizationAdminPermissions", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(fieldInfo);
        var permissions = (IReadOnlySet<string>)fieldInfo.GetValue(null)!;

        // Assert Phase 3.6 Permissions
        Assert.Contains("Supplier.View", permissions);
        Assert.Contains("Supplier.Create", permissions);
        Assert.Contains("Supplier.Update", permissions);
        Assert.Contains("Supplier.Deactivate", permissions);
        Assert.Contains("ExpenseCategory.View", permissions);
        Assert.Contains("ExpenseCategory.Manage", permissions);
        Assert.Contains("Expense.View", permissions);
        Assert.Contains("Expense.Create", permissions);
        Assert.Contains("Expense.UpdateDraft", permissions);
        Assert.Contains("Expense.Post", permissions);
        Assert.Contains("Expense.Reverse", permissions);
        Assert.Contains("Expense.Report.View", permissions);
        Assert.Contains("PurchaseInvoice.View", permissions);
        Assert.Contains("PurchaseInvoice.Create", permissions);
        Assert.Contains("PurchaseInvoice.UpdateDraft", permissions);
        Assert.Contains("PurchaseInvoice.Post", permissions);
        Assert.Contains("PurchaseInvoice.Reverse", permissions);
        Assert.Contains("PurchaseInvoice.ReceiveItems", permissions);
        Assert.Contains("SupplierPayment.View", permissions);
        Assert.Contains("SupplierPayment.Create", permissions);
        Assert.Contains("SupplierPayment.Reverse", permissions);
        Assert.Contains("SupplierBalance.View", permissions);

        // Assert Phase 3.7 Permissions
        Assert.Contains("Spray.View", permissions);
        Assert.Contains("Spray.Create", permissions);
        Assert.Contains("Spray.Update", permissions);
        Assert.Contains("Spray.Schedule", permissions);
        Assert.Contains("Spray.Start", permissions);
        Assert.Contains("Spray.Complete", permissions);
        Assert.Contains("Spray.Cancel", permissions);
        Assert.Contains("PlantProtectionProduct.View", permissions);
        Assert.Contains("PlantProtectionProduct.Create", permissions);
        Assert.Contains("PlantProtectionProduct.Update", permissions);
        Assert.Contains("PlantProtectionProduct.Activate", permissions);
        Assert.Contains("PlantProtectionProduct.Deactivate", permissions);
        Assert.Contains("ProductType.View", permissions);
        Assert.Contains("ProductType.Create", permissions);
        Assert.Contains("ProductType.Update", permissions);
        Assert.Contains("ProductType.Activate", permissions);
        Assert.Contains("ProductType.Deactivate", permissions);
        Assert.Contains("Target.View", permissions);
        Assert.Contains("Target.Create", permissions);
        Assert.Contains("Target.Update", permissions);
        Assert.Contains("Target.Activate", permissions);
        Assert.Contains("Target.Deactivate", permissions);
        Assert.Contains("ApplicationMethod.View", permissions);
        Assert.Contains("ApplicationMethod.Create", permissions);
        Assert.Contains("ApplicationMethod.Update", permissions);
        Assert.Contains("ApplicationMethod.Activate", permissions);
        Assert.Contains("ApplicationMethod.Deactivate", permissions);
    }

    [Fact]
    public void SupervisorPermissions_ContainsFieldExecutionAndInventoryPermissions()
    {
        // Arrange
        var fieldInfo = typeof(IdentityDataSeeder)
            .GetField("SupervisorPermissions", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(fieldInfo);
        var permissions = (IReadOnlySet<string>)fieldInfo.GetValue(null)!;

        // Assert Inventory Permissions
        Assert.Contains("InventoryItem.View", permissions);
        Assert.Contains("StorageLocation.View", permissions);
        Assert.Contains("InventoryStock.View", permissions);
        Assert.Contains("InventoryTransaction.Create", permissions);

        // Assert Field Execution Permissions
        Assert.Contains("Attendance.View", permissions);
        Assert.Contains("Attendance.Create", permissions);
        Assert.Contains("LaborActivity.Create", permissions);

        // Assert Phase 3.7 Spray Permissions
        Assert.Contains("Spray.View", permissions);
        Assert.Contains("Spray.Start", permissions);
        Assert.Contains("Spray.Complete", permissions);
        Assert.Contains("PlantProtectionProduct.View", permissions);
        Assert.Contains("ProductType.View", permissions);
        Assert.Contains("Target.View", permissions);
        Assert.Contains("ApplicationMethod.View", permissions);
    }
}
