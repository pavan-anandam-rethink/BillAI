using BillingService.Domain.Interfaces.Client;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Clients;
using BillingService.Domain.Models.Clients.History;
using BillingService.Web.Controllers;
using BillingService.Web.Helpers.HttpClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Models;

namespace BillingService.Web.XUnit.Tests.Controllers;

public class ClientChargeHistoryControllerTests
{
    private readonly Mock<IClientChargeHistoryService> _mockChargeHistoryService = new();
    private readonly Mock<IClientService> _mockClientService = new();
    private readonly Mock<ICommonService> _mockCommonService = new();
    private readonly Mock<IConfiguration> _mockConfiguration = new();
    private readonly Mock<IBaseHttpClient> _mockHttpClient = new();
    private readonly Mock<ILogger<ClientChargeHistoryController>> _mockLogger = new();

    private ClientChargeHistoryController CreateController()
    {
        return new ClientChargeHistoryController(
            _mockHttpClient.Object,
            _mockConfiguration.Object,
            _mockClientService.Object,
            _mockCommonService.Object,
            _mockChargeHistoryService.Object,
            _mockLogger.Object
        );
    }

    //[Fact]
    //public async Task GetClientHistoryClaim_ReturnsOk_WhenDataExists()
    //{
    //    // Arrange
    //    int accountInfoId = 1;
    //    var expectedData = new List<ChildProfileEntityModel>
    //    { 
    //        new() { Id = 1, FirstName = "Test1" },
    //        new() { Id = 2, FirstName = "Test2" }
    //    };

    //    _mockChargeHistoryService.Setup(s => s.GetClientHistoryClaimAsync(accountInfoId))
    //        .ReturnsAsync(expectedData);

    //    var controller = CreateController();

    //    // Act
    //    var result = await controller.GetClientHistoryClaim(accountInfoId);

    //    // Assert
    //    var okResult = Assert.IsType<OkObjectResult>(result.Result);
    //    Assert.Equal(expectedData, okResult.Value);
    //}

    //[Fact]
    //public async Task GetClientHistoryClaim_ReturnsNotFound_WhenDataIsNull()
    //{
    //    // Arrange
    //    int accountInfoId = 1;
    //    _mockChargeHistoryService.Setup(s => s.GetClientHistoryClaimAsync(accountInfoId))
    //        .ReturnsAsync((IEnumerable<ChildProfileEntityModel>)null);

    //    var controller = CreateController();

    //    // Act
    //    var result = await controller.GetClientHistoryClaim(accountInfoId);

    //    // Assert
    //    var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
    //    Assert.Equal("No claim history found for this account.", notFoundResult.Value);
    //}

