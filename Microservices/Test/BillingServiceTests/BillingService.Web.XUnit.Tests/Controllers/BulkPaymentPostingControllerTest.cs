using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models.BulkPaymentPosting;
using BillingService.Domain.Models.PaymentClaims;
using BillingService.Domain.Models.PaymentClaimServiceLine;
using BillingService.Domain.Models.PaymentClaimServiceLineAdjustment;
using BillingService.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BillingService.Web.XUnit.Tests.Controllers
{
    public class BulkPaymentPostingControllerTest
    {
        private readonly Mock<IPaymentClaimService> _mockpaymentClaimService;
        private readonly Mock<IBulkPaymentPostingService> _mockBulkPaymentPostingService;
        private readonly Mock<IFunderService> _mockFunderService;
        private readonly Mock<IPaymentServiceLineAdjustmentService> _mockPaymentServiceLineAdjustmentService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<AppointmentController>> _logger;

        private readonly BulkPaymentPostingController _controller;


        public BulkPaymentPostingControllerTest()
        {
            _mockpaymentClaimService = new Mock<IPaymentClaimService>();
            _mockBulkPaymentPostingService = new Mock<IBulkPaymentPostingService>();
            _mockFunderService = new Mock<IFunderService>();
            _mockPaymentServiceLineAdjustmentService = new Mock<IPaymentServiceLineAdjustmentService>();
            _mockMapper = new Mock<IMapper>();
            _logger = new Mock<ILogger<AppointmentController>>();

            _controller = new BulkPaymentPostingController(_mockpaymentClaimService.Object, _mockBulkPaymentPostingService.Object, _mockFunderService.Object, _mockPaymentServiceLineAdjustmentService.Object, _mockMapper.Object, _logger.Object);
        }

        [Fact]
        public async Task GetAllPaymentsForPosting_ReturnsPagedResult_WithPayments()
        {
            // Arrange
            var requestModel = new BulkPaymentPostingRequestModel
            {
                AccountInfoId = 18421,
                MemberId = 105815,
                Ids = new int[] { 14276, 14279, 14280 },
                Skip = 0,
                Take = 1
            };

            var mockResponse = new List<BulkPaymentResponseModel>
            {
                new BulkPaymentResponseModel
                {
                    Id = 21868,
                    ClaimId = 2761,
                    ClaimIdentifier = "250620-09QBA-2",
                    Adjustments = new List<PaymentClaimServiceLineAdjustmentModel>
                    {
                        new PaymentClaimServiceLineAdjustmentModel
                        {
                            Id = 529870,
                            serviceLineId = 21868,
                            PaymentId = 14278,
                            Amount = 8.00m,
                            isPositive = false,
                            GroupCode = "PR",
                            ReasonCode = "201",
                            PostDate = DateTime.Parse("2025-06-23T03:15:22.63")
                        },
                        new PaymentClaimServiceLineAdjustmentModel
                        {
                            Id = 529871,
                            serviceLineId = 21868,
                            PaymentId = 14278,
                            Amount = 67.00m,
                            isPositive = false,
                            GroupCode = "CO",
                            ReasonCode = "45",
                            PostDate = DateTime.Parse("2025-06-23T03:15:22.63")
                        }
                    }
                }
            };

            _mockBulkPaymentPostingService
                .Setup(service => service.GetAllPayments(requestModel))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _controller.GetAllPaymentsForPosting(requestModel);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<BulkPaymentResponseModel>>(okResult.Value);

            Assert.Single(returnValue); 
            _mockBulkPaymentPostingService.Verify(service => service.GetAllPayments(requestModel), Times.Once);
        }

        [Fact]
        public async Task GetAllPaymentsForPosting_ReturnsEmptyList_WhenNoPayments()
        {
            // Arrange
            var requestModel = new BulkPaymentPostingRequestModel
            {
                Skip = 0,
                Take = 10
            };

            var emptyResponse = new List<BulkPaymentResponseModel>();

            _mockBulkPaymentPostingService
                .Setup(s => s.GetAllPayments(requestModel))
                .ReturnsAsync(emptyResponse);

            // Act
            var result = await _controller.GetAllPaymentsForPosting(requestModel);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<BulkPaymentResponseModel>>(okResult.Value);
            Assert.Empty(returnValue);

            _mockBulkPaymentPostingService.Verify(s => s.GetAllPayments(requestModel), Times.Once);
        }

        [Fact]
        public async Task AddOrUpdateBulkPaymentPostingAdjustments_ReturnsEmptyList_WhenAllSuccess()
        {
            // Arrange
            List<AddOrEditAdjustmentModelForBulkPosting> adjustments = GetAdjustments();

            _mockPaymentServiceLineAdjustmentService
            .Setup(s => s.DeleteServiceLineAdjustmentsAsync(It.IsAny<AddOrEditAdjustmentModelForBulkPosting>()))
            .Returns(Task.CompletedTask);

            _mockPaymentServiceLineAdjustmentService
                .Setup(s => s.AddPaymentServiceLineAdjustmentsAsync(It.IsAny<AddOrEditAdjustmentModel>()))
                .ReturnsAsync(new List<PaymentClaimServiceLineAdjustmentModel>());

            _mockpaymentClaimService
                .Setup(s => s.UpdatePaymentClaimServiceLineAmountsAsync(It.IsAny<UpdatePaymentServiceLineAmountsModelWithUserInfo>()))
                .Returns(Task.CompletedTask);

            _mockMapper.Setup(x => x.Map<List<AddOrEditAdjustmentModel>>(It.IsAny<List<AddOrEditAdjustmentModelForBulkPosting>>()))
                .Returns((List<AddOrEditAdjustmentModelForBulkPosting> source) => source.Select(a => new AddOrEditAdjustmentModel
                {
                    AccountInfoId = a.AccountInfoId,
                    MemberId = a.MemberId,
                    ClaimId = a.ClaimId,
                    ServiceLineId = a.ServiceLineId,
                    AdjustmentDetails = a.AdjustmentDetails.Select(ad => new AdjustmentDetailsModel
                    {
                        AdjustmentId = ad.AdjustmentId,
                        Amount = ad.Amount,
                        isPositive = ad.isPositive,
                        GroupCode = ad.GroupCode,
                        ReasonCode = ad.ReasonCode
                    }).ToList()
                }).ToList());

            // Act
            var result = await _controller.AddOrUpdateBulkPaymentPostingAdjustments(adjustments);

            // Assert
            Assert.IsType<OkObjectResult>(result);

        }

        [Fact]
        public async Task AddOrUpdateBulkPaymentPostingAdjustments_DeleteThrowsException_LogsErrorAndContinues()
        {
            // Arrange
            List<AddOrEditAdjustmentModelForBulkPosting> adjustments = GetAdjustments();

            _mockPaymentServiceLineAdjustmentService
                .SetupSequence(s => s.DeleteServiceLineAdjustmentsAsync(It.IsAny<AddOrEditAdjustmentModelForBulkPosting>()))
                .ThrowsAsync(new Exception("Delete failed"))
                .Returns(Task.CompletedTask);

            _mockPaymentServiceLineAdjustmentService
                .Setup(s => s.AddPaymentServiceLineAdjustmentsAsync(It.IsAny<AddOrEditAdjustmentModel>()))
                .ReturnsAsync(new List<PaymentClaimServiceLineAdjustmentModel>());

            _mockpaymentClaimService
                .Setup(s => s.UpdatePaymentClaimServiceLineAmountsAsync(It.IsAny<UpdatePaymentServiceLineAmountsModelWithUserInfo>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AddOrUpdateBulkPaymentPostingAdjustments(adjustments);

            // Assert
           Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task AddOrUpdateBulkPaymentPostingAdjustments_AddUpdateThrowsException_LogsErrorAndContinues()
        {
            // Arrange
            List<AddOrEditAdjustmentModelForBulkPosting> adjustments = GetAdjustments();

            _mockPaymentServiceLineAdjustmentService
               .Setup(s => s.DeleteServiceLineAdjustmentsAsync(It.IsAny<AddOrEditAdjustmentModelForBulkPosting>()))
               .Returns(Task.CompletedTask);

            // Throw exception on first AddPaymentServiceLineAdjustmentsAsync call, succeed on second
            _mockPaymentServiceLineAdjustmentService
                .SetupSequence(s => s.AddPaymentServiceLineAdjustmentsAsync(It.IsAny<AddOrEditAdjustmentModelForBulkPosting>()))
                .ThrowsAsync(new Exception("Add/Update failed"))
                .ReturnsAsync(new List<PaymentClaimServiceLineAdjustmentModel>());

            _mockpaymentClaimService
               .Setup(s => s.UpdatePaymentClaimServiceLineAmountsAsync(It.IsAny<UpdatePaymentServiceLineAmountsModelWithUserInfo>()))
               .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AddOrUpdateBulkPaymentPostingAdjustments(adjustments);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task AddOrUpdateBulkPaymentPostingAdjustments_UpdatePaymentThrowsException_LogsErrorAndContinues()
        {
            // Arrange
            List<AddOrEditAdjustmentModelForBulkPosting> adjustments = GetAdjustments();

            _mockPaymentServiceLineAdjustmentService
               .Setup(s => s.DeleteServiceLineAdjustmentsAsync(It.IsAny<AddOrEditAdjustmentModelForBulkPosting>()))
               .Returns(Task.CompletedTask);

            _mockPaymentServiceLineAdjustmentService
                .Setup(s => s.AddPaymentServiceLineAdjustmentsAsync(It.IsAny<AddOrEditAdjustmentModel>()))
                .ReturnsAsync(new List<PaymentClaimServiceLineAdjustmentModel>());

            // Throw exception on first UpdatePaymentClaimServiceLineAmountsAsync call, succeed on second
            _mockpaymentClaimService
                .SetupSequence(s => s.UpdatePaymentClaimServiceLineAmountsAsync(It.IsAny<UpdatePaymentServiceLineAmountsModelWithUserInfo>()))
                .ThrowsAsync(new Exception("Update payment failed"))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AddOrUpdateBulkPaymentPostingAdjustments(adjustments);

            // Assert
           Assert.IsType<OkObjectResult>(result);
        }

        private static List<AddOrEditAdjustmentModelForBulkPosting> GetAdjustments()
        {
            return new List<AddOrEditAdjustmentModelForBulkPosting>
            {
                new AddOrEditAdjustmentModelForBulkPosting
                {
                    AccountInfoId = 18421,
                    MemberId = 105815,
                    ClaimId = 2922,
                    ServiceLineId = 21880,
                    AdjustmentDetails = new List<AdjustmentDetailsModel>
                    {
                        new AdjustmentDetailsModel
                        {
                            AdjustmentId = null,
                            Amount = 10,
                            isPositive = false,
                            GroupCode = "CO",
                            ReasonCode = "45"
                        }
                    }
                },
                new AddOrEditAdjustmentModelForBulkPosting
                {
                    AccountInfoId = 18421,
                    MemberId = 105815,
                    ClaimId = 2757,
                    ServiceLineId = 21882,
                    AdjustmentDetails = new List<AdjustmentDetailsModel>
                    {
                        new AdjustmentDetailsModel
                        {
                            AdjustmentId = null,
                            Amount = 20,
                            isPositive = false,
                            GroupCode = "CO",
                            ReasonCode = "50"
                        }
                    }
                }
            };
        }
    }
}
