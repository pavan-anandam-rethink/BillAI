using AutoMapper;
using BillingService.Domain.Interfaces.Files;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Domain.Services.Billing;
using BillingService.XUnit.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Claim
{
    public class ClaimAttachmentServiceTest : BaseTest
    {
        private readonly ClaimAttachmentService _service;

        // ---- Lightweight test DbContext (does NOT depend on your BaseDbContext) ----
        private class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
            public DbSet<ClaimAttachmentEntity> ClaimAttachments { get; set; }
            public DbSet<ClaimEntity> Claims { get; set; }
        }

        private readonly Mock<IRepository<BillingDbContext, ClaimAttachmentEntity>> _mockAttachmentRepo
            = new Mock<IRepository<BillingDbContext, ClaimAttachmentEntity>>();
        private readonly Mock<IRepository<BillingDbContext, ClaimEntity>> _mockClaimRepo
            = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
        private readonly Mock<IRethinkMasterDataMicroServices> _mockMasterData
            = new Mock<IRethinkMasterDataMicroServices>();
        private readonly Mock<IFileManagerService> _mockFileManager = new Mock<IFileManagerService>();
        private readonly Mock<IFileService> _mockFileService = new Mock<IFileService>();
        private readonly Mock<IMapper> _mockMapper = new Mock<IMapper>();

        private ClaimAttachmentService CreateSut() => new ClaimAttachmentService(
            _mockAttachmentRepo.Object,
            _mockClaimRepo.Object,
            _mockMapper.Object,
            _mockFileManager.Object,
            _mockFileService.Object,
            _mockMasterData.Object
        );

        [Fact]
        public async Task GetForClaimAsync_ReturnsFilteredAttachments_AndMapsCreatedBy()
        {
            // Arrange
            var claimId = 123;
            var memberId = 456;
            var accountId = 789;

            var input = new IdWithUserInfo
            {
                Id = claimId,
                MemberId = memberId,
                AccountInfoId = accountId
            };

            // Use a simple TestDbContext with InMemory provider (async provider included)
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var ctx = new TestDbContext(options);

            // Seed data (only Id=1 and Id=2 should match)
            var now = DateTime.UtcNow;
            var seed = new List<ClaimAttachmentEntity>
        {
            new ClaimAttachmentEntity { Id = 1, ClaimId = claimId, CreatedBy = memberId, FileName = "file1.pdf", DateCreated = now, DateDeleted = null },
            new ClaimAttachmentEntity { Id = 2, ClaimId = claimId, CreatedBy = memberId, FileName = "file2.pdf", DateCreated = now, DateDeleted = null },
            new ClaimAttachmentEntity { Id = 3, ClaimId = 999,    CreatedBy = memberId, FileName = "other.pdf", DateCreated = now, DateDeleted = null },         // filtered by ClaimId
            new ClaimAttachmentEntity { Id = 4, ClaimId = claimId, CreatedBy = 999,     FileName = "otherMember.pdf", DateCreated = now, DateDeleted = null }, // filtered by CreatedBy
            new ClaimAttachmentEntity { Id = 5, ClaimId = claimId, CreatedBy = memberId, FileName = "deleted.pdf", DateCreated = now, DateDeleted = now }      // filtered by DateDeleted
        };

            ctx.ClaimAttachments.AddRange(seed);
            await ctx.SaveChangesAsync();

            // IMPORTANT: return EF IQueryable from our TestDbContext DbSet (async-capable)
            _mockAttachmentRepo
                .Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<ClaimAttachmentEntity, bool>>>(),
                    It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(ctx.ClaimAttachments.AsQueryable());

            // Mock member lookup
            _mockMasterData
                .Setup(m => m.GetMemberAsync(accountId, memberId))
                .ReturnsAsync(new RethinkAccountMember { userName = "John Doe" });

            var sut = CreateSut();

            // Act
            var result = await sut.GetForClaimAsync(input);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Data.Count);

            var ids = result.Data.Select(x => x.Id).OrderBy(x => x).ToList();
            Assert.Equal(new List<int> { 1, 2 }, ids);
            Assert.All(result.Data, x => Assert.Equal("John Doe", x.CreatedBy));
            Assert.Contains(result.Data, x => x.Filename == "file1.pdf");
            Assert.Contains(result.Data, x => x.Filename == "file2.pdf");

            _mockAttachmentRepo.Verify(r => r.GetAllAsync(
                It.IsAny<Expression<Func<ClaimAttachmentEntity, bool>>>(),
                It.IsAny<IEnumerable<string>>()), Times.Once);

            _mockMasterData.Verify(m => m.GetMemberAsync(accountId, memberId), Times.Once);

        }

        [Fact]
        public async Task Get_ReturnsMappedItem_WhenFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var ctx = new TestDbContext(options);
            ctx.ClaimAttachments.AddRange(new List<ClaimAttachmentEntity>
        {
            new ClaimAttachmentEntity { Id = 1, FileName = "a.pdf" },
            new ClaimAttachmentEntity { Id = 2, FileName = "b.pdf" }
        });
            await ctx.SaveChangesAsync();

            // Repository returns an async-capable IQueryable from EF Core
            _mockAttachmentRepo.Setup(r => r.Query()).Returns(ctx.ClaimAttachments.AsQueryable());

            var expectedMapped = new ClaimAttachmentItem { Id = 1, FileName = "a.pdf" };
            _mockMapper.Setup(m => m.Map<ClaimAttachmentItem>(It.IsAny<ClaimAttachmentEntity>()))
                       .Returns(expectedMapped);
            var sut = CreateSut();
            // Act
            var result = await sut.Get(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("a.pdf", result.FileName);

            _mockAttachmentRepo.Verify(r => r.Query(), Times.Once);
            _mockMapper.Verify(m => m.Map<ClaimAttachmentItem>(
                It.Is<ClaimAttachmentEntity>(x => x.Id == 1 && x.FileName == "a.pdf")),
                Times.Once);
        }

        [Fact]
        public async Task Get_ReturnsNull_WhenNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var ctx = new TestDbContext(options);
            ctx.ClaimAttachments.Add(new ClaimAttachmentEntity { Id = 1, FileName = "a.pdf" });
            await ctx.SaveChangesAsync();

            _mockAttachmentRepo.Setup(r => r.Query()).Returns(ctx.ClaimAttachments.AsQueryable());
            _mockMapper.Setup(m => m.Map<ClaimAttachmentItem>(null)).Returns((ClaimAttachmentItem)null);

            // Act
            var sut = CreateSut();
            var result = await sut.Get(999); // ID not in DB

            // Assert
            Assert.Null(result);
            _mockAttachmentRepo.Verify(r => r.Query(), Times.Once);
            _mockMapper.Verify(m => m.Map<ClaimAttachmentItem>(null), Times.Once);
        }

        [Fact]
        public async Task Save_WhenClaimExists_AddsNewAndUpdatesExisting_AndCommits_AndMaps()
        {
            // Arrange
            var claimId = 123;
            var memberId = 456;
            var accountInfoId = 789;

            // Build EF InMemory context
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var ctx = new TestDbContext(options);

            // Seed data
            ctx.Claims.Add(new ClaimEntity { Id = claimId, AccountInfoId = accountInfoId });
            ctx.ClaimAttachments.Add(new ClaimAttachmentEntity { Id = 100, ClaimId = claimId, FileName = "old.pdf" });
            await ctx.SaveChangesAsync();

            // Mock repos
            var mockAttachmentRepo = new Mock<IRepository<BillingDbContext, ClaimAttachmentEntity>>();
            var mockClaimRepo = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockMapper = new Mock<IMapper>();

            // Async-capable IQueryable
            mockClaimRepo.Setup(r => r.Query()).Returns(ctx.Claims);
            mockAttachmentRepo.Setup(r => r.Query()).Returns(ctx.ClaimAttachments);

            // Items (new + existing)
            var items = new List<ClaimAttachmentItem>
            {
                new ClaimAttachmentItem { Id = 0, FileName = "new-file.pdf" },
                new ClaimAttachmentItem { Id = 100, FileName = "updated-file.pdf" }
            };

            // Capture Add/Update
            ClaimAttachmentEntity addedEntity = null;
            ClaimAttachmentEntity updatedEntity = null;

            mockAttachmentRepo
                .Setup(r => r.Add(It.IsAny<ClaimAttachmentEntity>()))
                .Callback<ClaimAttachmentEntity>(e => addedEntity = e);

            mockAttachmentRepo
                .Setup(r => r.Update(It.IsAny<ClaimAttachmentEntity>()))
                .Callback<ClaimAttachmentEntity>(e => updatedEntity = e);

            mockAttachmentRepo
                .Setup(r => r.CommitAsync())
                .Returns(Task.CompletedTask);

            mockMapper
                .Setup(m => m.Map<List<ClaimAttachmentItem>>(It.IsAny<List<ClaimAttachmentEntity>>()))
                .Returns((List<ClaimAttachmentEntity> src) =>
                    src.Select(e => new ClaimAttachmentItem { Id = e.Id, FileName = e.FileName }).ToList());

            // Create SUT using these mocks
            var sut = new ClaimAttachmentService(
                mockAttachmentRepo.Object,
                mockClaimRepo.Object,
                mockMapper.Object,
                Mock.Of<IFileManagerService>(),
                Mock.Of<IFileService>(),
                Mock.Of<IRethinkMasterDataMicroServices>()
            );

            // Act
            var result = await sut.Save(items, claimId, memberId, accountInfoId);

            // Assert
            Assert.NotNull(addedEntity);
            Assert.Equal(claimId, addedEntity.ClaimId);
            Assert.Equal(memberId, addedEntity.CreatedBy);
            Assert.Equal("new-file.pdf", addedEntity.FileName);

            Assert.NotNull(updatedEntity);
            Assert.Equal(100, updatedEntity.Id);
            Assert.Equal("updated-file.pdf", updatedEntity.FileName);
            Assert.Equal(memberId, updatedEntity.ModifiedBy);

            mockAttachmentRepo.Verify(r => r.Add(It.IsAny<ClaimAttachmentEntity>()), Times.Once);
            mockAttachmentRepo.Verify(r => r.Update(It.IsAny<ClaimAttachmentEntity>()), Times.Once);
            mockAttachmentRepo.Verify(r => r.CommitAsync(), Times.Once);

            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.FileName == "new-file.pdf");
            Assert.Contains(result, x => x.FileName == "updated-file.pdf");
        }

        [Fact]
        public async Task RenameAttachmentAsync_ThrowsNullReference_WhenAttachmentDoesNotExist()
        {
            // Arrange
            var model = new RenameAttachmentModelWithUserInfo
            {
                AttachmentId = 999,
                MemberId = 1,
                FileName = "test.pdf"
            };

            _mockAttachmentRepo
                .Setup(x => x.GetByIdAsync(999))
                .ReturnsAsync((ClaimAttachmentEntity)null);

            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(() =>
                _service.RenameAttachmentAsync(model));
        }

        [Fact]
        public async Task UploadFileAsync_Succeeds_WhenClaimExists_NoDuplicate()
        {
            // Arrange
            var claimId = 10;
            var accountId = 99;

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var ctx = new TestDbContext(options);

            // Seed the claim your method is looking for
            ctx.Claims.Add(new ClaimEntity { Id = claimId, AccountInfoId = accountId });
            await ctx.SaveChangesAsync();

            var mockClaimRepo = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockAttachmentRepo = new Mock<IRepository<BillingDbContext, ClaimAttachmentEntity>>();
            var mockFileService = new Mock<IFileService>();
            var mockFileManager = new Mock<IFileManagerService>();
            var mockMapper = new Mock<IMapper>();

            // CRITICAL: return the DbSet directly (EF async-capable IQueryable)
            mockClaimRepo.Setup(r => r.Query()).Returns(ctx.Claims);
            mockAttachmentRepo.Setup(r => r.Query()).Returns(ctx.ClaimAttachments);


            // File service returns a folder path; the code concatenates fileName to it
            var folderPath = "/encounters/"; // ensure ends with a separator, since code does: folder + fileName

            mockFileService
                            .Setup(f => f.PrepareFolderForEncounterAttachmentFile(
                                It.IsAny<int>(),
                                It.IsAny<string>(),
                                It.IsAny<string>(),
                                It.IsAny<bool>(),
                                It.IsAny<int?>(),
                                It.IsAny<string>()))
                            .Returns(folderPath);

            // Capture the stream passed to UploadFileAsync
            MemoryStream captured = null;
            mockFileManager
              .Setup(m => m.UploadFileAsync(folderPath, "report.pdf", It.IsAny<Stream>(), ""));

            // AddAndGetAsync returns the entity with an Id (what method returns)
            mockAttachmentRepo
                .Setup(r => r.AddAndGetAsync(It.IsAny<ClaimAttachmentEntity>()))
                .ReturnsAsync((ClaimAttachmentEntity e) =>
                {
                    e.Id = 42;
                    return e;
                });


            var sut = new ClaimAttachmentService(
                mockAttachmentRepo.Object,
                mockClaimRepo.Object,
                mockMapper.Object,
                mockFileManager.Object,
                mockFileService.Object,
                Mock.Of<IRethinkMasterDataMicroServices>()
            );

            var model = new ClaimUploadModelWithUserInfo
            {
                ClaimId = claimId,
                AccountInfoId = accountId,
                MemberId = 7,
                FileName = "report.pdf",
                FileMimeType = "application/pdf",
                Data = new byte[] { 1, 2, 3 }
            };

            // Act (this is the line that was breaking before)
            var id = await sut.UploadFileAsync(model);

            // Assert
            Assert.Equal(42, id);
        }


        [Fact]
        public async Task Delete_WhenAttachmentAlreadyDeleted_NoUpdate_NoCommit_ReturnsMapped()
        {
            // Arrange
            var accountId = 99;
            var memberId = 5;
            var claimId = 123;
            var attachId = 11;

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var ctx = new TestDbContext(options);

            // Seed
            ctx.Claims.Add(new ClaimEntity { Id = claimId, AccountInfoId = accountId });
            ctx.ClaimAttachments.Add(new ClaimAttachmentEntity
            {
                Id = attachId,
                ClaimId = claimId,
                FileName = "no-claim.pdf",
                DateDeleted = null
            });
            await ctx.SaveChangesAsync();

            // Create ONE set of mocks and use them end-to-end
            var mockAttRepo = new Mock<IRepository<BillingDbContext, ClaimAttachmentEntity>>();
            var mockClaimRepo = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockMapper = new Mock<IMapper>();

            // Return DbSet directly (EF async-capable)
            mockAttRepo.Setup(r => r.Query()).Returns(ctx.ClaimAttachments);
            mockClaimRepo.Setup(r => r.Query()).Returns(ctx.Claims);

            // Mapper setup used by SUT
            mockMapper
                .Setup(m => m.Map<ClaimAttachmentItem>(It.IsAny<ClaimAttachmentEntity>()))
                .Returns((ClaimAttachmentEntity e) => e == null ? null : new ClaimAttachmentItem { Id = e.Id, FileName = e.FileName });

            // Build SUT with THESE mocks
            var sut = new ClaimAttachmentService(
                mockAttRepo.Object,
                mockClaimRepo.Object,
                mockMapper.Object,
                Mock.Of<IFileManagerService>(),
                Mock.Of<IFileService>(),
                Mock.Of<IRethinkMasterDataMicroServices>()
            );

            var input = new ClaimAttachmentItem { Id = attachId };

            // Act
            var result = await sut.Delete(input, memberId, accountId);

            // Assert

            Assert.Equal(attachId, result.Id);

            _mockAttachmentRepo.Verify(r => r.Update(It.IsAny<ClaimAttachmentEntity>()), Times.Never);
            _mockAttachmentRepo.Verify(r => r.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task Delete_WhenClaimMissing_NoUpdate_NoCommit_ReturnsMappedEntityAnyway()
        {
            // Arrange
            var accountId = 99;
            var memberId = 5;
            var claimId = 123;
            var attachId = 12;

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            // Seed
            await using var ctx = new TestDbContext(options);
            ctx.Claims.Add(new ClaimEntity { Id = claimId, AccountInfoId = accountId });
            ctx.ClaimAttachments.Add(new ClaimAttachmentEntity
            {
                Id = attachId,
                ClaimId = claimId,
                FileName = "no-claim.pdf",
                DateDeleted = null
            });
            await ctx.SaveChangesAsync();

            // Create ONE set of mocks and use them end-to-end
            var mockAttRepo = new Mock<IRepository<BillingDbContext, ClaimAttachmentEntity>>();
            var mockClaimRepo = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockMapper = new Mock<IMapper>();

            // Return DbSet directly (EF async-capable)
            mockAttRepo.Setup(r => r.Query()).Returns(ctx.ClaimAttachments);
            mockClaimRepo.Setup(r => r.Query()).Returns(ctx.Claims);

            // Mapper setup used by SUT
            mockMapper
                .Setup(m => m.Map<ClaimAttachmentItem>(It.IsAny<ClaimAttachmentEntity>()))
                .Returns((ClaimAttachmentEntity e) => e == null ? null : new ClaimAttachmentItem { Id = e.Id, FileName = e.FileName });

            // Build SUT with THESE mocks
            var sut = new ClaimAttachmentService(
                mockAttRepo.Object,
                mockClaimRepo.Object,
                mockMapper.Object,
                Mock.Of<IFileManagerService>(),
                Mock.Of<IFileService>(),
                Mock.Of<IRethinkMasterDataMicroServices>()
            );

            var input = new ClaimAttachmentItem { Id = attachId };

            // Act
            var result = await sut.Delete(input, memberId, accountId);

            // Assert
            // Service returns mapped entity even when claim is null (since it maps at end)

            Assert.Equal(attachId, result.Id);

            _mockAttachmentRepo.Verify(r => r.Update(It.IsAny<ClaimAttachmentEntity>()), Times.Never);
            _mockAttachmentRepo.Verify(r => r.CommitAsync(), Times.Never);
        }



        [Fact]
        public async Task RenameAttachmentAsync_Throws_WhenAttachmentNotFound()
        {
            // Arrange
            var mockAttachmentRepo = new Mock<IRepository<BillingDbContext, ClaimAttachmentEntity>>();
            mockAttachmentRepo
                .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((ClaimAttachmentEntity)null);

            var sut = CreateSut();

            var model = new RenameAttachmentModelWithUserInfo
            {
                AttachmentId = 999,
                MemberId = 7,
                FileName = "new.pdf"
            };

            // Act + Assert
            var ex = await Assert.ThrowsAsync<NullReferenceException>(() => sut.RenameAttachmentAsync(model));
            Assert.Equal("Attachment with such id does not exist", ex.Message);

            mockAttachmentRepo.Verify(r => r.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task RenameAttachmentAsync_Throws_WhenUserDoesNotOwnAttachment()
        {
            var mockAttachmentRepo = new Mock<IRepository<BillingDbContext, ClaimAttachmentEntity>>();
            var mockClaimRepo = new Mock<IRepository<BillingDbContext, ClaimEntity>>();

            mockAttachmentRepo
                .Setup(r => r.GetByIdAsync(42))
                .ReturnsAsync(new ClaimAttachmentEntity { Id = 42, CreatedBy = 111, FileName = "old.pdf" });

            var sut = new ClaimAttachmentService(
                mockAttachmentRepo.Object, mockClaimRepo.Object, Mock.Of<IMapper>(),
                Mock.Of<IFileManagerService>(), Mock.Of<IFileService>(), Mock.Of<IRethinkMasterDataMicroServices>());

            var model = new RenameAttachmentModelWithUserInfo { AttachmentId = 42, MemberId = 222, FileName = "new.pdf" };

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.RenameAttachmentAsync(model));
            Assert.Equal("User does not own this attachment", ex.Message);
            mockAttachmentRepo.Verify(r => r.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task RenameAttachmentAsync_WhenOwner_UpdatesName_MarksUpdated_AndCommits()
        {
            // Arrange
            var attachmentId = 42;
            var memberId = 7;

            var entity = new ClaimAttachmentEntity
            {
                Id = attachmentId,
                CreatedBy = memberId,     // must match model.MemberId to avoid UnauthorizedAccessException
                FileName = "old.pdf",
                ModifiedBy = 0,
                DateLastModified = default
            };

            var mockAttachmentRepo = new Mock<IRepository<BillingDbContext, ClaimAttachmentEntity>>();
            var mockClaimRepo = new Mock<IRepository<BillingDbContext, ClaimEntity>>();

            // Return a real entity so the code doesn’t throw on null
            mockAttachmentRepo
                .Setup(r => r.GetByIdAsync(attachmentId))
                .ReturnsAsync(entity);

            // Allow commit
            mockAttachmentRepo
                .Setup(r => r.CommitAsync())
                .Returns(Task.CompletedTask)
                .Verifiable();

            var sut = new ClaimAttachmentService(
                mockAttachmentRepo.Object,
                mockClaimRepo.Object,
                Mock.Of<IMapper>(),
                Mock.Of<IFileManagerService>(),
                Mock.Of<IFileService>(),
                Mock.Of<IRethinkMasterDataMicroServices>()
            );

            var model = new RenameAttachmentModelWithUserInfo
            {
                AttachmentId = attachmentId,
                MemberId = memberId,          // MUST equal entity.CreatedBy
                FileName = "new-name.pdf",
                AccountInfoId = 123
            };

            // Act
            await sut.RenameAttachmentAsync(model);

            // Assert: these lines ran
            Assert.Equal("new-name.pdf", entity.FileName);     // set just before MarkUpdated
            Assert.Equal(memberId, entity.ModifiedBy);         // set by MarkUpdated
            Assert.NotEqual(default, entity.DateLastModified); // set by MarkUpdated

            mockAttachmentRepo.Verify(r => r.GetByIdAsync(attachmentId), Times.Once);
            mockAttachmentRepo.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteUpload_WhenOwner_SoftDeletes_AndCommits()
        {
            // Arrange
            var memberId = 7;
            var attachment = new ClaimAttachmentEntity
            {
                Id = 42,
                CreatedBy = memberId,
                FileName = "doc.pdf",
                DateDeleted = null,
                DateLastModified = default,
                ModifiedBy = 0
            };

            var mockRepo = new Mock<IRepository<BillingDbContext, ClaimAttachmentEntity>>();
            mockRepo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(attachment);
            mockRepo.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask).Verifiable();

            // Build SUT with THIS mock
            var sut = new ClaimAttachmentService(
                mockRepo.Object,
                Mock.Of<IRepository<BillingDbContext, ClaimEntity>>(),
                Mock.Of<IMapper>(),
                Mock.Of<IFileManagerService>(),
                Mock.Of<IFileService>(),
                Mock.Of<IRethinkMasterDataMicroServices>()
            );

            var model = new IdWithUserInfo { Id = 42, MemberId = memberId, AccountInfoId = 123 };

            // Act
            await sut.DeleteUpload(model);

            // Assert
            Assert.NotNull(attachment.DateDeleted);
            Assert.NotEqual(default, attachment.DateDeleted);
            Assert.Equal(memberId, attachment.ModifiedBy);
            Assert.NotEqual(default, attachment.DateLastModified);

            mockRepo.Verify(r => r.GetByIdAsync(42), Times.Once);
            mockRepo.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteUpload_ThrowsNullReference_WhenAttachmentNotFound()
        {
            // Arrange
            var mockAttachmentRepo = new Mock<IRepository<BillingDbContext, ClaimAttachmentEntity>>();
            var mockClaimRepo = new Mock<IRepository<BillingDbContext, ClaimEntity>>();

            // Simulate "attachment not found"
            mockAttachmentRepo
                .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((ClaimAttachmentEntity)null);

            // Create service with mocks
            var sut = new ClaimAttachmentService(
                mockAttachmentRepo.Object,
                mockClaimRepo.Object,
                Mock.Of<IMapper>(),
                Mock.Of<IFileManagerService>(),
                Mock.Of<IFileService>(),
                Mock.Of<IRethinkMasterDataMicroServices>()
            );

            var model = new IdWithUserInfo
            {
                Id = 999,
                MemberId = 1,
                AccountInfoId = 123
            };

            // Act + Assert
            var ex = await Assert.ThrowsAsync<NullReferenceException>(() => sut.DeleteUpload(model));

            Assert.Equal("Attachment with such id does not exist", ex.Message);

            // Also confirm that CommitAsync was never called
            mockAttachmentRepo.Verify(r => r.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteUpload_ThrowsUnauthorizedAccess_WhenUserNotOwner()
        {
            // Arrange
            var mockAttachmentRepo = new Mock<IRepository<BillingDbContext, ClaimAttachmentEntity>>();
            var mockClaimRepo = new Mock<IRepository<BillingDbContext, ClaimEntity>>();

            // Simulate attachment owned by someone else
            var attachment = new ClaimAttachmentEntity
            {
                Id = 42,
                CreatedBy = 111,      // owner in DB
                FileName = "secret.pdf"
            };

            mockAttachmentRepo
                .Setup(r => r.GetByIdAsync(42))
                .ReturnsAsync(attachment);

            var sut = new ClaimAttachmentService(
                mockAttachmentRepo.Object,
                mockClaimRepo.Object,
                Mock.Of<IMapper>(),
                Mock.Of<IFileManagerService>(),
                Mock.Of<IFileService>(),
                Mock.Of<IRethinkMasterDataMicroServices>()
            );

            // The user trying to delete is NOT the owner
            var model = new IdWithUserInfo
            {
                Id = 42,
                MemberId = 222,          // different user
                AccountInfoId = 123
            };

            // Act + Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.DeleteUpload(model));

            Assert.Equal("User does not own this attachment", ex.Message);

            // Verify that we did NOT persist anything
            mockAttachmentRepo.Verify(r => r.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task GetUploadAsync_ReturnsLink_WhenOwnerAndExists()
        {
            // Arrange
            var memberId = 7;
            var expectedUrl = "https://files.example.com/uploads/report.pdf";

            var attachment = new ClaimAttachmentEntity
            {
                Id = 42,
                CreatedBy = memberId,                // user owns it
                FilePath = "/uploads/report.pdf"
            };

            var mockAttachmentRepo = new Mock<IRepository<BillingDbContext, ClaimAttachmentEntity>>();
            var mockClaimRepo = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFileManager = new Mock<IFileManagerService>();

            // Return a valid attachment
            mockAttachmentRepo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(attachment);

            // Return a file URL when requested
            mockFileManager
                 .Setup(m => m.GetFileUrl("/uploads/report.pdf", It.IsAny<int>(), It.IsAny<string>()))
                 .ReturnsAsync(expectedUrl);
            var sut = new ClaimAttachmentService(
                mockAttachmentRepo.Object,
                mockClaimRepo.Object,
                Mock.Of<IMapper>(),
                mockFileManager.Object,
                Mock.Of<IFileService>(),
                Mock.Of<IRethinkMasterDataMicroServices>()
            );

            var model = new IdWithUserInfo
            {
                Id = 42,
                MemberId = memberId,
                AccountInfoId = 123
            };

            // Act
            var result = await sut.GetUploadAsync(model);

            // Assert
            Assert.Equal(expectedUrl, result);

            // Verify the call sequence
            mockAttachmentRepo.Verify(r => r.GetByIdAsync(42), Times.Once);

            // If GetFileUrl has only one parameter:
            mockFileManager
    .Verify(m => m.GetFileUrl("/uploads/report.pdf", It.IsAny<int>(), It.IsAny<string>()), Times.Once);


        }

        [Fact]
        public async Task GetUploadAsync_ThrowsNullReference_WhenAttachmentNotFound()
        {
            // Arrange
            var mockAttachmentRepo = new Mock<IRepository<BillingDbContext, ClaimAttachmentEntity>>();
            var mockClaimRepo = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFileManager = new Mock<IFileManagerService>();

            // Return null to simulate "not found"
            mockAttachmentRepo
                .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((ClaimAttachmentEntity)null);

            // Build the service under test
            var sut = new ClaimAttachmentService(
                mockAttachmentRepo.Object,
                mockClaimRepo.Object,
                Mock.Of<IMapper>(),
                mockFileManager.Object,
                Mock.Of<IFileService>(),
                Mock.Of<IRethinkMasterDataMicroServices>()
            );

            var model = new IdWithUserInfo
            {
                Id = 999,
                MemberId = 1,
                AccountInfoId = 123
            };

            // Act + Assert
            var ex = await Assert.ThrowsAsync<NullReferenceException>(() => sut.GetUploadAsync(model));

            Assert.Equal("Attachment with such id does not exist", ex.Message);

            // Verify no file operations or commits occurred
            mockFileManager.Verify(
               m => m.GetFileUrl(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            mockAttachmentRepo.Verify(r => r.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task GetUploadAsync_ThrowsUnauthorized_WhenUserNotOwner()
        {
            // Arrange
            var attachment = new ClaimAttachmentEntity
            {
                Id = 42,
                CreatedBy = 111,
                FilePath = "/encounters/42/report.pdf"
            };

            var mockAttachmentRepo = new Mock<IRepository<BillingDbContext, ClaimAttachmentEntity>>();
            var mockClaimRepo = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            var mockFileManager = new Mock<IFileManagerService>();

            mockAttachmentRepo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(attachment);

            var sut = new ClaimAttachmentService(
                mockAttachmentRepo.Object,
                mockClaimRepo.Object,
                Mock.Of<IMapper>(),
                mockFileManager.Object,
                Mock.Of<IFileService>(),
                Mock.Of<IRethinkMasterDataMicroServices>()
            );

            var model = new IdWithUserInfo
            {
                Id = 42,
                MemberId = 222,
                AccountInfoId = 123
            };

            // Act + Assert
            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.GetUploadAsync(model));
            Assert.Equal("User does not own this attachment", ex.Message);

            mockFileManager.Verify(m => m.GetFileUrl(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }
    }

}
