using BillingService.Domain.DataObjects.Base;
using BillingService.Domain.DataObjects.Billing;
using BillingService.Domain.DataObjects.CompanyAccount;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Client;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.Funders;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Web.Controllers;
using BillingService.Web.Helpers.HttpClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Entities.Billing;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Handlers;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Domain.Interfaces;


namespace BillingService.Web.XUnit.Tests.Controllers;
public class ClaimControllerTests
{
    private readonly Mock<IClaimService> _mockClaimService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IClientService> _clientService;
    private readonly Mock<IProviderLocationService> _providerLocationService;
    private readonly Mock<IMemberAccountService> _memberAccountService;
    private readonly Mock<ICommonService> _commonService;
    private readonly Mock<IRethinkMasterDataMicroServices> _rethinkServices;
    private readonly Mock<IClaimCreateService> _claimCreateService;
    private readonly Mock<IKeyVaultProviderService> _mockKeyVaultProviderService;
    private readonly Mock<ILogger<ClaimController>> _mockLogger;
    private readonly ClaimController _controller;
    private readonly Mock<IClaimHistoryService> _mockClaimHistoryService;
    private readonly Mock<IClaimVersionService> _mockClaimVersionService;


    public ClaimControllerTests()
    {
        var mockHttpClient = new Mock<IBaseHttpClient>();
        var mockConfiguration = new Mock<IConfiguration>();
        _mockClaimService = new Mock<IClaimService>();
        _clientService = new Mock<IClientService>();
        _providerLocationService = new Mock<IProviderLocationService>();
        _memberAccountService = new Mock<IMemberAccountService>();
        _commonService = new Mock<ICommonService>();
        _rethinkServices = new Mock<IRethinkMasterDataMicroServices>();
        _claimCreateService = new Mock<IClaimCreateService>();
        _mockLogger = new Mock<ILogger<ClaimController>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockKeyVaultProviderService = new Mock<IKeyVaultProviderService>();
        _mockClaimHistoryService = new Mock<IClaimHistoryService>();
        _mockClaimVersionService = new Mock<IClaimVersionService>();

        var providerLocationService = new Mock<IProviderLocationService>();
        var memberAccountService = new Mock<IMemberAccountService>();
        var claimHistoryService = new Mock<IClaimHistoryService>();
        var claimVersionService = new Mock<IClaimVersionService>();
        var claimCreateService = new Mock<IClaimCreateService>();
        var rethinkService = new Mock<IRethinkMasterDataMicroServices>();
        var commonService = new Mock<ICommonService>();
        var clientService = new Mock<IClientService>();
        var keyVaultProviderService = new Mock<IKeyVaultProviderService>();

        _controller = new ClaimController(
            mockHttpClient.Object,
            _mockConfiguration.Object,
            _mockLogger.Object,
            _mockClaimService.Object,
            _providerLocationService.Object,
            _memberAccountService.Object,
            _mockClaimHistoryService.Object,
            _mockClaimVersionService.Object,
            _claimCreateService.Object,
            _rethinkServices.Object,
            _commonService.Object,
            _clientService.Object,
            _mockKeyVaultProviderService.Object
        );
    }


    [Fact]
    public async Task GetClaimHeaders_FlagOn_DataNull_DoesNotThrow()
    {
        var requestModel = new ClaimGetRequestSortFilterWithUserInfo
        {
            MemberId = 1
        };

        var response = new ClaimHeaderModelResponseModel
        {
            Data = null
        };

        _mockConfiguration
            .Setup(c => c["UseNewClaimProcessing"])
            .Returns("UseNewClaimProcessing");

        _mockKeyVaultProviderService
            .Setup(k => k.GetSecretAsync("UseNewClaimProcessing"))
            .ReturnsAsync("true");

        _mockClaimService
            .Setup(s => s.GetClaimHeadersAsync(requestModel))
            .ReturnsAsync(response);

        var result = await _controller.GetClaimHeaders(requestModel);

        var ok = Assert.IsType<OkObjectResult>(result);
        var model = Assert.IsType<ClaimHeaderModelResponseModel>(ok.Value);

        Assert.Null(model.Data);

        VerifyGetClaimHeadersInfoLogCalled(requestModel.MemberId);
    }

    [Fact]
    public async Task GetClaimHeaders_FlagOn_DataEmpty_ForEachCovered()
    {
        var requestModel = new ClaimGetRequestSortFilterWithUserInfo
        {
            MemberId = 2
        };

        var response = new ClaimHeaderModelResponseModel
        {
            Data = new List<ClaimHeaderModel>()
        };

        _mockConfiguration
            .Setup(c => c["UseNewClaimProcessing"])
            .Returns("UseNewClaimProcessing");

        _mockKeyVaultProviderService
            .Setup(k => k.GetSecretAsync("UseNewClaimProcessing"))
            .ReturnsAsync("true");

        _mockClaimService
            .Setup(s => s.GetClaimHeadersAsync(requestModel))
            .ReturnsAsync(response);

        var result = await _controller.GetClaimHeaders(requestModel);

        var ok = Assert.IsType<OkObjectResult>(result);
        var model = Assert.IsType<ClaimHeaderModelResponseModel>(ok.Value);

        Assert.Empty(model.Data);

        VerifyGetClaimHeadersInfoLogCalled(requestModel.MemberId);
    }

    [Fact]
    public async Task GetClaimHeaders_FlagOn_SetsUseNewClaimProcessing_ForEachCovered()
    {
        var requestModel = new ClaimGetRequestSortFilterWithUserInfo
        {
            MemberId = 3
        };

        var response = new ClaimHeaderModelResponseModel
        {
            Data = new List<ClaimHeaderModel>
        {
            new ClaimHeaderModel { Id = 1, UseNewClaimProcessing = false },
            new ClaimHeaderModel { Id = 2, UseNewClaimProcessing = false }
        }
        };

        _mockConfiguration
            .Setup(c => c["UseNewClaimProcessing"])
            .Returns("UseNewClaimProcessing");

        _mockKeyVaultProviderService
            .Setup(k => k.GetSecretAsync("UseNewClaimProcessing"))
            .ReturnsAsync("true");

        _mockClaimService
            .Setup(s => s.GetClaimHeadersAsync(requestModel))
            .ReturnsAsync(response);

        var result = await _controller.GetClaimHeaders(requestModel);

        var ok = Assert.IsType<OkObjectResult>(result);
        var model = Assert.IsType<ClaimHeaderModelResponseModel>(ok.Value);

        Assert.All(model.Data, c =>
        {
            Assert.True(c.UseNewClaimProcessing);
        });

        VerifyGetClaimHeadersInfoLogCalled(requestModel.MemberId);
    }

    [Fact]
    public async Task GetClaimHeaders_Exception_LogsError_AndReturnsBadRequest()
    {
        // Arrange
        var requestModel = new ClaimGetRequestSortFilterWithUserInfo
        {
            MemberId = 99
        };

        _mockConfiguration
            .Setup(c => c["UseNewClaimProcessing"])
            .Returns(string.Empty);

        _mockClaimService
            .Setup(s => s.GetClaimHeadersAsync(It.IsAny<ClaimGetRequestSortFilterWithUserInfo>()))
            .ThrowsAsync(new Exception("failure"));

        // Act
        var result = await _controller.GetClaimHeaders(requestModel);

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("failure", bad.Value);

        VerifyGetClaimHeadersErrorLogCalled(requestModel.MemberId);
    }


