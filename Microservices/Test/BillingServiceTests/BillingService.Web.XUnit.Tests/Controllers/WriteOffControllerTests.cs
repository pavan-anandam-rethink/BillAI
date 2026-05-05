using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.Claims.WriteOff;
using BillingService.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.Web.XUnit.Tests.Controllers
{
    public class WriteOffControllerTests
    {
        private readonly Mock<IWriteOffService> _writeOffServiceMock;
        private readonly WriteOffController _controller;
        private readonly Mock<ILogger<WriteOffController>> _loggerMock;

        public WriteOffControllerTests()
        {
            _writeOffServiceMock = new Mock<IWriteOffService>();
            _loggerMock = new Mock<ILogger<WriteOffController>>();
            _controller = new WriteOffController(_writeOffServiceMock.Object,_loggerMock.Object);
        }

        [Fact]
        public async Task AddWriteOff_ReturnsOk_WhenServiceSucceeds()
        {
            // Arrange
            var model = new WriteOffClaimModelWithUserInfo { ClaimId = 1 };
            var expectedResponse = new AddWriteOffResponseModel();

            _writeOffServiceMock
                .Setup(s => s.AddAsync(model))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.AddWriteOff(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResponse, okResult.Value);
        }

        [Fact]
        public async Task AddWriteOff_ReturnsBadRequest_WhenServiceThrowsException()
        {
            // Arrange
            var model = new WriteOffClaimModelWithUserInfo { ClaimId = 1 };
            _writeOffServiceMock
                .Setup(s => s.AddAsync(model))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.AddWriteOff(model);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Database error", badRequestResult.Value);
        }

        [Fact]
        public async Task GetChargeEntryWriteOffsByChargeId_ReturnsOk()
        {
            // Arrange
            var model = new GetChargeEntryWriteOffModel { Id = 1, IsServiceLineId = false };
            var expectedResponse = new List<ClaimChargeEntryWriteOffModel>
            {
                new ClaimChargeEntryWriteOffModel()
            };

            _writeOffServiceMock
                .Setup(s => s.GetChargeEntryWriteOffsByChargeIdAsync(model))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetChargeEntryWriteOffsByChargeId(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResponse, okResult.Value);
        }

        [Fact]
        public async Task DeleteChargeEntryWriteOffsByCharge_ReturnsOk()
        {
            // Arrange
            var model = new IdsWithUserInfo { Ids = new[] { 1, 2 } };
            _writeOffServiceMock
                .Setup(s => s.DeleteChargeEntryWriteOffsByChargeIdAsync(model))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteChargeEntryWriteOffsByCharge(model);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task UpdateChargeEntryWriteOffsByChargeId_ReturnsOk_FirstItem()
        {
            // Arrange
            var model = new EditChargeEntryWriteOffModelWithUserInfo();
            var updatedList = new List<ClaimChargeEntryWriteOffModel>
            {
                new ClaimChargeEntryWriteOffModel()
            };

            _writeOffServiceMock
                .Setup(s => s.UpdateChargeEntryWriteOffsByChargeIdAsync(model))
                .ReturnsAsync(updatedList);

            // Act
            var result = await _controller.UpdateChargeEntryWriteOffsByChargeId(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(updatedList.FirstOrDefault(), okResult.Value);
        }

        [Fact]
        public async Task GetReasonCodes_ReturnsOk_WhenServiceSucceeds()
        {
            // Arrange
            var expectedResponse = new List<WriteOffReasonCodDescriptionModel>
            {
                new WriteOffReasonCodDescriptionModel()
            };

            _writeOffServiceMock
                .Setup(s => s.GetReasonCodesAsync())
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetReasonCodes();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResponse, okResult.Value);
        }

        [Fact]
        public async Task GetReasonCodes_ReturnsBadRequest_WhenServiceThrowsException()
        {
            // Arrange
            _writeOffServiceMock
                .Setup(s => s.GetReasonCodesAsync())
                .ThrowsAsync(new Exception("Service failed"));

            // Act
            var result = await _controller.GetReasonCodes();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Service failed", badRequestResult.Value);
        }
    }
}
