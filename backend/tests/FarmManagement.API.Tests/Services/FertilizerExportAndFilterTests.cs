using System.Reflection;
using System.Text;
using FarmManagement.Application.Common.Helpers;
using FarmManagement.Application.DTOs.Fertilizer;
using FarmManagement.Application.Interfaces.Fertilizer;
using FarmManagement.Application.Services;
using FarmManagement.Application.Services.Fertilizer;
using FarmManagement.Domain.Constants;
using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Services;

public class FertilizerExportAndFilterTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FertilizerApplicationServiceTests.FakeFertilizerStore _store = new();
    private readonly FertilizerApplicationService _sut;

    public FertilizerExportAndFilterTests()
    {
        var stockManager = new FertilizerStockManager(_store);
        var validator = new FertilizerValidator(_store);
        var referenceGenerator = new TestReferenceGenerator();
        _sut = new FertilizerApplicationService(_store, validator, referenceGenerator, stockManager);
    }

    private sealed class TestReferenceGenerator : IFertilizerReferenceGenerator
    {
        private int _counter;
        public Task<string> NextReferenceNumberAsync(Guid organizationId, int year, CancellationToken cancellationToken = default)
        {
            _counter++;
            return Task.FromResult($"FERT-{year}-{_counter:D6}");
        }
    }

    private FertilizerActor CreateActor(Guid? orgId = null, Guid? userId = null) =>
        new(userId ?? _userId, orgId ?? _orgId);

    // =========================================================================
    // 1. CsvExportHelper Unit Tests: RFC 4180, Escaping, BOM & Formula Defense
    // =========================================================================

    [Fact]
    public void CsvExportHelper_EscapeRfc4180_StandardText_ReturnsUnchanged()
    {
        Assert.Equal("Normal Text", CsvExportHelper.EscapeRfc4180("Normal Text"));
        Assert.Equal("12345", CsvExportHelper.EscapeRfc4180("12345"));
        Assert.Equal(string.Empty, CsvExportHelper.EscapeRfc4180(null!));
        Assert.Equal(string.Empty, CsvExportHelper.EscapeRfc4180(""));
    }

    [Fact]
    public void CsvExportHelper_EscapeRfc4180_ContainsCommaOrQuotesOrNewlines_EscapesProperly()
    {
        // Contains comma
        Assert.Equal("\"Field, with comma\"", CsvExportHelper.EscapeRfc4180("Field, with comma"));

        // Contains quotes
        Assert.Equal("\"Field with \"\"quotes\"\"\"", CsvExportHelper.EscapeRfc4180("Field with \"quotes\""));

        // Contains newline
        Assert.Equal("\"Line 1\nLine 2\"", CsvExportHelper.EscapeRfc4180("Line 1\nLine 2"));
        Assert.Equal("\"Line 1\r\nLine 2\"", CsvExportHelper.EscapeRfc4180("Line 1\r\nLine 2"));
    }

    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("+44123456789", "'+44123456789")]
    [InlineData("-DANGEROUS", "'-DANGEROUS")]
    [InlineData("@SUM(A1:A10)", "'@SUM(A1:A10)")]
    [InlineData("\tTABBED", "'\tTABBED")]
    [InlineData("\rCARRIAGE", "\"'\rCARRIAGE\"")] // RFC 4180 quotes because of \r, and starts with '
    public void CsvExportHelper_EscapeText_SanitizesFormulaTriggers(string input, string expected)
    {
        var sanitized = CsvExportHelper.EscapeText(input);
        Assert.Equal(expected, sanitized);
    }

    [Fact]
    public void CsvExportHelper_EscapeNumber_PreservesNegativeNumbersWithoutFormulaPrefix()
    {
        Assert.Equal("-15.5", CsvExportHelper.EscapeNumber(-15.50m));
        Assert.Equal("0", CsvExportHelper.EscapeNumber(0m));
        Assert.Equal("100.25", CsvExportHelper.EscapeNumber(100.25m));
        Assert.Equal(string.Empty, CsvExportHelper.EscapeNumber(null));
    }

    [Fact]
    public void CsvExportHelper_CreateCsvBytes_PrependsBOM()
    {
        var csv = "col1,col2\nval1,val2";
        var bytes = CsvExportHelper.CreateCsvBytes(csv);

        Assert.True(bytes.Length >= 3);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);

        var decoded = Encoding.UTF8.GetString(bytes.AsSpan(3));
        Assert.Equal(csv, decoded);
    }

    // =========================================================================
    // 2. Multi-product Expansion and Blank Direct Completed Values
    // =========================================================================

    [Fact]
    public async Task ExportCsvAsync_MultiProductApplication_ExpandsOneRowPerProduct()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm("Sunny Valley Farm");
        var area = CreateFarmArea(farm.Id, "Plot Alpha");
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item1 = CreateInventoryItem("Urea 46%", "SKU-UREA");
        var item2 = CreateInventoryItem("Potash MOP", "SKU-POTASH");
        var loc = CreateStorageLocation(farm.Id, "Barn 1");

        var app = new FertilizerApplication(_orgId, "FA-2026-00001", _userId, farmId: farm.Id, farmAreaId: area.Id, applicationMethodId: method.Id, purposeReason: "Pre-season fertilization");
        SetPrivateProperty(app, nameof(FertilizerApplication.PlannedDate), new DateOnly(2026, 6, 1));
        SetPrivateProperty(app, nameof(FertilizerApplication.PlannedArea), 10.5m);
        SetPrivateProperty(app, nameof(FertilizerApplication.Farm), farm);
        SetPrivateProperty(app, nameof(FertilizerApplication.FarmArea), area);
        SetPrivateProperty(app, nameof(FertilizerApplication.ApplicationMethod), method);

        var prod1 = new FertilizerApplicationProduct(app.Id, item1.Id, 50m, _userId, loc.Id);
        SetPrivateProperty(prod1, nameof(FertilizerApplicationProduct.InventoryItem), item1);
        SetPrivateProperty(prod1, nameof(FertilizerApplicationProduct.StorageLocation), loc);

        var prod2 = new FertilizerApplicationProduct(app.Id, item2.Id, 25m, _userId, loc.Id);
        SetPrivateProperty(prod2, nameof(FertilizerApplicationProduct.InventoryItem), item2);
        SetPrivateProperty(prod2, nameof(FertilizerApplicationProduct.StorageLocation), loc);

        app.AddProduct(prod1);
        app.AddProduct(prod2);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Scheduled);

        _store.Applications.Add(app);

        // Act
        var bytes = await _sut.ExportCsvAsync(actor, new FertilizerApplicationListQuery());

        // Assert
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 3);

        var content = Encoding.UTF8.GetString(bytes.AsSpan(3));
        var lines = content.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);

        // Header + 2 product rows
        Assert.Equal(3, lines.Length);

        // First row has Urea
        Assert.Contains("FA-2026-00001", lines[1]);
        Assert.Contains("Sunny Valley Farm", lines[1]);
        Assert.Contains("Plot Alpha", lines[1]);
        Assert.Contains("Urea 46%", lines[1]);
        Assert.Contains("50", lines[1]);

        // Second row has Potash
        Assert.Contains("FA-2026-00001", lines[2]);
        Assert.Contains("Sunny Valley Farm", lines[2]);
        Assert.Contains("Plot Alpha", lines[2]);
        Assert.Contains("Potash MOP", lines[2]);
        Assert.Contains("25", lines[2]);
    }

    [Fact]
    public async Task ExportCsvAsync_EmptyProductsApplication_ProducesSinglePlaceholderRow()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm("Highland Orchard");
        var method = CreateMethod("FOLIAR", "Foliar Spray");

        var app = new FertilizerApplication(_orgId, "FA-2026-00002", _userId, farmId: farm.Id, applicationMethodId: method.Id, purposeReason: "Draft planning with no products yet");
        SetPrivateProperty(app, nameof(FertilizerApplication.Farm), farm);
        SetPrivateProperty(app, nameof(FertilizerApplication.ApplicationMethod), method);
        _store.Applications.Add(app);

        // Act
        var bytes = await _sut.ExportCsvAsync(actor, new FertilizerApplicationListQuery());

        // Assert
        var content = Encoding.UTF8.GetString(bytes.AsSpan(3));
        var lines = content.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(2, lines.Length); // Header + 1 placeholder row
        Assert.Contains("FA-2026-00002", lines[1]);
        Assert.Contains("Highland Orchard", lines[1]);
        Assert.Contains("Draft planning with no products yet", lines[1]);
    }

    [Fact]
    public async Task ExportCsvAsync_DirectCompletedApplication_OutputsBlankPlannedAndVariance()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm("Riverbend Estate");
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem("DAP 18-46-0", "SKU-DAP");
        var loc = CreateStorageLocation(farm.Id, "Central Warehouse");
        var stockMovementId = Guid.NewGuid();

        var app = new FertilizerApplication(_orgId, "FA-2026-00003", _userId, farmId: farm.Id, applicationMethodId: method.Id, purposeReason: "Direct field broadcast");
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Completed);
        SetPrivateProperty(app, nameof(FertilizerApplication.PlannedDate), null); // Direct Completed has no planned date
        SetPrivateProperty(app, nameof(FertilizerApplication.ActualApplicationDate), new DateOnly(2026, 6, 15));
        SetPrivateProperty(app, nameof(FertilizerApplication.Farm), farm);
        SetPrivateProperty(app, nameof(FertilizerApplication.ApplicationMethod), method);

        var prod = new FertilizerApplicationProduct(app.Id, item.Id, 0m, _userId, loc.Id, 40m, stockMovementId);
        SetPrivateProperty(prod, nameof(FertilizerApplicationProduct.InventoryItem), item);
        SetPrivateProperty(prod, nameof(FertilizerApplicationProduct.StorageLocation), loc);

        app.AddDirectProduct(prod);
        _store.Applications.Add(app);

        // Act
        var bytes = await _sut.ExportCsvAsync(actor, new FertilizerApplicationListQuery());

        // Assert
        var content = Encoding.UTF8.GetString(bytes.AsSpan(3));
        var lines = content.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);

        var dataRow = lines[1];
        Assert.Contains("FA-2026-00003", dataRow);
        Assert.Contains("DAP 18-46-0", dataRow);
        Assert.Contains("40", dataRow);
        Assert.Contains(stockMovementId.ToString(), dataRow);

        var fields = ParseCsvLine(dataRow);
        // Col 22: Planned Qty, Col 23: Actual Qty, Col 24: Variance Qty
        Assert.Equal("40", fields[23]); // Actual
        Assert.Equal(string.Empty, fields[22]); // Planned should be blank
        Assert.Equal(string.Empty, fields[24]); // Variance should be blank
    }

    [Fact]
    public async Task ExportCsvAsync_NormalCompletedApplication_OutputsNumericPlannedAndVariance()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm("Riverbend Estate");
        var method = CreateMethod("BROADCASTING", "Broadcasting");
        var item = CreateInventoryItem("DAP 18-46-0", "SKU-DAP");
        var loc = CreateStorageLocation(farm.Id, "Central Warehouse");
        var stockMovementId = Guid.NewGuid();

        var app = new FertilizerApplication(_orgId, "FA-2026-00004", _userId, farmId: farm.Id, applicationMethodId: method.Id, purposeReason: "Standard cycle completion");
        SetPrivateProperty(app, nameof(FertilizerApplication.PlannedDate), new DateOnly(2026, 6, 10));
        SetPrivateProperty(app, nameof(FertilizerApplication.ActualApplicationDate), new DateOnly(2026, 6, 12));
        SetPrivateProperty(app, nameof(FertilizerApplication.Farm), farm);
        SetPrivateProperty(app, nameof(FertilizerApplication.ApplicationMethod), method);

        var prod = new FertilizerApplicationProduct(app.Id, item.Id, 50m, _userId, loc.Id, 45m, stockMovementId);
        SetPrivateProperty(prod, nameof(FertilizerApplicationProduct.InventoryItem), item);
        SetPrivateProperty(prod, nameof(FertilizerApplicationProduct.StorageLocation), loc);

        app.AddProduct(prod);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Completed);
        _store.Applications.Add(app);

        // Act
        var bytes = await _sut.ExportCsvAsync(actor, new FertilizerApplicationListQuery());

        // Assert
        var content = Encoding.UTF8.GetString(bytes.AsSpan(3));
        var lines = content.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        var fields = ParseCsvLine(lines[1]);

        Assert.Equal("50", fields[22]); // Planned Qty
        Assert.Equal("45", fields[23]); // Actual Qty
        Assert.Equal("-5", fields[24]); // Variance Qty = 45 - 50 = -5
    }

    // =========================================================================
    // 3. Cancelled Applications: Zero Usage vs Positive Usage
    // =========================================================================

    [Fact]
    public async Task ExportCsvAsync_CancelledWithPositiveUsage_IncludesCancellationAuditAndUsage()
    {
        // Arrange
        var cancelUserId = Guid.NewGuid();
        var actor = CreateActor();
        var farm = CreateFarm("Green Meadows");
        var method = CreateMethod("FERTIGATION", "Fertigation");
        var item = CreateInventoryItem("Liquid Nitrogen 28%", "SKU-LN28");
        var loc = CreateStorageLocation(farm.Id, "Shed B");
        var movementId = Guid.NewGuid();

        var app = new FertilizerApplication(_orgId, "FA-2026-00005", _userId, farmId: farm.Id, applicationMethodId: method.Id, purposeReason: "Mid-season boost");
        SetPrivateProperty(app, nameof(FertilizerApplication.PlannedDate), new DateOnly(2026, 6, 1));
        SetPrivateProperty(app, nameof(FertilizerApplication.ActualApplicationDate), new DateOnly(2026, 6, 2));
        SetPrivateProperty(app, nameof(FertilizerApplication.CancellationReason), "Equipment broke down mid-application");
        SetPrivateProperty(app, nameof(FertilizerApplication.CancelledAt), new DateTimeOffset(2026, 6, 2, 14, 30, 0, TimeSpan.Zero));
        SetPrivateProperty(app, nameof(FertilizerApplication.CancelledBy), cancelUserId);
        SetPrivateProperty(app, nameof(FertilizerApplication.Farm), farm);
        SetPrivateProperty(app, nameof(FertilizerApplication.ApplicationMethod), method);

        var prod = new FertilizerApplicationProduct(app.Id, item.Id, 100m, _userId, loc.Id, 30m, movementId);
        SetPrivateProperty(prod, nameof(FertilizerApplicationProduct.InventoryItem), item);
        SetPrivateProperty(prod, nameof(FertilizerApplicationProduct.StorageLocation), loc);

        app.AddProduct(prod);
        SetPrivateProperty(app, nameof(FertilizerApplication.Status), FertilizerApplicationStatus.Cancelled);
        _store.Applications.Add(app);

        // Act
        var bytes = await _sut.ExportCsvAsync(actor, new FertilizerApplicationListQuery());

        // Assert
        var content = Encoding.UTF8.GetString(bytes.AsSpan(3));
        var lines = content.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        var fields = ParseCsvLine(lines[1]);

        Assert.Equal("CANCELLED", fields[1]);
        Assert.Equal("Equipment broke down mid-application", fields[26]); // Cancellation Reason
        Assert.Equal("2026-06-02 14:30:00", fields[33]); // Cancelled At
        Assert.Equal("30", fields[23]); // Actual Quantity
        Assert.Equal("-70", fields[24]); // Variance = 30 - 100 = -70
        Assert.Equal(movementId.ToString(), fields[25]); // Stock Movement ID
    }

    // =========================================================================
    // 4. Formula Injection Defense in Stored Data
    // =========================================================================

    [Fact]
    public async Task ExportCsvAsync_FieldsWithFormulaTriggers_AreSanitizedInExport()
    {
        // Arrange
        var actor = CreateActor();
        var farm = CreateFarm("=SUM(1+1)");
        var method = CreateMethod("BROADCASTING", "@METHOD");
        var item = CreateInventoryItem("+Dangerous SKU Item", "-SKU123");
        var loc = CreateStorageLocation(farm.Id, "Default Shed");
        SetPrivateProperty(loc, nameof(StorageLocation.Name), "\tLocationTab");

        var app = new FertilizerApplication(_orgId, "=cmd|' /C calc'!A0", _userId, farmId: farm.Id, applicationMethodId: method.Id, purposeReason: "@malicious formula in notes");
        SetPrivateProperty(app, nameof(FertilizerApplication.Farm), farm);
        SetPrivateProperty(app, nameof(FertilizerApplication.ApplicationMethod), method);

        var prod = new FertilizerApplicationProduct(app.Id, item.Id, 10m, _userId, loc.Id);
        SetPrivateProperty(prod, nameof(FertilizerApplicationProduct.InventoryItem), item);
        SetPrivateProperty(prod, nameof(FertilizerApplicationProduct.StorageLocation), loc);

        app.AddProduct(prod);
        _store.Applications.Add(app);

        // Act
        var bytes = await _sut.ExportCsvAsync(actor, new FertilizerApplicationListQuery());

        // Assert
        var content = Encoding.UTF8.GetString(bytes.AsSpan(3));
        var lines = content.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        var fields = ParseCsvLine(lines[1]);

        // Reference Number: prefixed with '
        Assert.StartsWith("'=CMD|", fields[0], StringComparison.OrdinalIgnoreCase);

        // Farm Name: prefixed with '
        Assert.StartsWith("'=SUM(1+1)", fields[2]);

        // Method Name: prefixed with '
        Assert.StartsWith("'@METHOD", fields[7]);

        // Purpose: prefixed with '
        Assert.StartsWith("'@malicious", fields[10]);

        // Product Name: prefixed with '
        Assert.StartsWith("'+Dangerous", fields[18]);

        // SKU: prefixed with '
        Assert.StartsWith("'-SKU123", fields[19]);

        // Storage Location: prefixed with '
        Assert.StartsWith("'\tLocationTab", fields[21]);
    }

    // =========================================================================
    // 5. Tenant Isolation
    // =========================================================================

    [Fact]
    public async Task ExportCsvAsync_StrictTenantIsolation_ExcludesOtherOrgRecords()
    {
        // Arrange
        var otherOrgId = Guid.NewGuid();
        var actor = CreateActor(); // Uses _orgId

        var myFarm = CreateFarm("My Farm", _orgId);
        var otherFarm = CreateFarm("Other Org Farm", otherOrgId);
        var method = CreateMethod("BROADCASTING", "Broadcasting");

        var myApp = new FertilizerApplication(_orgId, "FA-MINE-001", _userId, farmId: myFarm.Id, applicationMethodId: method.Id);
        SetPrivateProperty(myApp, nameof(FertilizerApplication.Farm), myFarm);
        SetPrivateProperty(myApp, nameof(FertilizerApplication.ApplicationMethod), method);

        var otherApp = new FertilizerApplication(otherOrgId, "FA-OTHER-999", Guid.NewGuid(), farmId: otherFarm.Id, applicationMethodId: method.Id);
        SetPrivateProperty(otherApp, nameof(FertilizerApplication.Farm), otherFarm);
        SetPrivateProperty(otherApp, nameof(FertilizerApplication.ApplicationMethod), method);

        _store.Applications.Add(myApp);
        _store.Applications.Add(otherApp);

        // Act
        var bytes = await _sut.ExportCsvAsync(actor, new FertilizerApplicationListQuery());

        // Assert
        var content = Encoding.UTF8.GetString(bytes.AsSpan(3));
        Assert.Contains("FA-MINE-001", content);
        Assert.DoesNotContain("FA-OTHER-999", content);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private Farm CreateFarm(string name, Guid? orgId = null)
    {
        var farm = new Farm(orgId ?? _orgId, name, Guid.NewGuid(), Guid.NewGuid(), 100m, _userId);
        _store.Farms.Add(farm);
        return farm;
    }

    private FarmArea CreateFarmArea(Guid farmId, string name)
    {
        var area = new FarmArea(_orgId, farmId, null, name, 25m, Guid.NewGuid(), _userId);
        _store.FarmAreas.Add(area);
        return area;
    }

    private ApplicationMethod CreateMethod(string code, string name)
    {
        var method = new ApplicationMethod(_orgId, code, name, isSystem: false, description: null, displayOrder: 1, createdBy: _userId, module: "FERTILIZER");
        _store.ApplicationMethods.Add(method);
        return method;
    }

    private InventoryItem CreateInventoryItem(string name, string sku)
    {
        var item = new InventoryItem(_orgId, name, Guid.NewGuid(), _userId, sku, null, FarmInventoryCategories.FertilizersSoilId);
        _store.InventoryItems.Add(item);
        return item;
    }

    private StorageLocation CreateStorageLocation(Guid farmId, string name)
    {
        var loc = new StorageLocation(_orgId, farmId, name, _userId, "Main Storage");
        _store.StorageLocations.Add(loc);
        return loc;
    }

    private static void SetPrivateProperty<T>(T target, string propertyName, object? value)
    {
        var prop = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(target, value);
        }
        else
        {
            var field = typeof(T).GetField($"<{propertyName}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(target, value);
        }
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // skip next quote
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }
        result.Add(current.ToString());
        return result;
    }
}
