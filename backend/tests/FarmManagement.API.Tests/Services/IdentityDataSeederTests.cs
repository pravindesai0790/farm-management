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
    }
}
