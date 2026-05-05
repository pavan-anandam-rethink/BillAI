using AutoFixture;
using Azure;
using Azure.Storage.Blobs.Models;
using BillingService.Domain.Models;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Domain.Services.Payment;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.Tests.Domain.Services.Payment
{

    public class TestAsyncEnumerable1<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable1(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerable1(Expression expression) : base(expression) { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }


    public class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;
        public TestAsyncQueryProvider(IQueryProvider inner) { _inner = inner; }

        public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);

        public object Execute(Expression expression) => _inner.Execute(expression);
        public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

        public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression) =>
            new TestAsyncEnumerable<TResult>(expression);

        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken) =>
            Task.FromResult(Execute<TResult>(expression));

        TResult IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    public class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerable(Expression expression) : base(expression) { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

        public ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
        public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());
        public T Current => _inner.Current;
    }

    public class PaymentAttachmentServiceTests
    {
        private readonly Mock<IBlobProcessingService> _blobProcessingMock;
        private readonly Mock<IRepository<BillingDbContext, PaymentAttachmentEntity>> _repoMock;
        private readonly Mock<IRethinkMasterDataMicroServices> _rethinkMock;
        private readonly PaymentAttachmentService _service;
        private readonly Fixture Fixture = new();

        public PaymentAttachmentServiceTests()
        {
            _blobProcessingMock = new Mock<IBlobProcessingService>();
            _repoMock = new Mock<IRepository<BillingDbContext, PaymentAttachmentEntity>>();
            _rethinkMock = new Mock<IRethinkMasterDataMicroServices>();

            _service = new PaymentAttachmentService(
                _blobProcessingMock.Object,
                _repoMock.Object,
                _rethinkMock.Object
            );
        }

        [Fact]
        public async Task UploadFile_ReturnsZero_WhenFileAlreadyExists()
        {
            // Arrange
            var model = new PaymentUploadModelWithUserInfo
            {
                Data = new byte[] { 1, 2, 3, 4 },
                FileName = "invoice.pdf",
                FileMimeType = "application/pdf",
                PaymentId = 12345,
                MemberId = 1001
            };

            var existingAttachments = new List<PaymentAttachmentEntity>
            {
                new PaymentAttachmentEntity
                {
                    PaymentId = 12345,
                    FileName = "invoice.pdf",
                    BlobFileName = Guid.NewGuid().ToString(),
                    FileSize = 1024,
                    FileMimeType = "application/pdf",
                    FilePath = "paymentattachment/" + Guid.NewGuid().ToString(),
                    CreatedBy = 1001,
                    DateCreated = DateTime.UtcNow,
                    DateDeleted = null,
                    Member = null
                }
            };


            var asyncQueryable = new TestAsyncEnumerable<PaymentAttachmentEntity>(existingAttachments);

            _repoMock.Setup(r => r.Query())
                .Returns(asyncQueryable);


            // Act
            var result = await _service.UploadFile(model);

            // Assert
            Assert.Equal(0, result);
            _repoMock.Verify(r => r.Query(), Times.Once);
            _repoMock.Verify(r => r.AddAndGetAsync(It.IsAny<PaymentAttachmentEntity>()), Times.Never);
            _blobProcessingMock.Verify(b => b.UploadIntoContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()), Times.Never);
        }

        [Fact]
        public async Task UploadFile_AddsFile_WhenFileDoesNotExist()
        {
            // Arrange
            var model = new PaymentUploadModelWithUserInfo
            {
                Data = new byte[] { 9, 8, 7 },
                FileName = "newfile.pdf",
                FileMimeType = "application/pdf",
                PaymentId = 555,
                MemberId = 2000
            };

            // repo.Query returns no existing attachments
            var emptyList = new List<PaymentAttachmentEntity>();
            var asyncQueryable = new TestAsyncEnumerable<PaymentAttachmentEntity>(emptyList);
            _repoMock.Setup(r => r.Query()).Returns(asyncQueryable);

            // repository should add and return the new entity with an Id
            var createdEntity = new PaymentAttachmentEntity { Id = 42 };
            _repoMock.Setup(r => r.AddAndGetAsync(It.IsAny<PaymentAttachmentEntity>()))
              .ReturnsAsync(createdEntity);

            _blobProcessingMock.Setup(b => b.UploadIntoContainerAsync("paymentattachment", It.IsAny<string>(), It.IsAny<MemoryStream>()))
              .ReturnsAsync(It.IsAny<Response<BlobContentInfo>>());

            // Act
            var result = await _service.UploadFile(model);

            // Assert
            Assert.Equal(42, result);
            _repoMock.Verify(r => r.Query(), Times.Once);
            _blobProcessingMock.Verify(b => b.UploadIntoContainerAsync("paymentattachment", It.IsAny<string>(), It.IsAny<MemoryStream>()), Times.Once);
            _repoMock.Verify(r => r.AddAndGetAsync(It.IsAny<PaymentAttachmentEntity>()), Times.Once);
        }

        [Fact]
        public async Task UploadFile_ShouldUploadFile_WhenFileWithSameNameIsDeleted()
        {
            // Arrange
            var model = new PaymentUploadModelWithUserInfo
            {
                FileName = "restored.pdf",
                FileMimeType = "application/pdf",
                PaymentId = 100,
                MemberId = 1,
                AccountInfoId = 5,
                Data = new byte[] { 1, 2, 3 }
            };

            var deletedAttachment = new PaymentAttachmentEntity
            {
                Id = 10,
                FileName = "restored.pdf",
                PaymentId = 100,
                DateDeleted = DateTime.UtcNow.AddDays(-1)
            };

            var asyncQueryable = new TestAsyncEnumerable<PaymentAttachmentEntity>(new List<PaymentAttachmentEntity> { deletedAttachment });
            _repoMock.Setup(r => r.Query()).Returns(asyncQueryable);

            var expectedAttachmentId = 50;
            _repoMock.Setup(r => r.AddAndGetAsync(It.IsAny<PaymentAttachmentEntity>()))
                .ReturnsAsync((PaymentAttachmentEntity entity) =>
                {
                    entity.Id = expectedAttachmentId;
                    return entity;
                });

            _blobProcessingMock.Setup(b => b.UploadIntoContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
                .ReturnsAsync(It.IsAny<Response<BlobContentInfo>>());

            // Act
            var result = await _service.UploadFile(model);

            // Assert
            Assert.Equal(expectedAttachmentId, result);
            _repoMock.Verify(r => r.AddAndGetAsync(It.IsAny<PaymentAttachmentEntity>()), Times.Once);
            _blobProcessingMock.Verify(b => b.UploadIntoContainerAsync("paymentattachment", It.IsAny<string>(), It.IsAny<MemoryStream>()), Times.Once);
        }

        [Fact]
        public async Task UploadFile_ShouldSetCorrectFileProperties_WhenUploadingFile()
        {
            // Arrange
            var model = new PaymentUploadModelWithUserInfo
            {
                FileName = "invoice.docx",
                FileMimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                PaymentId = 200,
                MemberId = 10,
                AccountInfoId = 20,
                Data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            };

            PaymentAttachmentEntity capturedEntity = null;

            var asyncQueryable = new TestAsyncEnumerable<PaymentAttachmentEntity>(new List<PaymentAttachmentEntity>());
            _repoMock.Setup(r => r.Query()).Returns(asyncQueryable);

            _repoMock.Setup(r => r.AddAndGetAsync(It.IsAny<PaymentAttachmentEntity>()))
                .ReturnsAsync((PaymentAttachmentEntity entity) =>
                {
                    capturedEntity = entity;
                    entity.Id = 99;
                    return entity;
                });

            _blobProcessingMock.Setup(b => b.UploadIntoContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
                .ReturnsAsync(It.IsAny<Response<BlobContentInfo>>());

            // Act
            await _service.UploadFile(model);

            // Assert
            Assert.NotNull(capturedEntity);
            Assert.Equal(model.FileName, capturedEntity.FileName);
            Assert.Equal(model.FileMimeType, capturedEntity.FileMimeType);
            Assert.Equal(model.PaymentId, capturedEntity.PaymentId);
            Assert.Equal(model.MemberId, capturedEntity.CreatedBy);
            Assert.Equal(model.Data.Length, capturedEntity.FileSize);
            Assert.NotNull(capturedEntity.BlobFileName);
            Assert.NotNull(capturedEntity.FilePath);
            Assert.StartsWith("paymentattachment/", capturedEntity.FilePath);
        }

        [Fact]
        public async Task UploadFile_ShouldGenerateUniqueGuid_ForBlobFileName()
        {
            // Arrange
            var model = new PaymentUploadModelWithUserInfo
            {
                FileName = "test.pdf",
                FileMimeType = "application/pdf",
                PaymentId = 100,
                MemberId = 1,
                AccountInfoId = 5,
                Data = new byte[] { 1, 2, 3 }
            };

            var capturedBlobFileNames = new List<string>();

            var asyncQueryable = new TestAsyncEnumerable<PaymentAttachmentEntity>(new List<PaymentAttachmentEntity>());
            _repoMock.Setup(r => r.Query()).Returns(asyncQueryable);

            _repoMock.Setup(r => r.AddAndGetAsync(It.IsAny<PaymentAttachmentEntity>()))
                .ReturnsAsync((PaymentAttachmentEntity entity) =>
                {
                    capturedBlobFileNames.Add(entity.BlobFileName);
                    entity.Id = 1;
                    return entity;
                });

            _blobProcessingMock.Setup(b => b.UploadIntoContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
                .ReturnsAsync(It.IsAny<Response<BlobContentInfo>>());

            // Act
            await _service.UploadFile(model);
            await _service.UploadFile(model);

            // Assert
            Assert.Equal(2, capturedBlobFileNames.Count);
            Assert.NotEqual(capturedBlobFileNames[0], capturedBlobFileNames[1]);
            Assert.All(capturedBlobFileNames, name => Assert.True(Guid.TryParse(name, out _)));
        }

        [Fact]
        public async Task UploadFile_ShouldUploadToCorrectContainer_WithCorrectBlobName()
        {
            // Arrange
            var model = new PaymentUploadModelWithUserInfo
            {
                FileName = "test.pdf",
                FileMimeType = "application/pdf",
                PaymentId = 100,
                MemberId = 1,
                AccountInfoId = 5,
                Data = new byte[] { 1, 2, 3, 4, 5 }
            };

            string capturedContainerName = null;
            string capturedBlobName = null;
            MemoryStream capturedStream = null;

            var asyncQueryable = new TestAsyncEnumerable<PaymentAttachmentEntity>(new List<PaymentAttachmentEntity>());
            _repoMock.Setup(r => r.Query()).Returns(asyncQueryable);

            _repoMock.Setup(r => r.AddAndGetAsync(It.IsAny<PaymentAttachmentEntity>()))
                .ReturnsAsync((PaymentAttachmentEntity entity) =>
                {
                    entity.Id = 1;
                    return entity;
                });

            _blobProcessingMock.Setup(b => b.UploadIntoContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
                .Callback<string, string, MemoryStream>((container, blob, stream) =>
                {
                    capturedContainerName = container;
                    capturedBlobName = blob;
                    capturedStream = stream;
                })
                .ReturnsAsync(It.IsAny<Response<BlobContentInfo>>());

            // Act
            await _service.UploadFile(model);

            // Assert
            Assert.Equal("paymentattachment", capturedContainerName);
            Assert.NotNull(capturedBlobName);
            Assert.True(Guid.TryParse(capturedBlobName, out _));
            Assert.NotNull(capturedStream);
            Assert.Equal(model.Data.Length, capturedStream.Length);
        }

        [Fact]
        public async Task UploadFile_ShouldHandleEmptyFileData()
        {
            // Arrange
            var model = new PaymentUploadModelWithUserInfo
            {
                FileName = "empty.txt",
                FileMimeType = "text/plain",
                PaymentId = 100,
                MemberId = 1,
                AccountInfoId = 5,
                Data = new byte[] { }
            };

            var asyncQueryable = new TestAsyncEnumerable<PaymentAttachmentEntity>(new List<PaymentAttachmentEntity>());
            _repoMock.Setup(r => r.Query()).Returns(asyncQueryable);

            _repoMock.Setup(r => r.AddAndGetAsync(It.IsAny<PaymentAttachmentEntity>()))
                .ReturnsAsync((PaymentAttachmentEntity entity) =>
                {
                    entity.Id = 1;
                    return entity;
                });

            _blobProcessingMock.Setup(b => b.UploadIntoContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
                .ReturnsAsync(It.IsAny<Response<BlobContentInfo>>());

            // Act
            var result = await _service.UploadFile(model);

            // Assert
            Assert.NotEqual(0, result);
            _repoMock.Verify(r => r.AddAndGetAsync(It.Is<PaymentAttachmentEntity>(e => e.FileSize == 0)), Times.Once);
        }

        [Fact]
        public async Task UploadFile_ShouldReturnZero_WhenMultipleFilesWithSameNameExist()
        {
            // Arrange
            var model = new PaymentUploadModelWithUserInfo
            {
                FileName = "duplicate.pdf",
                FileMimeType = "application/pdf",
                PaymentId = 100,
                MemberId = 1,
                AccountInfoId = 5,
                Data = new byte[] { 1, 2, 3 }
            };

            var existingAttachments = new List<PaymentAttachmentEntity>
            {
                new PaymentAttachmentEntity
                {
                    Id = 10,
                    FileName = "duplicate.pdf",
                    PaymentId = 100,
                    DateDeleted = null
                },
                new PaymentAttachmentEntity
                {
                    Id = 11,
                    FileName = "duplicate.pdf",
                    PaymentId = 100,
                    DateDeleted = null
                }
            };

            var asyncQueryable = new TestAsyncEnumerable<PaymentAttachmentEntity>(existingAttachments);
            _repoMock.Setup(r => r.Query()).Returns(asyncQueryable);

            // Act
            var result = await _service.UploadFile(model);

            // Assert
            Assert.Equal(0, result);
            _repoMock.Verify(r => r.AddAndGetAsync(It.IsAny<PaymentAttachmentEntity>()), Times.Never);
            _blobProcessingMock.Verify(b => b.UploadIntoContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()), Times.Never);
        }

        [Fact]
        public async Task UploadFile_ShouldUploadFile_ForDifferentPaymentIds()
        {
            // Arrange
            var model = new PaymentUploadModelWithUserInfo
            {
                FileName = "receipt.pdf",
                FileMimeType = "application/pdf",
                PaymentId = 200,
                MemberId = 1,
                AccountInfoId = 5,
                Data = new byte[] { 1, 2, 3 }
            };

            var existingAttachment = new PaymentAttachmentEntity
            {
                Id = 10,
                FileName = "receipt.pdf",
                PaymentId = 100,
                DateDeleted = null
            };

            var asyncQueryable = new TestAsyncEnumerable<PaymentAttachmentEntity>(new List<PaymentAttachmentEntity> { existingAttachment });
            _repoMock.Setup(r => r.Query()).Returns(asyncQueryable);

            _repoMock.Setup(r => r.AddAndGetAsync(It.IsAny<PaymentAttachmentEntity>()))
                .ReturnsAsync((PaymentAttachmentEntity entity) =>
                {
                    entity.Id = 20;
                    return entity;
                });

            _blobProcessingMock.Setup(b => b.UploadIntoContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
                .ReturnsAsync(It.IsAny<Response<BlobContentInfo>>());

            // Act
            var result = await _service.UploadFile(model);

            // Assert
            Assert.Equal(20, result);
            _repoMock.Verify(r => r.AddAndGetAsync(It.IsAny<PaymentAttachmentEntity>()), Times.Once);
        }

        [Fact]
        public async Task UploadFile_ShouldSetAuditFields_WhenCreatingEntity()
        {
            // Arrange
            var model = new PaymentUploadModelWithUserInfo
            {
                FileName = "test.pdf",
                FileMimeType = "application/pdf",
                PaymentId = 100,
                MemberId = 42,
                AccountInfoId = 5,
                Data = new byte[] { 1, 2, 3 }
            };

            PaymentAttachmentEntity capturedEntity = null;

            var asyncQueryable = new TestAsyncEnumerable<PaymentAttachmentEntity>(new List<PaymentAttachmentEntity>());
            _repoMock.Setup(r => r.Query()).Returns(asyncQueryable);

            _repoMock.Setup(r => r.AddAndGetAsync(It.IsAny<PaymentAttachmentEntity>()))
                .ReturnsAsync((PaymentAttachmentEntity entity) =>
                {
                    capturedEntity = entity;
                    entity.Id = 1;
                    return entity;
                });

            _blobProcessingMock.Setup(b => b.UploadIntoContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
                .ReturnsAsync(It.IsAny<Response<BlobContentInfo>>());

            // Act
            await _service.UploadFile(model);

            // Assert
            Assert.NotNull(capturedEntity);
            Assert.Equal(42, capturedEntity.CreatedBy);
            Assert.Equal(42, capturedEntity.ModifiedBy);
            Assert.NotNull(capturedEntity.DateCreated);
            Assert.NotNull(capturedEntity.DateLastModified);
        }

        [Fact]
        public async Task DeleteUpload_ThrowsUnauthorized_WhenNotOwner()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 100,
                MemberId = 1
            };

            var attachment = new PaymentAttachmentEntity
            {
                Id = 100,
                CreatedBy = 999, // different owner
                FilePath = "paymentattachment/guid"
            };

            _repoMock.Setup(r => r.GetByIdAsync(model.Id)).ReturnsAsync(attachment);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.DeleteUpload(model));

            // Ensure no delete was attempted on blob service and no commit
            _blobProcessingMock.Verify(b => b.DeleteBlobFromContainerAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _repoMock.Verify(r => r.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteUpload_DeletesBlobAndSoftDeletesEntity_WhenOwner()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 300,
                MemberId = 5000
            };

            var attachment = new PaymentAttachmentEntity
            {
                Id = 300,
                CreatedBy = 5000,
                FilePath = "paymentattachment/guid-to-delete",
                DateDeleted = null
            };

            _repoMock.Setup(r => r.GetByIdAsync(model.Id)).ReturnsAsync(attachment);

            _blobProcessingMock.Setup(b => b.DeleteBlobFromContainerAsync("paymentattachment", "guid-to-delete"))
              .Returns(Task.CompletedTask);

            _repoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            // Act
            await _service.DeleteUpload(model);

            // Assert
            _blobProcessingMock.Verify(b => b.DeleteBlobFromContainerAsync("paymentattachment", "guid-to-delete"), Times.Once);
            _repoMock.Verify(r => r.CommitAsync(), Times.Once);
            Assert.NotNull(attachment.DateDeleted);
        }

        [Fact]
        public async Task GetUpload_ReturnsStreamAndFilename()
        {
            // Arrange
            var id = 200;
            var fileBytes = new byte[] { 10, 20, 30 };
            var attachment = new PaymentAttachmentEntity
            {
                Id = id,
                FilePath = "paymentattachment/guid123",
                FileName = "receipt.png"
            };

            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(attachment);

            _blobProcessingMock.Setup(b => b.DownloadBlobFromContainerAsync("paymentattachment", "guid123"))
                .ReturnsAsync(new MemoryStream(fileBytes));

            // Act
            var result = await _service.GetUpload(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("receipt.png", result.Filename);
            Assert.Equal(fileBytes.Length, result.MemoryStream.Length);
            // stream should be positioned at 0 after service Seek
            Assert.Equal(0, result.MemoryStream.Position);
        }

        [Fact]
        public async Task DeleteUploads_DeletesOnlyOwnedAttachments_AndDownloadsBlobs()
        {
            // Arrange
            var model = new DeleteAttachmentsModelWithUserInfo
            {
                Ids = new List<int> { 1, 2 },
                MemberId = 1001
            };

            var attachments = new List<PaymentAttachmentEntity>
            {
                new PaymentAttachmentEntity
                {
                    Id = 1,
                    FilePath = "paymentattachment/file1.pdf",
                    CreatedBy = 1001,   // SHOULD BE DELETED
                    DateDeleted = null
                },
                new PaymentAttachmentEntity
                {
                    Id = 2,
                    FilePath = "paymentattachment/file2.pdf",
                    CreatedBy = 9999,   // NOT OWNER -> SKIPPED
                    DateDeleted = null
                }
            };

            // Provide async-capable list
            var asyncAttachments = new TestAsyncEnumerable<PaymentAttachmentEntity>(attachments);

            // Mock repository to return async attachments
            _repoMock.Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<PaymentAttachmentEntity, bool>>>(),
                    null
                ))
                .ReturnsAsync(asyncAttachments);


            // Mock blob download
            var mam = new MemoryStream(new byte[] { 1, 2, 3 });
            _blobProcessingMock.Setup(b =>
                    b.DownloadBlobFromContainerAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(mam);

            // Mock commit
            _repoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            // Act
            await _service.DeleteUploads(model);

            // Assert
            // Owner's attachment should be processed
            _blobProcessingMock.Verify(b =>
                b.DownloadBlobFromContainerAsync("paymentattachment", "file1.pdf"),
                Times.Once);

            // Unauthorized attachment should be skipped
            _blobProcessingMock.Verify(b =>
                b.DownloadBlobFromContainerAsync("paymentattachment", "file2.pdf"),
                Times.Never);

            // Soft delete ONLY for authorized attachment
            Assert.NotNull(attachments[0].DateDeleted);
            Assert.Null(attachments[1].DateDeleted);

            // Ensure CommitAsync is called
            _repoMock.Verify(r => r.CommitAsync(), Times.Once);

            // Ensure GetAllAsync executed once

            _repoMock.Verify(r => r.GetAllAsync(
                   It.IsAny<Expression<Func<PaymentAttachmentEntity, bool>>>(),
                   It.IsAny<IEnumerable<string>>()),
                   Times.Once);
        }

        [Fact]
        public async Task RenameAttachmentAsync_ShouldRenameAndCommit_WhenOwner()
        {
            // Arrange
            var existing = new PaymentAttachmentEntity
            {
                Id = 1,
                CreatedBy = 100,   // Owner
                FileName = "oldname.pdf"
            };

            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
            _repoMock.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

            var model = new RenameAttachmentModelWithUserInfo
            {
                AttachmentId = 1,
                FileName = "newname.pdf",
                MemberId = 100 // same as CreatedBy
            };

            // Act
            await _service.RenameAttachmentAsync(model);

            // Assert
            Assert.Equal("newname.pdf", existing.FileName);
            _repoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
            _repoMock.Verify(r => r.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task RenameAttachmentAsync_ShouldThrowUnauthorizedAccessException_WhenUserDoesNotOwnAttachment()
        {
            // Arrange
            var model = new RenameAttachmentModelWithUserInfo
            {
                AttachmentId = 1,
                MemberId = 100,  // The MemberId that is trying to rename the attachment
                FileName = "newFileName.pdf"
            };

            // Create an attachment with a different CreatedBy (e.g., member 200)
            var attachment = new PaymentAttachmentEntity
            {
                Id = 1,
                CreatedBy = 200,  // This is not the same as model.MemberId (100)
                FileName = "oldFileName.pdf"
            };

            // Mock the repository to return the attachment
            _repoMock.Setup(r => r.GetByIdAsync(model.AttachmentId))
                .ReturnsAsync(attachment);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.RenameAttachmentAsync(model)
            );

            // Assert
            Assert.Equal("User does not own this attachment", exception.Message);
        }
    }
}