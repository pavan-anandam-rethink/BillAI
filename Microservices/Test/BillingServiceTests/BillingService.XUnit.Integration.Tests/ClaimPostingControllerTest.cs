using BillingService.Domain.Interfaces;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Interfaces.Files;
using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.PaymentClaims;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Domain.Services.Payment;
using BillingService.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.ReportingModels;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Integration.Tests
{
    public class ClaimPostingControllerTest
    {
        private readonly Mock<IPaymentClaimService> _paymentClaimServiceMock = new();
        private readonly Mock<IChargePaymentService> _chargePaymentServiceMock = new();
        private readonly Mock<IReportService> _reportServiceMock = new();
        private readonly ClaimPostingController _controller;
        private readonly Mock<ILogger<ClaimPostingController>> _loggerMock = new();

        public ClaimPostingControllerTest()
        {
            _controller = new ClaimPostingController(
                _paymentClaimServiceMock.Object,
                _chargePaymentServiceMock.Object,
                _reportServiceMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task GetClaims_ReturnsOkResult()
        {
            var filter = new GetClaimFilterModel();
            var expected = new PaymentClaimsResponseModel();
            _paymentClaimServiceMock.Setup(s => s.GetPaymentClaimsAsync(filter)).ReturnsAsync(expected);

            var result = await _controller.GetClaims(filter);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GetClaims_ReturnsBadRequest_OnException()
        {
            var filter = new GetClaimFilterModel();
            _paymentClaimServiceMock.Setup(s => s.GetPaymentClaimsAsync(filter)).ThrowsAsync(new Exception("error"));

            var result = await _controller.GetClaims(filter);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("error", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetPaymentPatients_ReturnsOkResult()
        {
            int paymentId = 1;
            var expected = new List<PaymentPaitentModel>();
            _paymentClaimServiceMock.Setup(s => s.GetPatientsByPaymentAsync(paymentId)).ReturnsAsync(expected);

            var result = await _controller.GetPaymentPatients(paymentId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }


        [Fact]
        public async Task GetPatientClaims_ReturnsOkResult()
        {
            var filter = new GetClaimsModel();
            var expected = new PatientPaymentClaimsResponseModel();
            _paymentClaimServiceMock.Setup(s => s.GetPaymentClaimsByPatientsAsync(filter)).ReturnsAsync(expected);

            var result = await _controller.GetPatientClaims(filter);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Null(okResult.Value);
        }

        //[Fact]
        //public async Task GetPatientClaims_ReturnsBadRequest_OnException()
        //{
        //    var filter = new GetClaimsModel();
        //    _paymentClaimServiceMock.Setup(s => s.GetPaymentClaimsByPatientsAsync(filter)).ThrowsAsync(new Exception("fail"));

        //    var result = await _controller.GetPatientClaims(filter);

        //    var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        //    Assert.Equal("fail", badRequest.Value);
        //}

        [Fact]
        public async Task GetEOBClaims_ReturnsOkResult()
        {
            // Arrange
            int paymentId = 1;
            var expected = new List<ClaimEOBInfoModel> { new ClaimEOBInfoModel { PayerClaimNumber = "123" } };
            _paymentClaimServiceMock.Setup(s => s.GetEOBClaimsAsync(paymentId, null)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetEOBClaims(paymentId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GetEOBClaims_ReturnsBadRequest_OnException()
        {
            // Arrange
            int paymentId = 1;
            _paymentClaimServiceMock.Setup(s => s.GetEOBClaimsAsync(paymentId, null))
                .ThrowsAsync(new Exception("EOB error"));

            // Act
            IActionResult result;
            try
            {
                result = await _controller.GetEOBClaims(paymentId);
            }
            catch (Exception ex)
            {
                // If the controller does not handle exceptions, assert the exception is thrown
                Assert.Equal("EOB error", ex.Message);
                return;
            }

            // If you add try/catch in the controller, use this assertion:
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("EOB error", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetEOBPaymentClaimsPDF_ReturnsFileResult()
        {
            var model = new GetEOBClaimsModel();
            var expected = new byte[] { 1, 2, 3 };
            _paymentClaimServiceMock.Setup(s => s.GetEOBPaymentClaimPDFAsync(model)).ReturnsAsync(expected);

            var result = await _controller.GetEOBPaymentClaimsPDF(model);

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal(expected, fileResult.FileContents);
            Assert.Equal(MediaTypeNames.Application.Pdf, fileResult.ContentType);
            Assert.Equal("EOB Details", fileResult.FileDownloadName);
        }

        [Fact]
        public async Task GetEOBPaymentClaimsPDF_ReturnsBadRequest_OnException()
        {
            // Arrange
            var model = new GetEOBClaimsModel();
            _paymentClaimServiceMock.Setup(s => s.GetEOBPaymentClaimPDFAsync(model))
                .ThrowsAsync(new Exception("PDF error"));

            // Act
            IActionResult result;
            try
            {
                result = await _controller.GetEOBPaymentClaimsPDF(model);
            }
            catch (Exception ex)
            {
                // If the controller does not handle exceptions, assert the exception is thrown
                Assert.Equal("PDF error", ex.Message);
                return;
            }

            // If you add try/catch in the controller, use this assertion:
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("PDF error", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetSelectedEOBClaims_ReturnsOkResult()
        {
            // Arrange
            var model = new GetEOBClaimsModel
            {
                PaymentId = 1,
                Claims = new List<int> { 10, 20 }
            };
            var expected = new List<ClaimEOBInfoModel> { new ClaimEOBInfoModel { PayerClaimNumber = "123" } };
            _paymentClaimServiceMock.Setup(s => s.GetEOBClaimsAsync(model.PaymentId, model.Claims)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetSelectedEOBClaims(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GetSelectedEOBClaims_ReturnsBadRequest_OnException()
        {
            // Arrange
            var model = new GetEOBClaimsModel
            {
                PaymentId = 1,
                Claims = new List<int> { 10, 20 }
            };
            _paymentClaimServiceMock.Setup(s => s.GetEOBClaimsAsync(model.PaymentId, model.Claims))
                .ThrowsAsync(new Exception("EOB error"));

            // Act
            IActionResult result;
            try
            {
                result = await _controller.GetSelectedEOBClaims(model);
            }
            catch (Exception ex)
            {
                // If the controller does not handle exceptions, assert the exception is thrown
                Assert.Equal("EOB error", ex.Message);
                return;
            }

            // If you add try/catch in the controller, use this assertion:
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("EOB error", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetClaimDetails_ReturnsOkResult()
        {
            // Arrange
            var model = new IdWithUserInfo { Id = 1 };
            var expected = new PaymentClaimModel { PaymentId = 1, PatientId = 2 };
            _paymentClaimServiceMock.Setup(s => s.GetPaymentClaimAsync(model.Id)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetClaimDetails(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GetClaimDetails_ReturnsBadRequest_OnException()
        {
            // Arrange
            var model = new IdWithUserInfo { Id = 1 };
            _paymentClaimServiceMock.Setup(s => s.GetPaymentClaimAsync(model.Id))
                .ThrowsAsync(new Exception("Claim error"));

            // Act
            IActionResult result;
            try
            {
                result = await _controller.GetClaimDetails(model);
            }
            catch (Exception ex)
            {
                // If the controller does not handle exceptions, assert the exception is thrown
                Assert.Equal("Claim error", ex.Message);
                return;
            }

            // If you add try/catch in the controller, use this assertion:
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Claim error", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetPatientDetails_ReturnsOkResult()
        {
            // Arrange
            var model = new PatientDetailsModel { patientId = 1, AccountInfoId = 2 };
            var expected = new ChildProfileInfo { PatientId = 1 };
            _paymentClaimServiceMock.Setup(s => s.getPatientDetails(model.patientId, model.AccountInfoId)).ReturnsAsync(expected);

            // Act
            var result = await _controller.GetPatientDetails(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GetPatientDetails_ReturnsBadRequest_OnException()
        {
            // Arrange
            var model = new PatientDetailsModel { patientId = 1, AccountInfoId = 2 };
            _paymentClaimServiceMock.Setup(s => s.getPatientDetails(model.patientId, model.AccountInfoId))
                .ThrowsAsync(new Exception("Patient error"));

            // Act
            IActionResult result;
            try
            {
                result = await _controller.GetPatientDetails(model);
            }
            catch (Exception ex)
            {
                // If the controller does not handle exceptions, assert the exception is thrown
                Assert.Equal("Patient error", ex.Message);
                return;
            }

            // If you add try/catch in the controller, use this assertion:
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Patient error", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetPaymentClaimServiceLines_ReturnsOkResult()
        {
            int claimId = 1;
            var expected = new List<PaymentClaimServiceLineModel>();
            _paymentClaimServiceMock.Setup(s => s.GetPaymentClaimServiceLinesAsync(claimId)).ReturnsAsync(expected);

            var result = await _controller.GetPaymentClaimServiceLines(claimId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;
            var dataProp = value.GetType().GetProperty("data");
            var totalCountProp = value.GetType().GetProperty("totalCount");
            Assert.NotNull(dataProp);
            Assert.NotNull(totalCountProp);
            Assert.Equal(expected, dataProp.GetValue(value));
            Assert.Equal(expected.Count, totalCountProp.GetValue(value));
        }

        [Fact]
        public async Task GetPaymentClaimServiceLines_ReturnsBadRequest_OnException()
        {
            // Arrange
            int claimId = 1;
            _paymentClaimServiceMock.Setup(s => s.GetPaymentClaimServiceLinesAsync(claimId))
                .ThrowsAsync(new Exception("Service line error"));

            // Act
            IActionResult result;
            try
            {
                result = await _controller.GetPaymentClaimServiceLines(claimId);
            }
            catch (Exception ex)
            {
                // If the controller does not handle exceptions, assert the exception is thrown
                Assert.Equal("Service line error", ex.Message);
                return;
            }

            // If you add try/catch in the controller, use this assertion:
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Service line error", badRequest.Value.ToString());
        }

        //[Fact]
        //public async Task GetPatientPaymentClaimLinkedServiceLines_ReturnsOkResult()
        //{
        //    // Arrange
        //    var model = new GetPatientPaymentServiceLinesModel { PaymentId = 1, PatientId = 2 };
        //    var expected = new List<PaymentClaimServiceLineModel>
        //    {
        //        new PaymentClaimServiceLineModel { Id = 1, PatientId = 2 }
        //    };
        //    _paymentClaimServiceMock.Setup(s => s.GetPatientPaymentLinkedServiceLinesAsync(model, false)).ReturnsAsync(expected);

        //    // Act
        //    var result = await _controller.GetPatientPaymentClaimLinkedServiceLines(model);

        //    // Assert
        //    var okResult = Assert.IsType<OkObjectResult>(result);
        //    var value = okResult.Value;
        //    var dataProp = value.GetType().GetProperty("data");
        //    var totalCountProp = value.GetType().GetProperty("totalCount");
        //    Assert.NotNull(dataProp);
        //    Assert.NotNull(totalCountProp);
        //    Assert.Equal(expected, dataProp.GetValue(value));
        //    Assert.Equal(expected.Count, totalCountProp.GetValue(value));
        //}

        //[Fact]
        //public async Task GetPatientPaymentClaimLinkedServiceLines_ReturnsBadRequest_OnException()
        //{
        //    // Arrange
        //    var model = new GetPatientPaymentServiceLinesModel { PaymentId = 1, PatientId = 2 };
        //    _paymentClaimServiceMock.Setup(s => s.GetPatientPaymentLinkedServiceLinesAsync(model, false))
        //        .ThrowsAsync(new Exception("Linked service line error"));

        //    // Act
        //    IActionResult result;
        //    try
        //    {
        //        result = await _controller.GetPatientPaymentClaimLinkedServiceLines(model);
        //    }
        //    catch (Exception ex)
        //    {
        //        // If the controller does not handle exceptions, assert the exception is thrown
        //        Assert.Equal("Linked service line error", ex.Message);
        //        return;
        //    }

        //    // If you add try/catch in the controller, use this assertion:
        //    var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        //    Assert.Contains("Linked service line error", badRequest.Value.ToString());
        //}


        //[Fact]
        //public async Task GetPatientPaymentClaimUnlinkedServiceLines_ReturnsBadRequest_OnException()
        //{
        //    // Arrange
        //    var model = new GetPatientPaymentServiceLinesModel { PaymentId = 1, PatientId = 2 };
        //    _paymentClaimServiceMock
        //        .Setup(s => s.GetPatientPaymentUnlinkedServiceLinesAsync(model))
        //        .ThrowsAsync(new Exception("Unlinked service line error"));

        //    // Act
        //    IActionResult result;
        //    try
        //    {
        //        result = await _controller.GetPatientPaymentClaimUnlinkedServiceLines(model);
        //    }
        //    catch (Exception ex)
        //    {
        //        // If the controller does not handle exceptions, assert the exception is thrown
        //        Assert.Equal("Unlinked service line error", ex.Message);
        //        return;
        //    }

        //    // If you add try/catch in the controller, use this assertion:
        //    var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        //    Assert.Contains("Unlinked service line error", badRequest.Value.ToString());
        //}

        [Fact]
        public async Task GetPaymentClaimServiceLine_ReturnsOkResult()
        {
            // Arrange
            int serviceLineId = 1;
            var expected = new PaymentClaimServiceLineModel { Id = serviceLineId };
            _paymentClaimServiceMock
                .Setup(s => s.GetPaymentClaimServiceLineAsync(serviceLineId))
                .ReturnsAsync(expected);

            // Act
            var result = await _controller.GetPaymentClaimServiceLine(serviceLineId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GetPaymentClaimServiceLine_ReturnsBadRequest_OnException()
        {
            // Arrange
            int serviceLineId = 1;
            _paymentClaimServiceMock
                .Setup(s => s.GetPaymentClaimServiceLineAsync(serviceLineId))
                .ThrowsAsync(new Exception("Service line error"));

            // Act
            IActionResult result;
            try
            {
                result = await _controller.GetPaymentClaimServiceLine(serviceLineId);
            }
            catch (Exception ex)
            {
                // If the controller does not handle exceptions, assert the exception is thrown
                Assert.Equal("Service line error", ex.Message);
                return;
            }

            // If you add try/catch in the controller, use this assertion:
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Service line error", badRequest.Value.ToString());
        }

        [Fact]
        public async Task CreatePaymentPatientClaims_ReturnsOkResult()
        {
            // Arrange
            var model = new CreatePatientClaimsModel
            {
                PaymentId = 1,
                PatientIds = new[] { 1, 2 },
                AccountInfoId = 123
            };
            var expected = new List<AddPatientResponseModel>
                {
                    new AddPatientResponseModel { patientId = 1, patientName = "John Doe", isAttached = true }
                };
            _paymentClaimServiceMock
                .Setup(s => s.CreatePaymentClaimsAsync(model))
                .ReturnsAsync(expected);

            // Act
            var result = await _controller.CreatePaymentPatientClaims(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task CreatePaymentPatientClaims_ReturnsBadRequest_OnException()
        {
            // Arrange
            var model = new CreatePatientClaimsModel
            {
                PaymentId = 1,
                PatientIds = new[] { 1, 2 },
                AccountInfoId = 123
            };
            _paymentClaimServiceMock
                .Setup(s => s.CreatePaymentClaimsAsync(model))
                .ThrowsAsync(new Exception("Create error"));

            // Act
            var result = await _controller.CreatePaymentPatientClaims(model);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }
        [Fact]
        public async Task CreateClaimsToEraPayment_ReturnsOkResult()
        {
            // Arrange
            var model = new CreateEraClaimsModel
            {
                PaymentId = 1,
                ClaimsIds = new[] { 10, 20 },
                AccountInfoId = 123,
                MemberId = 456
            };
            var expected = 2;
            _paymentClaimServiceMock
                .Setup(s => s.CreateClaimsToEraAsync(model))
                .ReturnsAsync(expected);

            // Act
            var result = await _controller.CreateClaimsToEraPayment(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task CreateClaimsToEraPayment_LogsErrorAndReturnsBadRequest_WhenServiceThrows()
        {
            // Arrange
            var model = new CreateEraClaimsModel
            {
                PaymentId = 1,
                ClaimsIds = new[] { 10, 20 },
                AccountInfoId = 123,
                MemberId = 456
            };

            _paymentClaimServiceMock
                .Setup(s => s.CreateClaimsToEraAsync(model))
                .ThrowsAsync(new Exception("ERA error"));

            // Act
            var result = await _controller.CreateClaimsToEraPayment(model);

            // Assert - BadRequest
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("ERA error", badRequestResult.Value);

            // Assert - Service called
            _paymentClaimServiceMock.Verify(
                s => s.CreateClaimsToEraAsync(model),
                Times.Once);

            // Assert - LogError called (string-interpolated logging)
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString().Contains(
                            "Failed to create claims to ERA payment")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }


        [Fact]
        public async Task UpdatePaymentClaimServiceLineAmounts_ReturnsOkResult()
        {
            // Arrange
            var model = new UpdatePaymentServiceLineAmountsModelWithUserInfo
            {
                ServiceLineId = 1,
                AllowedAmount = 100,
                PaymentAmount = 50,
                AccountInfoId = 123,
                MemberId = 456,
                IsManual = true
            };
            _paymentClaimServiceMock
                .Setup(s => s.UpdatePaymentClaimServiceLineAmountsAsync(model))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdatePaymentClaimServiceLineAmounts(model);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task UpdatePaymentClaimServiceLineAmounts_LogsErrorAndReturnsBadRequest_WhenServiceThrows()
        {
            // Arrange
            var model = new UpdatePaymentServiceLineAmountsModelWithUserInfo
            {
                ServiceLineId = 1,
                AllowedAmount = 100,
                PaymentAmount = 50,
                AccountInfoId = 123,
                MemberId = 456,
                IsManual = true
            };

            _paymentClaimServiceMock
                .Setup(s => s.UpdatePaymentClaimServiceLineAmountsAsync(model))
                .ThrowsAsync(new Exception("Update error"));

            // Act
            var result = await _controller.UpdatePaymentClaimServiceLineAmounts(model);

            // Assert - BadRequest
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Update error", badRequestResult.Value);

            // Assert - Service called
            _paymentClaimServiceMock.Verify(
                s => s.UpdatePaymentClaimServiceLineAmountsAsync(model),
                Times.Once);

            // Assert - LogError (string-interpolated logging)
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString().Contains(
                            "Failed to update payment claim service line amounts")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task PostManualPaymentClaimLines_ReturnsOkResult()
        {
            // Arrange
            var model = new PostPaymentClaimsModel
            {
                PaymentId = 1,
                SelectedClaimLines = new List<PostPaymentClaimLinesModel>
        {
            new PostPaymentClaimLinesModel
            {
                ClaimId = 10,
                IsClaimSelected = true,
                SelectedLines = new List<PostPaymentLineModel>
                {
                    new PostPaymentLineModel { Id = 100, PaidAmount = 50, Balance = 50 }
                }
            }
        }
            };
            _paymentClaimServiceMock
                .Setup(s => s.PostPaymentClaimLines(model))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.PostManualPaymentClaimLines(model);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task PostManualPaymentClaimLines_ReturnsBadRequest_OnException()
        {
            // Arrange
            var model = new PostPaymentClaimsModel
            {
                PaymentId = 1,
                SelectedClaimLines = new List<PostPaymentClaimLinesModel>
        {
            new PostPaymentClaimLinesModel
            {
                ClaimId = 10,
                IsClaimSelected = true,
                SelectedLines = new List<PostPaymentLineModel>
                {
                    new PostPaymentLineModel { Id = 100, PaidAmount = 50, Balance = 50 }
                }
            }
        }
            };
            _paymentClaimServiceMock
                .Setup(s => s.PostPaymentClaimLines(model))
                .ThrowsAsync(new Exception("Manual payment error"));

            // Act
            var result = await _controller.PostManualPaymentClaimLines(model);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Manual payment error", badRequest.Value.ToString());
        }

        [Fact]
        public async Task PostManualPatientPaymentClaimLines_ReturnsOkResult()
        {
            // Arrange
            var model = new PostRemovePatientClaimsModel
            {
                PaymentId = 1,
                PatientServiceLines = new List<PatientServiceLinesModel>
        {
            new PatientServiceLinesModel
            {
                PatientId = 10,
                ServiceLines = new List<ServiceLinePostDeleteModel>
                {
                    new ServiceLinePostDeleteModel { Id = 100, ClaimId = 200 }
                }
            }
        },
                PostingCriteriaId = BulkPostingCriteria.NewestToOldest, // Use a valid enum value
                AccountInfoId = 123,
                MemberId = 456
            };
            var expected = "result-string";
            _paymentClaimServiceMock
                .Setup(s => s.PostPatientPaymentClaimLinesAsync(model))
                .ReturnsAsync(expected);

            // Act
            var result = await _controller.PostManualPatientPaymentClaimLines(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task PostManualPatientPaymentClaimLines_ReturnsBadRequest_OnException()
        {
            // Arrange
            var model = new PostRemovePatientClaimsModel
            {
                PaymentId = 1,
                PatientServiceLines = new List<PatientServiceLinesModel>
        {
            new PatientServiceLinesModel
            {
                PatientId = 10,
                ServiceLines = new List<ServiceLinePostDeleteModel>
                {
                    new ServiceLinePostDeleteModel { Id = 100, ClaimId = 200 }
                }
            }
        },
                PostingCriteriaId = BulkPostingCriteria.NewestToOldest, // Use a valid enum value
                AccountInfoId = 123,
                MemberId = 456
            };
            _paymentClaimServiceMock
                .Setup(s => s.PostPatientPaymentClaimLinesAsync(model))
                .ThrowsAsync(new Exception("Manual patient payment error"));

            // Act
            var result = await _controller.PostManualPatientPaymentClaimLines(model);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Manual patient payment error", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetPaymentClaimErrors_ReturnsOkResult()
        {
            // Arrange
            var model = new GetByIdSortFilterWithUserInfo
            {
                Id = 1,
                SortingModels = new List<SortingModel>(),
                Skip = 0,
                Take = 10,
                AccountInfoId = 123,
                MemberId = 456
            };
            var expected = new PaymentClaimErrorsResponseModel
            {
                Data = new List<PaymentClaimErrorModel>
        {
            new PaymentClaimErrorModel { Id = 1, PatientId = 2, PatientName = "John Doe", ErrorMessage = "Error" }
        },
                TotalCount = 1
            };
            _paymentClaimServiceMock
                .Setup(s => s.GetPaymentClaimErrorsAsync(model))
                .ReturnsAsync(expected);

            // Act
            var result = await _controller.GetPaymentClaimErrors(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GetPaymentClaimErrors_ReturnsBadRequest_OnException()
        {
            // Arrange
            var model = new GetByIdSortFilterWithUserInfo
            {
                Id = 1,
                SortingModels = new List<SortingModel>(),
                Skip = 0,
                Take = 10,
                AccountInfoId = 123,
                MemberId = 456
            };
            _paymentClaimServiceMock
                .Setup(s => s.GetPaymentClaimErrorsAsync(model))
                .ThrowsAsync(new Exception("Claim errors fetch error"));

            // Act
            IActionResult result;
            try
            {
                result = await _controller.GetPaymentClaimErrors(model);
            }
            catch (Exception ex)
            {
                // If the controller does not handle exceptions, assert the exception is thrown
                Assert.Equal("Claim errors fetch error", ex.Message);
                return;
            }

            // If you add try/catch in the controller, use this assertion:
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Claim errors fetch error", badRequest.Value.ToString());
        }

        [Fact]
        public async Task RemovePaymentClaims_ReturnsOkResult()
        {
            // Arrange
            var model = new RemovePaymentClaimsModel
            {
                PaymentId = 1,
                PaymentClaimsIds = new[] { 10, 20 },
                AccountInfoId = 123,
                MemberId = 456
            };
            _paymentClaimServiceMock
                .Setup(s => s.RemoveSelectedClaimsAsync(model))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RemovePaymentClaims(model);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task RemovePaymentClaims_ReturnsBadRequest_OnException()
        {
            // Arrange
            var model = new RemovePaymentClaimsModel
            {
                PaymentId = 1,
                PaymentClaimsIds = new[] { 10, 20 },
                AccountInfoId = 123,
                MemberId = 456
            };
            _paymentClaimServiceMock
                .Setup(s => s.RemoveSelectedClaimsAsync(model))
                .ThrowsAsync(new Exception("Remove error"));

            // Act
            IActionResult result;
            try
            {
                result = await _controller.RemovePaymentClaims(model);
            }
            catch (Exception ex)
            {
                // If the controller does not handle exceptions, assert the exception is thrown
                Assert.Equal("Remove error", ex.Message);
                return;
            }

            // If you add try/catch in the controller, use this assertion:
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Remove error", badRequest.Value.ToString());
        }

        [Fact]
        public async Task RemovePatientPaymentClaims_ReturnsOkResult()
        {
            // Arrange
            var model = new PostRemovePatientClaimsModel
            {
                PaymentId = 1,
                PatientServiceLines = new List<PatientServiceLinesModel>(),
                PostingCriteriaId = BulkPostingCriteria.NewestToOldest,
                AccountInfoId = 123,
                MemberId = 456
            };
            _paymentClaimServiceMock
                .Setup(s => s.RemoveSelectedPatientClaimsAsync(model))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RemovePatientPaymentClaims(model);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task RemovePatientPaymentClaims_ReturnsBadRequest_OnException()
        {
            // Arrange
            var model = new PostRemovePatientClaimsModel
            {
                PaymentId = 1,
                PatientServiceLines = new List<PatientServiceLinesModel>(),
                PostingCriteriaId = BulkPostingCriteria.NewestToOldest,
                AccountInfoId = 123,
                MemberId = 456
            };
            _paymentClaimServiceMock
                .Setup(s => s.RemoveSelectedPatientClaimsAsync(model))
                .ThrowsAsync(new Exception("Remove patient payment claims error"));

            // Act
            IActionResult result;
            try
            {
                result = await _controller.RemovePatientPaymentClaims(model);
            }
            catch (Exception ex)
            {
                // If the controller does not handle exceptions, assert the exception is thrown
                Assert.Equal("Remove patient payment claims error", ex.Message);
                return;
            }

            // If you add try/catch in the controller, use this assertion:
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Remove patient payment claims error", badRequest.Value.ToString());
        }

        [Fact]
        public async Task RemoveSelectedPatientPaymentAmounts_ReturnsOkResult()
        {
            // Arrange
            var model = new PostRemovePatientClaimsModel
            {
                PaymentId = 1,
                PatientServiceLines = new List<PatientServiceLinesModel>(),
                PostingCriteriaId = BulkPostingCriteria.NewestToOldest,
                AccountInfoId = 123,
                MemberId = 456
            };
            _paymentClaimServiceMock
                .Setup(s => s.RemoveSelectedPatientPaymentAmountsAsync(model))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RemoveSelectedPatientPaymentAmounts(model);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task RemoveSelectedPatientPaymentAmounts_ReturnsBadRequest_OnException()
        {
            // Arrange
            var model = new PostRemovePatientClaimsModel
            {
                PaymentId = 1,
                PatientServiceLines = new List<PatientServiceLinesModel>(),
                PostingCriteriaId = BulkPostingCriteria.NewestToOldest,
                AccountInfoId = 123,
                MemberId = 456
            };
            _paymentClaimServiceMock
                .Setup(s => s.RemoveSelectedPatientPaymentAmountsAsync(model))
                .ThrowsAsync(new Exception("Remove patient payment amounts error"));

            // Act
            IActionResult result;
            try
            {
                result = await _controller.RemoveSelectedPatientPaymentAmounts(model);
            }
            catch (Exception ex)
            {
                // If the controller does not handle exceptions, assert the exception is thrown
                Assert.Equal("Remove patient payment amounts error", ex.Message);
                return;
            }

            // If you add try/catch in the controller, use this assertion:
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Remove patient payment amounts error", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetPaymentClaimServiceLinesSmall_ReturnsOkResult()
        {
            // Arrange
            var model = new GetChargeDetailsModel
            {
                Id = 1,
                IsServiceLine = false
            };
            var expected = new List<GetPaymentClaimServiceLinesSmall>
    {
        new GetPaymentClaimServiceLinesSmall
        {
            Id = 1,
            PaymentId = 2,
            PaymentIdentifier = "ABC123",
            AllowedAmount = 100,
            PaidAmount = 50,
            DateLastModified = DateTime.UtcNow,
            PaymentType = "Patient Payment"
        }
    }.AsQueryable();

            _paymentClaimServiceMock
                .Setup(s => s.GetPaymentClaimServiceLinesSmallAsync(model))
                .ReturnsAsync(expected);

            // Act
            var result = await _controller.GetPaymentClaimServiceLinesSmall(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GetPaymentClaimServiceLinesSmall_ReturnsBadRequest_OnException()
        {
            // Arrange
            var model = new GetChargeDetailsModel
            {
                Id = 1,
                IsServiceLine = false
            };
            _paymentClaimServiceMock
                .Setup(s => s.GetPaymentClaimServiceLinesSmallAsync(model))
                .ThrowsAsync(new Exception("Service line error"));

            // Act
            var result = await _controller.GetPaymentClaimServiceLinesSmall(model);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Service line error", badRequest.Value.ToString());
        }

        [Fact]
        public async Task GetClientPrintDataById_ReturnsJsonResult()
        {
            // Arrange
            var model = new GetClientPrintDataRequest
            {
                Id = 1,
                AccountInfoId = 123,
                MemberId = 456,
                PatientId = 789,
                ClaimId = 1011
            };
            var expected = new ClientPrintData
            {
                ClaimId = model.ClaimId,
                PatientId = model.PatientId,
                CompanyName = "Test Company",
                CompanyAddress = "123 Main St,",
                CompanyPhones = "555-1234",
                CompanyLogoUrl = "logo-url",
                CompanyEmail = "test@test.com",
                ClientName = "John Doe",
                ClientAddress = "123 Main St,",
                PaymentPostingDate = DateTime.UtcNow,
                ClientAccountId = "123",
                TotalPayment = 455.55,
                Remaining = 10.05
            };
            _paymentClaimServiceMock
                .Setup(s => s.GetCompanyAccountInfoByPatientId(model))
                .ReturnsAsync(expected);

            // Act
            var result = await _controller.GetClientPrintDataById(model);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(expected, jsonResult.Value);
        }

        [Fact]
        public async Task GetClientPrintDataById_ReturnsJsonResult_OnNull()
        {
            // Arrange
            var model = new GetClientPrintDataRequest
            {
                Id = 1,
                AccountInfoId = 123,
                MemberId = 456,
                PatientId = 789,
                ClaimId = 1011
            };
            var expected = new ClientPrintData();
            _paymentClaimServiceMock
                .Setup(s => s.GetCompanyAccountInfoByPatientId(model))
                .ReturnsAsync(expected);

            // Act
            var result = await _controller.GetClientPrintDataById(model);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(expected, jsonResult.Value);
        }

        [Fact]
        public async Task SendReport_ReturnsBadRequest_WhenReportQueryModelIsNull()
        {
            // Arrange
            ReportQueryModel reportQueryModel = null;

            // Act
            var result = await _controller.SendReport(reportQueryModel);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task SendReport_ReturnsOk_WhenReportFrequencyIsMonthly()
        {
            // Arrange
            var reportQueryModel = new ReportQueryModel
            {
                ReportFrequency = ReportFrequency.Monthly,
                StartDate = DateTime.Today.AddMonths(-1),
                EndDate = DateTime.Today
            };

            _reportServiceMock.Setup(service => service.SendMonthlyReportAsync(It.IsAny<ReportQueryModel>()))
                .ReturnsAsync(true); // Simulate the method returning a successful result

            // Act
            var result = await _controller.SendReport(reportQueryModel);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.True((bool)okResult.Value);
        }

        [Fact]
        public async Task SendReport_ReturnsOk_WhenReportFrequencyIsWeekly()
        {
            // Arrange
            var reportQueryModel = new ReportQueryModel
            {
                ReportFrequency = ReportFrequency.Weekly,
                StartDate = DateTime.Today.AddDays(-7),
                EndDate = DateTime.Today
            };

            _reportServiceMock.Setup(service => service.SendWeeklyReportAsync(It.IsAny<ReportQueryModel>()))
                .ReturnsAsync(true); // Simulate the method returning a successful result

            // Act
            var result = await _controller.SendReport(reportQueryModel);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.True((bool)okResult.Value);
        }

        [Fact]
        public async Task SendReport_ReturnsBadRequest_WhenReportFrequencyIsInvalid()
        {
            // Arrange
            var reportQueryModel = new ReportQueryModel
            {
                ReportFrequency = (ReportFrequency)999, // Invalid frequency
                StartDate = DateTime.Today.AddMonths(-1),
                EndDate = DateTime.Today
            };

            // Act
            var result = await _controller.SendReport(reportQueryModel);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestResult>(result);
        }

    }
}
