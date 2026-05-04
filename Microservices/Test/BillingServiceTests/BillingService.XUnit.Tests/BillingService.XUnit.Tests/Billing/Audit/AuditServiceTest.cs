using BillingService.Domain.Services.History;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Enums.Billing.History;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Audit;

public class AuditServiceTests
{
    private readonly Mock<IRepository<BillingDbContext, AuditLogEntity>> _repoMock;
    private readonly AuditService _service;

    public AuditServiceTests()
    {
        _repoMock = new Mock<IRepository<BillingDbContext, AuditLogEntity>>();

        // Setup default async behavior
        _repoMock.Setup(x => x.AddAsync(It.IsAny<AuditLogEntity>()))
                 .Returns(Task.CompletedTask);

        _repoMock.Setup(x => x.SaveChangesAsync())
                 .Returns(Task.CompletedTask);

        _service = new AuditService(_repoMock.Object);
    }

    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task TrackAsync_Insert_Should_Save_NewValues()
    {
        var newEntity = new TestEntity { Id = 1, Name = "Test" };

        await _service.TrackAsync(
            ActionType.I,
            1, 10, 20,
            "TestEntity",
            newEntity: newEntity
        );

        _repoMock.Verify(x => x.AddAsync(It.Is<AuditLogEntity>(a =>
            a.ActionType == ActionType.I &&
            a.NewValue != null &&
            a.OldValue == null
        )), Times.Once);

        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task TrackAsync_Update_WithChanges_Should_Save_Old_And_NewValues()
    {
        var oldEntity = new TestEntity { Id = 1, Name = "Old" };
        var newEntity = new TestEntity { Id = 1, Name = "New" };

        await _service.TrackAsync(
            ActionType.U,
            1, 10, 20,
            "TestEntity",
            oldEntity,
            newEntity
        );

        _repoMock.Verify(x => x.AddAsync(It.Is<AuditLogEntity>(a =>
            a.ActionType == ActionType.U &&
            a.OldValue != null &&
            a.NewValue != null
        )), Times.Once);

        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task TrackAsync_Update_NoChanges_Should_Not_Save()
    {
        var entity = new TestEntity { Id = 1, Name = "Same" };

        await _service.TrackAsync(
            ActionType.U,
            1, 10, 20,
            "TestEntity",
            entity,
            entity
        );

        _repoMock.Verify(x => x.AddAsync(It.IsAny<AuditLogEntity>()), Times.Never);
        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task TrackAsync_Delete_Should_Save_OldValues()
    {
        var oldEntity = new TestEntity { Id = 1, Name = "Deleted" };

        await _service.TrackAsync(
            ActionType.D,
            1, 10, 20,
            "TestEntity",
            oldEntity
        );

        _repoMock.Verify(x => x.AddAsync(It.Is<AuditLogEntity>(a =>
            a.ActionType == ActionType.D &&
            a.OldValue != null &&
            a.NewValue == null
        )), Times.Once);

        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task TrackAsync_Insert_WithIgnoreFields_Should_Work()
    {
        var newEntity = new TestEntity { Id = 1, Name = "IgnoreTest" };
        var ignoreFields = new List<string> { "Id" };

        await _service.TrackAsync(
            ActionType.I,
            1, 10, 20,
            "TestEntity",
            newEntity: newEntity,
            ignoreFields: ignoreFields
        );

        _repoMock.Verify(x => x.AddAsync(It.Is<AuditLogEntity>(a =>
            a.ActionType == ActionType.I
        )), Times.Once);

        _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}