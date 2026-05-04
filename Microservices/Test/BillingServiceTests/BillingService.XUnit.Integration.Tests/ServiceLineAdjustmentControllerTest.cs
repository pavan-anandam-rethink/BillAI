using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.PaymentClaimServiceLine;
using BillingService.Domain.Models.PaymentClaimServiceLineAdjustment;
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
    public class ServiceLineAdjustmentControllerTests
    {
        private readonly Mock<IPaymentServiceLineAdjustmentService> _mockService;
        private readonly ServiceLineAdjustmentController _controller;
        private readonly Mock<ILogger<ServiceLineAdjustmentController>> _loggerMock;
        public ServiceLineAdjustmentControllerTests()
        {
            _mockService = new Mock<IPaymentServiceLineAdjustmentService>();
            _loggerMock = new Mock<ILogger<ServiceLineAdjustmentController>>();
            _controller = new ServiceLineAdjustmentController(_mockService.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetServiceLineAdjustments_ReturnsOkResult()
        {
            // Arrange
            int serviceLineId = 1;
            var expectedResult = new List<PaymentClaimServiceLineAdjustmentModel>
            {
                new PaymentClaimServiceLineAdjustmentModel
                {
                    Id = 1,
                    serviceLineId = 1001,
                    PaymentIdentifier = "PMT001",
                    PaymentId = 5001,
                    Amount = 120.50m,
                    isPositive = true,
                    GroupCode = "CO",
                    ReasonCode = "45",
                    Description = "Charge exceeds fee schedule",
                    PostDate = new DateTime(2025, 10, 20)
                }
            };
            _mockService.Setup(s => s.GetPaymentServiceLineAdjustments(serviceLineId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetServiceLineAdjustments(serviceLineId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResult, okResult.Value);
        }

        [Fact]
        public async Task GetServiceLineAdjustments_ThrowsException_ReturnsBadRequest()
        {
            // Arrange
            int serviceLineId = 1;
            _mockService.Setup(s => s.GetPaymentServiceLineAdjustments(serviceLineId)).ThrowsAsync(new Exception("Error"));

            // Act
            var result = await _controller.GetServiceLineAdjustments(serviceLineId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Error", badRequestResult.Value);
        }

        [Fact]
        public async Task AddPaymentServiceLineAdjustments_ReturnsOkResult()
        {
            // Arrange
            var model = new AddOrEditAdjustmentModel
            {
                AccountInfoId = 101,
                MemberId = 1,
                ClaimId = 5001,
                ServiceLineId = 2001,
                AdjustmentDetails = new List<AdjustmentDetailsModel>
                {
                    new AdjustmentDetailsModel
                    {
                        AdjustmentId = 1,
                        Amount = 100.00m,
                        isPositive = true,
                        GroupCode = "CO",
                        ReasonCode = "45"
                    }
                }
            };
            var expectedResult = new List<PaymentClaimServiceLineAdjustmentModel>
            {
                new PaymentClaimServiceLineAdjustmentModel
                {
                    Id = 1,
                    serviceLineId = 1001,
                    PaymentIdentifier = "PMT001",
                    PaymentId = 5001,
                    Amount = 120.50m,
                    isPositive = true,
                    GroupCode = "CO",
                    ReasonCode = "45",
                    Description = "Charge exceeds fee schedule",
                    PostDate = new DateTime(2025, 10, 20)
                }
            };
            _mockService.Setup(s => s.AddPaymentServiceLineAdjustmentsAsync(model)).ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.AddPaymentServiceLineAdjustments(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResult, okResult.Value);
        }

        [Fact]
        public async Task DeleteServiceLineAdjustments_ReturnsOkResult()
        {
            // Arrange
            var model = new IdsWithUserInfo
            {
                AccountInfoId = 101,
                MemberId = 1,
                Ids = new int[] { 1001, 1002, 1003, 1004 }
            };

            // Act
            var result = await _controller.DeleteServiceLineAdjustments(model);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            Assert.NotNull(okResult);
        }

        [Fact]
        public async Task UpdateServiceLineAdjustments_ReturnsOkResult()
        {
            // Arrange
            var model = new AddOrEditAdjustmentModel
            {
                AccountInfoId = 101,
                MemberId = 1,
                ClaimId = 5001,
                ServiceLineId = 2001,
                AdjustmentDetails = new List<AdjustmentDetailsModel>
                {
                    new AdjustmentDetailsModel
                    {
                        AdjustmentId = 1,
                        Amount = 100.00m,
                        isPositive = true,
                        GroupCode = "CO",
                        ReasonCode = "45"
                    }
                }
            };
            var expectedResult = new List<PaymentClaimServiceLineAdjustmentModel>
            {
                new PaymentClaimServiceLineAdjustmentModel
                {
                    Id = 1,
                    serviceLineId = 1001,
                    PaymentIdentifier = "PMT001",
                    PaymentId = 5001,
                    Amount = 120.50m,
                    isPositive = true,
                    GroupCode = "CO",
                    ReasonCode = "45",
                    Description = "Charge exceeds fee schedule",
                    PostDate = new DateTime(2025, 10, 20)
                }
            };
            _mockService.Setup(s => s.UpdateServiceLineAdjustmentsAsync(model)).ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.UpdateServiceLineAdjustments(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResult, okResult.Value);
        }

        [Fact]
        public async Task GetAdjustmentReasonDescriptions_ReturnsOkResult()
        {
            // Arrange
            string codes = "code1,code2";
            var expectedResult = new List<AdjustmentReasonCodes>
            {
                new AdjustmentReasonCodes
                {
                    ReasonCode = "1",
                    Description = "Deductible amount",
                    IsDefault = false
                }
            };
            _mockService.Setup(s => s.GetAdjustmentReasonDescriptionsAsync(codes)).ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetAdjustmentReasonDescriptions(codes);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResult, okResult.Value);
        }

        [Fact]
        public async Task ReapplyPRAdjustmentsAfterSecondaryBilling_ReturnsOkResult()
        {
            // Arrange
            int claimId = 1;

            // Act
            var result = await _controller.ReapplyPRAdjustmentsAfterSecondaryBilling(claimId);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            Assert.NotNull(okResult);
        }
    }
}
