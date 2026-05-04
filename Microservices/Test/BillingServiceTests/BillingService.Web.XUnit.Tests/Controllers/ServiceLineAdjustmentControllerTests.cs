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
using System.Threading.Tasks;
using Xunit;

namespace BillingService.Web.XUnit.Tests.Controllers
{
    public class ServiceLineAdjustmentControllerTests
    {
        private readonly Mock<IPaymentServiceLineAdjustmentService> _serviceMock;
        private readonly ServiceLineAdjustmentController _controller;
        private readonly Mock<ILogger<ServiceLineAdjustmentController>> _mockLogger;

        public ServiceLineAdjustmentControllerTests()
        {
            _serviceMock = new Mock<IPaymentServiceLineAdjustmentService>();
            _mockLogger = new Mock<ILogger<ServiceLineAdjustmentController>>();
            _controller = new ServiceLineAdjustmentController(_serviceMock.Object, _mockLogger.Object);
        }

        private List<PaymentClaimServiceLineAdjustmentModel> CreateSampleAdjustments() =>
            new List<PaymentClaimServiceLineAdjustmentModel> { new PaymentClaimServiceLineAdjustmentModel() };

        // GetServiceLineAdjustments Tests
        [Fact]
        public async Task GetServiceLineAdjustments_ShouldReturnOkResult_WhenServiceReturnsData()
        {
            var serviceLineId = 1;
            var expected = CreateSampleAdjustments();

            _serviceMock.Setup(s => s.GetPaymentServiceLineAdjustments(serviceLineId))
                        .ReturnsAsync(expected);

            var result = await _controller.GetServiceLineAdjustments(serviceLineId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GetServiceLineAdjustments_ShouldReturnBadRequest_WhenServiceThrowsException()
        {
            var serviceLineId = 1;
            var errorMessage = "Database error";

            _serviceMock.Setup(s => s.GetPaymentServiceLineAdjustments(serviceLineId))
                        .ThrowsAsync(new Exception(errorMessage));

            var result = await _controller.GetServiceLineAdjustments(serviceLineId);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(errorMessage, badRequest.Value);
        }

        // GetServiceLineAdjustmentsByCharge Tests
        [Fact]
        public async Task GetServiceLineAdjustmentsByCharge_ShouldReturnOkResult_WhenServiceReturnsData()
        {
            var model = new GetChargeDetailsModel();
            var expected = CreateSampleAdjustments();

            _serviceMock.Setup(s => s.GetPaymentServiceLineAdjustmentsByCharge(model))
                        .ReturnsAsync(expected);

            var result = await _controller.GetServiceLineAdjustmentsByCharge(model);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        // AddPaymentServiceLineAdjustments Tests
        [Fact]
        public async Task AddPaymentServiceLineAdjustments_ShouldReturnOkResult_WhenServiceReturnsData()
        {
            var model = new AddOrEditAdjustmentModel { ServiceLineId = 1 };
            var expected = CreateSampleAdjustments();

            _serviceMock.Setup(s => s.AddPaymentServiceLineAdjustmentsAsync(model))
                        .ReturnsAsync(expected);

            var result = await _controller.AddPaymentServiceLineAdjustments(model);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        // DeleteServiceLineAdjustments Tests
        [Fact]
        public async Task DeleteServiceLineAdjustments_ShouldReturnOkResult_WhenServiceCompletes()
        {
            var model = new IdsWithUserInfo { Ids = new[] { 1, 2 } };

            _serviceMock.Setup(s => s.DeleteServiceLineAdjustmentsAsync(model))
                        .Returns(Task.CompletedTask);

            var result = await _controller.DeleteServiceLineAdjustments(model);

            Assert.IsType<OkResult>(result);
        }

        // UpdateServiceLineAdjustments Tests
        [Fact]
        public async Task UpdateServiceLineAdjustments_ShouldReturnOkResult_WhenServiceReturnsData()
        {
            var model = new AddOrEditAdjustmentModel { ServiceLineId = 1 };
            var expected = CreateSampleAdjustments();

            _serviceMock.Setup(s => s.UpdateServiceLineAdjustmentsAsync(model))
                        .ReturnsAsync(expected);

            var result = await _controller.UpdateServiceLineAdjustments(model);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        // GetAdjustmentReasonDescriptions Tests
        [Fact]
        public async Task GetAdjustmentReasonDescriptions_ShouldReturnOkResult_WhenServiceReturnsData()
        {
            var codes = "PR1,CO45";
            var expected = new List<AdjustmentReasonCodes>
            {
                new AdjustmentReasonCodes { ReasonCode = "PR1", Description = "Patient Responsibility" }
            };

            _serviceMock.Setup(s => s.GetAdjustmentReasonDescriptionsAsync(codes))
                        .ReturnsAsync(expected);

            var result = await _controller.GetAdjustmentReasonDescriptions(codes);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        // ReapplyPRAdjustmentsAfterSecondaryBilling Tests
        [Fact]
        public async Task ReapplyPRAdjustmentsAfterSecondaryBilling_ShouldReturnOkResult_WhenServiceCompletes()
        {
            var claimId = 1;

            _serviceMock.Setup(s => s.ReapplyPRAdjustmentsAfterSecondaryBillingAsync(claimId))
                        .Returns(Task.CompletedTask);

            var result = await _controller.ReapplyPRAdjustmentsAfterSecondaryBilling(claimId);

            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task ReapplyPRAdjustmentsAfterSecondaryBilling_ShouldReturnBadRequest_WhenServiceThrowsException()
        {
            var claimId = 1;
            var errorMessage = "Reapply failed";

            _serviceMock.Setup(s => s.ReapplyPRAdjustmentsAfterSecondaryBillingAsync(claimId))
                        .ThrowsAsync(new Exception(errorMessage));

            var result = await _controller.ReapplyPRAdjustmentsAfterSecondaryBilling(claimId);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(errorMessage, badRequest.Value);
        }
    }
}
