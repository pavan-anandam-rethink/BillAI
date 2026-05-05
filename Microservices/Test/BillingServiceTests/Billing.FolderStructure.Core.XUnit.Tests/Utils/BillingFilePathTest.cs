using AutoMapper;
using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Services;
using Billing.FolderStructure.Core.Utils;
using BillingService.XUnit.Tests.Common.Mocks;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using System.Text;

namespace Billing.FolderStructure.Core.XUnit.Tests.Utils
{
    public class BillingFilePathTest
    {
        private readonly Mock<IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity>> _claimSubmissionFunderSequenceMock;
        private readonly Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>> _claimSubmissionEntityMock;
        private readonly Mock<IRepository<BillingDbContext, ClaimEdiFilesEntity>> _claimEdiFilesEntityMock;
        private readonly Mock<IBillingBlobService> _mockBillingBlobService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<BillingFilePath>> _mockLogger;
        private readonly BillingFilePath _billingFilePath;

        public BillingFilePathTest()
        {
            _claimSubmissionFunderSequenceMock = new Mock<IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity>>();
            _claimSubmissionEntityMock = new Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>>();
            _claimEdiFilesEntityMock = new Mock<IRepository<BillingDbContext, ClaimEdiFilesEntity>>();
            _mockBillingBlobService = new Mock<IBillingBlobService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<BillingFilePath>>();
            _billingFilePath = new BillingFilePath(_claimSubmissionFunderSequenceMock.Object, _claimSubmissionEntityMock.Object, _claimEdiFilesEntityMock.Object, _mockBillingBlobService.Object, _mockMapper.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task CreateFolderPath_ValidRequest_WithEDIData_ShouldReturnFormattedPath()
        {
            // Arrange
            var ediData = Encoding.UTF8.GetBytes("ISA*00*       *00*          *ZZ*SENDER         *ZZ*RECEIVER       *210101*1200*U*00801*000000001*0*P*:~ST*837*0001~");
            var billingRequest = new BillingRequest
            {
                FieldIdentifier = "837/837/claim.txt",
                BillingContainerName = "billing-container",
                FolderName = "claims",
                SubFolderName = "pending",
                AccountInfoId = 12345,
                Data = ediData
            };

            // Act
            var result = await _billingFilePath.CreateFolderPath(billingRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("billing-container", result);
            Assert.Contains("837", result);
            Assert.Contains("12345", result);
            Assert.Contains("claims", result);
            Assert.Contains("pending", result);
            Assert.Contains("claim.txt", result);
            Assert.False(result.StartsWith("/"));
            Assert.False(result.StartsWith("\\"));
        }

        [Fact]
        public async Task CreateFolderPath_NoEDIData_ShouldUseFieldIdentifierFileType()
        {
            // Arrange
            var billingRequest = new BillingRequest
            {
                FieldIdentifier = "835/835/era.txt",
                BillingContainerName = "billing-container",
                FolderName = "era",
                SubFolderName = "processed",
                AccountInfoId = 54321,
                Data = null
            };

            // Act
            var result = await _billingFilePath.CreateFolderPath(billingRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("835", result);
            Assert.Contains("54321", result);
            Assert.Contains("era.txt", result);
        }

        [Fact]
        public async Task CreateFolderPath_ZeroAccountInfoId_ShouldUsePrefixGlobalError()
        {
            // Arrange
            var ediData = Encoding.UTF8.GetBytes("ISA*00*       *00*   *ZZ*SENDER         *ZZ*RECEIVER    *210101*1200*U*00801*000000001*0*P*:~ST*999*0001~");
            var billingRequest = new BillingRequest
            {
                FieldIdentifier = "999/999/ack.txt",
                BillingContainerName = "billing-container",
                FolderName = "acknowledgments",
                SubFolderName = null,
                AccountInfoId = 0,
                Data = ediData
            };

            // Act
            var result = await _billingFilePath.CreateFolderPath(billingRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Global Error", result);
        }

        [Fact]
        public async Task CreateFolderPath_NullAccountInfoId_ShouldUsePrefixGlobalError()
        {
            // Arrange
            var billingRequest = new BillingRequest
            {
                FieldIdentifier = "277/277/status.txt",
                BillingContainerName = "billing-container",
                FolderName = "status",
                SubFolderName = null,
                AccountInfoId = null,
                Data = null
            };

            // Act
            var result = await _billingFilePath.CreateFolderPath(billingRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Global Error", result);
        }

        [Fact]
        public async Task CreateFolderPath_EmptySubFolderName_ShouldIncludeEmptyFolder()
        {
            // Arrange
            var billingRequest = new BillingRequest
            {
                FieldIdentifier = "837/837/claim.txt",
                BillingContainerName = "billing-container",
                FolderName = "claims",
                SubFolderName = "",
                AccountInfoId = 12345,
                Data = null
            };

            // Act
            var result = await _billingFilePath.CreateFolderPath(billingRequest);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.StartsWith("/"));
        }


        [Fact]
        public async Task SplitFilePath_ValidPath_ShouldSplitOnFirstSlash()
        {
            // Arrange
            var filePath = "container/folder/subfolder/file.txt";

            // Act
            var (containerName, fullPath) = await _billingFilePath.SplitFilePath(filePath);

            // Assert
            Assert.Equal("container", containerName);
            Assert.Equal("folder/subfolder/file.txt", fullPath);
        }

        [Fact]
        public async Task SplitFilePath_NoSlash_ShouldReturnEntireStringAsContainer()
        {
            // Arrange
            var filePath = "container";

            // Act
            var (containerName, fullPath) = await _billingFilePath.SplitFilePath(filePath);

            // Assert
            Assert.Equal("container", containerName);
            Assert.Equal(string.Empty, fullPath);
        }

        [Fact]
        public async Task SplitFilePath_SingleSlash_ShouldSplitCorrectly()
        {
            // Arrange
            var filePath = "container/file.txt";

            // Act
            var (containerName, fullPath) = await _billingFilePath.SplitFilePath(filePath);

            // Assert
            Assert.Equal("container", containerName);
            Assert.Equal("file.txt", fullPath);
        }

        [Fact]
        public async Task SplitFilePath_EmptyString_ShouldReturnEmptyContainerAndPath()
        {
            // Arrange
            var filePath = string.Empty;

            // Act
            var (containerName, fullPath) = await _billingFilePath.SplitFilePath(filePath);

            // Assert
            Assert.Equal(string.Empty, containerName);
            Assert.Equal(string.Empty, fullPath);
        }

        [Fact]
        public async Task SplitFilePath_LeadingSlash_ShouldHandleCorrectly()
        {
            // Arrange
            var filePath = "/container/file.txt";

            // Act
            var (containerName, fullPath) = await _billingFilePath.SplitFilePath(filePath);

            // Assert
            Assert.Equal("", containerName);
            Assert.Equal("container/file.txt", fullPath);
        }


        [Fact]
        public async Task GetTransactionControlNumber_Type837_ShouldExtractClaimIdentifiers()
        {
            // Arrange
            var ediData = @"ISA*00*          *00*          *ZZ*SENDER    *ZZ*RECEIVER       *210101*1200*U*00801*000000001*0*P*:~
                            GS*HC*APPSENDER*RECEIVER*20210101*1200*1*X*004010X098A1~
                            ST*837*0001*005010X222~
                            BHT*0019*00*244*20210101*1200*CH~
                            CLM*CLAIM001*500*~
                            CLM*CLAIM002*600*~
                            SE*10*0001~
                            GE*1*1~
                            IEA*1*000000001~";
            var ediDataWithoutLineBreaks =
                string.Concat(ediData.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));


            // Act
            var result = await _billingFilePath.GetTransactionControlNumber(ediDataWithoutLineBreaks);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("837", result.FileType);
            Assert.NotNull(result.ClaimIdentifiers);
            Assert.Contains("CLAIM001", result.ClaimIdentifiers);
            Assert.Contains("CLAIM002", result.ClaimIdentifiers);
        }

        [Fact]
        public async Task GetTransactionControlNumber_Type835_ShouldExtractControlNumbersAndNPI()
        {
            // Arrange
            var ediData = @"ISA*00*          *00*       *ZZ*SENDER         *ZZ*RECEIVER       *210101*1200*U*00801*000000001*0*P*:~
                            GS*HP*APPSENDER*RECEIVER*20210101*1200*1*X*004010~
                            ST*835*0001~
                            BPR*I*5000*C*ACH*004*100000001*121201*XX*999999999*123456789*999*WEB~
                            CLP*CLAIM001*1*500*500*0*15~
                            CLP*CLAIM002*1*600*600*0*15~
                            N1*1L*Test Provider*XX*1234567890~
                            REF*TJ*555555555~
                            SE*10*0001~
                            GE*1*1~
                            IEA*1*000000001~";
            var ediDataWithoutLineBreaks =
                string.Concat(ediData.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

            // Act
            var result = await _billingFilePath.GetTransactionControlNumber(ediDataWithoutLineBreaks);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("835", result.FileType);
            Assert.NotNull(result.ClaimIdentifiers);
            Assert.Contains("CLAIM001", result.ClaimIdentifiers);
            Assert.Contains("CLAIM002", result.ClaimIdentifiers);
            Assert.Equal("1234567890", result.NpiNumber);
            Assert.Equal("555555555", result.FederalTaxId);
        }


        [Fact]
        public async Task GetTransactionControlNumber_Type277_ShouldExtractClaimIdentifiersFromHL()
        {
            // Arrange
            var ediData = @"ISA*00*          *00*          *ZZ*SENDER*ZZ*RECEIVER *210101*1200*U*00801*000000001*0*P*:~
                            GS*HB*APPSENDER*RECEIVER*20210101*1200*1*X*004010~
                            ST*277*0001~
                            BHT*0019*00*244*20210101*1200*CH~
                            HL*1**20*1~
                            HL*2*1*21*0~
                            TRN*1*CLAIM001*APP~
                            HL*3*2*22*0~
                            SE*10*0001~
                            GE*1*1~
                            IEA*1*000000001~";
            var ediDataWithoutLineBreaks =
                 string.Concat(ediData.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            // Act
            var result = await _billingFilePath.GetTransactionControlNumber(ediDataWithoutLineBreaks);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("277", result.FileType);
            Assert.NotNull(result.ClaimIdentifiers);
            Assert.Contains("CLAIM001", result.ClaimIdentifiers);
        }

        [Fact]
        public async Task GetTransactionControlNumber_EmptyData_ShouldReturnUnknown()
        {
            // Arrange
            var ediData = string.Empty;

            // Act
            var result = await _billingFilePath.GetTransactionControlNumber(ediData);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Unknown", result.FileType);
        }

        [Fact]
        public async Task GetTransactionControlNumber_InvalidFormat_ShouldReturnUnknown()
        {
            // Arrange
            var ediData = "InvalidEDIData";

            // Act
            var result = await _billingFilePath.GetTransactionControlNumber(ediData);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Unknown", result.FileType);
        }

        [Fact]
        public async Task GetTransactionControlNumber_MultipleInterchanges_ShouldProcessAll()
        {
            // Arrange
            var ediData = @"ISA*00*    *00*          *ZZ*SENDER  *ZZ*RECEIVER     *210101*1200*U*00801*000000001*0*P*:~
                            GS*HC*APPSENDER*RECEIVER*20210101*1200*1*X*004010X098A1~
                            ST*837*0001*005010X222~
                            CLM*CLAIM001*500*~
                            SE*10*0001~
                            GE*1*1~
                            IEA*1*000000001~ISA*00*    *00**ZZ*SENDER     *ZZ*RECEIVER       *210101*1300*U*00801*000000002*0*P*:~
                            GS*HC*APPSENDER*RECEIVER*20210101*1300*1*X*004010X098A1~
                            ST*837*0002*005010X222~
                            CLM*CLAIM002*600*~
                            SE*10*0002~
                            GE*1*1~
                            IEA*1*000000002~";
            var ediDataWithoutLineBreaks =
                 string.Concat(ediData.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

            // Act
            var result = await _billingFilePath.GetTransactionControlNumber(ediDataWithoutLineBreaks);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("837", result.FileType);
            Assert.True(result.ClaimIdentifiers.Length >= 1);
        }

        [Fact]
        public async Task GetTransactionControlNumber_Type999_ShouldExtractControlNumber()
        {
            // Arrange
            var ediData = @"ISA*00*          *00*          *ZZ*SENDER*ZZ*RECEIVER*210101*1200*U*00801*000000001*0*P*:~
                    GS*FA*SENDER*RECEIVER*20210101*1200*1*X*005010~
                    ST*999*0001~
                    AK2*837*12345~
                    SE*5*0001~
                    GE*1*1~
                    IEA*1*000000001~";

            var ediDataWithoutLineBreaks =
                string.Concat(ediData.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

            // Act
            var result = await _billingFilePath.GetTransactionControlNumber(ediDataWithoutLineBreaks);

            // Assert
            Assert.Equal("999", result.FileType);
            Assert.Contains(12345, result.ControlNumbers);
        }

        [Fact]
        public async Task GetTransactionControlNumber_Type999_InvalidAK2_ShouldAddNullControlNumber()
        {
            // Arrange
            var ediData = @"ISA*00*          *00*          *ZZ*SENDER*ZZ*RECEIVER*210101*1200*U*00801*000000001*0*P*:~
                    GS*FA*SENDER*RECEIVER*20210101*1200*1*X*005010~
                    ST*999*0001~
                    AK2*837*INVALID~
                    SE*5*0001~
                    GE*1*1~
                    IEA*1*000000001~";

            var ediDataWithoutLineBreaks =
                string.Concat(ediData.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

            // Act
            var result = await _billingFilePath.GetTransactionControlNumber(ediDataWithoutLineBreaks);

            // Assert
            Assert.Equal("999", result.FileType);
            Assert.Contains(result.ControlNumbers, x => x == null);
        }

        [Fact]
        public async Task GetTransactionControlNumber_InvalidSTControlNumber_ShouldAddNull()
        {
            // Arrange
            var ediData = @"ISA*00*          *00*          *ZZ*SENDER*ZZ*RECEIVER*210101*1200*U*00801*000000001*0*P*:~
                    GS*HC*SENDER*RECEIVER*20210101*1200*1*X*004010~
                    ST*837*INVALID~
                    CLM*CLAIM001*500~
                    SE*5*INVALID~
                    GE*1*1~
                    IEA*1*000000001~";

            var ediDataWithoutLineBreaks =
                string.Concat(ediData.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

            // Act
            var result = await _billingFilePath.GetTransactionControlNumber(ediDataWithoutLineBreaks);

            // Assert
            Assert.Contains(result.ControlNumbers, x => x == null);
        }

        [Fact]
        public async Task GetTransactionControlNumber_UnknownTransactionType_ShouldAddControlNumber()
        {
            // Arrange
            var ediData = @"ISA*00*          *00*          *ZZ*SENDER*ZZ*RECEIVER*210101*1200*U*00801*000000001*0*P*:~
                    GS*XX*SENDER*RECEIVER*20210101*1200*1*X*004010~
                    ST*123*9999~
                    SE*5*9999~
                    GE*1*1~
                    IEA*1*000000001~";

            var ediDataWithoutLineBreaks =
                string.Concat(ediData.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

            // Act
            var result = await _billingFilePath.GetTransactionControlNumber(ediDataWithoutLineBreaks);

            // Assert
            Assert.Equal("123", result.FileType);
            Assert.Contains(9999, result.ControlNumbers);
        }

        [Fact]
        public async Task GetTransactionControlNumber_UnknownTransactionType_InvalidControlNumber_ShouldAddNull()
        {
            var ediData = @"ISA*00*          *00*          *ZZ*SENDER*ZZ*RECEIVER*210101*1200*U*00801*000000001*0*P*:~
                    GS*XX*SENDER*RECEIVER*20210101*1200*1*X*004010~
                    ST*123*INVALID~
                    SE*5*INVALID~
                    GE*1*1~
                    IEA*1*000000001~";

            var ediDataWithoutLineBreaks =
                string.Concat(ediData.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

            var result = await _billingFilePath.GetTransactionControlNumber(ediDataWithoutLineBreaks);

            Assert.Contains(result.ControlNumbers, x => x == null);
        }

        [Fact]
        public async Task GetTransactionControlNumber_NoSTSegment_ShouldSkip()
        {
            var ediData = @"ISA*00*          *00*          *ZZ*SENDER*ZZ*RECEIVER~
                    GS*HC*SENDER*RECEIVER~
                    SE*5*0001~
                    GE*1*1~
                    IEA*1*000000001~";

            var ediDataWithoutLineBreaks =
                string.Concat(ediData.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

            var result = await _billingFilePath.GetTransactionControlNumber(ediDataWithoutLineBreaks);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetTransactionControlNumber_STSegmentWithInsufficientFields_ShouldSkip()
        {
            var ediData = @"ISA*00*          *00*          *ZZ*SENDER*ZZ*RECEIVER~
                    ST*837~
                    SE*5*0001~
                    IEA*1*000000001~";

            var ediDataWithoutLineBreaks =
                string.Concat(ediData.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

            var result = await _billingFilePath.GetTransactionControlNumber(ediDataWithoutLineBreaks);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetTransactionControlNumber_N1WithoutXX_ShouldNotExtractNPI()
        {
            var ediData = @"ISA*00*          *00*          *ZZ*SENDER*ZZ*RECEIVER~
                    ST*835*0001~
                    CLP*CLAIM001*1*500~
                    N1*1L*Provider*FI*123456789~
                    SE*5*0001~
                    IEA*1*000000001~";

            var ediDataWithoutLineBreaks =
                string.Concat(ediData.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

            var result = await _billingFilePath.GetTransactionControlNumber(ediDataWithoutLineBreaks);

            Assert.Null(result.NpiNumber);
        }

        [Fact]
        public async Task GetTransactionControlNumber_REFWithoutTJ_ShouldNotExtractTaxId()
        {
            var ediData = @"ISA*00*          *00*          *ZZ*SENDER*ZZ*RECEIVER~
                    ST*835*0001~
                    CLP*CLAIM001*1*500~
                    REF*AB*123456789~
                    SE*5*0001~
                    IEA*1*000000001~";

            var ediDataWithoutLineBreaks =
                string.Concat(ediData.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

            var result = await _billingFilePath.GetTransactionControlNumber(ediDataWithoutLineBreaks);

            Assert.Null(result.FederalTaxId);
        }

        [Fact]
        public async Task FetchClaimSubmissionDataForERA_Type999_WithControlNumbers_ShouldFilterByControlNumber()
        {
            // Arrange
            var claimSubmissions = new List<ClaimSubmissionEntity>
            {
                new ClaimSubmissionEntity { Id = 1, ClaimSubmissionIdentifier = "CLAIM001", Claim = new ClaimEntity { AccountInfoId = 123 } },
                new ClaimSubmissionEntity { Id = 2, ClaimSubmissionIdentifier = "CLAIM002", Claim = new ClaimEntity { AccountInfoId = 123 } },
                new ClaimSubmissionEntity { Id = 3, ClaimSubmissionIdentifier = "CLAIM003", Claim = new ClaimEntity { AccountInfoId = 123 } }
            };

            var mockQueryable = claimSubmissions.AsQueryable();
            var mockDbSet = DbMock.Create(claimSubmissions);

            _claimSubmissionEntityMock
               .Setup(r => r.Query())
               .Returns(mockQueryable);

            var model = new TransactionControlNumberModel
            {
                FileType = "999",
                ControlNumbers = new int?[] { 1, 2 },
                ClaimIdentifiers = Array.Empty<string>()
            };

            // Act
            var result = await _billingFilePath.FetchClaimSubmissionDataForERA(model);

            // Assert - The method should query by control numbers
            _claimSubmissionEntityMock.Verify(r => r.Query(), Times.Once);
        }

        [Fact]
        public async Task FetchClaimSubmissionDataForERA_Type835_WithTaxIdAndNPI_ShouldFilterByTaxIdAndNPI()
        {
            // Arrange
            var claimSubmissions = new List<ClaimSubmissionEntity>
            {
                new ClaimSubmissionEntity
                {
                    Id = 1,
                    ClaimSubmissionIdentifier = "CLAIM001",
                    AccountFederalTaxId = "555555555",
                    AccountNpiNumber = "1234567890",
                    Claim = new ClaimEntity { AccountInfoId = 123 }
                },
                new ClaimSubmissionEntity
                {
                    Id = 2,
                    ClaimSubmissionIdentifier = "CLAIM002",
                    AccountFederalTaxId = "666666666",
                    AccountNpiNumber = "9876543210",
                    Claim = new ClaimEntity { AccountInfoId = 123 }
                }
            };

            var mockQueryable = claimSubmissions.AsQueryable();

            _claimSubmissionEntityMock
               .Setup(r => r.Query())
               .Returns(mockQueryable);


            var model = new TransactionControlNumberModel
            {
                FileType = "835",
                FederalTaxId = "555555555",
                NpiNumber = "1234567890",
                ControlNumbers = new int?[] { 1 },
                ClaimIdentifiers = new string[] { "CLAIM001" }
            };

            // Act
            var result = await _billingFilePath.FetchClaimSubmissionDataForERA(model);

            // Assert
            _claimSubmissionEntityMock.Verify(r => r.Query(), Times.Once);
        }

        [Fact]
        public async Task FetchClaimSubmissionDataForERA_WithClaimIdentifiers_ShouldFilterByIdentifiers()
        {
            // Arrange
            var claimSubmissions = new List<ClaimSubmissionEntity>
            {
                new ClaimSubmissionEntity
                {
                    Id = 1,
                    ClaimSubmissionIdentifier = "CLAIM001",
                    Claim = new ClaimEntity { AccountInfoId = 123 }
                },
                new ClaimSubmissionEntity
                {
                    Id = 2,
                    ClaimSubmissionIdentifier = "CLAIM002",
                    Claim = new ClaimEntity { AccountInfoId = 123 }
                }
            };

            var mockQueryable = claimSubmissions.AsQueryable();

            _claimSubmissionEntityMock
               .Setup(r => r.Query())
               .Returns(mockQueryable);

            var model = new TransactionControlNumberModel
            {
                FileType = "837",
                ControlNumbers = Array.Empty<int?>(),
                ClaimIdentifiers = new string[] { "CLAIM001", "CLAIM002" }
            };

            // Act
            var result = await _billingFilePath.FetchClaimSubmissionDataForERA(model);

            // Assert
            _claimSubmissionEntityMock.Verify(r => r.Query(), Times.Once);
        }

        [Fact]
        public async Task FetchClaimSubmissionDataForERA_NoMatch_ShouldReturnNull()
        {
            // Arrange
            var claimSubmissions = new List<ClaimSubmissionEntity>();

            var mockQueryable = claimSubmissions.AsQueryable();

            _claimSubmissionEntityMock
               .Setup(r => r.Query())
               .Returns(mockQueryable);

            var model = new TransactionControlNumberModel
            {
                FileType = "837",
                ControlNumbers = Array.Empty<int?>(),
                ClaimIdentifiers = new string[] { "NONEXISTENT" }
            };

            // Act
            var result = await _billingFilePath.FetchClaimSubmissionDataForERA(model);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task FetchClaimSubmissionDataForManualERA_NullClaimIdentifiers_ShouldHandleGracefully()
        {
            // Arrange
            var claimSubmissions = new List<ClaimSubmissionEntity>
            {
                new ClaimSubmissionEntity
                {
                    Id = 10,
                    ClaimSubmissionIdentifier = "CLAIM100",
                    AccountFederalTaxId = "111",
                    AccountNpiNumber = "222",
                    Claim = new ClaimEntity()
                }
            };

            _claimSubmissionEntityMock
                .Setup(x => x.Query())
                .Returns(claimSubmissions.AsQueryable());

            var model = new TransactionControlNumberModel
            {
                FileType = "835",
                ClaimIdentifiers = null,
                FederalTaxId = "111",
                NpiNumber = "222"
            };

            // Act
            var result = await _billingFilePath.FetchClaimSubmissionDataForManualERA(model, 1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task FetchClaimSubmissionDataForManualERA_ClaimIdentifiers_WithWhitespace_ShouldTrimAndMatch()
        {
            // Arrange
            var claimSubmissions = new List<ClaimSubmissionEntity>
            {
                new ClaimSubmissionEntity
                {
                    Id = 20,
                    ClaimSubmissionIdentifier = "CLAIM200",
                    AccountFederalTaxId = "FTID",
                    AccountNpiNumber = "NPI123",
                    Claim = new ClaimEntity()
                }
            };

            var mockDbSet = DbMock.Create(claimSubmissions);

            _claimSubmissionEntityMock
                .Setup(x => x.Query())
                .Returns(mockDbSet);

            var model = new TransactionControlNumberModel
            {
                ClaimIdentifiers = new string[] { "  CLAIM200  " },
                FederalTaxId = "FTID",
                NpiNumber = "NPI123"
            };

            // Act
            var result = await _billingFilePath.FetchClaimSubmissionDataForManualERA(model, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("CLAIM200", result.ClaimSubmissionIdentifier);
        }

        [Fact]
        public async Task FetchClaimSubmissionDataForManualERA_ShouldFilterByFederalTaxId()
        {
            // Arrange
            var claimSubmissions = new List<ClaimSubmissionEntity>
            {
                new ClaimSubmissionEntity
                {
                    Id = 30,
                    ClaimSubmissionIdentifier = "CLAIM300",
                    AccountFederalTaxId = "MATCH",
                    AccountNpiNumber = "NO_MATCH",
                    Claim = new ClaimEntity()
                }
            };

            var mockDbSet = DbMock.Create(claimSubmissions);

            _claimSubmissionEntityMock
                .Setup(x => x.Query())
                .Returns(mockDbSet);

            var model = new TransactionControlNumberModel
            {
                ClaimIdentifiers = new string[] { "CLAIM300" },
                FederalTaxId = "MATCH"
            };

            // Act
            var result = await _billingFilePath.FetchClaimSubmissionDataForManualERA(model, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("CLAIM300", result.ClaimSubmissionIdentifier);
        }


        [Fact]
        public async Task FetchClaimSubmissionDataForManualERA_ShouldFilterByNpi()
        {
            // Arrange
            var claimSubmissions = new List<ClaimSubmissionEntity>
               {
                   new ClaimSubmissionEntity
                   {
                       Id = 40,
                       ClaimSubmissionIdentifier = "CLAIM400",
                       AccountFederalTaxId = "OTHER",
                       AccountNpiNumber = "MATCH_NPI",
                       Claim = new ClaimEntity()
                   }
               };

            var mockDbSet = DbMock.Create(claimSubmissions);

            _claimSubmissionEntityMock
                .Setup(x => x.Query())
                .Returns(mockDbSet);

            var model = new TransactionControlNumberModel
            {
                ClaimIdentifiers = new string[] { "CLAIM400" },
                NpiNumber = "MATCH_NPI"
            };

            // Act
            var result = await _billingFilePath.FetchClaimSubmissionDataForManualERA(model, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("CLAIM400", result.ClaimSubmissionIdentifier);
        }

        [Fact]
        public async Task FetchClaimSubmissionDataForManualERA_ShouldExecuteWhereClauseFully()
        {
            // Arrange
            var claimSubmissions = new List<ClaimSubmissionEntity>
            {
                new ClaimSubmissionEntity
                {
                    Id = 50,
                    ClaimSubmissionIdentifier = "CLAIM500",
                    AccountFederalTaxId = "FT500",
                    AccountNpiNumber = "NPI500",
                    Claim = new ClaimEntity()
                },
                new ClaimSubmissionEntity
                {
                    Id = 51,
                    ClaimSubmissionIdentifier = "CLAIM501",
                    AccountFederalTaxId = "OTHER",
                    AccountNpiNumber = "OTHER",
                    Claim = new ClaimEntity()
                }
            };

            var mockDbSet = DbMock.Create(claimSubmissions);

            _claimSubmissionEntityMock
                .Setup(x => x.Query())
                .Returns(mockDbSet);

            var model = new TransactionControlNumberModel
            {
                ClaimIdentifiers = new string[] { "CLAIM500" },
                FederalTaxId = "FT500",
                NpiNumber = "NPI500"
            };

            // Act
            var result = await _billingFilePath.FetchClaimSubmissionDataForManualERA(model, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("CLAIM500", result.ClaimSubmissionIdentifier);
            Assert.Equal(50, result.Id);
        }


        [Fact]
        public async Task FetchClaimSubmissionDataForManualERA_ShouldReturnLatestRecord()
        {
            // Arrange
            var claimSubmissions = new List<ClaimSubmissionEntity>
            {
                new ClaimSubmissionEntity
                {
                    Id = 60,
                    ClaimSubmissionIdentifier = "CLAIM600",
                    Claim = new ClaimEntity()
                },
                new ClaimSubmissionEntity
                {
                    Id = 61,
                    ClaimSubmissionIdentifier = "CLAIM600",
                    Claim = new ClaimEntity()
                }
            };

            var mockDbSet = DbMock.Create(claimSubmissions);

            _claimSubmissionEntityMock
                .Setup(x => x.Query())
                .Returns(mockDbSet);

            var model = new TransactionControlNumberModel
            {
                ClaimIdentifiers = new string[] { "CLAIM600" }
            };

            // Act
            var result = await _billingFilePath.FetchClaimSubmissionDataForManualERA(model, 1);

            // Assert
            Assert.NotNull(result);

            // Latest Id should be returned
            Assert.Equal(61, result.Id);
            Assert.Equal("CLAIM600", result.ClaimSubmissionIdentifier);
        }


        [Fact]
        public async Task FetchClaimSubmissionDataForManualERA_NoIdentifiersAndNoControlNumbers_ShouldReturnNull()
        {
            var model = new TransactionControlNumberModel
            {
                ClaimIdentifiers = new string[] { "   " },
                ControlNumbers = null
            };

            var result = await _billingFilePath.FetchClaimSubmissionDataForManualERA(model, 1);

            Assert.Null(result);
        }


        [Fact]
        public async Task FetchClaimSubmissionDataForManualERA_NoClaimIdentifiersOrControlNumbers_ShouldReturnNull()
        {
            // Arrange
            var claimSubmissions = new List<ClaimSubmissionEntity>();

            var mockQueryable = claimSubmissions.AsQueryable();

            _claimSubmissionEntityMock
               .Setup(r => r.Query())
               .Returns(mockQueryable);

            var model = new TransactionControlNumberModel
            {
                FileType = "837",
                ControlNumbers = Array.Empty<int?>(),
                ClaimIdentifiers = Array.Empty<string>(),
                FederalTaxId = null,
                NpiNumber = null
            };

            // Act
            var result = await _billingFilePath.FetchClaimSubmissionDataForManualERA(model, 123);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateFolderPath_PathStartsWithSlash_ShouldNormalizeCorrectly()
        {
            // Arrange
            var billingRequest = new BillingRequest
            {
                FieldIdentifier = "/837/837/claim.txt",
                BillingContainerName = "container",
                FolderName = "folder",
                SubFolderName = "subfolder",
                AccountInfoId = 123,
                Data = null
            };

            // Act
            var result = await _billingFilePath.CreateFolderPath(billingRequest);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.StartsWith("/"));
        }

        [Fact]
        public async Task CreateFolderPath_Type837EDI_ShouldExtractCorrectFileType()
        {
            // Arrange
            var ediData = Encoding.UTF8.GetBytes("ISA~ST*837*0001~");
            var billingRequest = new BillingRequest
            {
                FieldIdentifier = "999/unknown/claim.txt",
                BillingContainerName = "container",
                FolderName = "folder",
                SubFolderName = null,
                AccountInfoId = 123,
                Data = ediData
            };

            // Act
            var result = await _billingFilePath.CreateFolderPath(billingRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("837", result); // Should use extracted type, not field identifier
        }

        [Fact]
        public async Task CreateFolderPath_Type835EDI_ShouldExtractCorrectFileType()
        {
            // Arrange
            var ediData = Encoding.UTF8.GetBytes("ISA~ST*835*0001~");
            var billingRequest = new BillingRequest
            {
                FieldIdentifier = "999/unknown/era.txt",
                BillingContainerName = "container",
                FolderName = "folder",
                SubFolderName = null,
                AccountInfoId = 123,
                Data = ediData
            };

            // Act
            var result = await _billingFilePath.CreateFolderPath(billingRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("835", result);
        }

        [Fact]
        public async Task AddOrUpdateBlobFilePath_ShouldAddNewRecord_WhenNoExistingEntity()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 456,
                ClaimId = 789,
                PaymentId = 101112,
                FileType = "837",
                BlobFilePath = "path/to/edi/file"
            };

            var emptyList = new List<ClaimEdiFilesEntity>();
            var mockDbSet = DbMock.Create(emptyList);

            _claimEdiFilesEntityMock.Setup(repo => repo.Query())
                                     .Returns(mockDbSet);

            _mockMapper.Setup(m => m.Map<ClaimEdiFilesEntity>(It.IsAny<ClaimEdiFilesModel>()))
                       .Returns(new ClaimEdiFilesEntity
                       {
                           AccountInfoId = model.AccountInfoId,
                           ClaimSubmissionId = model.ClaimSubmissionId,
                           ClaimId = model.ClaimId,
                           PaymentId = model.PaymentId,
                           FileType = model.FileType,
                           BlobFilePath = model.BlobFilePath
                       });

            // Act
            await _billingFilePath.AddOrUpdateBlobFilePath(model);

            // Assert
            _claimEdiFilesEntityMock.Verify(repo => repo.AddAsync(It.IsAny<ClaimEdiFilesEntity>()), Times.Once);
            _claimEdiFilesEntityMock.Verify(repo => repo.Update(It.IsAny<ClaimEdiFilesEntity>()), Times.Never);
            _claimEdiFilesEntityMock.Verify(repo => repo.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task AddOrUpdateBlobFilePath_ShouldUpdateExistingRecord_WhenMatchingEntityExists()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 456,
                ClaimId = 789,
                PaymentId = 101112,
                FileType = "837",
                BlobFilePath = "new/path/to/edi/file",
                MemberId = 42
            };

            var existingEntity = new ClaimEdiFilesEntity
            {
                Id = 1,
                AccountInfoId = 123,
                ClaimSubmissionId = 456,
                ClaimId = 789,
                PaymentId = 101112,
                FileType = "837",
                BlobFilePath = "old/path/to/edi/file"
            };

            var entityList = new List<ClaimEdiFilesEntity> { existingEntity };
            var mockDbSet = DbMock.Create(entityList);

            _claimEdiFilesEntityMock.Setup(repo => repo.Query())
                                     .Returns(mockDbSet);

            _mockMapper.Setup(m => m.Map(It.IsAny<ClaimEdiFilesModel>(), It.IsAny<ClaimEdiFilesEntity>()))
                       .Returns(existingEntity);

            // Act
            await _billingFilePath.AddOrUpdateBlobFilePath(model);

            // Assert
            _claimEdiFilesEntityMock.Verify(repo => repo.Update(It.IsAny<ClaimEdiFilesEntity>()), Times.Once);
            _claimEdiFilesEntityMock.Verify(repo => repo.AddAsync(It.IsAny<ClaimEdiFilesEntity>()), Times.Never);
            _claimEdiFilesEntityMock.Verify(repo => repo.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task AddOrUpdateBlobFilePath_ShouldAddNewRecord_WhenExistingEntityHasDifferentFileType()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 456,
                ClaimId = 789,
                PaymentId = 101112,
                FileType = "835",
                BlobFilePath = "path/to/835/file"
            };

            var existingEntity = new ClaimEdiFilesEntity
            {
                Id = 1,
                AccountInfoId = 123,
                ClaimSubmissionId = 456,
                ClaimId = 789,
                PaymentId = 101112,
                FileType = "837",
                BlobFilePath = "path/to/837/file"
            };

            var entityList = new List<ClaimEdiFilesEntity> { existingEntity };
            var mockDbSet = DbMock.Create(entityList);

            _claimEdiFilesEntityMock.Setup(repo => repo.Query())
                                     .Returns(mockDbSet);

            _mockMapper.Setup(m => m.Map<ClaimEdiFilesEntity>(It.IsAny<ClaimEdiFilesModel>()))
                       .Returns(new ClaimEdiFilesEntity());

            // Act
            await _billingFilePath.AddOrUpdateBlobFilePath(model);

            // Assert - FileType mismatch means it should add, not update
            _claimEdiFilesEntityMock.Verify(repo => repo.AddAsync(It.IsAny<ClaimEdiFilesEntity>()), Times.Once);
            _claimEdiFilesEntityMock.Verify(repo => repo.Update(It.IsAny<ClaimEdiFilesEntity>()), Times.Never);
            _claimEdiFilesEntityMock.Verify(repo => repo.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task AddOrUpdateBlobFilePath_ShouldAddNewRecord_WhenExistingEntityHasDifferentClaimId()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 456,
                ClaimId = 999,
                PaymentId = 0,
                FileType = "837",
                BlobFilePath = "path/to/new/claim"
            };

            var existingEntity = new ClaimEdiFilesEntity
            {
                Id = 1,
                AccountInfoId = 123,
                ClaimSubmissionId = 456,
                ClaimId = 789,
                PaymentId = 101112,
                FileType = "837",
                BlobFilePath = "path/to/old/claim"
            };

            var entityList = new List<ClaimEdiFilesEntity> { existingEntity };
            var mockDbSet = DbMock.Create(entityList);

            _claimEdiFilesEntityMock.Setup(repo => repo.Query())
                                     .Returns(mockDbSet);

            _mockMapper.Setup(m => m.Map<ClaimEdiFilesEntity>(It.IsAny<ClaimEdiFilesModel>()))
                       .Returns(new ClaimEdiFilesEntity());

            // Act
            await _billingFilePath.AddOrUpdateBlobFilePath(model);

            // Assert - ClaimId mismatch means it should add, not update
            _claimEdiFilesEntityMock.Verify(repo => repo.AddAsync(It.IsAny<ClaimEdiFilesEntity>()), Times.Once);
            _claimEdiFilesEntityMock.Verify(repo => repo.Update(It.IsAny<ClaimEdiFilesEntity>()), Times.Never);
            _claimEdiFilesEntityMock.Verify(repo => repo.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task AddOrUpdateBlobFilePath_ShouldAddNewRecord_WhenModelFileTypeIsNull()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 456,
                ClaimId = 789,
                PaymentId = 101112,
                FileType = null,
                BlobFilePath = "path/to/edi/file"
            };

            var existingEntity = new ClaimEdiFilesEntity
            {
                Id = 1,
                AccountInfoId = 123,
                ClaimSubmissionId = 456,
                ClaimId = 789,
                PaymentId = 101112,
                FileType = "837",
                BlobFilePath = "old/path"
            };

            var entityList = new List<ClaimEdiFilesEntity> { existingEntity };
            var mockDbSet = DbMock.Create(entityList);

            _claimEdiFilesEntityMock.Setup(repo => repo.Query())
                                     .Returns(mockDbSet);

            _mockMapper.Setup(m => m.Map<ClaimEdiFilesEntity>(It.IsAny<ClaimEdiFilesModel>()))
                       .Returns(new ClaimEdiFilesEntity());

            // Act
            await _billingFilePath.AddOrUpdateBlobFilePath(model);

            // Assert - model.FileType is null, so FileType?.Equals(...) returns null (not true) → goes to add branch
            _claimEdiFilesEntityMock.Verify(repo => repo.AddAsync(It.IsAny<ClaimEdiFilesEntity>()), Times.Once);
            _claimEdiFilesEntityMock.Verify(repo => repo.Update(It.IsAny<ClaimEdiFilesEntity>()), Times.Never);
            _claimEdiFilesEntityMock.Verify(repo => repo.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task AddOrUpdateBlobFilePath_ShouldSetAuditFields_WhenAddingNewRecord()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 100,
                ClaimSubmissionId = 0,
                ClaimId = 0,
                PaymentId = 0,
                FileType = "999",
                BlobFilePath = "path/to/999/file",
                MemberId = 55
            };

            ClaimEdiFilesEntity capturedEntity = null;

            var emptyList = new List<ClaimEdiFilesEntity>();
            var mockDbSet = DbMock.Create(emptyList);

            _claimEdiFilesEntityMock.Setup(repo => repo.Query())
                                     .Returns(mockDbSet);

            _mockMapper.Setup(m => m.Map<ClaimEdiFilesEntity>(It.IsAny<ClaimEdiFilesModel>()))
                       .Returns(new ClaimEdiFilesEntity
                       {
                           AccountInfoId = model.AccountInfoId,
                           FileType = model.FileType,
                           BlobFilePath = model.BlobFilePath
                       });

            _claimEdiFilesEntityMock.Setup(repo => repo.AddAsync(It.IsAny<ClaimEdiFilesEntity>()))
                                     .Callback<ClaimEdiFilesEntity>(e => capturedEntity = e);

            // Act
            await _billingFilePath.AddOrUpdateBlobFilePath(model);

            // Assert - MarkCreated should have set CreatedBy and DateCreated
            Assert.NotNull(capturedEntity);
            Assert.Equal(55, capturedEntity.CreatedBy);
            Assert.True(capturedEntity.DateCreated > DateTime.MinValue);
        }

        [Fact]
        public async Task AddOrUpdateBlobFilePath_ShouldSetAuditFields_WhenUpdatingExistingRecord()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 456,
                ClaimId = 789,
                PaymentId = 101112,
                FileType = "837",
                BlobFilePath = "updated/path",
                MemberId = 77
            };

            var existingEntity = new ClaimEdiFilesEntity
            {
                Id = 1,
                AccountInfoId = 123,
                ClaimSubmissionId = 456,
                ClaimId = 789,
                PaymentId = 101112,
                FileType = "837",
                BlobFilePath = "old/path",
                CreatedBy = 10,
                DateCreated = DateTime.UtcNow.AddDays(-1)
            };

            ClaimEdiFilesEntity capturedEntity = null;

            var entityList = new List<ClaimEdiFilesEntity> { existingEntity };
            var mockDbSet = DbMock.Create(entityList);

            _claimEdiFilesEntityMock.Setup(repo => repo.Query())
                                     .Returns(mockDbSet);

            _mockMapper.Setup(m => m.Map(It.IsAny<ClaimEdiFilesModel>(), It.IsAny<ClaimEdiFilesEntity>()))
                       .Returns(existingEntity);

            _claimEdiFilesEntityMock.Setup(repo => repo.Update(It.IsAny<ClaimEdiFilesEntity>()))
                                     .Callback<ClaimEdiFilesEntity>(e => capturedEntity = e);

            // Act
            await _billingFilePath.AddOrUpdateBlobFilePath(model);

            // Assert - MarkUpdated should have set ModifiedBy and DateLastModified
            Assert.NotNull(capturedEntity);
            Assert.Equal(77, capturedEntity.ModifiedBy);
            Assert.NotNull(capturedEntity.DateLastModified);
        }

        [Fact]
        public async Task AddOrUpdateBlobFilePath_ShouldUseDefaultMemberId_WhenMemberIdIsNull()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 0,
                ClaimId = 0,
                PaymentId = 0,
                FileType = "837",
                BlobFilePath = "path/to/file",
                MemberId = 0
            };

            ClaimEdiFilesEntity capturedEntity = null;

            var emptyList = new List<ClaimEdiFilesEntity>();
            var mockDbSet = DbMock.Create(emptyList);

            _claimEdiFilesEntityMock.Setup(repo => repo.Query())
                                     .Returns(mockDbSet);

            _mockMapper.Setup(m => m.Map<ClaimEdiFilesEntity>(It.IsAny<ClaimEdiFilesModel>()))
                       .Returns(new ClaimEdiFilesEntity());

            _claimEdiFilesEntityMock.Setup(repo => repo.AddAsync(It.IsAny<ClaimEdiFilesEntity>()))
                                     .Callback<ClaimEdiFilesEntity>(e => capturedEntity = e);

            // Act
            await _billingFilePath.AddOrUpdateBlobFilePath(model);

            // Assert - MemberId ?? 0 should default to 0
            Assert.NotNull(capturedEntity);
            Assert.Equal(0, capturedEntity.CreatedBy);
        }

        [Fact]
        public async Task AddOrUpdateBlobFilePath_ShouldSkipDeletedRecords_WhenQueryingExisting()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 456,
                ClaimId = 789,
                PaymentId = 101112,
                FileType = "837",
                BlobFilePath = "path/to/edi/file"
            };

            var deletedEntity = new ClaimEdiFilesEntity
            {
                Id = 1,
                AccountInfoId = 123,
                ClaimSubmissionId = 456,
                ClaimId = 789,
                PaymentId = 101112,
                FileType = "837",
                BlobFilePath = "old/path",
                DateDeleted = DateTime.UtcNow // Soft-deleted
            };

            var entityList = new List<ClaimEdiFilesEntity> { deletedEntity };
            var mockDbSet = DbMock.Create(entityList);

            _claimEdiFilesEntityMock.Setup(repo => repo.Query())
                                     .Returns(mockDbSet);

            _mockMapper.Setup(m => m.Map<ClaimEdiFilesEntity>(It.IsAny<ClaimEdiFilesModel>()))
                       .Returns(new ClaimEdiFilesEntity());

            // Act
            await _billingFilePath.AddOrUpdateBlobFilePath(model);

            // Assert - Deleted entity should be ignored, so a new record is added
            _claimEdiFilesEntityMock.Verify(repo => repo.AddAsync(It.IsAny<ClaimEdiFilesEntity>()), Times.Once);
            _claimEdiFilesEntityMock.Verify(repo => repo.Update(It.IsAny<ClaimEdiFilesEntity>()), Times.Never);
            _claimEdiFilesEntityMock.Verify(repo => repo.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task AddOrUpdateBlobFilePath_ShouldUpdateCaseInsensitiveFileType_WhenMatchExists()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 456,
                ClaimId = 789,
                PaymentId = 101112,
                FileType = "837", // lowercase-ish match
                BlobFilePath = "new/path"
            };

            var existingEntity = new ClaimEdiFilesEntity
            {
                Id = 1,
                AccountInfoId = 123,
                ClaimSubmissionId = 456,
                ClaimId = 789,
                PaymentId = 101112,
                FileType = "837", // same type
                BlobFilePath = "old/path"
            };

            var entityList = new List<ClaimEdiFilesEntity> { existingEntity };
            var mockDbSet = DbMock.Create(entityList);

            _claimEdiFilesEntityMock.Setup(repo => repo.Query())
                                     .Returns(mockDbSet);

            _mockMapper.Setup(m => m.Map(It.IsAny<ClaimEdiFilesModel>(), It.IsAny<ClaimEdiFilesEntity>()))
                       .Returns(existingEntity);

            // Act
            await _billingFilePath.AddOrUpdateBlobFilePath(model);

            // Assert - OrdinalIgnoreCase comparison should match
            _claimEdiFilesEntityMock.Verify(repo => repo.Update(It.IsAny<ClaimEdiFilesEntity>()), Times.Once);
            _claimEdiFilesEntityMock.Verify(repo => repo.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task AddOrUpdateBlobFilePath_ShouldCallMapperMap_WhenUpdatingExistingEntity()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 456,
                ClaimId = 789,
                PaymentId = 101112,
                FileType = "837",
                BlobFilePath = "updated/path"
            };

            var existingEntity = new ClaimEdiFilesEntity
            {
                Id = 1,
                AccountInfoId = 123,
                ClaimSubmissionId = 456,
                ClaimId = 789,
                PaymentId = 101112,
                FileType = "837",
                BlobFilePath = "old/path"
            };

            var entityList = new List<ClaimEdiFilesEntity> { existingEntity };
            var mockDbSet = DbMock.Create(entityList);

            _claimEdiFilesEntityMock.Setup(repo => repo.Query())
                                     .Returns(mockDbSet);

            _mockMapper.Setup(m => m.Map(It.IsAny<ClaimEdiFilesModel>(), It.IsAny<ClaimEdiFilesEntity>()))
                       .Returns(existingEntity);

            // Act
            await _billingFilePath.AddOrUpdateBlobFilePath(model);

            // Assert - Mapper.Map(model, existingEntity) should be called for update
            _mockMapper.Verify(m => m.Map(model, existingEntity), Times.Once);
        }

        [Fact]
        public async Task AddOrUpdateBlobFilePath_ShouldCallMapperMapToEntity_WhenAddingNewRecord()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 200,
                ClaimSubmissionId = 0,
                ClaimId = 0,
                PaymentId = 0,
                FileType = "277",
                BlobFilePath = "path/to/277"
            };

            var emptyList = new List<ClaimEdiFilesEntity>();
            var mockDbSet = DbMock.Create(emptyList);

            _claimEdiFilesEntityMock.Setup(repo => repo.Query())
                                     .Returns(mockDbSet);

            _mockMapper.Setup(m => m.Map<ClaimEdiFilesEntity>(It.IsAny<ClaimEdiFilesModel>()))
                       .Returns(new ClaimEdiFilesEntity());

            // Act
            await _billingFilePath.AddOrUpdateBlobFilePath(model);

            // Assert - Mapper.Map<ClaimEdiFilesEntity>(model) should be called for add
            _mockMapper.Verify(m => m.Map<ClaimEdiFilesEntity>(model), Times.Once);
        }

        [Fact]
        public async Task AddOrUpdateBlobFilePath_ShouldThrowException_WhenRepositoryFails()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                FileType = "837",
                BlobFilePath = "path/to/file"
            };

            _claimEdiFilesEntityMock.Setup(repo => repo.Query())
                                     .Throws(new InvalidOperationException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _billingFilePath.AddOrUpdateBlobFilePath(model));
        }

        [Fact]
        public async Task AddOrUpdateBlobFilePath_ShouldThrowException_WhenCommitFails()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 0,
                ClaimId = 0,
                PaymentId = 0,
                FileType = "837",
                BlobFilePath = "path/to/file"
            };

            var emptyList = new List<ClaimEdiFilesEntity>();
            var mockDbSet = DbMock.Create(emptyList);

            _claimEdiFilesEntityMock.Setup(repo => repo.Query())
                                     .Returns(mockDbSet);

            _mockMapper.Setup(m => m.Map<ClaimEdiFilesEntity>(It.IsAny<ClaimEdiFilesModel>()))
                       .Returns(new ClaimEdiFilesEntity());

            _claimEdiFilesEntityMock.Setup(repo => repo.CommitAsync())
                                     .ThrowsAsync(new Exception("Commit failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                () => _billingFilePath.AddOrUpdateBlobFilePath(model));
        }

        [Fact]
        public async Task AddOrUpdateBlobFilePath_ShouldAddNewRecord_WhenZeroIdsSkipFiltering()
        {
            // Arrange - All optional IDs are 0, so the query filter uses "true" for those conditions
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 500,
                ClaimSubmissionId = 0,
                ClaimId = 0,
                PaymentId = 0,
                FileType = "",
                BlobFilePath = "path/with/zero/ids"
            };

            var emptyList = new List<ClaimEdiFilesEntity>();
            var mockDbSet = DbMock.Create(emptyList);

            _claimEdiFilesEntityMock.Setup(repo => repo.Query())
                                     .Returns(mockDbSet);

            _mockMapper.Setup(m => m.Map<ClaimEdiFilesEntity>(It.IsAny<ClaimEdiFilesModel>()))
                       .Returns(new ClaimEdiFilesEntity());

            // Act
            await _billingFilePath.AddOrUpdateBlobFilePath(model);

            // Assert
            _claimEdiFilesEntityMock.Verify(repo => repo.AddAsync(It.IsAny<ClaimEdiFilesEntity>()), Times.Once);
            _claimEdiFilesEntityMock.Verify(repo => repo.CommitAsync(), Times.Once);
        }


        [Fact]
        public async Task GetEdiFilesFromBlob_ShouldReturnEdiContent_WhenMatchingRecordAndBlobExist()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 456,
                ClaimId = 789,
                PaymentId = 101112,
                FileType = "837"
            };

            var entityList = new List<ClaimEdiFilesEntity>
            {
                new ClaimEdiFilesEntity
                {
                    AccountInfoId = 123,
                    ClaimSubmissionId = 456,
                    ClaimId = 789,
                    PaymentId = 101112,
                    FileType = "837",
                    BlobFilePath = "path/to/edi/file.txt",
                    DateDeleted = null
                }
            };

            var mockDbSet = DbMock.Create(entityList);
            _claimEdiFilesEntityMock.Setup(repo => repo.Query()).Returns(mockDbSet);

            var ediContent = "ISA*00*          *00*          *ZZ*SENDER*ZZ*RECEIVER~ST*837*0001~CLM*CLAIM001*500~";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(ediContent));

            _mockBillingBlobService
                .Setup(s => s.DownloadBlobFromContainerAsync("billing-container", "path/to/edi/file.txt"))
                .ReturnsAsync(stream);

            // Act
            var result = await _billingFilePath.GetEdiFilesFromBlob(model);

            // Assert
            Assert.Equal(ediContent, result);
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ShouldReturnEmpty_WhenNoMatchingRecordInDb()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 999,
                ClaimSubmissionId = 0,
                ClaimId = 0,
                PaymentId = 0,
                FileType = "837"
            };

            var emptyList = new List<ClaimEdiFilesEntity>();
            var mockDbSet = DbMock.Create(emptyList);
            _claimEdiFilesEntityMock.Setup(repo => repo.Query()).Returns(mockDbSet);

            // Act
            var result = await _billingFilePath.GetEdiFilesFromBlob(model);

            // Assert
            Assert.Equal(string.Empty, result);
            _mockBillingBlobService.Verify(
                s => s.DownloadBlobFromContainerAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ShouldReturnEmpty_WhenBlobFilePathIsNull()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 0,
                ClaimId = 0,
                PaymentId = 0,
                FileType = null
            };

            var entityList = new List<ClaimEdiFilesEntity>
            {
                new ClaimEdiFilesEntity
                {
                    AccountInfoId = 123,
                    BlobFilePath = null,
                    DateDeleted = null
                }
            };

            var mockDbSet = DbMock.Create(entityList);
            _claimEdiFilesEntityMock.Setup(repo => repo.Query()).Returns(mockDbSet);

            // Act
            var result = await _billingFilePath.GetEdiFilesFromBlob(model);

            // Assert
            Assert.Equal(string.Empty, result);
            _mockBillingBlobService.Verify(
                s => s.DownloadBlobFromContainerAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ShouldReturnEmpty_WhenBlobFilePathIsEmptyString()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 0,
                ClaimId = 0,
                PaymentId = 0,
                FileType = null
            };

            var entityList = new List<ClaimEdiFilesEntity>
            {
                new ClaimEdiFilesEntity
                {
                    AccountInfoId = 123,
                    BlobFilePath = "",
                    DateDeleted = null
                }
            };

            var mockDbSet = DbMock.Create(entityList);
            _claimEdiFilesEntityMock.Setup(repo => repo.Query()).Returns(mockDbSet);

            // Act
            var result = await _billingFilePath.GetEdiFilesFromBlob(model);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ShouldReturnEmpty_WhenBlobStreamIsNull()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 456,
                ClaimId = 789,
                PaymentId = 101112,
                FileType = "837"
            };

            var entityList = new List<ClaimEdiFilesEntity>
            {
                new ClaimEdiFilesEntity
                {
                    AccountInfoId = 123,
                    ClaimSubmissionId = 456,
                    ClaimId = 789,
                    PaymentId = 101112,
                    FileType = "837",
                    BlobFilePath = "path/to/missing/blob.txt",
                    DateDeleted = null
                }
            };

            var mockDbSet = DbMock.Create(entityList);
            _claimEdiFilesEntityMock.Setup(repo => repo.Query()).Returns(mockDbSet);

            _mockBillingBlobService
                .Setup(s => s.DownloadBlobFromContainerAsync("billing-container", "path/to/missing/blob.txt"))
                .ReturnsAsync((MemoryStream)null);

            // Act
            var result = await _billingFilePath.GetEdiFilesFromBlob(model);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ShouldSkipSoftDeletedRecords()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 456,
                ClaimId = 789,
                PaymentId = 0,
                FileType = "837"
            };

            var entityList = new List<ClaimEdiFilesEntity>
            {
                new ClaimEdiFilesEntity
                {
                    AccountInfoId = 123,
                    ClaimSubmissionId = 456,
                    ClaimId = 789,
                    FileType = "837",
                    BlobFilePath = "path/to/deleted.txt",
                    DateDeleted = DateTime.UtcNow
                }
            };

            var mockDbSet = DbMock.Create(entityList);
            _claimEdiFilesEntityMock.Setup(repo => repo.Query()).Returns(mockDbSet);

            // Act
            var result = await _billingFilePath.GetEdiFilesFromBlob(model);

            // Assert
            Assert.Equal(string.Empty, result);
            _mockBillingBlobService.Verify(
                s => s.DownloadBlobFromContainerAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ShouldBypassOptionalFilters_WhenZeroOrNull()
        {
            // Arrange - All optional IDs are 0/null so filters should be bypassed
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 500,
                ClaimSubmissionId = 0,
                ClaimId = 0,
                PaymentId = 0,
                FileType = null
            };

            var entityList = new List<ClaimEdiFilesEntity>
            {
                new ClaimEdiFilesEntity
                {
                    AccountInfoId = 500,
                    ClaimSubmissionId = 999,
                    ClaimId = 888,
                    PaymentId = 777,
                    FileType = "835",
                    BlobFilePath = "path/to/bypassed.txt",
                    DateDeleted = null
                }
            };

            var mockDbSet = DbMock.Create(entityList);
            _claimEdiFilesEntityMock.Setup(repo => repo.Query()).Returns(mockDbSet);

            var stream = new MemoryStream(Encoding.UTF8.GetBytes("EDI bypassed content"));
            _mockBillingBlobService
                .Setup(s => s.DownloadBlobFromContainerAsync("billing-container", "path/to/bypassed.txt"))
                .ReturnsAsync(stream);

            // Act
            var result = await _billingFilePath.GetEdiFilesFromBlob(model);

            // Assert
            Assert.Equal("EDI bypassed content", result);
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ShouldFilterByPaymentId_WhenGreaterThanZero()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 0,
                ClaimId = 0,
                PaymentId = 555,
                FileType = null
            };

            var entityList = new List<ClaimEdiFilesEntity>
            {
                new ClaimEdiFilesEntity
                {
                    AccountInfoId = 123,
                    PaymentId = 999,
                    BlobFilePath = "path/wrong-payment.txt",
                    DateDeleted = null
                },
                new ClaimEdiFilesEntity
                {
                    AccountInfoId = 123,
                    PaymentId = 555,
                    BlobFilePath = "path/correct-payment.txt",
                    DateDeleted = null
                }
            };

            var mockDbSet = DbMock.Create(entityList);
            _claimEdiFilesEntityMock.Setup(repo => repo.Query()).Returns(mockDbSet);

            var stream = new MemoryStream(Encoding.UTF8.GetBytes("Payment EDI"));
            _mockBillingBlobService
                .Setup(s => s.DownloadBlobFromContainerAsync("billing-container", "path/correct-payment.txt"))
                .ReturnsAsync(stream);

            // Act
            var result = await _billingFilePath.GetEdiFilesFromBlob(model);

            // Assert
            Assert.Equal("Payment EDI", result);
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ShouldFilterByFileType_WhenNotEmpty()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 0,
                ClaimId = 0,
                PaymentId = 0,
                FileType = "835"
            };

            var entityList = new List<ClaimEdiFilesEntity>
            {
                new ClaimEdiFilesEntity
                {
                    AccountInfoId = 123,
                    FileType = "837",
                    BlobFilePath = "path/837.txt",
                    DateDeleted = null
                },
                new ClaimEdiFilesEntity
                {
                    AccountInfoId = 123,
                    FileType = "835",
                    BlobFilePath = "path/835.txt",
                    DateDeleted = null
                }
            };

            var mockDbSet = DbMock.Create(entityList);
            _claimEdiFilesEntityMock.Setup(repo => repo.Query()).Returns(mockDbSet);

            var stream = new MemoryStream(Encoding.UTF8.GetBytes("835 ERA content"));
            _mockBillingBlobService
                .Setup(s => s.DownloadBlobFromContainerAsync("billing-container", "path/835.txt"))
                .ReturnsAsync(stream);

            // Act
            var result = await _billingFilePath.GetEdiFilesFromBlob(model);

            // Assert
            Assert.Equal("835 ERA content", result);
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ShouldUseBillingContainerNameConstant()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 0,
                ClaimId = 0,
                PaymentId = 0,
                FileType = null
            };

            var entityList = new List<ClaimEdiFilesEntity>
            {
                new ClaimEdiFilesEntity
                {
                    AccountInfoId = 123,
                    BlobFilePath = "some/blob/path.txt",
                    DateDeleted = null
                }
            };

            var mockDbSet = DbMock.Create(entityList);
            _claimEdiFilesEntityMock.Setup(repo => repo.Query()).Returns(mockDbSet);

            var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
            _mockBillingBlobService
                .Setup(s => s.DownloadBlobFromContainerAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(stream);

            // Act
            await _billingFilePath.GetEdiFilesFromBlob(model);

            // Assert - Must use BillingConstants.BillingContainerName
            _mockBillingBlobService.Verify(
                s => s.DownloadBlobFromContainerAsync("billing-container", "some/blob/path.txt"),
                Times.Once);
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ShouldResetStreamPosition_WhenStreamIsSeekable()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 0,
                ClaimId = 0,
                PaymentId = 0,
                FileType = null
            };

            var entityList = new List<ClaimEdiFilesEntity>
            {
                new ClaimEdiFilesEntity
                {
                    AccountInfoId = 123,
                    BlobFilePath = "path/seekable.txt",
                    DateDeleted = null
                }
            };

            var mockDbSet = DbMock.Create(entityList);
            _claimEdiFilesEntityMock.Setup(repo => repo.Query()).Returns(mockDbSet);

            var ediText = "ISA*00*ST*837*0001~CLM*CLAIM001*500~";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(ediText));
            stream.Position = stream.Length; // Simulate stream already read

            _mockBillingBlobService
                .Setup(s => s.DownloadBlobFromContainerAsync("billing-container", "path/seekable.txt"))
                .ReturnsAsync(stream);

            // Act
            var result = await _billingFilePath.GetEdiFilesFromBlob(model);

            // Assert - Stream position was reset so content should be fully read
            Assert.Equal(ediText, result);
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ShouldReadLargeEdiContent()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 0,
                ClaimId = 0,
                PaymentId = 0,
                FileType = null
            };

            var entityList = new List<ClaimEdiFilesEntity>
            {
                new ClaimEdiFilesEntity
                {
                    AccountInfoId = 123,
                    BlobFilePath = "path/large.txt",
                    DateDeleted = null
                }
            };

            var mockDbSet = DbMock.Create(entityList);
            _claimEdiFilesEntityMock.Setup(repo => repo.Query()).Returns(mockDbSet);

            // Build a large EDI string exceeding the 1024-byte buffer
            var largeEdi = new StringBuilder();
            for (int i = 0; i < 200; i++)
                largeEdi.Append($"CLM*CLAIM{i:D5}*500*11::1*~");

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(largeEdi.ToString()));

            _mockBillingBlobService
                .Setup(s => s.DownloadBlobFromContainerAsync("billing-container", "path/large.txt"))
                .ReturnsAsync(stream);

            // Act
            var result = await _billingFilePath.GetEdiFilesFromBlob(model);

            // Assert
            Assert.Equal(largeEdi.ToString(), result);
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ShouldThrowException_WhenRepositoryFails()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                FileType = "837"
            };

            _claimEdiFilesEntityMock.Setup(repo => repo.Query())
                .Throws(new InvalidOperationException("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _billingFilePath.GetEdiFilesFromBlob(model));
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ShouldThrowException_WhenBlobServiceFails()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 0,
                ClaimId = 0,
                PaymentId = 0,
                FileType = null
            };

            var entityList = new List<ClaimEdiFilesEntity>
            {
                new ClaimEdiFilesEntity
                {
                    AccountInfoId = 123,
                    BlobFilePath = "path/to/blob.txt",
                    DateDeleted = null
                }
            };

            var mockDbSet = DbMock.Create(entityList);
            _claimEdiFilesEntityMock.Setup(repo => repo.Query()).Returns(mockDbSet);

            _mockBillingBlobService
                .Setup(s => s.DownloadBlobFromContainerAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Blob storage unavailable"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                () => _billingFilePath.GetEdiFilesFromBlob(model));
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ShouldFilterByAllCriteria_WhenAllProvided()
        {
            // Arrange - All filters active simultaneously
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 100,
                ClaimSubmissionId = 200,
                ClaimId = 300,
                PaymentId = 400,
                FileType = "835"
            };

            var entityList = new List<ClaimEdiFilesEntity>
            {
                new ClaimEdiFilesEntity
                {
                    AccountInfoId = 100,
                    ClaimSubmissionId = 200,
                    ClaimId = 300,
                    PaymentId = 400,
                    FileType = "835",
                    BlobFilePath = "path/full-match.txt",
                    DateDeleted = null
                },
                new ClaimEdiFilesEntity
                {
                    AccountInfoId = 100,
                    ClaimSubmissionId = 200,
                    ClaimId = 300,
                    PaymentId = 999, // PaymentId mismatch
                    FileType = "835",
                    BlobFilePath = "path/partial-match.txt",
                    DateDeleted = null
                }
            };

            var mockDbSet = DbMock.Create(entityList);
            _claimEdiFilesEntityMock.Setup(repo => repo.Query()).Returns(mockDbSet);

            var stream = new MemoryStream(Encoding.UTF8.GetBytes("Full match EDI"));
            _mockBillingBlobService
                .Setup(s => s.DownloadBlobFromContainerAsync("billing-container", "path/full-match.txt"))
                .ReturnsAsync(stream);

            // Act
            var result = await _billingFilePath.GetEdiFilesFromBlob(model);

            // Assert
            Assert.Equal("Full match EDI", result);
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ShouldReturnEmpty_WhenAccountInfoIdDoesNotMatch()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 999,
                ClaimSubmissionId = 0,
                ClaimId = 0,
                PaymentId = 0,
                FileType = null
            };

            var entityList = new List<ClaimEdiFilesEntity>
            {
                new ClaimEdiFilesEntity
                {
                    AccountInfoId = 123, // Different AccountInfoId
                    BlobFilePath = "path/other.txt",
                    DateDeleted = null
                }
            };

            var mockDbSet = DbMock.Create(entityList);
            _claimEdiFilesEntityMock.Setup(repo => repo.Query()).Returns(mockDbSet);

            // Act
            var result = await _billingFilePath.GetEdiFilesFromBlob(model);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ShouldReturnFirstMatch_WhenMultipleRecordsExist()
        {
            // Arrange
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 0,
                ClaimId = 0,
                PaymentId = 0,
                FileType = null
            };

            var entityList = new List<ClaimEdiFilesEntity>
            {
                new ClaimEdiFilesEntity
                {
                    AccountInfoId = 123,
                    BlobFilePath = "path/first.txt",
                    DateDeleted = null
                },
                new ClaimEdiFilesEntity
                {
                    AccountInfoId = 123,
                    BlobFilePath = "path/second.txt",
                    DateDeleted = null
                }
            };

            var mockDbSet = DbMock.Create(entityList);
            _claimEdiFilesEntityMock.Setup(repo => repo.Query()).Returns(mockDbSet);

            var stream = new MemoryStream(Encoding.UTF8.GetBytes("First EDI"));
            _mockBillingBlobService
                .Setup(s => s.DownloadBlobFromContainerAsync("billing-container", "path/first.txt"))
                .ReturnsAsync(stream);

            // Act
            var result = await _billingFilePath.GetEdiFilesFromBlob(model);

            // Assert
            Assert.Equal("First EDI", result);
        }

        [Fact]
        public async Task GetEdiFilesFromBlob_ShouldReturnEmptyString_WhenEmptyFileType_BypassesFilter()
        {
            // Arrange - FileType is empty string → filter condition (!string.IsNullOrEmpty) is false → true
            var model = new ClaimEdiFilesModel
            {
                AccountInfoId = 123,
                ClaimSubmissionId = 0,
                ClaimId = 0,
                PaymentId = 0,
                FileType = ""
            };

            var entityList = new List<ClaimEdiFilesEntity>
            {
                new ClaimEdiFilesEntity
                {
                    AccountInfoId = 123,
                    FileType = "999",
                    BlobFilePath = "path/empty-filetype.txt",
                    DateDeleted = null
                }
            };

            var mockDbSet = DbMock.Create(entityList);
            _claimEdiFilesEntityMock.Setup(repo => repo.Query()).Returns(mockDbSet);

            var stream = new MemoryStream(Encoding.UTF8.GetBytes("Bypassed FileType EDI"));
            _mockBillingBlobService
                .Setup(s => s.DownloadBlobFromContainerAsync("billing-container", "path/empty-filetype.txt"))
                .ReturnsAsync(stream);

            // Act
            var result = await _billingFilePath.GetEdiFilesFromBlob(model);

            // Assert - Empty FileType bypasses filter, so it matches
            Assert.Equal("Bypassed FileType EDI", result);
        }

    }
}