    [Fact]
    public async Task GetClaimRecords_ReturnsOk_WhenCalled()
    {
        // Arrange
        var request = new ClientHistoryRequestModel
        {
            clientHistoryRequest = new(),
            clientRecordFilterModel = new()
        };

        var expectedResult = new ClientHistoryResponseModel
        {
            Total = 2,
            clientHistoryResponse = new List<ClientHistoryResponse>
            {
                new() { ClientId = "1", ClientName = "Test1" },
                new() { ClientId = "2", ClientName = "Test2" }
            }
        };
        _mockChargeHistoryService.Setup(s => s.GetClientRecordAsync(request.clientHistoryRequest, request.clientRecordFilterModel))
            .ReturnsAsync(expectedResult);

        var controller = CreateController();

        // Act
        var result = await controller.GetClientRecords(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task GetClientChargeHistoryDetails_ReturnsOk_WhenCalled()
    {
        // Arrange
        var request = new ClientHistoryChargeDetailsRequestModel
        {
            clientHistoryChargeDetailsRequest = new(),
            clientHistoryChargeFilterModel = new()
        };

        var expectedResult = new ClientHistoryChargeDetailsResponse
        {
            Total = 2,
            ChargeDetails = new List<ClientHistoryChargeDetails>
            {
                new() { DateOfService = DateTime.Today, BillingCode = "100.0m" },
                new() { DateOfService = DateTime.Today, BillingCode = "200.0m" }
            }
        };

        _mockChargeHistoryService.Setup(s => s.GetClientChargeHistoryDetailsAsync(
            request.clientHistoryChargeDetailsRequest,
            request.clientHistoryChargeFilterModel))
            .ReturnsAsync(expectedResult);

        var controller = CreateController();

        // Act
        var result = await controller.GetClientChargeHistoryDetails(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);
    }


    [Fact]
    public async Task GetClientHistoryClaim_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        int accountInfoId = 1;
        _mockChargeHistoryService.Setup(s => s.GetClientHistoryClaimAsync(accountInfoId))
            .ThrowsAsync(new Exception("Service failure"));

        var controller = CreateController();

        // Act
        var result = await controller.GetClientHistoryClaim(accountInfoId);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
        Assert.Contains("Internal server error", statusResult.Value.ToString());
    }

    [Fact]
    public async Task GetClaimRecords_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var request = new ClientHistoryRequestModel
        {
            clientHistoryRequest = new(),
            clientRecordFilterModel = new()
        };

        _mockChargeHistoryService.Setup(s => s.GetClientRecordAsync(
            request.clientHistoryRequest,
            request.clientRecordFilterModel))
            .ThrowsAsync(new Exception("Service failure"));

        var controller = CreateController();

        // Act
        var result = await controller.GetClientRecords(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
        Assert.Contains("Internal server error", statusResult.Value.ToString());
    }

    [Fact]
    public async Task GetClientChargeHistoryDetails_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var request = new ClientHistoryChargeDetailsRequestModel
        {
            clientHistoryChargeDetailsRequest = new(),
            clientHistoryChargeFilterModel = new()
        };

        _mockChargeHistoryService.Setup(s => s.GetClientChargeHistoryDetailsAsync(
            request.clientHistoryChargeDetailsRequest,
            request.clientHistoryChargeFilterModel))
            .ThrowsAsync(new Exception("Service failure"));

        var controller = CreateController();

        // Act
        var result = await controller.GetClientChargeHistoryDetails(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
        Assert.Contains("Internal server error", statusResult.Value.ToString());
    }

    [Fact]
    public async Task SearchInvoices_ReturnsOk_WhenCalled()
    {
        // Arrange
        var request = new InvoiceHistoryRequestModel
        {
            InvoiceHistoryRequest = new()
            {
                ClientId = 1,
                Skip = 0,
                Take = 20
            },
            InvoiceHistoryRequestFilterModel = new()
            {
                AccountInfoId = 1,
            }
        };

        var expectedResult = new InvoiceHistoryResponseModel
        {
            // Populate with expected properties as needed for your test
        };
        _mockChargeHistoryService
            .Setup(s => s.InvoicesSearchAsync(request.InvoiceHistoryRequest, request.InvoiceHistoryRequestFilterModel))
            .ReturnsAsync(expectedResult);

        var controller = CreateController();

        // Act
        var result = await controller.SearchInvoices(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task SearchInvoices_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var request = new InvoiceHistoryRequestModel
        {
            InvoiceHistoryRequest = new(),
            InvoiceHistoryRequestFilterModel = new()
        };

        _mockChargeHistoryService
            .Setup(s => s.InvoicesSearchAsync(request.InvoiceHistoryRequest, request.InvoiceHistoryRequestFilterModel))
            .ThrowsAsync(new Exception("Service failure"));

        var controller = CreateController();

        // Act
        var result = await controller.SearchInvoices(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
        Assert.Contains("Internal server error", statusResult.Value.ToString());
    }

    [Fact]
    public async Task GetAllAuthorizationNumbers_ReturnsOk_WhenServiceReturnsData()
    {
        // Arrange
        var model = new UserInfo
        {
            MemberId = 123
        };

        var expectedResult = new List<AuthorizationNumberResponse>
    {
        new AuthorizationNumberResponse { Name = "AUTH123" },
        new AuthorizationNumberResponse { Name = "AUTH456" }
    };

        _mockChargeHistoryService
            .Setup(s => s.GetAllAuthorizationNumbersAsync(model))
            .ReturnsAsync(expectedResult); 

        var controller = CreateController();

        // Act
        var result = await controller.GetAllAuthorizationNumbers(model);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task GetAllAuthorizationNumbers_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var model = new UserInfo
        {
            MemberId = 123
        };

        _mockChargeHistoryService
            .Setup(s => s.GetAllAuthorizationNumbersAsync(model))
            .ThrowsAsync(new Exception("Service failure"));

        var controller = CreateController();

        // Act
        var result = await controller.GetAllAuthorizationNumbers(model);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
        Assert.Contains("Internal server error", statusResult.Value.ToString());
    }

    [Fact]
    public async Task GetAllAuthorizationNumbers_CallsServiceOnce()
    {
        // Arrange
        var model = new UserInfo
        {
            MemberId = 123
        };

        var response = new List<AuthorizationNumberResponse>
    {
        new AuthorizationNumberResponse { Name = "AUTH123" }
    };

        _mockChargeHistoryService
            .Setup(s => s.GetAllAuthorizationNumbersAsync(It.IsAny<UserInfo>()))
            .Returns(Task.FromResult(response)); // ✅ correct type + compatible with Moq

        var controller = CreateController();

        // Act
        await controller.GetAllAuthorizationNumbers(model);

        // Assert
        _mockChargeHistoryService.Verify(
            s => s.GetAllAuthorizationNumbersAsync(It.IsAny<UserInfo>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetClientHistoryClaim_ReturnsOk_WhenDataExists()
    {
        // Arrange
        int accountInfoId = 1;

        var expectedData = new List<int> { 101, 102, 103 };

        _mockChargeHistoryService
            .Setup(s => s.GetClientHistoryClaimAsync(accountInfoId))
            .Returns(Task.FromResult(expectedData)); // ✅ FIXED

        var controller = CreateController();

        // Act
        var result = await controller.GetClientHistoryClaim(accountInfoId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedData, okResult.Value);
    }

    [Fact]
    public async Task GetClientHistoryClaim_ReturnsNotFound_WhenDataIsNull()
    {
        // Arrange
        int accountInfoId = 1;

        _mockChargeHistoryService
            .Setup(s => s.GetClientHistoryClaimAsync(accountInfoId))
            .Returns(Task.FromResult<List<int>>(null)); // ✅ FIXED

        var controller = CreateController();

        // Act
        var result = await controller.GetClientHistoryClaim(accountInfoId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("No claim history found for this account.", notFoundResult.Value);
    }

    [Fact]
    public async Task GetClientRecords_ReturnsOk_WhenServiceReturnsData()
    {
        // Arrange
        var request = new ClientHistoryRequestModel
        {
            clientHistoryRequest = new(),
            clientRecordFilterModel = new()
        };

        var expectedResult = new ClientHistoryResponseModel
        {
            Total = 2,
            clientHistoryResponse = new List<ClientHistoryResponse>
        {
            new() { ClientId = "1", ClientName = "Test1" },
            new() { ClientId = "2", ClientName = "Test2" }
        }
        };

        _mockChargeHistoryService
            .Setup(s => s.GetClientRecordAsync(
                request.clientHistoryRequest,
                request.clientRecordFilterModel))
            .Returns(Task.FromResult(expectedResult)); // ✅ safe async mock

        var controller = CreateController();

        // Act
        var result = await controller.GetClientRecords(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task GetClientRecords_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var request = new ClientHistoryRequestModel
        {
            clientHistoryRequest = new(),
            clientRecordFilterModel = new()
        };

        _mockChargeHistoryService
            .Setup(s => s.GetClientRecordAsync(
                request.clientHistoryRequest,
                request.clientRecordFilterModel))
            .ThrowsAsync(new Exception("Service failure"));

        var controller = CreateController();

        // Act
        var result = await controller.GetClientRecords(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
        Assert.Contains("Internal server error", statusResult.Value.ToString());
    }

    [Fact]
    public async Task GetClientRecords_CallsServiceOnce()
    {
        // Arrange
        var request = new ClientHistoryRequestModel
        {
            clientHistoryRequest = new(),
            clientRecordFilterModel = new()
        };

        _mockChargeHistoryService
            .Setup(s => s.GetClientRecordAsync(
                It.IsAny<ClientHistoryRequest>(),
                It.IsAny<ClientRecordFilterModel>()))
            .Returns(Task.FromResult(new ClientHistoryResponseModel()));

        var controller = CreateController();

        // Act
        await controller.GetClientRecords(request);

        // Assert
        _mockChargeHistoryService.Verify(
            s => s.GetClientRecordAsync(
                It.IsAny<ClientHistoryRequest>(),
                It.IsAny<ClientRecordFilterModel>()),
            Times.Once);
    }

    [Fact]
    public async Task GetClientChargeHistoryDetails_ReturnsOk_WhenServiceReturnsData()
    {
        // Arrange
        var request = new ClientHistoryChargeDetailsRequestModel
        {
            clientHistoryChargeDetailsRequest = new(),
            clientHistoryChargeFilterModel = new()
        };

        var expectedResult = new ClientHistoryChargeDetailsResponse
        {
            Total = 2,
            ChargeDetails = new List<ClientHistoryChargeDetails>
        {
            new() { BillingCode = "100", DateOfService = DateTime.Today },
            new() { BillingCode = "200", DateOfService = DateTime.Today }
        }
        };

        _mockChargeHistoryService
            .Setup(s => s.GetClientChargeHistoryDetailsAsync(
                request.clientHistoryChargeDetailsRequest,
                request.clientHistoryChargeFilterModel))
            .Returns(Task.FromResult(expectedResult)); // ✅ SAFE

        var controller = CreateController();

        // Act
        var result = await controller.GetClientChargeHistoryDetails(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task GetClientChargeHistoryDetails_CallsServiceOnce()
    {
        // Arrange
        var request = new ClientHistoryChargeDetailsRequestModel
        {
            clientHistoryChargeDetailsRequest = new(),
            clientHistoryChargeFilterModel = new()
        };

        _mockChargeHistoryService
            .Setup(s => s.GetClientChargeHistoryDetailsAsync(
                It.IsAny<ClientHistoryChargeDetailsRequest>(),
                It.IsAny<ClientHistoryChargeFilterModel>()))
            .Returns(Task.FromResult(new ClientHistoryChargeDetailsResponse()));

        var controller = CreateController();

        // Act
        await controller.GetClientChargeHistoryDetails(request);

        // Assert
        _mockChargeHistoryService.Verify(
            s => s.GetClientChargeHistoryDetailsAsync(
                It.IsAny<ClientHistoryChargeDetailsRequest>(),
                It.IsAny<ClientHistoryChargeFilterModel>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchInvoices_ReturnsBadRequest_WhenRequestIsNull()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.SearchInvoices(null);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Request or required properties are null.", badRequest.Value);
    }

    [Fact]
    public async Task SearchInvoices_ReturnsBadRequest_WhenInnerPropertiesAreNull()
    {
        // Arrange
        var request = new InvoiceHistoryRequestModel
        {
            InvoiceHistoryRequest = null,
            InvoiceHistoryRequestFilterModel = null
        };

        var controller = CreateController();

        // Act
        var result = await controller.SearchInvoices(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Request or required properties are null.", badRequest.Value);
    }

    [Fact]
    public async Task SearchInvoices_ReturnsOk_WhenServiceReturnsData()
    {
        // Arrange
        var request = new InvoiceHistoryRequestModel
        {
            InvoiceHistoryRequest = new(),
            InvoiceHistoryRequestFilterModel = new()
        };

        var expectedResult = new InvoiceHistoryResponseModel
        {
            // Populate minimal properties if needed
        };

        _mockChargeHistoryService
            .Setup(s => s.InvoicesSearchAsync(
                request.InvoiceHistoryRequest,
                request.InvoiceHistoryRequestFilterModel))
            .Returns(Task.FromResult(expectedResult)); // ✅ safe

        var controller = CreateController();

        // Act
        var result = await controller.SearchInvoices(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);
    }

}