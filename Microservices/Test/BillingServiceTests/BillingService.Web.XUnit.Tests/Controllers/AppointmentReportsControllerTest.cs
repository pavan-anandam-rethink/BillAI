using BillingService.Domain.DataObjects.Base;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Services.Billing;
using BillingService.Web.Controllers;
using BillingService.Web.Helpers.HttpClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.ReportingModels;

namespace BillingService.Web.XUnit.Tests.Controllers;

public class AppointmentReportsControllerTests
{
    private readonly Mock<IBaseHttpClient> _mockHttpClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IAppointmentService> _mockAppointmentService;
    private readonly Mock<IAppointmentReportService> _mockAppointmentReportService;
    private readonly Mock<IClaimSearchService> _mockClaimSearchService;

    private readonly AppointmentReportsController _controller;
    private readonly Mock<ILogger<AppointmentReportsController>> _mockLogger;


    public AppointmentReportsControllerTests()
    {
        _mockHttpClient = new Mock<IBaseHttpClient>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockAppointmentService = new Mock<IAppointmentService>();
        _mockAppointmentReportService = new Mock<IAppointmentReportService>();
        _mockClaimSearchService = new Mock<IClaimSearchService>();
        _mockLogger = new Mock<ILogger<AppointmentReportsController>>();

        _controller = new AppointmentReportsController(
            _mockHttpClient.Object,
            _mockConfiguration.Object,
            _mockAppointmentService.Object,
            _mockAppointmentReportService.Object,
            _mockClaimSearchService.Object,
              _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetUnbilledAppointments_ReturnsOkResult_WithExpectedData()
    {
        // Arrange
        var requestModel = new UnbilledAppointmentsRequestModel
        {
            Clients = [1],
        };

        var expectedResult = new AppointmentModelWithCount
        {
            totalCount = 1,
            appointmentModels = []
        };

        _mockAppointmentReportService
            .Setup(x => x.GetUnbilledAppointmentDetails(requestModel))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetUnbilledAppointments(requestModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task CreateClaimsForUnbilledAppointments_ClaimCreated_ReturnsOkResult()
    {
        // Arrange
        var requestModel = new IdsWithUserInfo
        {
            Ids = [1, 2, 3],
            AccountInfoId = 1,
            MemberId = 2,
        };

        _mockAppointmentReportService
            .Setup(x => x.CreateClaimsForUnbilledAppointmentsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int[]>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CreateClaimsForUnbilledAppointments(requestModel);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task ExportUnbilledAppointmentData_ReturnsOk_WithBase64Data()
    {
        // Arrange
        var model = new UnbilledAppointmentsRequestModel
        {
            Clients = [1]
            // Add any other necessary fields your controller expects
        };

        // Simulate Excel file as byte array
        byte[] fakeExcelBytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        string expectedBase64 = Convert.ToBase64String(fakeExcelBytes);

        // Mock the service method call
        _mockAppointmentReportService
            .Setup(s => s.ExportUnbilledAppointmentDataAsync(It.IsAny<ExportModelForUnbilledAppointments>()))
            .ReturnsAsync(fakeExcelBytes);

        // Act
        var result = await _controller.ExportUnbilledAppointmentData(model);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        // Assuming the controller returns: return Ok(new { data = base64String });
        var dataProperty = okResult.Value.GetType().GetProperty("data");
        Assert.NotNull(dataProperty);

        var actualBase64 = dataProperty.GetValue(okResult.Value)?.ToString();
        Assert.Equal(expectedBase64, actualBase64);
    }

    [Fact]
    public async Task GetClientListByIds_ReturnsOkResult_WithExpectedData()
    {
        // Arrange
        var requestModel = new UserInfo
        {
            AccountInfoId = 1,
        };

        var responseModel = new List<BaseNameOption>
        {
            new (){ Id = 1, Name = "Test"}
        };

        _mockClaimSearchService
            .Setup(x => x.GetAllClientsForAccount(It.IsAny<int>()))
            .ReturnsAsync(responseModel);

        // Act
        var result = await _controller.GetClientListByIds(requestModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        Assert.Equal(responseModel, okResult.Value);
    }

    [Fact]
    public async Task GetFunderListByIds_ReturnsOkResult_WithExpectedData()
    {
        // Arrange
        var requestModel = new UserInfo
        {
            AccountInfoId = 1,
        };

        var responseModel = new List<BaseNameOption>
        {
            new (){ Id = 1, Name = "Test"}
        };

        _mockClaimSearchService
            .Setup(x => x.GetFunderInfoByIds(It.IsAny<int>()))
            .ReturnsAsync(responseModel);

        // Act
        var result = await _controller.GetFunderListByIds(requestModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        Assert.Equal(responseModel, okResult.Value);
    }

    [Fact]
    public async Task GetStaffListByIds_ReturnsOkResult_WithExpectedData()
    {
        // Arrange
        var requestModel = new UserInfo
        {
            AccountInfoId = 1,
        };

        var responseModel = new List<StaffBaseNameOption>
        {
            new (){ Id = 1, Name = "Test"}
        };

        _mockClaimSearchService
            .Setup(x => x.GetStaffInfoByIds(It.IsAny<int>()))
            .ReturnsAsync(responseModel);

        // Act
        var result = await _controller.GetStaffListByIds(requestModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        Assert.Equal(responseModel, okResult.Value);
    }

    [Fact]
    public async Task GetPoSListByIds_ReturnsOkResult_WithExpectedData()
    {
        // Arrange
        var requestModel = new UserInfo
        {
            AccountInfoId = 1,
        };

        var responseModel = new List<BaseNameOption>
        {
            new (){ Id = 1, Name = "Test"}
        };

        _mockClaimSearchService
            .Setup(x => x.GetPlaceOfServiceInfoByIds(It.IsAny<int>()))
            .ReturnsAsync(responseModel);

        // Act
        var result = await _controller.GetPoSListByIds(requestModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        Assert.Equal(responseModel, okResult.Value);
    }

    [Fact]
    public async Task GetLocationListByIds_ReturnsOkResult_WithExpectedData()
    {
        // Arrange
        var requestModel = new UserInfo
        {
            AccountInfoId = 1,
        };

        var responseModel = new List<BaseNameOption>
        {
            new (){ Id = 1, Name = "Test"}
        };

        _mockClaimSearchService
            .Setup(x => x.GetLocationInfoByIds(It.IsAny<int>()))
            .ReturnsAsync(responseModel);

        // Act
        var result = await _controller.GetLocationListByIds(requestModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        Assert.Equal(responseModel, okResult.Value);
    }

    //------------------ Exception (only for one endpoint because there is no scenarios for exception handling)

    [Fact]
    public async Task ExportUnbilledAppointmentData_LogsErrorAndReturnsBadRequest_WhenServiceThrows()
    {
        // Arrange
        var requestModel = new UnbilledAppointmentsRequestModel
        {
            AccountInfoId = 1,
            MemberId = 2
        };

        _mockAppointmentReportService
            .Setup(s => s.ExportUnbilledAppointmentDataAsync(
                It.IsAny<ExportModelForUnbilledAppointments>()))
            .ThrowsAsync(new Exception("Service failure"));

        // Act
        var result = await _controller.ExportUnbilledAppointmentData(requestModel);

        // Assert - BadRequest
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);

        // anonymous object: { message = "Failed to export data. Please try again." }
        var value = badRequestResult.Value!;
        var messageProperty = value.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        Assert.Equal(
            "Failed to export data. Please try again.",
            messageProperty.GetValue(value)?.ToString());

        // Assert - LogError called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString().Contains(
                        "AppointmentReportsController.ExportUnbilledAppointmentData - Failed to export unbilled appointment data")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }


    [Fact]
    public async Task GetUnprocessedAppointmentsCount_ReturnsOkResult_WithExpectedData()
    {
        // Arrange

        var responseModel = 5; // Assume service returns an integer count

        _mockAppointmentReportService
            .Setup(x => x.UnprocessedAppointmentsCountAsync(It.IsAny<int>()))
            .ReturnsAsync(responseModel);

        // Act
        var result = await _controller.GetUnprocessedAppointmentsCount(It.IsAny<int>());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(responseModel, okResult.Value);
    }

    [Fact]
    public async Task GetUnprocessedAppointments_ReturnsOkResult_WithExpectedData()
    {
        // Arrange
        var requestModel = new UnprocessedAppointmentsRequestModel
        {
            AccountInfoId = 1
        };

        var responseModel = new AppointmentModelWithCount
        {
            totalCount = 1,
            appointmentModels = new List<AppointmentModel>
            {
                new()
                {
                    Id = 1,
                }
            }
        };

        _mockAppointmentReportService
            .Setup(x => x.UnprocessedAppointments(It.IsAny<UnprocessedAppointmentsRequestModel>()))
            .ReturnsAsync(responseModel);


        // Act
        var result = await _controller.GetUnprocessedAppointments(requestModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(responseModel, okResult.Value);
    }

    [Fact]
    public async Task GetUnbilledAppointments_ReturnsOkResult_WhenServiceReturnsNull()
    {
        // Arrange
        var requestModel = new UnbilledAppointmentsRequestModel
        {
            Clients = [1],
        };

        _mockAppointmentReportService
            .Setup(x => x.GetUnbilledAppointmentDetails(requestModel))
            .ReturnsAsync((AppointmentModelWithCount)null);

        // Act
        var result = await _controller.GetUnbilledAppointments(requestModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Null(okResult.Value);
    }

    [Fact]
    public async Task GetUnbilledAppointments_WithEmptyClientsList_ReturnsOkWithEmptyResult()
    {
        // Arrange
        var requestModel = new UnbilledAppointmentsRequestModel
        {
            Clients = []
        };

        var expectedResult = new AppointmentModelWithCount
        {
            totalCount = 0,
            appointmentModels = []
        };

        _mockAppointmentReportService
            .Setup(x => x.GetUnbilledAppointmentDetails(requestModel))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetUnbilledAppointments(requestModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task GetUnbilledAppointments_WithMultipleClients_ReturnsOkResult()
    {
        // Arrange
        var requestModel = new UnbilledAppointmentsRequestModel
        {
            Clients = [1, 2, 3]
        };

        var expectedResult = new AppointmentModelWithCount
        {
            totalCount = 3,
            appointmentModels = []
        };

        _mockAppointmentReportService
            .Setup(x => x.GetUnbilledAppointmentDetails(requestModel))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetUnbilledAppointments(requestModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult.totalCount, ((AppointmentModelWithCount)okResult.Value).totalCount);
    }

    [Fact]
    public async Task CreateClaimsForUnbilledAppointments_WhenServiceReturnsFalse_StillReturnsOk()
    {
        // Arrange
        var requestModel = new IdsWithUserInfo
        {
            Ids = [1, 2],
            AccountInfoId = 10,
            MemberId = 20
        };

        _mockAppointmentReportService
            .Setup(x => x.CreateClaimsForUnbilledAppointmentsAsync(
                requestModel.AccountInfoId,
                requestModel.MemberId,
                requestModel.Ids))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CreateClaimsForUnbilledAppointments(requestModel);

        // Assert
        var okResult = Assert.IsType<OkResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task CreateClaimsForUnbilledAppointments_EmptyIds_ReturnsOk()
    {
        // Arrange
        var model = new IdsWithUserInfo
        {
            Ids = [],
            AccountInfoId = 5,
            MemberId = 10
        };

        _mockAppointmentReportService
            .Setup(x => x.CreateClaimsForUnbilledAppointmentsAsync(5, 10, Array.Empty<int>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CreateClaimsForUnbilledAppointments(model);

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task CreateClaimsForUnbilledAppointments_VerifiesServiceCall()
    {
        // Arrange
        var model = new IdsWithUserInfo
        {
            Ids = [4, 5],
            AccountInfoId = 7,
            MemberId = 9
        };

        _mockAppointmentReportService
            .Setup(x => x.CreateClaimsForUnbilledAppointmentsAsync(7, 9, new[] { 4, 5 }))
            .ReturnsAsync(true);

        // Act
        await _controller.CreateClaimsForUnbilledAppointments(model);

        // Assert
        _mockAppointmentReportService.Verify(x =>
            x.CreateClaimsForUnbilledAppointmentsAsync(7, 9, new[] { 4, 5 }),
            Times.Once);
    }

    [Fact]
    public async Task ExportUnbilledAppointmentData_VerifiesServiceCall()
    {
        // Arrange
        var requestModel = new UnbilledAppointmentsRequestModel
        {
            Clients = [10]
        };

        _mockAppointmentReportService
            .Setup(s => s.ExportUnbilledAppointmentDataAsync(It.IsAny<ExportModelForUnbilledAppointments>()))
            .ReturnsAsync(new byte[] { 1 });

        // Act
        await _controller.ExportUnbilledAppointmentData(requestModel);

        // Assert
        _mockAppointmentReportService.Verify(
            x => x.ExportUnbilledAppointmentDataAsync(It.Is<ExportModelForUnbilledAppointments>(m =>
                m.Model == requestModel
            )),
            Times.Once
        );
    }

    [Fact]
    public async Task ExportUnbilledAppointmentData_WhenFilterIsNull_StillReturnsOk()
    {
        // Arrange
        var requestModel = new UnbilledAppointmentsRequestModel();
        var fileBytes = new byte[] { 9, 9, 9 };

        // If GetFiltersForExport is internal/virtual, mock it here.
        // If not mockable, skip this test (I can help refactor).

        _mockAppointmentReportService
            .Setup(s => s.ExportUnbilledAppointmentDataAsync(It.IsAny<ExportModelForUnbilledAppointments>()))
            .ReturnsAsync(fileBytes);

        // Act
        var result = await _controller.ExportUnbilledAppointmentData(requestModel);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ExportUnbilledAppointmentData_VerifiesExportModelPassedToService()
    {
        // Arrange
        var requestModel = new UnbilledAppointmentsRequestModel();

        _mockAppointmentReportService
            .Setup(s => s.ExportUnbilledAppointmentDataAsync(It.IsAny<ExportModelForUnbilledAppointments>()))
            .ReturnsAsync(new byte[] { 1 });

        // Act
        await _controller.ExportUnbilledAppointmentData(requestModel);

        // Assert
        _mockAppointmentReportService.Verify(
            s => s.ExportUnbilledAppointmentDataAsync(
                It.Is<ExportModelForUnbilledAppointments>(m => m.Model == requestModel)
            ),
            Times.Once);
    }

    [Fact]
    public async Task GetClientListByIds_WhenServiceReturnsNull_ReturnsOkWithNull()
    {
        // Arrange
        var requestModel = new UserInfo { AccountInfoId = 1 };

        _mockClaimSearchService
            .Setup(x => x.GetAllClientsForAccount(1))
            .ReturnsAsync((List<BaseNameOption>)null);

        // Act
        var result = await _controller.GetClientListByIds(requestModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Null(okResult.Value);
    }

    [Fact]
    public async Task GetClientListByIds_WhenServiceReturnsEmptyList_ReturnsOkWithEmptyList()
    {
        // Arrange
        var requestModel = new UserInfo { AccountInfoId = 1 };

        var responseList = new List<BaseNameOption>();

        _mockClaimSearchService
            .Setup(x => x.GetAllClientsForAccount(1))
            .ReturnsAsync(responseList);

        // Act
        var result = await _controller.GetClientListByIds(requestModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Empty((List<BaseNameOption>)okResult.Value);
    }

    [Fact]
    public async Task GetClientListByIds_VerifyServiceCalledWithCorrectId()
    {
        // Arrange
        var requestModel = new UserInfo { AccountInfoId = 5 };

        _mockClaimSearchService
            .Setup(x => x.GetAllClientsForAccount(5))
            .ReturnsAsync(new List<BaseNameOption>());

        // Act
        await _controller.GetClientListByIds(requestModel);

        // Assert
        _mockClaimSearchService.Verify(
            x => x.GetAllClientsForAccount(5),
            Times.Once
        );
    }

    [Fact]
    public async Task GetFunderListByIds_CallsServiceExactlyOnce()
    {
        // Arrange
        var model = new UserInfo { AccountInfoId = 5 };

        _mockClaimSearchService
            .Setup(x => x.GetFunderInfoByIds(model.AccountInfoId))
            .ReturnsAsync(new List<BaseNameOption>());

        // Act
        await _controller.GetFunderListByIds(model);

        // Assert
        _mockClaimSearchService.Verify(
            x => x.GetFunderInfoByIds(model.AccountInfoId),
            Times.Once);
    }

    [Fact]
    public async Task GetFunderListByIds_NoDataFound_ReturnsEmptyList()
    {
        // Arrange
        var model = new UserInfo { AccountInfoId = 1 };

        _mockClaimSearchService
            .Setup(x => x.GetFunderInfoByIds(It.IsAny<int>()))
            .ReturnsAsync(new List<BaseNameOption>());

        // Act
        var result = await _controller.GetFunderListByIds(model);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<List<BaseNameOption>>(okResult.Value);

        Assert.Empty(list);
    }

    [Fact]
    public async Task GetClientHistoryFunderListByIds_ReturnsOk_WithExpectedData()
    {
        // Arrange
        var requestModel = new ClientHistoryUserInfo
        {
            AccountInfoId = 1,
            ClientId = 10
        };

        var expected = new List<BaseNameOption>
    {
        new() { Id = 1, Name = "Funder A" }
    };

        _mockClaimSearchService
            .Setup(s => s.GetClientHistoryFunderInfoByIds(1, 10))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetClientHistoryFunderListByIds(requestModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, okResult.Value);
    }

    [Fact]
    public async Task GetClientHistoryFunderListByIds_NoDataFound_ReturnsEmptyList()
    {
        // Arrange
        var requestModel = new ClientHistoryUserInfo
        {
            AccountInfoId = 1,
            ClientId = 999
        };

        _mockClaimSearchService
            .Setup(s => s.GetClientHistoryFunderInfoByIds(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<BaseNameOption>());

        // Act
        var result = await _controller.GetClientHistoryFunderListByIds(requestModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsAssignableFrom<List<BaseNameOption>>(okResult.Value);
        Assert.Empty(data);
    }

    [Fact]
    public async Task GetClientHistoryFunderListByIds_CallsServiceOnce_WithCorrectParams()
    {
        // Arrange
        var model = new ClientHistoryUserInfo
        {
            AccountInfoId = 5,
            ClientId = 20
        };

        _mockClaimSearchService
            .Setup(s => s.GetClientHistoryFunderInfoByIds(5, 20))
            .ReturnsAsync(new List<BaseNameOption>());

        // Act
        await _controller.GetClientHistoryFunderListByIds(model);

        // Assert
        _mockClaimSearchService.Verify(
            s => s.GetClientHistoryFunderInfoByIds(5, 20),
            Times.Once
        );
    }

    [Fact]
    public async Task GetClientHistoryFunderListByIds_LogsErrorAndReturnsBadRequest_WhenServiceThrows()
    {
        // Arrange
        var model = new ClientHistoryUserInfo
        {
            AccountInfoId = 1,
            ClientId = 10
        };

        _mockClaimSearchService
            .Setup(s => s.GetClientHistoryFunderInfoByIds(
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ThrowsAsync(new Exception("Service failed"));

        // Act
        var result = await _controller.GetClientHistoryFunderListByIds(model);

        // Assert - BadRequest
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);

        var value = badRequestResult.Value!;
        var messageProperty = value.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        Assert.Equal(
            "Service failed",
            messageProperty.GetValue(value)?.ToString());

        // Assert - LogError
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString().Contains(
                        "AppointmentReportsController.GetClientHistoryFunderListByIds - Failed to get client history funder list")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }


    [Fact]
    public async Task GetStaffListByIds_CallsServiceWithCorrectId()
    {
        // Arrange
        var model = new UserInfo { AccountInfoId = 5 };

        _mockClaimSearchService
            .Setup(s => s.GetStaffInfoByIds(5))
            .ReturnsAsync(new List<StaffBaseNameOption>());

        // Act
        await _controller.GetStaffListByIds(model);

        // Assert
        _mockClaimSearchService.Verify(
            s => s.GetStaffInfoByIds(5),
            Times.Once
        );
    }

    [Fact]
    public async Task GetStaffListByIds_LogsErrorAndReturnsBadRequest_WhenServiceThrows()
    {
        // Arrange
        var model = new UserInfo
        {
            AccountInfoId = 1
        };

        _mockClaimSearchService
            .Setup(s => s.GetStaffInfoByIds(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Service failed"));

        // Act
        var result = await _controller.GetStaffListByIds(model);

        // Assert - BadRequest
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);

        var value = badRequestResult.Value!;
        var messageProperty = value.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        Assert.Equal(
            "Service failed",
            messageProperty.GetValue(value)?.ToString());

        // Assert - LogError
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString().Contains(
                        "AppointmentReportsController.GetStaffListByIds - Failed to get staff list")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }


    [Fact]
    public async Task GetPoSListByIds_CallsServiceWithCorrectId()
    {
        // Arrange
        var model = new UserInfo { AccountInfoId = 99 };

        _mockClaimSearchService
            .Setup(s => s.GetPlaceOfServiceInfoByIds(99))
            .ReturnsAsync(new List<BaseNameOption>());

        // Act
        await _controller.GetPoSListByIds(model);

        // Assert
        _mockClaimSearchService.Verify(
            s => s.GetPlaceOfServiceInfoByIds(99),
            Times.Once
        );
    }

    [Fact]
    public async Task GetPoSListByIds_LogsErrorAndReturnsBadRequest_WhenServiceThrows()
    {
        // Arrange
        var model = new UserInfo
        {
            AccountInfoId = 1
        };

        _mockClaimSearchService
            .Setup(s => s.GetPlaceOfServiceInfoByIds(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetPoSListByIds(model);

        // Assert - BadRequest
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);

        var value = badRequestResult.Value!;
        var messageProperty = value.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        Assert.Equal(
            "Service error",
            messageProperty.GetValue(value)?.ToString());

        // Assert - LogError
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString().Contains(
                        "AppointmentReportsController.GetPoSListByIds - Failed to get place of service list")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }


    [Fact]
    public async Task GetLocationListByIds_CallsServiceWithCorrectId()
    {
        // Arrange
        var model = new UserInfo { AccountInfoId = 99 };

        _mockClaimSearchService
            .Setup(s => s.GetLocationInfoByIds(99))
            .ReturnsAsync(new List<BaseNameOption>());

        // Act
        await _controller.GetLocationListByIds(model);

        // Assert
        _mockClaimSearchService.Verify(
            s => s.GetLocationInfoByIds(99),
            Times.Once
        );
    }

    [Fact]
    public async Task GetLocationListByIds_LogsErrorAndReturnsBadRequest_WhenServiceThrows()
    {
        // Arrange
        var model = new UserInfo
        {
            AccountInfoId = 1
        };

        _mockClaimSearchService
            .Setup(s => s.GetLocationInfoByIds(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Service failure"));

        // Act
        var result = await _controller.GetLocationListByIds(model);

        // Assert - BadRequest
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);

        var value = badRequestResult.Value!;
        var messageProperty = value.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        Assert.Equal(
            "Service failure",
            messageProperty.GetValue(value)?.ToString());

        // Assert - LogError
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString().Contains(
                        "AppointmentReportsController.GetLocationListByIds - Failed to get location list")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }


    [Fact]
    public async Task GetUnprocessedAppointmentsCount_CallsServiceWithCorrectId()
    {
        // Arrange
        int expectedId = 10;

        _mockAppointmentReportService
            .Setup(x => x.UnprocessedAppointmentsCountAsync(expectedId))
            .ReturnsAsync(3);

        // Act
        await _controller.GetUnprocessedAppointmentsCount(expectedId);

        // Assert
        _mockAppointmentReportService.Verify(
            x => x.UnprocessedAppointmentsCountAsync(expectedId),
            Times.Once
        );
    }

    [Fact]
    public async Task GetUnprocessedAppointmentsCount_WhenZeroAppointments_ReturnsZero()
    {
        // Arrange
        _mockAppointmentReportService
            .Setup(x => x.UnprocessedAppointmentsCountAsync(It.IsAny<int>()))
            .ReturnsAsync(0);

        // Act
        var result = await _controller.GetUnprocessedAppointmentsCount(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(0, okResult.Value);
    }

    [Fact]
    public async Task GetUnprocessedAppointmentsCount_ServiceReturnsZero_ReturnsOkWithZero()
    {
        // Arrange
        _mockAppointmentReportService
            .Setup(x => x.UnprocessedAppointmentsCountAsync(It.IsAny<int>()))
            .ReturnsAsync(0);

        // Act
        var result = await _controller.GetUnprocessedAppointmentsCount(5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(0, okResult.Value);
    }

    [Fact]
    public async Task GetUnprocessedAppointments_PassesCorrectModelToService()
    {
        // Arrange
        var requestModel = new UnprocessedAppointmentsRequestModel
        {
            AccountInfoId = 10
        };

        _mockAppointmentReportService
            .Setup(x => x.UnprocessedAppointments(requestModel))
            .ReturnsAsync(new AppointmentModelWithCount());

        // Act
        await _controller.GetUnprocessedAppointments(requestModel);

        // Assert
        _mockAppointmentReportService.Verify(
            x => x.UnprocessedAppointments(requestModel),
            Times.Once
        );
    }


    [Fact]
    public async Task GetUnprocessedAppointments_ServiceReturnsEmptyList_ReturnsOk()
    {
        // Arrange
        var requestModel = new UnprocessedAppointmentsRequestModel
        {
            AccountInfoId = 1
        };

        var emptyResponse = new AppointmentModelWithCount
        {
            totalCount = 0,
            appointmentModels = new List<AppointmentModel>()
        };

        _mockAppointmentReportService
            .Setup(x => x.UnprocessedAppointments(It.IsAny<UnprocessedAppointmentsRequestModel>()))
            .ReturnsAsync(emptyResponse);

        // Act
        var result = await _controller.GetUnprocessedAppointments(requestModel);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(emptyResponse, okResult.Value);
    }

}

