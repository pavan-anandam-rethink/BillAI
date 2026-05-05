using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Web.Controllers;
using BillingService.Web.Helpers.HttpClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Models.Claim;

namespace BillingService.Web.XUnit.Tests.Controllers
{
    public class AppointmentControllerTests
    {
        private readonly Mock<IAppointmentService> _appointmentServiceMock;
        private readonly Mock<IClaimSyncService> _claimSyncServiceMock;
        private readonly Mock<ILogger<AppointmentController>> _loggerMock;
        private readonly Mock<IBaseHttpClient> _httpClientMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly AppointmentController _controller;

        public AppointmentControllerTests()
        {
            _appointmentServiceMock = new Mock<IAppointmentService>();
            _claimSyncServiceMock = new Mock<IClaimSyncService>();
            _loggerMock = new Mock<ILogger<AppointmentController>>();
            _httpClientMock = new Mock<IBaseHttpClient>();
            _configurationMock = new Mock<IConfiguration>();

            _controller = new AppointmentController(
                _appointmentServiceMock.Object,
                _claimSyncServiceMock.Object,
                _loggerMock.Object,
                _httpClientMock.Object,
                _configurationMock.Object
            );
        }

        [Fact]
        public async Task GetClaimsAssignee_ReturnsOk_WhenServiceReturnsData()
        {
            // Arrange
            var request = new ClaimFilterGetModel { AccountInfoId = 1 };
            var expected = new List<ClaimsAssigneeResponse>
            {
                new ClaimsAssigneeResponse { MemberId = 0, Name = "Unassigned" },
                new ClaimsAssigneeResponse { MemberId = 1, Name = "John Doe" },
                new ClaimsAssigneeResponse { MemberId = 2, Name = "Jane Smith" }
            };
            _appointmentServiceMock.Setup(s => s.GetClaimsAssignees(request)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetClaimsAssignee(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualData = Assert.IsAssignableFrom<List<ClaimsAssigneeResponse>>(okResult.Value);
            Assert.Equal(3, actualData.Count);
            Assert.Equal("Unassigned", actualData[0].Name);
            Assert.Equal("John Doe", actualData[1].Name);
        }

        [Fact]
        public async Task GetClaimsAssignee_Returns502_WhenServiceReturnsNull()
        {
            // Arrange
            var request = new ClaimFilterGetModel { AccountInfoId = 1 };
            _appointmentServiceMock.Setup(s => s.GetClaimsAssignees(request)).ReturnsAsync((List<ClaimsAssigneeResponse>)null);

            // Act
            var result = await _controller.GetClaimsAssignee(request);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(502, statusResult.StatusCode);
            Assert.Equal("BH API returned no data", statusResult.Value);
        }

        [Fact]
        public async Task GetClaimsAssignee_ReturnsBadRequest_WhenExceptionThrown()
        {
            // Arrange
            var request = new ClaimFilterGetModel { AccountInfoId = 1 };
            _appointmentServiceMock.Setup(s => s.GetClaimsAssignees(It.IsAny<ClaimFilterGetModel>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GetClaimsAssignee(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Service error", badRequest.Value);
        }

        [Fact]
        public async Task GetClaimsAssignee_LogsError_WhenExceptionThrown()
        {
            // Arrange
            var request = new ClaimFilterGetModel { AccountInfoId = 1, Tab = 0, SearchValue = "" };
            _appointmentServiceMock
                .Setup(s => s.GetClaimsAssignees(It.IsAny<ClaimFilterGetModel>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            await _controller.GetClaimsAssignee(request);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString()!.Contains("Failed to get claim assignees")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetForClaim_ReturnsOk_WhenServiceSucceeds()
        {
            // Arrange
            var model = new IdWithUserInfo { AccountInfoId = 1, MemberId = 2, Id = 3 };
            var expected = new List<AppointmentModel>();
            _appointmentServiceMock.Setup(s => s.GetForClaim(1, 2, 3)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetForClaim(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GetForClaim_ReturnsBadRequest_WhenServiceThrowsException()
        {
            // Arrange
            var model = new IdWithUserInfo { AccountInfoId = 1, MemberId = 2, Id = 3 };
            var exceptionMessage = "Service failure";

            _appointmentServiceMock
                .Setup(s => s.GetForClaim(1, 2, 3))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _controller.GetForClaim(model);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(exceptionMessage, badRequest.Value);
        }

        [Fact]
        public async Task GetForClaim_LogsError_WhenExceptionOccurs()
        {
            // Arrange
            var model = new IdWithUserInfo { AccountInfoId = 1, MemberId = 2, Id = 3 };

            _appointmentServiceMock
                .Setup(s => s.GetForClaim(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            await _controller.GetForClaim(model);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to get appointment for claim")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetForClaim_CallsService_WithCorrectParameters()
        {
            // Arrange
            var model = new IdWithUserInfo { AccountInfoId = 10, MemberId = 20, Id = 30 };

            _appointmentServiceMock
                .Setup(s => s.GetForClaim(10, 20, 30))
                .ReturnsAsync(new List<AppointmentModel>());

            // Act
            await _controller.GetForClaim(model);

            // Assert
            _appointmentServiceMock.Verify(
                s => s.GetForClaim(10, 20, 30),
                Times.Once);
        }

        [Fact]
        public async Task GetForClaim_ReturnsOk_WithEmptyList()
        {
            // Arrange
            var model = new IdWithUserInfo { AccountInfoId = 1, MemberId = 2, Id = 3 };
            var emptyList = new List<AppointmentModel>();

            _appointmentServiceMock
                .Setup(s => s.GetForClaim(1, 2, 3))
                .ReturnsAsync(emptyList);

            // Act
            var result = await _controller.GetForClaim(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            Assert.Empty((List<AppointmentModel>)okResult.Value);
        }

        [Fact]
        public async Task GetForClaim_ReturnsOk_WhenServiceReturnsNull()
        {
            // Arrange
            var model = new IdWithUserInfo { AccountInfoId = 1, MemberId = 2, Id = 3 };

            _appointmentServiceMock
                .Setup(s => s.GetForClaim(1, 2, 3))
                .ReturnsAsync((List<AppointmentModel>)null);

            // Act
            var result = await _controller.GetForClaim(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Null(okResult.Value);
        }

        [Fact]
        public async Task GetFor_ReturnsBadRequest_WhenServiceThrowsException()
        {
            // Arrange
            var request = new AppointmentGetRequest
            {
                AccountInfoId = 1,
                MemberId = 2,
                ClaimId = 3,
                ClientId = 4,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow,
                LocationId = 5
            };

            var exceptionMessage = "Service error";

            _appointmentServiceMock
                .Setup(s => s.GetFor(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>()))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _controller.GetFor(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(exceptionMessage, badRequest.Value);
        }

        [Fact]
        public async Task GetFor_LogsError_WhenExceptionOccurs()
        {
            // Arrange
            var request = new AppointmentGetRequest
            {
                AccountInfoId = 1,
                MemberId = 2,
                ClaimId = 3
            };

            _appointmentServiceMock
                .Setup(s => s.GetFor(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            await _controller.GetFor(request);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString().Contains("AppointmentController.GetFor -Error getting appointments")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetFor_CallsService_WithCorrectParameters()
        {
            // Arrange
            var request = new AppointmentGetRequest
            {
                AccountInfoId = 10,
                MemberId = 20,
                ClaimId = 30,
                ClientId = 40,
                StartDate = DateTime.UtcNow.AddDays(-7),
                EndDate = DateTime.UtcNow,
                LocationId = 50
            };

            _appointmentServiceMock
                .Setup(s => s.GetFor(
                    10, 20, 30, 40, 20,
                    request.StartDate, request.EndDate, 50))
                .ReturnsAsync(new List<AppointmentModel>());

            // Act
            await _controller.GetFor(request);

            // Assert
            _appointmentServiceMock.Verify(
                s => s.GetFor(
                    10, 20, 30, 40, 20,
                    request.StartDate, request.EndDate, 50),
                Times.Once);
        }

        [Fact]
        public async Task GetFor_ReturnsOk_WithEmptyList()
        {
            // Arrange
            var request = new AppointmentGetRequest
            {
                AccountInfoId = 1,
                MemberId = 2,
                ClaimId = 3
            };

            _appointmentServiceMock
                .Setup(s => s.GetFor(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>()))
                .ReturnsAsync(new List<AppointmentModel>());

            // Act
            var result = await _controller.GetFor(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Empty((List<AppointmentModel>)okResult.Value);
        }

        [Fact]
        public async Task GetFor_ReturnsOk_WhenServiceReturnsNull()
        {
            // Arrange
            var request = new AppointmentGetRequest
            {
                AccountInfoId = 1,
                MemberId = 2
            };

            _appointmentServiceMock
                .Setup(s => s.GetFor(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>()))
                .ReturnsAsync((List<AppointmentModel>)null);

            // Act
            var result = await _controller.GetFor(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Null(okResult.Value);
        }

        [Fact]
        public async Task LinkAppointments_ReturnsBadRequest_WhenServiceThrowsException()
        {
            // Arrange
            var request = new LinkAppointmentsRequest
            {
                AccountInfoId = 1,
                MemberId = 2,
                ClaimId = 3,
                AppointmentIds = new List<int> { 10 }
            };

            var exceptionMessage = "Linking failed";

            _appointmentServiceMock
                .Setup(s => s.LinkAppointments(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<List<int>>()))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _controller.LinkAppointments(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(exceptionMessage, badRequest.Value);
        }

        [Fact]
        public async Task LinkAppointments_CallsService_WithCorrectParameters()
        {
            // Arrange
            var request = new LinkAppointmentsRequest
            {
                AccountInfoId = 10,
                MemberId = 20,
                ClaimId = 30,
                AppointmentIds = new List<int> { 100, 200 }
            };

            _appointmentServiceMock
                .Setup(s => s.LinkAppointments(10, 20, 30, request.AppointmentIds))
                .ReturnsAsync((true, null, null));

            // Act
            await _controller.LinkAppointments(request);

            // Assert
            _appointmentServiceMock.Verify(
                s => s.LinkAppointments(10, 20, 30, request.AppointmentIds),
                Times.Once);
        }

        [Fact]
        public async Task LinkAppointments_ReturnsBadRequest_WhenAppointmentIdsIsNull_AndServiceThrows()
        {
            // Arrange
            var request = new LinkAppointmentsRequest
            {
                AccountInfoId = 1,
                MemberId = 2,
                ClaimId = 3,
                AppointmentIds = null
            };

            _appointmentServiceMock
                .Setup(s => s.LinkAppointments(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    null))
                .ThrowsAsync(new Exception("AppointmentIds cannot be null"));

            // Act
            var result = await _controller.LinkAppointments(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("AppointmentIds cannot be null", badRequest.Value);
        }

        [Fact]
        public async Task UnLinkAppointments_CallsService_WithCorrectParameters()
        {
            // Arrange
            var request = new LinkAppointmentsRequest
            {
                AccountInfoId = 10,
                MemberId = 20,
                ClaimId = 30,
                AppointmentIds = new List<int> { 100 }
            };

            _appointmentServiceMock
                .Setup(s => s.UnLinkAppointments(10, 20, 30, request.AppointmentIds))
                .ReturnsAsync((true, null, null));

            // Act
            await _controller.UnLinkAppointments(request);

            // Assert
            _appointmentServiceMock.Verify(
                s => s.UnLinkAppointments(10, 20, 30, request.AppointmentIds),
                Times.Once);
        }

        [Fact]
        public async Task UnLinkAppointments_ThrowsException_WhenServiceThrows()
        {
            // Arrange
            var request = new LinkAppointmentsRequest
            {
                AccountInfoId = 1,
                MemberId = 2,
                ClaimId = 3,
                AppointmentIds = new List<int> { 10 }
            };

            _appointmentServiceMock
                .Setup(s => s.UnLinkAppointments(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<List<int>>()))
                .ThrowsAsync(new Exception("Unlink failed"));

            // Act
            var result = await _controller.UnLinkAppointments(request);

            // Assert - BadRequest
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Unlink failed", badRequestResult.Value);

            // Assert - LogError called
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString().Contains("AppointmentController.UnLinkAppointments -Failed to unlink appointments")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task SyncClaim_CallsService_WithCorrectParameters()
        {
            // Arrange
            var request = new AutoClaimRequestModel
            {
                appointmentId = 10,
                accountId = 20
            };

            _claimSyncServiceMock
                .Setup(s => s.SyncClaimAsync(10, 20, false))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.SyncClaim(request);

            // Assert
            _claimSyncServiceMock.Verify(
                s => s.SyncClaimAsync(10, 20, false),
                Times.Once);
        }

        [Fact]
        public async Task SyncClaim_ReturnsBadRequest_WhenServiceThrowsException()
        {
            // Arrange
            var request = new AutoClaimRequestModel
            {
                appointmentId = 1,
                accountId = 2
            };

            var exceptionMessage = "Sync failed";

            _claimSyncServiceMock
                .Setup(s => s.SyncClaimAsync(1, 2, false))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _controller.SyncClaim(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(exceptionMessage, badRequest.Value);
        }

        [Fact]
        public async Task SyncClaim_LogsError_WhenExceptionOccurs()
        {
            // Arrange
            var request = new AutoClaimRequestModel
            {
                appointmentId = 5,
                accountId = 6
            };

            _claimSyncServiceMock
                .Setup(s => s.SyncClaimAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            await _controller.SyncClaim(request);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString().Contains("Error creating claim for appointment")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task SyncClaim_ReturnsOk_WhenIdsAreZero_AndServiceSucceeds()
        {
            // Arrange
            var request = new AutoClaimRequestModel
            {
                appointmentId = 0,
                accountId = 0
            };

            _claimSyncServiceMock
                .Setup(s => s.SyncClaimAsync(0, 0, false))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.SyncClaim(request);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task SyncClaim_DoesNotLogError_WhenServiceSucceeds()
        {
            // Arrange
            var request = new AutoClaimRequestModel
            {
                appointmentId = 1,
                accountId = 2
            };

            _claimSyncServiceMock
                .Setup(s => s.SyncClaimAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.SyncClaim(request);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task SyncClaimDelete_CallsService_WithCorrectAppointmentId()
        {
            // Arrange
            var appointmentId = 456;

            _claimSyncServiceMock
                .Setup(s => s.SyncClaimDeleteAsync(appointmentId))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.SyncClaimDelete(appointmentId);

            // Assert
            _claimSyncServiceMock.Verify(
                s => s.SyncClaimDeleteAsync(appointmentId),
                Times.Once);
        }

        [Fact]
        public async Task SyncClaimDelete_ThrowsException_WhenServiceThrows()
        {
            // Arrange
            var appointmentId = 999;

            _claimSyncServiceMock
                .Setup(s => s.SyncClaimDeleteAsync(appointmentId))
                .ThrowsAsync(new Exception("Delete failed"));

            // Act
            var result = await _controller.SyncClaimDelete(appointmentId);

            // Assert - BadRequest
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Delete failed", badRequestResult.Value);

            // Assert - LogError
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString().Contains("AppointmentController.SyncClaimDelete -Failed to delete synced claim for appointment")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task SyncClaimDelete_ReturnsOk_WhenAppointmentIdIsZero()
        {
            // Arrange
            var appointmentId = 0;

            _claimSyncServiceMock
                .Setup(s => s.SyncClaimDeleteAsync(0))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.SyncClaimDelete(appointmentId);

            // Assert
            Assert.IsType<OkResult>(result);
        }


        [Fact]
        public async Task GetForClaim_ReturnsBadRequest_WhenExceptionThrown()
        {
            // Arrange
            var model = new IdWithUserInfo { AccountInfoId = 1, MemberId = 2, Id = 3 };
            _appointmentServiceMock.Setup(s => s.GetForClaim(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("error"));

            // Act
            var result = await _controller.GetForClaim(model);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("error", badRequest.Value);
        }

        [Fact]
        public async Task GetFor_ReturnsOk_WhenServiceSucceeds()
        {
            // Arrange
            var req = new AppointmentGetRequest
            {
                AccountInfoId = 1,
                MemberId = 2,
                ClaimId = 3,
                ClientId = 4,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1),
                LocationId = 5
            };
            var expected = new List<AppointmentModel>();
            _appointmentServiceMock.Setup(s => s.GetFor(
                req.AccountInfoId, req.MemberId, req.ClaimId, req.ClientId, req.MemberId, req.StartDate, req.EndDate, req.LocationId))
                .ReturnsAsync(expected);

            // Act
            var result = await _controller.GetFor(req);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GetFor_ReturnsBadRequest_WhenExceptionThrown()
        {
            // Arrange
            var req = new AppointmentGetRequest
            {
                AccountInfoId = 1,
                MemberId = 2,
                ClaimId = 3,
                ClientId = 4,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1),
                LocationId = 5
            };
            _appointmentServiceMock.Setup(s => s.GetFor(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>()))
                .ThrowsAsync(new Exception("fail"));

            // Act
            var result = await _controller.GetFor(req);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("fail", badRequest.Value);
        }

        [Fact]
        public async Task LinkAppointments_ReturnsOk_WhenServiceSucceeds()
        {
            // Arrange
            var req = new LinkAppointmentsRequest
            {
                AccountInfoId = 1,
                MemberId = 2,
                ClaimId = 3,
                AppointmentIds = new List<int> { 10, 20 }
            };
            var tuple = (true, DateTime.UtcNow, DateTime.UtcNow.AddHours(1));
            _appointmentServiceMock.Setup(s => s.LinkAppointments(1, 2, 3, req.AppointmentIds)).ReturnsAsync(tuple);

            // Act
            var result = await _controller.LinkAppointments(req);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult);
            var response = okResult.Value;

            // Use reflection to get property values
            var startDate = response.GetType().GetProperty("StartDate")?.GetValue(response);
            var endDate = response.GetType().GetProperty("EndDate")?.GetValue(response);

            Assert.Equal(tuple.Item2, (DateTime)startDate);
            Assert.Equal(tuple.Item3, (DateTime)endDate);
        }

        [Fact]
        public async Task LinkAppointments_ReturnsBadRequest_WhenExceptionThrown()
        {
            // Arrange
            var req = new LinkAppointmentsRequest
            {
                AccountInfoId = 1,
                MemberId = 2,
                ClaimId = 3,
                AppointmentIds = new List<int> { 10, 20 }
            };
            _appointmentServiceMock.Setup(s => s.LinkAppointments(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<int>>()))
                .ThrowsAsync(new Exception("link error"));

            // Act
            var result = await _controller.LinkAppointments(req);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("link error", badRequest.Value);
        }

        [Fact]
        public async Task UnLinkAppointments_ReturnsOk_WhenServiceSucceeds()
        {
            // Arrange
            var req = new LinkAppointmentsRequest
            {
                AccountInfoId = 1,
                MemberId = 2,
                ClaimId = 3,
                AppointmentIds = new List<int> { 10, 20 }
            };
            var tuple = (false, DateTime.UtcNow, DateTime.UtcNow.AddHours(1));
            _appointmentServiceMock.Setup(s => s.UnLinkAppointments(1, 2, 3, req.AppointmentIds)).ReturnsAsync(tuple);

            // Act
            var result = await _controller.UnLinkAppointments(req);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult);
            var response = okResult.Value;

            // Use reflection to get property values
            var startDate = response.GetType().GetProperty("StartDate")?.GetValue(response);
            var endDate = response.GetType().GetProperty("EndDate")?.GetValue(response);

            Assert.Equal(tuple.Item2, (DateTime)startDate);
            Assert.Equal(tuple.Item3, (DateTime)endDate);
        }

        [Fact]
        public async Task SyncClaim_ReturnsOk_WhenServiceSucceeds()
        {
            // Arrange
            var req = new AutoClaimRequestModel { appointmentId = 1, accountId = 2 };
            _claimSyncServiceMock.Setup(s => s.SyncClaimAsync(1, 2, false)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.SyncClaim(req);

            // Assert
            Assert.IsType<OkResult>(result);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SyncClaim_ReturnsBadRequest_WhenExceptionThrown()
        {
            // Arrange
            var req = new AutoClaimRequestModel { appointmentId = 1, accountId = 2 };
            _claimSyncServiceMock.Setup(s => s.SyncClaimAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("sync error"));

            // Act
            var result = await _controller.SyncClaim(req);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("sync error", badRequest.Value);
        }

        [Fact]
        public async Task SyncClaimDelete_ReturnsOk_WhenServiceSucceeds()
        {
            // Arrange
            int appointmentId = 5;
            _claimSyncServiceMock.Setup(s => s.SyncClaimDeleteAsync(appointmentId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.SyncClaimDelete(appointmentId);

            // Assert
            Assert.IsType<OkResult>(result);
            Assert.NotNull(result);
        }
    }
}