    private void VerifyGetClaimHeadersInfoLogCalled(int memberId)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString().Contains("Getting claim headers") &&
                    v.ToString().Contains($"MemberId={memberId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
    private void VerifyGetClaimHeadersErrorLogCalled(int memberId)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString().Contains("Error getting claim headers") &&
                    v.ToString().Contains($"memberId={memberId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }


    [Fact]
    public async Task GetAllCarcCodes_ReturnsOk_WhenCarcCodesFound()
    {
        // Arrange
        var expectedCarcCodes = new List<CarcCodeResponseModel>
        {
            new CarcCodeResponseModel { Code = "A1", Description = "Description A1" },
            new CarcCodeResponseModel { Code = "B2", Description = "Description B2" }
        };

        _mockClaimService
            .Setup(s => s.GetAllCarcCodes())
            .ReturnsAsync(expectedCarcCodes);

        // Act
        var result = await _controller.GetAllCarcCodes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedCarcCodes, okResult.Value);
    }

    [Fact]
    public async Task GetAllCarcCodes_ReturnsBadRequest_OnException()
    {
        // Arrange
        _mockClaimService
            .Setup(s => s.GetAllCarcCodes())
            .ThrowsAsync(new Exception("service failure"));

        // Act
        var result = await _controller.GetAllCarcCodes();

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("service failure", badResult.Value);
    }

    [Fact]
    public async Task GetRenderingProvidersForAccount_ReturnsOk_WhenProvidersFound()
    {
        // Arrange
        int accountInfoId = 123;
        var expectedProviders = new List<AuthRenderingProviderType>
            {
                new AuthRenderingProviderType { Id = 1, Name = "Provider A" },
                new AuthRenderingProviderType { Id = 2, Name = "Provider B" }
            };

        _mockClaimService
            .Setup(s => s.GetRenderingProviders(accountInfoId))
            .ReturnsAsync(expectedProviders);

        // Act
        var result = await _controller.GetRenderingProvidersForAccount(accountInfoId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedProviders, okResult.Value);
    }

    [Fact]
    public async Task GetRenderingProvidersForAccount_ReturnsBadRequest_WhenExceptionThrown()
    {
        // Arrange
        int accountInfoId = 456;
        var exceptionMessage = "Something went wrong";

        _mockClaimService
            .Setup(s => s.GetRenderingProviders(accountInfoId))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _controller.GetRenderingProvidersForAccount(accountInfoId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(exceptionMessage, badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateClaimStatus_ReturnsBadRequest_WhenExceptionThrown()
    {
        // Arrange
        var updateClaimStatusModel = new UpdateClaimRequestModel
        {
            ClaimId = 1,
            ClaimStatusId = 5
        };
        var exceptionMessage = "Something went wrong";

        _mockClaimService
            .Setup(s => s.UpdateClaimsStatusAsync(updateClaimStatusModel))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _controller.UpdateClaimStatus(updateClaimStatusModel);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(exceptionMessage, badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateClaimStatus_ReturnsOk_WhenStatusUpdate()
    {
        // Arrange
        var updateClaimStatusModel = new UpdateClaimRequestModel
        {
            ClaimId = 1,
            ClaimStatusId = 5
        };

        _mockClaimService
            .Setup(s => s.UpdateClaimsStatusAsync(updateClaimStatusModel))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateClaimStatus(updateClaimStatusModel);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task GetStaffLocations_ReturnsOk_WhenLocationsFound()
    {
        // Arrange
        var expectedLocations = new List<BaseNameOption>
        {
            new BaseNameOption { Id = 1, Name = "New York Office" },
            new BaseNameOption { Id = 2, Name = "Los Angeles Office" },
            new BaseNameOption { Id = 3, Name = "Chicago Office" },
            new BaseNameOption { Id = 4, Name = "New gersy" },
            new BaseNameOption { Id = 5, Name = "North region" }
        };

        var requestModel = new ClaimFilterGetModel
        {
            AccountInfoId = 18421
        };

        _mockClaimService
            .Setup(s => s.GetStaffLocations(requestModel))
            .ReturnsAsync(expectedLocations);

        // Act
        var result = await _controller.GetStaffLocations(requestModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedLocations, okResult.Value);
    }

    [Fact]
    public async Task GetStaffLocations_ReturnsBadRequest_WhenExceptionIsThrown()
    {
        // Arrange
        var requestModel = new ClaimFilterGetModel
        {
        };

        _mockClaimService.Setup(service => service.GetStaffLocations(It.IsAny<ClaimFilterGetModel>()))
            .ThrowsAsync(new Exception("An error occurred"));

        // Act
        var result = await _controller.GetStaffLocations(requestModel);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("An error occurred", badRequestResult.Value);
    }

    [Fact]
    public async Task GetStaffLocations_LogsError_WhenExceptionIsThrown()
    {
        // Arrange
        var requestModel = new ClaimFilterGetModel
        {
        };
        var exceptionMessage = "An error occurred";

        _mockClaimService.Setup(service => service.GetStaffLocations(It.IsAny<ClaimFilterGetModel>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        await _controller.GetStaffLocations(requestModel);

        // Assert
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(exceptionMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task SubmitClaimToServiceBus_ReturnsOk_WhenClaimProcessed()
    {
        // Arrange
        var request = new ClaimsSubmitModel
        {
            AccountInfoId = 18421,
            MemberId = 12345,
            Ids = [1, 2, 3]
        };

        _mockClaimService
            .Setup(s => s.SubmitClaimsToServiceBusAsync(request));

        // Act
        var result = await _controller.SubmitClaimToServiceBus(request);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SubmitClaimToServiceBus_ReturnsBadRequest_WhenExceptionIsThrown()
    {
        // Arrange
        var request = new ClaimsSubmitModel
        {
            AccountInfoId = 18421,
            MemberId = 12345,
            Ids = [1, 2, 3]
        };

        _mockClaimService
           .Setup(s => s.SubmitClaimsToServiceBusAsync(request))
            .ThrowsAsync(new Exception("An error occurred"));

        // Act
        var result = await _controller.SubmitClaimToServiceBus(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("An error occurred", badRequestResult.Value);
    }

    [Fact]
    public async Task SubmitClaimsForApproval_ReturnsOk_WhenClaimProcessed()
    {
        // Arrange
        var request = new IdsWithUserInfo
        {
            AccountInfoId = 18421,
            MemberId = 12345,
            Ids = [1, 2, 3]
        };

        _mockClaimService
            .Setup(s => s.SubmitClaimsToServiceBusTopicAsync(request));

        // Act
        var result = await _controller.SubmitClaimsForApproval(request);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task SubmitClaimsForApproval_ReturnsOk_WhenExceptionIsThrown()
    {
        // Arrange
        var request = new IdsWithUserInfo
        {
            AccountInfoId = 18421,
            MemberId = 12345,
            Ids = [1, 2, 3]
        };

        _mockClaimService
           .Setup(s => s.SubmitClaimsToServiceBusTopicAsync(request))
            .ThrowsAsync(new Exception("An error occurred"));

        // Act
        var result = await _controller.SubmitClaimsForApproval(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("An error occurred", badRequestResult.Value);
    }

    [Fact]
    public async Task GetClaimDetails_ReturnsOk()
    {
        _mockClaimService
            .Setup(s => s.GetClaimDetailsAsync(It.IsAny<IdWithUserInfo>(), null))
            .ReturnsAsync(new ClaimDetailsModel());

        var result = await _controller.GetClaimDetails(
            new IdWithUserInfo { Id = 1, MemberId = 1 });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetClaimDetails_ReturnsBadRequest_OnException()
    {
        _mockClaimService
            .Setup(s => s.GetClaimDetailsAsync(It.IsAny<IdWithUserInfo>(), null))
            .ThrowsAsync(new Exception("error"));

        var result = await _controller.GetClaimDetails(new IdWithUserInfo());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetHFCAClaimDetails_ReturnsOk()
    {
        _mockClaimService
            .Setup(s => s.GetHFCAClaimDetailsAsync(It.IsAny<IdsWithUserInfo>()))
            .ReturnsAsync(new List<ClaimHFCAModel>());

        var result = await _controller.GetHFCAClaimDetails(
            new IdsWithUserInfo { Ids = new[] { 1 }, AccountInfoId = 1, MemberId = 1 });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetHFCAClaimDetails_ReturnsBadRequest_OnException()
    {
        _mockClaimService
            .Setup(s => s.GetHFCAClaimDetailsAsync(It.IsAny<IdsWithUserInfo>()))
            .ThrowsAsync(new Exception("error"));

        var result = await _controller.GetHFCAClaimDetails(
            new IdsWithUserInfo { Ids = new[] { 1 } });

        Assert.IsType<BadRequestObjectResult>(result);
    }


    [Fact]
    public async Task GetClaimPatients_ReturnsOk()
    {
        _mockClaimService
            .Setup(s => s.GetClaimPatientsAsync(It.IsAny<ClaimFilterGetModel>()))
            .Returns(Task.FromResult(new List<ClaimFilterOptionModel>()));

        var result = await _controller.GetClaimPatients(new ClaimFilterGetModel());
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetClaimPatients_ReturnsBadRequest_OnException()
    {
        // Arrange
        _mockClaimService
            .Setup(s => s.GetClaimPatientsAsync(It.IsAny<ClaimFilterGetModel>()))
            .ThrowsAsync(new Exception("service error"));

        // Act
        var result = await _controller.GetClaimPatients(new ClaimFilterGetModel());

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("service error", badResult.Value);
    }

    [Fact]
    public async Task GetClaimFunders_ReturnsOk()
    {
        _mockClaimService
            .Setup(s => s.GetClaimFundersAsync(It.IsAny<ClaimFilterGetModel>()))
            .Returns(Task.FromResult(new List<ClaimFilterOptionModel>()));

        var result = await _controller.GetClaimFunders(new ClaimFilterGetModel());
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetClaimFunders_ReturnsBadRequest_OnException()
    {
        // Arrange
        _mockClaimService
            .Setup(s => s.GetClaimFundersAsync(It.IsAny<ClaimFilterGetModel>()))
            .ThrowsAsync(new Exception("service error"));

        // Act
        var result = await _controller.GetClaimFunders(new ClaimFilterGetModel
        {
            MemberId = 1
        });

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("service error", badResult.Value);
    }

    [Fact]
    public async Task GetClaimRenderingProvidersAsync_ReturnsOk()
    {
        _mockClaimService
            .Setup(s => s.GetClaimRenderingProvidersAsync(It.IsAny<ClaimFilterGetModel>()))
            .Returns(Task.FromResult(new List<ClaimFilterOptionModel>()));

        var result = await _controller.GetClaimRenderingProviders(
            new ClaimFilterGetModel { MemberId = 1 });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetClaimRenderingProvidersAsync_ReturnsBadRequest_OnException()
    {
        _mockClaimService
            .Setup(s => s.GetClaimRenderingProvidersAsync(It.IsAny<ClaimFilterGetModel>()))
            .ThrowsAsync(new Exception("service error"));

        var result = await _controller.GetClaimRenderingProviders(
            new ClaimFilterGetModel { MemberId = 1 });

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("service error", badResult.Value);
    }

    [Fact]
    public async Task ApproveClaims_ReturnsOk()
    {
        _mockClaimService
            .Setup(s => s.ApproveClaimsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int[]>()))
            .Returns(Task.FromResult(new List<ClaimApprovalResponseModel>()));

        var result = await _controller.ApproveClaims(new IdsWithUserInfo { Ids = [1] });
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ApproveClaims_ReturnsBadRequest_OnException()
    {
        // Arrange
        var model = new IdsWithUserInfo
        {
            AccountInfoId = 1,
            MemberId = 10,
            Ids = new[] { 1, 2 }
        };

        _mockClaimService
            .Setup(s => s.ApproveClaimsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int[]>()))
            .ThrowsAsync(new Exception("approve error"));

        // Act
        var result = await _controller.ApproveClaims(model);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("approve error", badResult.Value);
    }

    [Fact]
    public async Task UnapproveClaims_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var model = new IdsWithUserInfo
        {
            AccountInfoId = 1,
            MemberId = 10,
            Ids = new[] { 1, 2 }
        };

        var expectedResult = Array.Empty<int>();

        _mockClaimService
            .Setup(s => s.UnapproveClaimsAsync(model.AccountInfoId, model.MemberId, model.Ids))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.UnapproveClaims(model);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task UnapproveClaims_ReturnsBadRequest_OnException()
    {
        // Arrange
        var model = new IdsWithUserInfo
        {
            AccountInfoId = 1,
            MemberId = 10,
            Ids = new[] { 1, 2 }
        };

        _mockClaimService
            .Setup(s => s.UnapproveClaimsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int[]>()))
            .ThrowsAsync(new Exception("unapprove error"));

        // Act
        var result = await _controller.UnapproveClaims(model);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("unapprove error", badResult.Value);
    }

    [Fact]
    public async Task DeleteClaims_ReturnsOk()
    {
        // Arrange
        _mockClaimService
            .Setup(s => s.DeleteClaimsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int[]>(), It.IsAny<string>()))
            .ReturnsAsync(new List<ClaimDeleteResultModel>());

        var model = new DeleteClaimsInfo
        { 
            Ids = [1],
            AccountInfoId = 1,
            MemberId = 10,
            ImpersonationUserName = "admin@test.com"
        };

        // Act
        var result = await _controller.DeleteClaims(model);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeleteClaims_ReturnsBadRequest_OnException()
    {
        // Arrange
        _mockClaimService
            .Setup(s => s.DeleteClaimsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int[]>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("delete error"));

        var model = new DeleteClaimsInfo
        {
            AccountInfoId = 1,
            MemberId = 10,
            Ids = new[] { 1, 2 },
            ImpersonationUserName = "admin@test.com"
        };

        // Act
        var result = await _controller.DeleteClaims(model);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("delete error", badResult.Value);
    }

    [Fact]
    public async Task DeleteClaims_LogsInformation_WhenDeleting()
    {
        // Arrange
        _mockClaimService
            .Setup(s => s.DeleteClaimsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int[]>(), It.IsAny<string>()))
            .ReturnsAsync(new List<ClaimDeleteResultModel>());

        var model = new DeleteClaimsInfo
        {
            AccountInfoId = 1,
            MemberId = 10,
            Ids = new[] { 1, 2 }
        };

        // Act
        await _controller.DeleteClaims(model);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString().Contains("Deleting claims") &&
                    v.ToString().Contains("MemberId=10") &&
                    v.ToString().Contains("ClaimIds=1,2")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteClaims_LogsError_OnException()
    {
        // Arrange
        _mockClaimService
            .Setup(s => s.DeleteClaimsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int[]>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("delete error"));

        var model = new DeleteClaimsInfo
        {
            AccountInfoId = 1,
            MemberId = 10,
            Ids = new[] { 1, 2 }
        };

        // Act
        await _controller.DeleteClaims(model);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString().Contains("Failed to delete claims") &&
                    v.ToString().Contains("MemberId=10") &&
                    v.ToString().Contains("ClaimIds=1,2")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SubmitClaims_ReturnsOk()
    {
        _mockClaimService
            .Setup(s => s.SubmitClaimsAsync(It.IsAny<ClaimsSubmitModel>()))
            .Returns(Task.FromResult(new List<string>()));

        var result = await _controller.SubmitClaims(new ClaimsSubmitModel { Ids = [1] });
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task SubmitClaims_ReturnsBadRequest_OnException()
    {
        // Arrange
        _mockClaimService
            .Setup(s => s.SubmitClaimsAsync(It.IsAny<ClaimsSubmitModel>()))
            .ThrowsAsync(new Exception("submit error"));

        var model = new ClaimsSubmitModel
        {
            MemberId = 10,
            Ids = new[] { 1, 2 }
        };

        // Act
        var result = await _controller.SubmitClaims(model);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("submit error", badResult.Value);
    }

    [Fact]
    public async Task MarkBilledClaims_ReturnsOk()
    {
        _mockClaimService
            .Setup(s => s.MarkBilledClaimsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int[]>()))
            .Returns(Task.FromResult(Array.Empty<int>()));

        var result = await _controller.MarkBilledClaims(new IdsWithUserInfo { Ids = [1] });
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task MarkBilledClaims_ReturnsBadRequest_OnException()
    {
        // Arrange
        _mockClaimService
            .Setup(s => s.MarkBilledClaimsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int[]>()))
            .ThrowsAsync(new Exception("mark billed error"));

        var model = new IdsWithUserInfo
        {
            AccountInfoId = 1,
            MemberId = 10,
            Ids = new[] { 1, 2 }
        };

        // Act
        var result = await _controller.MarkBilledClaims(model);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("mark billed error", badResult.Value);
    }

    [Fact]
    public async Task VoidClaims_ReturnsOk()
    {
        // Arrange
        var model = new ClaimsVoidModelWithUserInfo
        {
            AccountInfoId = 1,
            MemberId = 1,
            ClaimsToVoid = new ClaimsVoidModel { /* set properties as needed */ }
        };

        _mockClaimService
            .Setup(s => s.VoidClaimsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<ClaimsVoidModel>(), It.IsAny<int>()))
            .ReturnsAsync(new List<string> { "Claim 1 voided", "Claim 2 voided" });

        // Act
        var result = await _controller.VoidClaims(model);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(new List<string> { "Claim 1 voided", "Claim 2 voided" }, ok.Value);
    }

    [Fact]
    public async Task VoidClaims_ReturnsBadRequest_OnException()
    {
        // Arrange
        var model = new ClaimsVoidModelWithUserInfo
        {
            AccountInfoId = 1,
            MemberId = 1,
            ClaimsToVoid = new ClaimsVoidModel { }
        };

        _mockClaimService
            .Setup(s => s.VoidClaimsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<ClaimsVoidModel>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("void error"));

        // Act
        var result = await _controller.VoidClaims(model);

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("void error", bad.Value);
    }

    [Fact]
    public async Task GetOptions_ReturnsOk()
    {
        // Arrange
        var userInfo = new ClaimIdWithUserInfo
        {
            AccountInfoId = 1,
            MemberId = 1,
            Id = 10
        };

        _clientService
            .Setup(s => s.GetClientsListForClaimAsync(1, 1))
            .Returns(Task.FromResult(new List<ClientOptionModel>
            {
            new ClientOptionModel { Id = 1, Name = "Client A" }
            }));

        _providerLocationService
            .Setup(s => s.GetForAccount(1))
            .Returns(Task.FromResult(new List<ProviderLocations>
            {
            new ProviderLocations
            {
                id = 1,
                name = "Loc A",
                isBillingLocation = true,
                agencyName = "Agency A"
            }
            }));

        _memberAccountService
            .Setup(s => s.GetMembersByAccountInfoId(1))
            .Returns(Task.FromResult(new List<MemberItem>
            {
            new MemberItem { Id = 1, FirstName = "John", LastName = "Doe" }
            }));

        _commonService
            .Setup(s => s.GetLocationCodes(1))
            .Returns(Task.FromResult(new List<LocationCodeData>
            {
            new LocationCodeData { Id = 1, Code = "LC", Description = "Desc" }
            }));

        _mockClaimService
            .Setup(s => s.GetClaimRenderingProviders(1))
            .Returns(Task.FromResult(new List<BasicOption>()));

        _mockClaimService
            .Setup(s => s.GetClaimReferringProviders(10, 1))
            .Returns(Task.FromResult(new List<BasicOption>()));

        _mockClaimService
            .Setup(s => s.GetIdsForAccountAsync(1))
            .Returns(Task.FromResult(new List<int> { 100 }));

        _rethinkServices
            .Setup(s => s.GetUnitTypesAsync())
            .Returns(Task.FromResult(new List<ClientUnitTypes>
            {
            new ClientUnitTypes { id = 1, unitString = "Unit" }
            }));

        // Act
        var result = await _controller.GetOptions(userInfo);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var options = Assert.IsType<ClaimOptions>(okResult.Value);

        Assert.NotEmpty(options.Clients);
        Assert.NotEmpty(options.Locations);
        Assert.NotEmpty(options.Members);
        Assert.NotEmpty(options.LocationCodes);
        Assert.NotEmpty(options.ClaimIds);
        Assert.NotEmpty(options.UnitTypes);
    }

    [Fact]
    public async Task GetOptions_ReturnsBadRequest_OnException()
    {
        _clientService
            .Setup(s => s.GetClientsListForClaimAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("error"));

        var result = await _controller.GetOptions(new ClaimIdWithUserInfo());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetBillingClaimDetails_ReturnsOk()
    {
        _mockClaimService
            .Setup(s => s.GetClaimChargesForAccountAsync(It.IsAny<GetBillingClaimDetailsModel>()))
            .ReturnsAsync(new List<BillingClaimDetailsModel>().AsQueryable());

        var result = await _controller.GetBillingClaimDetails(new GetBillingClaimDetailsModel { ClaimId = 1 });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetBillingClaimDetails_ReturnsBadRequest_OnException()
    {
        _mockClaimService
            .Setup(s => s.GetClaimChargesForAccountAsync(It.IsAny<GetBillingClaimDetailsModel>()))
            .ThrowsAsync(new Exception("error"));

        var result = await _controller.GetBillingClaimDetails(new GetBillingClaimDetailsModel());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RemoveBillingClaimDetails_ReturnsOk()
    {
        _mockClaimService
            .Setup(s => s.RemoveBillingClaimDetailAsync(It.IsAny<RemoveBillingClaimDetailsModel>()))
            .ReturnsAsync(It.IsAny<ActionResponse>());

        var result = await _controller.RemoveBillingClaimDetails(
            new RemoveBillingClaimDetailsModel { MemberId = 1 });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task RemoveBillingClaimDetails_ReturnsBadRequest_OnException()
    {
        _mockClaimService
            .Setup(s => s.RemoveBillingClaimDetailAsync(It.IsAny<RemoveBillingClaimDetailsModel>()))
            .ThrowsAsync(new Exception("error"));

        var result = await _controller.RemoveBillingClaimDetails(
            new RemoveBillingClaimDetailsModel { MemberId = 1 });

        Assert.IsType<BadRequestObjectResult>(result);
    }



    [Fact]
    public async Task ProcessClaimCreation_ReturnsOk()
    {
        _claimCreateService
            .Setup(s => s.ProcessClaimCreation(It.IsAny<ClaimCreateEnd>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.ProcessClaimCreation(
            new ClaimCreateEnd { ClaimId = 1, ClientId = 1 });

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task ProcessClaimCreation_ReturnsBadRequest_OnException()
    {
        _claimCreateService
            .Setup(s => s.ProcessClaimCreation(It.IsAny<ClaimCreateEnd>()))
            .ThrowsAsync(new Exception("error"));

        var result = await _controller.ProcessClaimCreation(new ClaimCreateEnd());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateBillingClaimDetails_ReturnsOk()
    {
        _mockClaimService
            .Setup(s => s.UpdateBillingClaimAsync(It.IsAny<UpdateBillingClaimDetailsListModel>(), It.IsAny<int>(), true))
            .Returns(Task.FromResult(new List<BillingClaimDetailsModel>()   // ✔ exact return type
            ));

        var result = await _controller.UpdateBillingClaimDetails(
            new UpdateBillingClaimDetailsListModel { MemberId = 1 });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateBillingClaimDetails_ReturnsBadRequest_OnException()
    {
        _mockClaimService
            .Setup(s => s.UpdateBillingClaimAsync(It.IsAny<UpdateBillingClaimDetailsListModel>(), It.IsAny<int>(), true))
            .ThrowsAsync(new Exception("error"));

        var result = await _controller.UpdateBillingClaimDetails(
            new UpdateBillingClaimDetailsListModel { MemberId = 1 });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateClaimDetails_ReturnsBadRequest_OnException()
    {
        _mockClaimService
            .Setup(s => s.UpdateClaimAsync(It.IsAny<UpdateDetails>()))
            .ThrowsAsync(new Exception("error"));

        var result = await _controller.UpdateClaimDetails(new UpdateDetails());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetClaimLineAppointments_ReturnsOk()
    {
        _mockClaimService
            .Setup(s => s.GetClaimLineAppointmentsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.FromResult(new List<ServiceLineAppointmentModel>()));

        var result = await _controller.GetClaimLineAppointments(
            new ServiceLineIdWithUserInfo
            {
                AccountInfoId = 1,
                ServiceLineId = 1
            });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetClaimLineAppointments_ReturnsBadRequest_OnException()
    {
        // Arrange
        _mockClaimService
            .Setup(s => s.GetClaimLineAppointmentsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("service error"));

        var model = new ServiceLineIdWithUserInfo
        {
            AccountInfoId = 1,
            ServiceLineId = 1
        };

        // Act
        var result = await _controller.GetClaimLineAppointments(model);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("service error", badResult.Value);
    }

    [Fact]
    public async Task GetClaimRenderingProviders_ReturnsOk()
    {
        _mockClaimService
            .Setup(s => s.GetClaimRenderingProvidersAsync(It.IsAny<ClaimFilterGetModel>()))
            .Returns(Task.FromResult(new List<ClaimFilterOptionModel>()));

        var result = await _controller.GetClaimRenderingProviders(new ClaimFilterGetModel());

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetClaimTabStatuses_ReturnsOk()
    {
        _mockClaimService
            .Setup(s => s.GetClaimTabStatusesAsync(It.IsAny<ClaimFilterGetModel>()))
            .Returns(Task.FromResult(new List<ClaimFilterOptionModel>()));

        var result = await _controller.GetClaimTabStatuses(new ClaimFilterGetModel());

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetClaimTabStatuses_ReturnsBadRequest_OnException()
    {
        // Arrange
        _mockClaimService
            .Setup(s => s.GetClaimTabStatusesAsync(It.IsAny<ClaimFilterGetModel>()))
            .ThrowsAsync(new Exception("service error"));

        // Act
        var result = await _controller.GetClaimTabStatuses(
            new ClaimFilterGetModel { MemberId = 1 });

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("service error", badResult.Value);
    }

    [Fact]
    public async Task Get_ReturnsBadRequest_OnException()
    {
        // Arrange
        _mockClaimService
            .Setup(s => s.GetClaimByIdentifierAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("error"));

        // Act
        var result = await _controller.Get(new ClaimIdWithUserInfo
        {
            ClaimIdentifier = "C123",
            AccountInfoId = 1
        });

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("error", bad.Value);
    }

    [Fact]
    public async Task GetClaimIdentifiers_ReturnsOk()
    {
        _mockClaimService
            .Setup(s => s.GetClaimIdentifiersAsync(It.IsAny<ClaimFilterGetModel>()))
            .ReturnsAsync(new List<ClaimFilterOptionModel>());

        var result = await _controller.GetClaimIdentifiers(new ClaimFilterGetModel { MemberId = 1 });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetClaimIdentifiers_ReturnsBadRequest_OnException()
    {
        _mockClaimService
            .Setup(s => s.GetClaimIdentifiersAsync(It.IsAny<ClaimFilterGetModel>()))
            .ThrowsAsync(new Exception("error"));

        var result = await _controller.GetClaimIdentifiers(new ClaimFilterGetModel { MemberId = 1 });

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("error", badResult.Value);
    }

    [Fact]
    public async Task GetAccountClaims_ReturnsOk()
    {
        _mockClaimService
            .Setup(s => s.GetAccountClaimByIdOrPatientNameAsync(It.IsAny<ClaimSearchModel>()))
            .Returns(Task.FromResult(new List<ClaimDropdownModel>()));

        var result = await _controller.GetAccountClaims(new ClaimSearchModel());

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAccountClaims_ReturnsBadRequest_OnException()
    {
        // Arrange
        _mockClaimService
            .Setup(s => s.GetAccountClaimByIdOrPatientNameAsync(It.IsAny<ClaimSearchModel>()))
            .ThrowsAsync(new Exception("service error"));

        var model = new ClaimSearchModel
        {
            MemberId = 1
        };

        // Act
        var result = await _controller.GetAccountClaims(model);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("service error", badResult.Value);
    }

    [Fact]
    public async Task ValidateClaimData_ThrowsException_WhenModelIsNull()
    {
        await Assert.ThrowsAsync<NullReferenceException>(async () =>
        {
            await _controller.ValidateClaimData(null);
        });
    }

    [Fact]
    public async Task ValidateClaimData_ReturnsOkMinusOne_OnServiceException()
    {
        // Arrange
        var model = new ClaimValidationModel
        {
            Id = 10,
            MemberId = 5
        };

        _mockClaimService
            .Setup(s => s.ValidateClaimDataAsync(model)) // 👈 exact instance
            .ThrowsAsync(new Exception("validation failed"));

        // Act
        var result = await _controller.ValidateClaimData(model);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(-1, okResult.Value);
    }

    [Fact]
    public async Task GetErrorsSources_ReturnsBadRequest_OnException()
    {
        _mockClaimService
            .Setup(s => s.GetErrorsSourcesAsync())
            .ThrowsAsync(new Exception("error"));

        var result = await _controller.GetErrorsSources(new UserInfo());

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("error", badResult.Value);
    }

    [Fact]
    public async Task GetErrorsCodes_ReturnsBadRequest_OnException()
    {
        _mockClaimService
            .Setup(s => s.GetErrorsCodesAsync())
            .ThrowsAsync(new Exception("error"));

        var result = await _controller.GetErrorsCodes(new UserInfo());

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("error", badResult.Value);
    }

    [Fact]
    public async Task GetErrorsCodes_ReturnsOk()
    {
        _mockClaimService
            .Setup(s => s.GetErrorsCodesAsync())
            .Returns(Task.FromResult(new ClaimErrorsCodesModel()));

        var result = await _controller.GetErrorsCodes(new UserInfo());
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task SaveClaim_ReturnsBadRequest_OnException()
    {
        // Arrange
        _mockClaimService
            .Setup(s => s.SaveClaimAsync(It.IsAny<ClaimSaveModelWithUserInfo>()))
            .ThrowsAsync(new Exception("error"));

        var model = new ClaimSaveModelWithUserInfo
        {
            AccountInfoId = 1,
            MemberId = 1,
            Claim = new ClaimSaveModel()
        };

        // Act
        var result = await _controller.SaveClaim(model);

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(bad.Value);
        Assert.Contains("SaveClaim", bad.Value.ToString());
    }

    [Fact]
    public async Task FlagClaims_ReturnsBadRequest_OnException()
    {
        _mockClaimService
            .Setup(s => s.FlagClaimsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int[]>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("error"));

        var result = await _controller.FlagClaims(new UnflagImperson
        {
            Ids = new[] { 1 },
            MemberId = 1,
            AccountInfoId = 1,
            Rethinkuser = "test_user"
        });

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("error", badResult.Value);
    }

    [Fact]
    public async Task FlagClaimsWithReasons_ReturnsBadRequest_WhenReasonsMissing()
    {
        var model = new FlagClaimsRequest
        {
            MemberId = 1,
            AccountInfoId = 1,
            ClaimIds = new List<int> { 10 },
            Reasons = null
        };

        var result = await _controller.FlagClaimsWithReasons(model);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("At least one reason is required.", bad.Value);
    }

    [Fact]
    public async Task FlagClaimsWithReasons_ReturnsBadRequest_WhenClaimIdsMissing()
    {
        var model = new FlagClaimsRequest
        {
            MemberId = 1,
            AccountInfoId = 1,
            ClaimIds = null,
            Reasons = new List<FlagReasonRequest> { new FlagReasonRequest { ReasonId = 1 } }
        };

        var result = await _controller.FlagClaimsWithReasons(model);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("At least one claim is required.", bad.Value);

    }

    [Fact]
    public async Task FlagClaimsWithReasons_ReturnsOk_WhenRequestIsValid()
    {
        var model = new FlagClaimsRequest
        {
            MemberId = 5,
            AccountInfoId = 10,
            ClaimIds = new List<int> { 100, 101 },
            Reasons = new List<FlagReasonRequest>
        {
            new FlagReasonRequest { ReasonId = 1 },
            new FlagReasonRequest { ReasonId = 2 }
        },
            Notes = "Test notes"
        };

        var expectedFlaggedIds = new[] { 100, 101 };

        _mockClaimService
            .Setup(s => s.FlagClaimsAsync(
                model.AccountInfoId,
                model.MemberId,
                It.IsAny<int[]>(),
                It.IsAny<int[]>(),
                model.Notes,
                It.IsAny<int?>(), It.IsAny<string>()))
            .ReturnsAsync(expectedFlaggedIds);

        var result = await _controller.FlagClaimsWithReasons(model);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedFlaggedIds, ok.Value);

        _mockClaimService.Verify(s =>
            s.FlagClaimsAsync(
                model.AccountInfoId,
                model.MemberId,
                new[] { 100, 101 },
                new[] { 1, 2 },
                model.Notes,
                null,model.ImpersonationUserName),
            Times.Once);
    }

    [Fact]
    public async Task FlagClaimsWithReasons_ReturnsBadRequest_WhenExceptionThrown()
    {
        var model = new FlagClaimsRequest
        {
            MemberId = 9,
            AccountInfoId = 3,
            ClaimIds = new List<int> { 200 },
            Reasons = new List<FlagReasonRequest>
        {
            new FlagReasonRequest { ReasonId = 99 }
        }
        };

        _mockClaimService
            .Setup(s => s.FlagClaimsAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int[]>(),
                It.IsAny<int[]>(),
                It.IsAny<string>(),
                It.IsAny<int?>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Service failure"));

        var result = await _controller.FlagClaimsWithReasons(model);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Service failure", bad.Value);

        VerifyFlagClaimsWithReasonsErrorLoggerCalled(model.MemberId);
    }


    private void VerifyFlagClaimsWithReasonsErrorLoggerCalled(int memberId)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString().Contains("Error flagging claims") &&
                    v.ToString().Contains($"memberId={memberId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }




    [Fact]
    public async Task UnflagClaims_ReturnsBadRequest_OnException()
    {
        _mockClaimService
            .Setup(s => s.UnflagClaimsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int[]>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("error"));

        var result = await _controller.UnflagClaims(new UnflagImperson
        {
            Ids = new[] { 1 },
            MemberId = 1,
            AccountInfoId = 1,
            Rethinkuser = "test_user"
        });

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("error", badResult.Value);
    }

    [Fact]
    public async Task IsDiagnosisServiceLineHasActiveClaims_ReturnsOk()
    {
        // Arrange
        var model = new IsDiagnosisInUseModel
        {
            ClientId = 10,
            DiagnosisCodeId = 20
        };

        _mockClaimService
            .Setup(s => s.IsDiagnosisServiceLineHasActiveClaims(10, 20))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.IsDiagnosisServiceLineHasActiveClaims(model);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(true, okResult.Value);
    }

    [Fact]
    public async Task IsDiagnosisServiceLineHasActiveClaims_ReturnsBadRequest_OnException()
    {
        // Arrange
        var model = new IsDiagnosisInUseModel
        {
            ClientId = 10,
            DiagnosisCodeId = 20
        };

        _mockClaimService
            .Setup(s => s.IsDiagnosisServiceLineHasActiveClaims(10, 20))
            .ThrowsAsync(new Exception("service error"));

        // Act
        var result = await _controller.IsDiagnosisServiceLineHasActiveClaims(model);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("service error", badResult.Value);
    }

    [Fact]
    public async Task GetDiagnosisServiceLineUsedByClaims_ReturnsOk()
    {
        // Arrange
        var model = new IsDiagnosisInUseModel
        {
            ClientId = 5,
            DiagnosisCodeId = 12
        };

        var expectedResult = new List<ClientDiagnosisServiceLine>
    {
        new ClientDiagnosisServiceLine() // ✅ no Id
    };

        _mockClaimService
            .Setup(s => s.GetDiagnosisServiceLineUsedByClaims(5, 12))
            .Returns(Task.FromResult(expectedResult));
        // Act
        var result = await _controller.GetDiagnosisServiceLineUsedByClaims(model);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task GetDiagnosisServiceLineUsedByClaims_ReturnsBadRequest_OnException()
    {
        var model = new IsDiagnosisInUseModel
        {
            ClientId = 5,
            DiagnosisCodeId = 12
        };

        _mockClaimService
            .Setup(s => s.GetDiagnosisServiceLineUsedByClaims(5, 12))
            .ThrowsAsync(new Exception("service error"));

        var result = await _controller.GetDiagnosisServiceLineUsedByClaims(model);

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("service error", badResult.Value);
    }
    [Fact]
    public async Task SaveSelectedColumns_ReturnsOk()
    {
        // Arrange
        var model = new MemberViewSettingWithUserInfo
        {
            AccountInfoId = 1,
            MemberId = 10,
            SelectedColumns = new List<string> { "Column1", "Column2" }
        };

        var expectedResult = new MemberViewSettingEntity();

        _mockClaimService
            .Setup(s => s.SaveSelectedColumnsAsync(model.AccountInfoId, model.MemberId, model.SelectedColumns))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SaveSelectedColumns(model);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task SaveSelectedColumns_ReturnsBadRequest_OnException()
    {
        var model = new MemberViewSettingWithUserInfo
        {
            AccountInfoId = 1,
            MemberId = 10,
            SelectedColumns = new List<string>()
        };

        _mockClaimService
            .Setup(s => s.SaveSelectedColumnsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<string>>()))
            .ThrowsAsync(new Exception("save failed"));

        var result = await _controller.SaveSelectedColumns(model);

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("save failed", badResult.Value);
    }

    [Fact]
    public async Task GetMemberViewSettings_ReturnsOk()
    {
        // Arrange
        var model = new UserInfo
        {
            MemberId = 10
        };

        var expectedResult = new MemberViewSettingEntity();

        _mockClaimService
            .Setup(s => s.GetMemberViewSettingsAsync(model.MemberId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetMemberViewSettings(model);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task GetMemberViewSettings_ReturnsBadRequest_OnException()
    {
        var model = new UserInfo
        {
            MemberId = 10
        };

        _mockClaimService
            .Setup(s => s.GetMemberViewSettingsAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("service error"));

        var result = await _controller.GetMemberViewSettings(model);

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("service error", badResult.Value);
    }

    [Fact]
    public async Task GetClaimHistory_ReturnsOk()
    {
        // Arrange
        var model = new IdWithUserInfo
        {
            Id = 100,
            AccountInfoId = 1,
            MemberId = 10
        };

        var expectedResult = new List<ClaimHistoryModel>();

        _mockClaimHistoryService
            .Setup(s => s.GetAllAsync(model.Id, model.AccountInfoId, model.MemberId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetClaimHistory(model);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task GetClaimHistory_ReturnsBadRequest_OnException()
    {
        // Arrange
        var model = new IdWithUserInfo
        {
            Id = 100,
            AccountInfoId = 1,
            MemberId = 10
        };

        _mockClaimHistoryService
            .Setup(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("history error"));

        // Act
        var result = await _controller.GetClaimHistory(model);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("history error", badResult.Value);
    }

    [Fact]
    public async Task ValidateFunderChangedClaimsData_ReturnsOk()
    {
        // Arrange
        var model = new ClaimFunderChangedModel
        {
            Id = 100,
            ClientFunderId = 200,
            MemberId = 10,
            FunderModifiedDate = DateTime.UtcNow
        };

        _mockClaimService
            .Setup(s => s.ValidateClaimsOnFunderChangedAsync(
                model.Id,
                model.ClientFunderId,
                model.FunderModifiedDate,
                model.MemberId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ValidateFunderChangedClaimsData(model);

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task ValidateFunderChangedClaimsData_ReturnsBadRequest_OnException()
    {
        // Arrange
        var model = new ClaimFunderChangedModel
        {
            Id = 100,
            ClientFunderId = 200,
            MemberId = 10,
            FunderModifiedDate = DateTime.UtcNow
        };

        _mockClaimService
            .Setup(s => s.ValidateClaimsOnFunderChangedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("validation error"));

        // Act
        var result = await _controller.ValidateFunderChangedClaimsData(model);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("validation error", badResult.Value);
    }

    [Fact]
    public async Task GetClaimErrorsAndAlerts_ReturnsOk()
    {
        // Arrange
        var model = new IdWithUserInfo
        {
            Id = 100
        };

        var expectedResult = new List<ClaimErrorAlertViewModel>
        {
            new ClaimErrorAlertViewModel()
        };

        _mockClaimService
            .Setup(s => s.GetClaimErrorsAndAlertsAsync(100))
            .Returns(Task.FromResult(expectedResult)); // ✅ FIX

        // Act
        var result = await _controller.GetClaimErrorsAndAlerts(model);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);
    }



    [Fact]
    public async Task GetClaimErrorsAndAlerts_ReturnsBadRequest_OnException()
    {
        var model = new IdWithUserInfo
        {
            Id = 100
        };

        _mockClaimService
            .Setup(s => s.GetClaimErrorsAndAlertsAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("service error"));

        var result = await _controller.GetClaimErrorsAndAlerts(model);

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("service error", badResult.Value);
    }

    [Fact]
    public async Task CompleteClaims_ReturnsOk()
    {
        _mockClaimService
            .Setup(s => s.CompleteSelectedClaimsAsync(It.IsAny<int[]>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.FromResult(new List<string> { "C1", "C2" }));

        var result = await _controller.CompleteClaims(
            new IdsWithUserInfo
            {
                Ids = new[] { 1, 2 },
                AccountInfoId = 1,
                MemberId = 1
            });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CompleteClaims_ReturnsBadRequest_OnException()
    {
        _mockClaimService
            .Setup(s => s.CompleteSelectedClaimsAsync(It.IsAny<int[]>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("error"));

        var result = await _controller.CompleteClaims(
            new IdsWithUserInfo
            {
                Ids = new[] { 1 },
                AccountInfoId = 1,
                MemberId = 1
            });

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("error", bad.Value);
    }

    [Fact]
    public async Task RebillClaims_ReturnsOk()
    {
        // Arrange
        _mockClaimService
            .Setup(s => s.RebillClaimsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<ClaimsRebillModel>(), 0))
            .ReturnsAsync(new List<string> { "Completed1", "Completed2" });

        var model = new ClaimsRebillModelWithUserInfo
        {
            AccountInfoId = 1,
            MemberId = 1,
            ClaimsToRebill = new ClaimsRebillModel
            {
                ClaimIds = new[] { 101, 102 }
            }
        };

        // Act
        var result = await _controller.RebillClaims(model);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsType<List<string>>(okResult.Value);
        Assert.Equal(2, data.Count);
    }

    [Fact]
    public async Task RebillClaims_ReturnsBadRequest_OnException()
    {
        // Arrange
        _mockClaimService
            .Setup(s => s.RebillClaimsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<ClaimsRebillModel>(), 0))
            .ThrowsAsync(new Exception("rebill error"));

        var model = new ClaimsRebillModelWithUserInfo
        {
            AccountInfoId = 1,
            MemberId = 1,
            ClaimsToRebill = new ClaimsRebillModel
            {
                ClaimIds = new[] { 101 }
            }
        };

        // Act
        var result = await _controller.RebillClaims(model);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("rebill error", badResult.Value);
    }

    [Fact]
    public async Task SecondaryBillingRebillClaims_ReturnsOk()
    {
        // Arrange
        var model = new SecondaryBillingClaimsRebillModel
        {
            MemberId = 10,
            SecondaryFunderDetails = new List<SecondaryFunderDetailsModel>
            {
            new SecondaryFunderDetailsModel() // empty is fine, only Count is used
            }
        };

        var expectedResult = new List<string> { "Rebilled1", "Rebilled2" };

        _mockClaimService
            .Setup(s => s.SecondaryBillingRebillClaimsAsync(model))
            .Returns(Task.FromResult(expectedResult));

        // Act
        var result = await _controller.SecondaryBillingRebillClaims(model);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);

        _mockLogger.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, _) =>
                              v.ToString().Contains("Starting secondary billing rebill") &&
                              v.ToString().Contains("MemberId=10") &&
                              v.ToString().Contains("DetailsCount=1")),
                              null,
                        It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                    Times.Once);
    }

    [Fact]
    public async Task SecondaryBillingRebillClaims_ReturnsBadRequest_OnException()
    {
        // Arrange
        var model = new SecondaryBillingClaimsRebillModel
        {
            MemberId = 10,
            SecondaryFunderDetails = new List<SecondaryFunderDetailsModel>
        {
            new SecondaryFunderDetailsModel()
        }
        };

        _mockClaimService
            .Setup(s => s.SecondaryBillingRebillClaimsAsync(It.IsAny<SecondaryBillingClaimsRebillModel>()))
            .ThrowsAsync(new Exception("rebill failed"));

        // Act
        var result = await _controller.SecondaryBillingRebillClaims(model);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal("rebill failed", badResult.Value);
        _mockLogger.Verify(x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) =>
                        v.ToString().Contains("Starting secondary billing rebill") &&
                        v.ToString().Contains("MemberId=10") &&
                        v.ToString().Contains("DetailsCount=1")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                    Times.Once);
    }

    [Fact]
    public async Task SecondaryBillingRebillClaims_ReturnsOk_WhenSecondaryFunderDetailsIsNull()
    {
        // Arrange
        var model = new SecondaryBillingClaimsRebillModel
        {
            MemberId = 10,
            SecondaryFunderDetails = null
        };

        var expectedResult = new List<string> { "Rebilled" };

        _mockClaimService
            .Setup(s => s.SecondaryBillingRebillClaimsAsync(model))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SecondaryBillingRebillClaims(model);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);

        _mockClaimService.Verify(
            s => s.SecondaryBillingRebillClaimsAsync(model),
            Times.Once);

        _mockLogger.Verify(x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) =>
                        v.ToString().Contains("Starting secondary billing rebill") &&
                        v.ToString().Contains("MemberId=10") &&
                        v.ToString().Contains("DetailsCount=0")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                    Times.Once);
    }

    [Fact]
    public async Task SecondaryBillingRebillClaims_ReturnsOk_WhenMultipleSecondaryFunders()
    {
        // Arrange
        var model = new SecondaryBillingClaimsRebillModel
        {
            MemberId = 40,
            SecondaryFunderDetails = new List<SecondaryFunderDetailsModel>
        {
            new SecondaryFunderDetailsModel(),
            new SecondaryFunderDetailsModel()
        }
        };

        var expectedResult = new List<string> { "RebilledMultiple" };

        _mockClaimService
            .Setup(s => s.SecondaryBillingRebillClaimsAsync(model))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SecondaryBillingRebillClaims(model);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString().Contains("MemberId=40") &&
                    v.ToString().Contains("DetailsCount=2")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }


    [Fact]
    public async Task GetClaimBillNextFunders_ReturnsOk()
    {
        // Arrange
        var model = new IdWithUserInfo
        {
            AccountInfoId = 1,
            MemberId = 10,
            Id = 100
        };

        var expectedResult = new ClaimNextFundersAndControlNumberModel();
        _mockClaimService
            .Setup(s => s.GetClaimBillNextFundersAndControlNumberAsync(1, 10, 100))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetClaimBillNextFunders(model);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expectedResult, ok.Value);
    }

    [Fact]
    public async Task GetClaimBillNextFunders_ReturnsBadRequest_OnException()
    {
        // Arrange
        var model = new IdWithUserInfo
        {
            AccountInfoId = 1,
            MemberId = 10,
            Id = 100
        };

        _mockClaimService
            .Setup(s => s.GetClaimBillNextFundersAndControlNumberAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("error"));

        // Act
        var result = await _controller.GetClaimBillNextFunders(model);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("error", badResult.Value);
    }

    [Fact]
    public async Task GetClaimProviderLocationUsageCount_ReturnsOk()
    {
        // Arrange
        int providerLocationId = 5;
        int expectedCount = 12;

        _mockClaimService
            .Setup(s => s.ClaimProviderLocationUsageCountAsync(providerLocationId))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _controller.GetClaimProviderLocationUsageCount(providerLocationId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedCount, okResult.Value);
    }

    [Fact]
    public async Task GetClaimProviderLocationUsageCount_ReturnsBadRequest_OnException()
    {
        // Arrange
        int providerLocationId = 5;

        _mockClaimService
            .Setup(s => s.ClaimProviderLocationUsageCountAsync(providerLocationId))
            .ThrowsAsync(new Exception("error"));

        // Act
        var result = await _controller.GetClaimProviderLocationUsageCount(providerLocationId);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("error", badResult.Value);
    }

    [Fact]
    public async Task GetClaimReferringProviderUsageCount_ReturnsOk()
    {
        // Arrange
        int providerId = 7;
        int expectedCount = 15;

        _mockClaimService
            .Setup(s => s.ClaimReferringProviderUsageCountAsync(providerId))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _controller.GetClaimReferringProviderUsageCount(providerId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedCount, okResult.Value);
    }

    [Fact]
    public async Task GetClaimReferringProviderUsageCount_ReturnsBadRequest_OnException()
    {
        // Arrange
        int providerId = 7;

        _mockClaimService
            .Setup(s => s.ClaimReferringProviderUsageCountAsync(providerId))
            .ThrowsAsync(new Exception("error"));

        // Act
        var result = await _controller.GetClaimReferringProviderUsageCount(providerId);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("error", badResult.Value);
    }

    [Fact]
    public async Task GetClaimStaffAsRendingProviderUsageCount_ReturnsOk()
    {
        // Arrange
        int staffId = 5;
        int expectedCount = 12;

        _mockClaimService
            .Setup(s => s.ClaimStaffAsRendingProviderUsageCountAsync(staffId))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _controller.GetClaimStaffAsRendingProviderUsageCount(staffId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedCount, okResult.Value);
    }

    [Fact]
    public async Task GetClaimStaffAsRendingProviderUsageCount_ReturnsBadRequest_OnException()
    {
        // Arrange
        int staffId = 5;

        _mockClaimService
            .Setup(s => s.ClaimStaffAsRendingProviderUsageCountAsync(staffId))
            .ThrowsAsync(new Exception("error"));

        // Act
        var result = await _controller.GetClaimStaffAsRendingProviderUsageCount(staffId);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("error", badResult.Value);
    }

    [Fact]
    public async Task SetEditAuthWarning_ReturnsOk()
    {
        // Arrange
        var model = new AuthorizationModifiedModel
        {
            AuthorizationId = 100,
            MemberId = 10
        };

        _mockClaimService
            .Setup(s => s.SetEditAuthWarningAsync(model))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SetEditAuthWarning(model);

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task SetEditAuthWarning_ReturnsBadRequest_OnException()
    {
        // Arrange
        var model = new AuthorizationModifiedModel
        {
            AuthorizationId = 100,
            MemberId = 10
        };

        _mockClaimService
            .Setup(s => s.SetEditAuthWarningAsync(It.IsAny<AuthorizationModifiedModel>()))
            .ThrowsAsync(new Exception("error"));

        // Act
        var result = await _controller.SetEditAuthWarning(model);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("error", badRequestResult.Value);
    }

    [Fact]
    public async Task CheckIsAuthInUseByClaim_ReturnsOk_WhenAuthIsInUse()
    {
        // Arrange
        int authorizationId = 10;

        _mockClaimService
            .Setup(s => s.CheckIsAuthUsedByClaimAsync(It.IsAny<int>()))
            .Returns(Task.FromResult(new List<AuthorizationBuitData> { new AuthorizationBuitData() }));

        // Act
        var result = await _controller.CheckIsAuthInUseByClaim(authorizationId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var authData = Assert.IsType<List<AuthorizationBuitData>>(okResult.Value);
        Assert.NotEmpty(authData);
    }

    [Fact]
    public async Task CheckIsAuthInUseByClaim_ReturnsOk_WhenAuthIsNotInUse()
    {
        // Arrange
        int authorizationId = 20;

        var expectedResult = new List<AuthorizationBuitData>();

        _mockClaimService
            .Setup(s => s.CheckIsAuthUsedByClaimAsync(authorizationId))
            .Returns(Task.FromResult(expectedResult));

        // Act
        var result = await _controller.CheckIsAuthInUseByClaim(authorizationId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var authData = Assert.IsType<List<AuthorizationBuitData>>(okResult.Value);
        Assert.Empty(authData);
    }

    [Fact]
    public async Task CheckIsAuthInUseByClaim_ReturnsBadRequest_OnException()
    {
        // Arrange
        int authorizationId = 10;

        _mockClaimService
            .Setup(s => s.CheckIsAuthUsedByClaimAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("error"));

        // Act
        var result = await _controller.CheckIsAuthInUseByClaim(authorizationId);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("error", badResult.Value);
    }

    [Fact]
    public async Task GetClaimHistoryVersion_ReturnsOk()
    {
        // Arrange
        var model = new IdWithUserInfo
        {
            Id = 10
        };

        var expectedResult = new ClaimVersionEntity();

        _mockClaimVersionService
            .Setup(s => s.GetByIdAsync(10))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetClaimHistoryVersion(model);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task GetClaimHistoryVersion_ReturnsBadRequest_OnException()
    {
        // Arrange
        var model = new IdWithUserInfo
        {
            Id = 10
        };

        _mockClaimVersionService
            .Setup(s => s.GetByIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("version error"));

        // Act
        var result = await _controller.GetClaimHistoryVersion(model);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("version error", badResult.Value);
    }

    [Fact]
    public async Task PropagateProvidersClaimData_ReturnsOk()
    {
        // Arrange
        var model = new PropagatingProvidersClaimDataModel
        {
            AccountInfoId = 1

        };

        var expectedResult = true;

        _mockClaimService
            .Setup(s => s.PopagateProvidersClaimDataAsync(model, 1))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.PropagateProvidersClaimData(model);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task PropagateProvidersClaimData_ReturnsBadRequest_OnException()
    {
        // Arrange
        var model = new PropagatingProvidersClaimDataModel
        {
            AccountInfoId = 1
        };

        _mockClaimService
            .Setup(s => s.PopagateProvidersClaimDataAsync(It.IsAny<PropagatingProvidersClaimDataModel>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("propagation failed"));

        // Act
        var result = await _controller.PropagateProvidersClaimData(model);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("propagation failed", badResult.Value);
    }

    [Fact]
    public async Task IsFunderHasActiveClaims_ReturnsOk_WhenClaimsExist()
    {
        // Arrange
        var model = new IsClientFundersInUseModel
        {
            ClientFunderIds = new List<int> { 1, 2 }
        };

        var expectedResult = new List<ClientFunderWithClaimModel>
    {
        new ClientFunderWithClaimModel(),
        new ClientFunderWithClaimModel()
    };

        _mockClaimService
            .Setup(s => s.IsFunderHasActiveClaimsAsync(model))
            .Returns(Task.FromResult(expectedResult));

        // Act
        var result = await _controller.IsFunderHasActiveClaims(model);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task IsFunderHasActiveClaims_ReturnsBadRequest_OnException()
    {
        // Arrange
        var model = new IsClientFundersInUseModel
        {
            ClientFunderIds = new List<int> { 1, 2 }
        };

        _mockClaimService
            .Setup(s => s.IsFunderHasActiveClaimsAsync(It.IsAny<IsClientFundersInUseModel>()))
            .ThrowsAsync(new Exception("service error"));

        // Act
        var result = await _controller.IsFunderHasActiveClaims(model);

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("service error", badResult.Value);
    }

    [Fact]
    public async Task GetClaimHistoryActions_ReturnsOk()
    {
        // Arrange
        var expectedResult = new List<ClaimHistoryActionEntity>
    {
        new ClaimHistoryActionEntity()
    };

        _mockClaimHistoryService
            .Setup(s => s.GetClaimHistoryActionsAsync())
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetClaimHistoryActions(new UserInfo());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task GetClaimHistoryActions_ReturnsBadRequest_OnException()
    {
        _mockClaimHistoryService
            .Setup(s => s.GetClaimHistoryActionsAsync())
            .ThrowsAsync(new Exception("history error"));

        var result = await _controller.GetClaimHistoryActions(new UserInfo());

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("history error", badResult.Value);
    }

    [Fact]
    public async Task GetStateInformation_ReturnsOk()
    {
        // Arrange
        var expectedStates = new List<StateDto>
        {
            new StateDto { StateCode = "CA", StateName = "California" },
            new StateDto { StateCode = "TX", StateName = "Texas" }
        };

        _mockClaimService
            .Setup(s => s.GetStatesAsync())
            .ReturnsAsync(expectedStates);

        // Act
        var result = await _controller.GetStateInformation();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedStates, okResult.Value);
    }

    [Fact]
    public async Task GetStateInformation_ReturnsBadRequest_OnException()
    {
        // Arrange
        _mockClaimService
            .Setup(s => s.GetStatesAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetStateInformation();

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Database error", badRequestResult.Value);
    }

    [Fact]
    public async Task GetAllExternalCodes_ReturnsOk_WithExternalCodes()
    {
        // Arrange
        var expectedCodes = new List<ExternalCodeResponseModel>
            {
                new ExternalCodeResponseModel { Code = "A1", Description = "Code A1" },
                new ExternalCodeResponseModel { Code = "B2", Description = "Code B2" }
            };
        _mockClaimService
            .Setup(s => s.GetAllExternalCodes())
            .ReturnsAsync(expectedCodes);

        // Act
        var actionResult = await _controller.GetAllExternalCodes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnedCodes = Assert.IsType<List<ExternalCodeResponseModel>>(okResult.Value);
        Assert.Equal(2, returnedCodes.Count);
        Assert.Equal("A1", returnedCodes[0].Code);
        Assert.Equal("B2", returnedCodes[1].Code);
    }

    [Fact]
    public async Task GetAllExternalCodes_ReturnsOk_WithEmptyList()
    {
        // Arrange
        _mockClaimService
            .Setup(s => s.GetAllExternalCodes())
            .ReturnsAsync(new List<ExternalCodeResponseModel>());

        // Act
        var actionResult = await _controller.GetAllExternalCodes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var returnedCodes = Assert.IsType<List<ExternalCodeResponseModel>>(okResult.Value);
        Assert.Empty(returnedCodes);
    }

    [Fact]
    public async Task GetAllExternalCodes_ReturnsBadRequest_WhenExceptionThrown()
    {
        // Arrange
        var exceptionMessage = "Database connection failed";
        _mockClaimService
            .Setup(s => s.GetAllExternalCodes())
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var actionResult = await _controller.GetAllExternalCodes();

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(exceptionMessage, badRequestResult.Value);
    }

    [Fact]
    public async Task GetAllExternalCodes_LogsInformation_OnCall()
    {
        // Arrange
        _mockClaimService
            .Setup(s => s.GetAllExternalCodes())
            .ReturnsAsync(new List<ExternalCodeResponseModel>());

        // Act
        await _controller.GetAllExternalCodes();

        // Assert
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("GetAllExternalCodes")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllExternalCodes_LogsError_WhenExceptionThrown()
    {
        // Arrange
        _mockClaimService
            .Setup(s => s.GetAllExternalCodes())
            .ThrowsAsync(new Exception("Some error"));

        // Act
        await _controller.GetAllExternalCodes();

        // Assert
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("GetAllExternalCodes")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}