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
using System.Text;
using System.Threading.Tasks;
using Xunit;
 
namespace BillingService.XUnit.Integration.Tests
{
    public class WriteOffControllerTest
    {
        private readonly Mock<IWriteOffService> _mockWriteOffService = new();

        private readonly Mock<ILogger<WriteOffController>> _loggerMock=new();

        private WriteOffController CreateController()
        {
            return new WriteOffController(_mockWriteOffService.Object,_loggerMock.Object);
        }

        [Fact]
        public async Task AddWriteOff_ReturnsOk_WhenServiceSucceeds()
        {
            // Arrange
            var model = new WriteOffClaimModelWithUserInfo();
            var expected = new AddWriteOffResponseModel { success = true, errorMsg = "Success" };

            _mockWriteOffService.Setup(s => s.AddAsync(model))
                .ReturnsAsync(expected);

            var controller = CreateController();

            // Act
            var result = await controller.AddWriteOff(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task AddWriteOff_ReturnsBadRequest_WhenServiceThrowsException()
        {
            // Arrange
            var model = new WriteOffClaimModelWithUserInfo();

            _mockWriteOffService.Setup(s => s.AddAsync(model))
                .ThrowsAsync(new Exception("Error occurred"));

            var controller = CreateController();

            // Act
            var result = await controller.AddWriteOff(model);

            // Assert
            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Error occurred", badResult.Value);
        }

        [Fact]
        public async Task GetChargeEntryWriteOffsByChargeId_ReturnsOk_WhenCalled()
        {
            // Arrange
            var model = new GetChargeEntryWriteOffModel();
            var expected = new List<ClaimChargeEntryWriteOffModel>
{
            new ClaimChargeEntryWriteOffModel
            {
                Id = 1,
                WriteOffReasonCodeId = 101,
                WriteOffAmount = 250.75m,
                Description = "Insurance adjustment",
                DateLastModified = DateTime.Now
            },
            new ClaimChargeEntryWriteOffModel
            {
                Id = 2,
                WriteOffReasonCodeId = 102,
                WriteOffAmount = 100.00m,
                Description = "Patient discount",
                DateLastModified = DateTime.Now
            }
           };

            _mockWriteOffService.Setup(s => s.GetChargeEntryWriteOffsByChargeIdAsync(model))
                .ReturnsAsync(expected);

            var controller = CreateController();

            // Act
            var result = await controller.GetChargeEntryWriteOffsByChargeId(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task DeleteChargeEntryWriteOffsByCharge_ReturnsOk_WhenCalled()
        {
            // Arrange
            var model = new IdsWithUserInfo();

            _mockWriteOffService.Setup(s => s.DeleteChargeEntryWriteOffsByChargeIdAsync(model))
                .Returns(Task.CompletedTask);

            var controller = CreateController();

            // Act
            var result = await controller.DeleteChargeEntryWriteOffsByCharge(model);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task UpdateChargeEntryWriteOffsByChargeId_ReturnsOk_WhenServiceSucceeds()
        {
            // Arrange
            var model = new EditChargeEntryWriteOffModelWithUserInfo();

            var expectedList = new List<ClaimChargeEntryWriteOffModel> { new() { Id = 123 } };

            _mockWriteOffService.Setup(s => s.UpdateChargeEntryWriteOffsByChargeIdAsync(model))
                .ReturnsAsync(expectedList);

            var controller = CreateController();

            // Act
            var result = await controller.UpdateChargeEntryWriteOffsByChargeId(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedList.FirstOrDefault(), okResult.Value);
        }

        [Fact]
        public async Task GetReasonCodes_ReturnsOk_WhenServiceSucceeds()
        {
            // Arrange
            var expected = new List<WriteOffReasonCodDescriptionModel>
            {
                new WriteOffReasonCodDescriptionModel { Id = 1, Description = "Test reason one" },
                new WriteOffReasonCodDescriptionModel { Id = 2, Description = "Test reason two" }
            };

            _mockWriteOffService.Setup(s => s.GetReasonCodesAsync())
                .ReturnsAsync(expected);

            var controller = CreateController();

            // Act
            var result = await controller.GetReasonCodes();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GetReasonCodes_ReturnsBadRequest_WhenServiceThrowsException()
        {
            // Arrange
            _mockWriteOffService.Setup(s => s.GetReasonCodesAsync())
                .ThrowsAsync(new Exception("Service failure"));

            var controller = CreateController();

            // Act
            var result = await controller.GetReasonCodes();

            // Assert
            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Service failure", badResult.Value);
        }
    }
}
