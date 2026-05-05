using AutoMapper;
using BillingService.Domain.DataObjects.Base;
using BillingService.Domain.DataObjects.Billing;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
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
    public class ChargePaymentControllerTest
    {
        private readonly Mock<IChargePaymentService> _chargePaymentServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly ChargePaymentController _controller;
        private readonly Mock<ILogger<ChargePaymentController>> _mockLogger;

        public ChargePaymentControllerTest()
        {
            _chargePaymentServiceMock = new Mock<IChargePaymentService>();
            _mapperMock = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<ChargePaymentController>>();
            _controller = new ChargePaymentController(_mapperMock.Object, _chargePaymentServiceMock.Object, _mockLogger.Object);

        }

        [Fact]
        public async Task GetForClaim_ReturnsJsonResult_WithMappedModels()
        {
            var model = new ClaimIdWithUserInfo { Id = 1, AccountInfoId = 2 };
            var serviceResult = new List<ChargePaymentItem>
            {
                new ChargePaymentItem
                {
                    Id = 10,
                    ChargeEntryId = 100,
                    Date = new DateTime(2024, 1, 1),
                    Amount = 123.45m,
                    CPTCode = "CPT123",
                    ReasonCodeId = 5,
                    ReasonCode = "Reason",
                    PaymentMethodId = 2,
                    PaymentMethod = "Credit",
                    Reference = "Ref123",
                    PostedBy = "UserA"
                }
            };
            var mappedResult = new List<ChargePaymentModel>
            {
                new ChargePaymentModel
                {
                    Id = 10,
                    ChargeEntryId = 100,
                    Date = new DateTime(2024, 1, 1),
                    Amount = 123.45m,
                    CPTCode = "CPT123",
                    ReasonCodeId = 5,
                    ReasonCode = "Reason",
                    PaymentMethodId = 2,
                    PaymentMethod = "Credit",
                    Reference = "Ref123",
                    PostedBy = "UserA"
                }
            };

            _chargePaymentServiceMock.Setup(s => s.GetForClaim(model.Id, model.AccountInfoId)).ReturnsAsync(serviceResult);
            _mapperMock.Setup(m => m.Map<List<ChargePaymentModel>>(serviceResult)).Returns(mappedResult);

            var result = await _controller.GetForClaim(model);

            var jsonResult = Assert.IsType<JsonResult>(result);
            var value = Assert.IsType<List<ChargePaymentModel>>(jsonResult.Value);
            Assert.Single(value);
            var cp = value[0];
            Assert.Equal(10, cp.Id);
            Assert.Equal(100, cp.ChargeEntryId);
            Assert.Equal(new DateTime(2024, 1, 1), cp.Date);
            Assert.Equal(123.45m, cp.Amount);
            Assert.Equal("CPT123", cp.CPTCode);
            Assert.Equal(5, cp.ReasonCodeId);
            Assert.Equal("Reason", cp.ReasonCode);
            Assert.Equal(2, cp.PaymentMethodId);
            Assert.Equal("Credit", cp.PaymentMethod);
            Assert.Equal("Ref123", cp.Reference);
            Assert.Equal("UserA", cp.PostedBy);
        }

        [Fact]
        public async Task GetForClaim_ReturnsBadRequest_WhenServiceThrowsException()
        {
            var model = new ClaimIdWithUserInfo { Id = 1, AccountInfoId = 2 };
            _chargePaymentServiceMock.Setup(s => s.GetForClaim(model.Id, model.AccountInfoId)).ThrowsAsync(new Exception("Service error"));

            var result = await _controller.GetForClaim(model);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Service error", badRequest.Value);
        }

        [Fact]
        public async Task GetPaymentOptions_ReturnsJsonResult()
        {
            var model = new ClaimIdWithUserInfo { Id = 1, AccountInfoId = 2 };
            var paymentOptions = new PaymentOptions
            {
                Charges = new List<BaseNameOption> { new BaseNameOption { Id = 1, Name = "Charge1" } },
                Reasons = new List<BaseNameOption> { new BaseNameOption { Id = 2, Name = "Reason1" } },
                PaymentMethods = new List<BaseNameOption> { new BaseNameOption { Id = 3, Name = "Method1" } }
            };

            _chargePaymentServiceMock.Setup(s => s.GetPaymentOptions(model.Id, model.AccountInfoId)).ReturnsAsync(paymentOptions);

            var result = await _controller.GetPaymentOptions(model);

            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(paymentOptions, jsonResult.Value);
        }

        [Fact]
        public async Task GetRemainingAmount_ReturnsJsonResult()
        {
            var model = new ChargeIdWithUserInfo { ChargeId = 1, AccountInfoId = 2 };
            decimal expectedAmount = 123.45m;

            _chargePaymentServiceMock.Setup(s => s.GetRemainingAmount(model.ChargeId, model.AccountInfoId)).ReturnsAsync(expectedAmount);

            var result = await _controller.GetRemainingAmount(model);

            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(expectedAmount, jsonResult.Value);
        }

        [Fact]
        public async Task Save_ReturnsJsonResult_WithAllModelProperties()
        {
            var paymentModel = new ChargePaymentModel
            {
                Id = 20,
                ChargeEntryId = 200,
                Date = new DateTime(2024, 2, 2),
                Amount = 222.22m,
                CPTCode = "CPT999",
                ReasonCodeId = 9,
                ReasonCode = "RCode",
                PaymentMethodId = 3,
                PaymentMethod = "Cash",
                Reference = "Ref999",
                PostedBy = "UserB"
            };
            var paymentModelWithUserInfo = new ChargePaymentModelWithUserInfo
            {
                ChargePaymentModel = paymentModel,
                MemberId = 5
            };
            var mappedItem = new ChargePaymentItem
            {
                Id = paymentModel.Id,
                ChargeEntryId = paymentModel.ChargeEntryId,
                Date = paymentModel.Date,
                Amount = paymentModel.Amount,
                CPTCode = paymentModel.CPTCode,
                ReasonCodeId = paymentModel.ReasonCodeId,
                ReasonCode = paymentModel.ReasonCode,
                PaymentMethodId = paymentModel.PaymentMethodId,
                PaymentMethod = paymentModel.PaymentMethod,
                Reference = paymentModel.Reference,
                PostedBy = paymentModel.PostedBy
            };
            var updatedItem = new ChargePaymentItem
            {
                Id = paymentModel.Id,
                ChargeEntryId = paymentModel.ChargeEntryId,
                Date = paymentModel.Date,
                Amount = paymentModel.Amount,
                CPTCode = paymentModel.CPTCode,
                ReasonCodeId = paymentModel.ReasonCodeId,
                ReasonCode = paymentModel.ReasonCode,
                PaymentMethodId = paymentModel.PaymentMethodId,
                PaymentMethod = paymentModel.PaymentMethod,
                Reference = paymentModel.Reference,
                PostedBy = paymentModel.PostedBy
            };

            _mapperMock.Setup(m => m.Map<ChargePaymentItem>(paymentModel)).Returns(mappedItem);
            _chargePaymentServiceMock.Setup(s => s.Save(mappedItem, paymentModelWithUserInfo.MemberId)).ReturnsAsync(updatedItem);

            var result = await _controller.Save(paymentModelWithUserInfo);

            var jsonResult = Assert.IsType<JsonResult>(result);
            var item = Assert.IsType<ChargePaymentItem>(jsonResult.Value);
            Assert.Equal(paymentModel.Id, item.Id);
            Assert.Equal(paymentModel.ChargeEntryId, item.ChargeEntryId);
            Assert.Equal(paymentModel.Date, item.Date);
            Assert.Equal(paymentModel.Amount, item.Amount);
            Assert.Equal(paymentModel.CPTCode, item.CPTCode);
            Assert.Equal(paymentModel.ReasonCodeId, item.ReasonCodeId);
            Assert.Equal(paymentModel.ReasonCode, item.ReasonCode);
            Assert.Equal(paymentModel.PaymentMethodId, item.PaymentMethodId);
            Assert.Equal(paymentModel.PaymentMethod, item.PaymentMethod);
            Assert.Equal(paymentModel.Reference, item.Reference);
            Assert.Equal(paymentModel.PostedBy, item.PostedBy);
        }

        [Fact]
        public async Task Delete_ReturnsJsonResult_WithAllModelProperties()
        {
            var paymentModel = new ChargePaymentModel
            {
                Id = 30,
                ChargeEntryId = 300,
                Date = new DateTime(2024, 3, 3),
                Amount = 333.33m,
                CPTCode = "CPT888",
                ReasonCodeId = 8,
                ReasonCode = "RCode2",
                PaymentMethodId = 4,
                PaymentMethod = "Check",
                Reference = "Ref888",
                PostedBy = "UserC"
            };
            var chargePaymentModelWithUserInfo = new ChargePaymentModelWithUserInfo
            {
                ChargePaymentModel = paymentModel,
                MemberId = 6
            };
            var mappedItem = new ChargePaymentItem
            {
                Id = paymentModel.Id,
                ChargeEntryId = paymentModel.ChargeEntryId,
                Date = paymentModel.Date,
                Amount = paymentModel.Amount,
                CPTCode = paymentModel.CPTCode,
                ReasonCodeId = paymentModel.ReasonCodeId,
                ReasonCode = paymentModel.ReasonCode,
                PaymentMethodId = paymentModel.PaymentMethodId,
                PaymentMethod = paymentModel.PaymentMethod,
                Reference = paymentModel.Reference,
                PostedBy = paymentModel.PostedBy
            };
            var deletedItem = new ChargePaymentItem
            {
                Id = paymentModel.Id,
                ChargeEntryId = paymentModel.ChargeEntryId,
                Date = paymentModel.Date,
                Amount = paymentModel.Amount,
                CPTCode = paymentModel.CPTCode,
                ReasonCodeId = paymentModel.ReasonCodeId,
                ReasonCode = paymentModel.ReasonCode,
                PaymentMethodId = paymentModel.PaymentMethodId,
                PaymentMethod = paymentModel.PaymentMethod,
                Reference = paymentModel.Reference,
                PostedBy = paymentModel.PostedBy
            };

            _mapperMock.Setup(m => m.Map<ChargePaymentItem>(paymentModel)).Returns(mappedItem);
            _chargePaymentServiceMock.Setup(s => s.Delete(mappedItem, chargePaymentModelWithUserInfo.MemberId)).ReturnsAsync(deletedItem);

            var result = await _controller.Delete(chargePaymentModelWithUserInfo);

            var jsonResult = Assert.IsType<JsonResult>(result);
            var item = Assert.IsType<ChargePaymentItem>(jsonResult.Value);
            Assert.Equal(paymentModel.Id, item.Id);
            Assert.Equal(paymentModel.ChargeEntryId, item.ChargeEntryId);
            Assert.Equal(paymentModel.Date, item.Date);
            Assert.Equal(paymentModel.Amount, item.Amount);
            Assert.Equal(paymentModel.CPTCode, item.CPTCode);
            Assert.Equal(paymentModel.ReasonCodeId, item.ReasonCodeId);
            Assert.Equal(paymentModel.ReasonCode, item.ReasonCode);
            Assert.Equal(paymentModel.PaymentMethodId, item.PaymentMethodId);
            Assert.Equal(paymentModel.PaymentMethod, item.PaymentMethod);
            Assert.Equal(paymentModel.Reference, item.Reference);
            Assert.Equal(paymentModel.PostedBy, item.PostedBy);
        }

        [Fact]
        public async Task Save_LogsErrorAndReturnsBadRequest_WhenServiceThrows()
        {
            // Arrange
            var paymentModel = new ChargePaymentModel { Id = 1 };
            var paymentModelWithUserInfo = new ChargePaymentModelWithUserInfo
            {
                ChargePaymentModel = paymentModel,
                MemberId = 2
            };
            var mappedItem = new ChargePaymentItem { Id = 1 };

            _mapperMock
                .Setup(m => m.Map<ChargePaymentItem>(paymentModel))
                .Returns(mappedItem);

            _chargePaymentServiceMock
                .Setup(s => s.Save(mappedItem, paymentModelWithUserInfo.MemberId))
                .ThrowsAsync(new Exception("Save error"));

            // Act
            var result = await _controller.Save(paymentModelWithUserInfo);

            // Assert - BadRequest
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Save error", badRequestResult.Value);

            // Assert - LogError
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString().Contains(
                            "ChargePaymentController.Save -Save ChargePayment failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

    }
}