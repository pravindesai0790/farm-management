using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using FarmManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public sealed class FertilizerConfigurationTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=farm_test;Username=postgres;Password=postgres")
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public void FertilizerApplication_Configuration_MapsTableAndKeysProperly()
    {
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(FertilizerApplication));

        Assert.NotNull(entityType);
        Assert.Equal("fertilizer_applications", entityType.GetTableName());

        // Check primary key
        var pk = entityType.FindPrimaryKey();
        Assert.NotNull(pk);
        Assert.Single(pk.Properties);
        Assert.Equal("Id", pk.Properties[0].Name);

        // Check concurrency token
        var versionProp = entityType.FindProperty(nameof(FertilizerApplication.Version));
        Assert.NotNull(versionProp);
        Assert.True(versionProp.IsConcurrencyToken);

        // Check status conversion
        var statusProp = entityType.FindProperty(nameof(FertilizerApplication.Status));
        Assert.NotNull(statusProp);
        Assert.NotNull(statusProp.GetValueConverter());
        Assert.Equal(typeof(string), statusProp.GetValueConverter()!.ProviderClrType);

        // Check foreign keys
        var farmFk = entityType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Farm));
        Assert.NotNull(farmFk);
        Assert.Equal(DeleteBehavior.Restrict, farmFk.DeleteBehavior);

        // Check unique index on org + reference_number
        var refIndex = entityType.GetIndexes()
            .FirstOrDefault(idx => idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "OrganizationId", "ReferenceNumber" }));
        Assert.NotNull(refIndex);
        Assert.True(refIndex.IsUnique);
    }

    [Fact]
    public void FertilizerApplicationProduct_Configuration_MapsTableAndIndexesProperly()
    {
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(FertilizerApplicationProduct));

        Assert.NotNull(entityType);
        Assert.Equal("fertilizer_application_products", entityType.GetTableName());

        // Cascade delete from FertilizerApplication
        var appFk = entityType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(FertilizerApplication));
        Assert.NotNull(appFk);
        Assert.Equal(DeleteBehavior.Cascade, appFk.DeleteBehavior);

        // Restrict delete from InventoryItem
        var itemFk = entityType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(InventoryItem));
        Assert.NotNull(itemFk);
        Assert.Equal(DeleteBehavior.Restrict, itemFk.DeleteBehavior);

        // Unique index on application + inventory item
        var appItemIndex = entityType.GetIndexes()
            .FirstOrDefault(idx => idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "FertilizerApplicationId", "InventoryItemId" }));
        Assert.NotNull(appItemIndex);
        Assert.True(appItemIndex.IsUnique);
    }

    [Fact]
    public void FertilizerReferenceSequence_Configuration_MapsCompositePrimaryKeyProperly()
    {
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(FertilizerReferenceSequence));

        Assert.NotNull(entityType);
        Assert.Equal("fertilizer_reference_sequences", entityType.GetTableName());

        var pk = entityType.FindPrimaryKey();
        Assert.NotNull(pk);
        Assert.Equal(2, pk.Properties.Count);
        Assert.Equal("OrganizationId", pk.Properties[0].Name);
        Assert.Equal("Year", pk.Properties[1].Name);
    }

    [Fact]
    public void StockMovement_Configuration_MapsFertilizerApplicationProperly()
    {
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(StockMovement));

        Assert.NotNull(entityType);
        var fertilizerFk = entityType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(FertilizerApplication));

        Assert.NotNull(fertilizerFk);
        Assert.Equal(DeleteBehavior.Restrict, fertilizerFk.DeleteBehavior);
    }

    [Fact]
    public void ApplicationMethod_Configuration_MapsModuleAndCompositeIndexesProperly()
    {
        using var context = CreateDbContext();
        var entityType = context.Model.FindEntityType(typeof(ApplicationMethod));

        Assert.NotNull(entityType);
        var moduleProp = entityType.FindProperty(nameof(ApplicationMethod.Module));
        Assert.NotNull(moduleProp);
        Assert.Equal("SPRAY", moduleProp.GetDefaultValue());

        var orgModuleCodeIndex = entityType.GetIndexes()
            .FirstOrDefault(idx => idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "OrganizationId", "Module", "Code" }));
        Assert.NotNull(orgModuleCodeIndex);
        Assert.True(orgModuleCodeIndex.IsUnique);

        var systemModuleCodeIndex = entityType.GetIndexes()
            .FirstOrDefault(idx => idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "Module", "Code" }));
        Assert.NotNull(systemModuleCodeIndex);
        Assert.True(systemModuleCodeIndex.IsUnique);
    }
}
