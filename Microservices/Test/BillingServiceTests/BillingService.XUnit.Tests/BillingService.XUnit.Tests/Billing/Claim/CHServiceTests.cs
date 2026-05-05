using Billing.FolderStructure.Core.Enum;
using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Services;
using Billing.FolderStructure.Core.Utils;
using BillingService.Domain.Interfaces.Files;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Services.Billing;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Handlers;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Messaging;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Services;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public class CHServiceTests
{
    private readonly Mock<IRepository<BillingDbContext, ClaimEntity>> _claimRepo = new();
    private readonly Mock<IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity>> _funderRepo = new();
    private readonly Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>> _submissionRepo = new();
    private readonly Mock<IMessageBus> _bus = new();
    private readonly Mock<IFileManagerService> _fileManager = new();
    private readonly Mock<IFileService> _fileService = new();
    private readonly Mock<IBlobProcessingService> _blobProcessing = new();
    private readonly Mock<IBillingBlobService> _billingBlob = new();
    private readonly Mock<IBillingFilePath> _billingPath = new();
    private readonly Mock<ILogger<CHService>> _logger = new();
    private readonly Mock<IEdiFilesDownload> _ediFilesDownload = new();

    private CHService CreateSut() =>
        new CHService(
            _claimRepo.Object,
            _bus.Object,
            _fileManager.Object,
            _fileService.Object,
            _blobProcessing.Object,
            _billingBlob.Object,
            _billingPath.Object,
            _funderRepo.Object,
            _submissionRepo.Object,
            _logger.Object,
            _ediFilesDownload.Object);

    // ---------------------------------------------------------
    // UploadFileAsync – SUCCESS
    // ---------------------------------------------------------
    [Fact]
    public async Task UploadFileAsync_ValidRequest_ReturnsTrue()
    {
        var claims = new[]
        {
            new ClaimEntity { Id = 1, AccountInfoId = 10, ClaimIdentifier = "CLM1" }
        }.AsQueryable().BuildMock();

        _claimRepo.Setup(r => r.Query()).Returns(claims);

        _fileService.Setup(x =>
            x.PrepareFolderForEncounterAttachmentFile(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), true, null, It.IsAny<string>()))
            .Returns("path/");

        SetupBillingBlob_Internal(FileTypes.Type837.ToString());

        var sut = CreateSut();

        var result = await sut.UploadFileAsync(new ClaimUploadModelWithUserInfo
        {
            ClaimId = 1,
            FileName = "test.edi",
            Data = Encoding.UTF8.GetBytes("DATA")
        });

        Assert.True(result);
    }

    // ---------------------------------------------------------
    // UploadFileAsync – ClaimId null
    // ---------------------------------------------------------
    [Fact]
    public async Task UploadFileAsync_ClaimIdNull_ReturnsFalse()
    {
        var sut = CreateSut();

        var result = await sut.UploadFileAsync(new ClaimUploadModelWithUserInfo
        {
            ClaimId = 145,
            FileName = "x",
            Data = Encoding.UTF8.GetBytes("x")
        });

        Assert.False(result);
    }

    // ---------------------------------------------------------
    // UploadFileAsync – Data null / empty
    // ---------------------------------------------------------
    [Theory]
    [InlineData(null)]
    [InlineData(new byte[0])]
    public async Task UploadFileAsync_DataInvalid_ReturnsFalse(byte[] data)
    {
        var claims = new[]
        {
            new ClaimEntity { Id = 1 }
        }.AsQueryable().BuildMock();

        _claimRepo.Setup(r => r.Query()).Returns(claims);

        var sut = CreateSut();

        var result = await sut.UploadFileAsync(new ClaimUploadModelWithUserInfo
        {
            ClaimId = 1,
            FileName = "x",
            Data = data
        });

        Assert.False(result);
    }

    // ---------------------------------------------------------
    // UploadERAErrorFileAsync – SUCCESS
    // ---------------------------------------------------------
    [Fact]
    public async Task UploadERAErrorFileAsync_Success_ReturnsTrue()
    {
        SetupBillingBlob_Internal(FileTypes.Type837.ToString());

        _fileService.Setup(x => x.PrepareFolderForERAErrorFile(It.IsAny<int>()))
            .Returns("path/");

        _fileManager.Setup(x =>
            x.UploadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _fileManager.Setup(x =>
            x.GetFileUrl(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync("url");

        var sut = CreateSut();

        var result = await sut.UploadERAErrorFileAsync(new ERAUploadModel
        {
            accountInfoId = 1,
            fileName = "era.edi",
            data = Encoding.UTF8.GetBytes("ERA"),
            containerName = "c"
        });

        Assert.True(result);
    }

    // ---------------------------------------------------------
    // UploadERAErrorFileAsync – Exception
    // ---------------------------------------------------------
    [Fact]
    public async Task UploadERAErrorFileAsync_Exception_ReturnsFalse()
    {
        _fileService.Setup(x => x.PrepareFolderForERAErrorFile(It.IsAny<int>()))
            .Throws(new Exception());

        var sut = CreateSut();

        var result = await sut.UploadERAErrorFileAsync(new ERAUploadModel());

        Assert.False(result);
    }

    // ---------------------------------------------------------
    // UploadEDIResponseFile – SUCCESS (result != null)
    // ---------------------------------------------------------
    [Fact]
    public async Task UploadEDIResponseFile_Success_ReturnsTrue()
    {
        SetupBillingBlob_Internal(FileTypes.Type835.ToString(), resultExists: true);

        _bus.Setup(x =>
            x.SendAsync(It.IsAny<EdiDownloadData>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        var result = await sut.UploadEDIResponseFile(new DownloadSftpDataModel
        {
            Title = "EDI",
            FileName = "835",
            Data = Encoding.UTF8.GetBytes("DATA")
        });

        Assert.True(result);
    }

    // ---------------------------------------------------------
    // UploadEDIResponseFile – Exception
    // ---------------------------------------------------------
    [Fact]
    public async Task UploadEDIResponseFile_Exception_ReturnsFalse()
    {
        _billingPath.Setup(x => x.GetTransactionControlNumber(It.IsAny<string>()))
            .ThrowsAsync(new Exception());

        var sut = CreateSut();

        var result = await sut.UploadEDIResponseFile(new DownloadSftpDataModel());

        Assert.False(result);
    }

    // ---------------------------------------------------------
    // UploadEDIResponseFilesToBlobBackup – Empty list
    // ---------------------------------------------------------
    [Fact]
    public async Task UploadEDIResponseFilesToBlobBackup_Empty_ReturnsFalse()
    {
        var sut = CreateSut();

        var result = await sut.UploadEDIResponseFilesToBlobBackup(
            new UploadAvailityFilesModel { files = new List<(MemoryStream, string)>() });

        Assert.False(result);
    }

    // ---------------------------------------------------------
    // UploadEDIResponseFilesToBlobBackup – Success
    // ---------------------------------------------------------
    [Fact]
    public async Task UploadEDIResponseFilesToBlobBackup_WithFiles_ReturnsTrue()
    {
        _billingBlob.Setup(x =>
            x.UploadAvailityFilesToBlobBackupAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
            .ReturnsAsync("ok");

        var sut = CreateSut();

        var result = await sut.UploadEDIResponseFilesToBlobBackup(
            new UploadAvailityFilesModel
            {
                files = new List<(MemoryStream, string)>
                {
                    (new MemoryStream(), "file.edi")
                }
            });

        Assert.True(result);
    }

    // ---------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------
    private void SetupBillingBlob_Internal(string fileType, bool resultExists = false)
    {
        _billingPath.Setup(x => x.GetTransactionControlNumber(It.IsAny<string>()))
            .ReturnsAsync(new TransactionControlNumberModel { FileType = fileType });

        _billingPath.Setup(x => x.FetchClaimSubmissionDataForERA(It.IsAny<TransactionControlNumberModel>()))
            .ReturnsAsync(resultExists
                ? new ClaimSubmissionEntity { Claim = new ClaimEntity { AccountInfoId = 10 } }
                : null);

        _billingPath.Setup(x => x.CreateFolderPath(It.IsAny<BillingRequest>()))
            .ReturnsAsync("path");

        _billingPath.Setup(x => x.SplitFilePath(It.IsAny<string>()))
            .ReturnsAsync(("c", "f"));

        _billingBlob.Setup(x =>
            x.UploadIntoContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
            .ReturnsAsync("ok");

        _billingBlob.Setup(x =>
            x.DeleteBlobFromContainerAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _blobProcessing.Setup(x =>
            x.UploadIntoContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()));
    }
}

