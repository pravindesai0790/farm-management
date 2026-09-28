using FarmManagement.Domain.Entities;
using FarmManagement.Domain.Enums;
using Xunit;

namespace FarmManagement.API.Tests.Domain;

public sealed class LaborActivityModelTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _farmId = Guid.NewGuid();
    private readonly Guid _farmAreaId = Guid.NewGuid();
    private readonly Guid _plantationId = Guid.NewGuid();
    private readonly Guid _cropCycleId = Guid.NewGuid();
    private readonly Guid _cropCycleStageId = Guid.NewGuid();
    private readonly Guid _laborActivityTypeId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void LaborActivity_Create_WithMinimalValidFields_SetsDefaults()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        var activity = new LaborActivity(
            _organizationId,
            date,
            _farmId,
            _laborActivityTypeId,
            _userId);

        Assert.NotEqual(Guid.Empty, activity.Id);
        Assert.Equal(_organizationId, activity.OrganizationId);
        Assert.Equal(date, activity.ActivityDate);
        Assert.Equal(_farmId, activity.FarmId);
        Assert.Equal(_laborActivityTypeId, activity.LaborActivityTypeId);
        Assert.Equal(_userId, activity.CreatedBy);
        Assert.Null(activity.FarmAreaId);
        Assert.Null(activity.PlantationId);
        Assert.Null(activity.CropCycleId);
        Assert.Null(activity.CropCycleStageId);
        Assert.Null(activity.Description);
        Assert.Equal(LaborActivityStatus.Completed, activity.Status);
    }

    [Fact]
    public void LaborActivity_Create_WithFullHierarchy_SetsAllProperties()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        var activity = new LaborActivity(
            _organizationId,
            date,
            _farmId,
            _laborActivityTypeId,
            _userId,
            _farmAreaId,
            _plantationId,
            _cropCycleId,
            _cropCycleStageId,
            " Weeding and soil preparation ",
            LaborActivityStatus.Draft);

        Assert.Equal(_farmAreaId, activity.FarmAreaId);
        Assert.Equal(_plantationId, activity.PlantationId);
        Assert.Equal(_cropCycleId, activity.CropCycleId);
        Assert.Equal(_cropCycleStageId, activity.CropCycleStageId);
        Assert.Equal("Weeding and soil preparation", activity.Description);
        Assert.Equal(LaborActivityStatus.Draft, activity.Status);
    }

    [Fact]
    public void LaborActivity_Create_ThrowsWhenRequiredIdentifiersEmpty()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() =>
            new LaborActivity(Guid.Empty, date, _farmId, _laborActivityTypeId, _userId));

        Assert.Throws<ArgumentException>(() =>
            new LaborActivity(_organizationId, date, Guid.Empty, _laborActivityTypeId, _userId));

        Assert.Throws<ArgumentException>(() =>
            new LaborActivity(_organizationId, date, _farmId, Guid.Empty, _userId));

        Assert.Throws<ArgumentException>(() =>
            new LaborActivity(_organizationId, date, _farmId, _laborActivityTypeId, Guid.Empty));
    }

    [Fact]
    public void LaborActivity_Create_ThrowsWhenOptionalIdentifiersAreEmptyGuid()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() =>
            new LaborActivity(_organizationId, date, _farmId, _laborActivityTypeId, _userId, farmAreaId: Guid.Empty));

        Assert.Throws<ArgumentException>(() =>
            new LaborActivity(_organizationId, date, _farmId, _laborActivityTypeId, _userId, plantationId: Guid.Empty));

        Assert.Throws<ArgumentException>(() =>
            new LaborActivity(_organizationId, date, _farmId, _laborActivityTypeId, _userId, cropCycleId: Guid.Empty));

        Assert.Throws<ArgumentException>(() =>
            new LaborActivity(_organizationId, date, _farmId, _laborActivityTypeId, _userId, cropCycleStageId: Guid.Empty));
    }

    [Fact]
    public void LaborActivity_Create_ThrowsWhenStageSpecifiedWithoutCycle()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        var ex = Assert.Throws<ArgumentException>(() =>
            new LaborActivity(
                _organizationId,
                date,
                _farmId,
                _laborActivityTypeId,
                _userId,
                cropCycleStageId: _cropCycleStageId));

        Assert.Contains("crop cycle is required", ex.Message);
    }

    [Fact]
    public void LaborActivity_Create_ThrowsWhenCycleSpecifiedWithoutPlantation()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        var ex = Assert.Throws<ArgumentException>(() =>
            new LaborActivity(
                _organizationId,
                date,
                _farmId,
                _laborActivityTypeId,
                _userId,
                cropCycleId: _cropCycleId));

        Assert.Contains("plantation is required", ex.Message);
    }

    [Fact]
    public void LaborActivity_Update_UpdatesFieldsAndTimestamp()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var activity = new LaborActivity(_organizationId, date, _farmId, _laborActivityTypeId, _userId, status: LaborActivityStatus.Draft);

        var newDate = date.AddDays(1);
        var newTypeId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        activity.Update(
            newDate,
            _farmId,
            _farmAreaId,
            _plantationId,
            _cropCycleId,
            _cropCycleStageId,
            newTypeId,
            "Updated notes",
            now,
            _userId);

        Assert.Equal(newDate, activity.ActivityDate);
        Assert.Equal(_farmAreaId, activity.FarmAreaId);
        Assert.Equal(_plantationId, activity.PlantationId);
        Assert.Equal(_cropCycleId, activity.CropCycleId);
        Assert.Equal(_cropCycleStageId, activity.CropCycleStageId);
        Assert.Equal(newTypeId, activity.LaborActivityTypeId);
        Assert.Equal("Updated notes", activity.Description);
        Assert.Equal(now, activity.UpdatedAt);
        Assert.Equal(_userId, activity.UpdatedBy);
    }

    [Fact]
    public void LaborActivity_Update_ThrowsWhenCancelled()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var activity = new LaborActivity(_organizationId, date, _farmId, _laborActivityTypeId, _userId, status: LaborActivityStatus.Draft);
        activity.Cancel(DateTimeOffset.UtcNow, _userId);

        Assert.Throws<InvalidOperationException>(() =>
            activity.Update(date, _farmId, null, null, null, null, _laborActivityTypeId, null, DateTimeOffset.UtcNow, _userId));
    }

    [Fact]
    public void LaborActivity_Cancel_SetsStatusAndTimestamp()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var activity = new LaborActivity(_organizationId, date, _farmId, _laborActivityTypeId, _userId, status: LaborActivityStatus.Draft);
        var now = DateTimeOffset.UtcNow;

        var result = activity.Cancel(now, _userId);

        Assert.True(result);
        Assert.Equal(LaborActivityStatus.Cancelled, activity.Status);
        Assert.Equal(now, activity.UpdatedAt);
        Assert.Equal(_userId, activity.UpdatedBy);

        // Subsequent cancels return false
        Assert.False(activity.Cancel(now, _userId));
    }

    [Fact]
    public void LaborActivity_Complete_SetsStatusAndTimestamp()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var activity = new LaborActivity(_organizationId, date, _farmId, _laborActivityTypeId, _userId, status: LaborActivityStatus.Draft);
        var now = DateTimeOffset.UtcNow;

        var result = activity.Complete(now, _userId);

        Assert.True(result);
        Assert.Equal(LaborActivityStatus.Completed, activity.Status);
        Assert.Equal(now, activity.UpdatedAt);
        Assert.Equal(_userId, activity.UpdatedBy);

        // Complete on cancelled throws
        var cancelled = new LaborActivity(_organizationId, date, _farmId, _laborActivityTypeId, _userId, status: LaborActivityStatus.Draft);
        cancelled.Cancel(now, _userId);
        Assert.Throws<InvalidOperationException>(() => cancelled.Complete(now, _userId));
    }
}
