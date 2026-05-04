using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models;
using BillingService.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.Web.XUnit.Tests.Controllers
{
    public class PaymentPostingControllerTests
    {
        private readonly Mock<IPaymentPostingService> _paymentPostingServiceMock;
        private readonly Mock<IFunderService> _funderServiceMock;
        private readonly Mock<IChildProfileService> _childProfileServiceMock;
        private readonly PaymentPostingController _controller;

        public PaymentPostingControllerTests()
        {
            _paymentPostingServiceMock = new Mock<IPaymentPostingService>();
            _funderServiceMock = new Mock<IFunderService>();
            _childProfileServiceMock = new Mock<IChildProfileService>();
            _controller = new PaymentPostingController(
                _paymentPostingServiceMock.Object,
                _funderServiceMock.Object,
                _childProfileServiceMock.Object);
        }

        [Fact]
        public async Task ReconcileClaim_ReturnsOkResult_WithExpectedValue()
        {
            // Arrange
            var model = new ClaimPaymentUpdateModel { PaymentId = new int[] { 1 }, ClaimId = 2, MemberId = 3 };
            var expectedResult = 1;
            _paymentPostingServiceMock
                .Setup(s => s.ReconcileClaimAsync(model.PaymentId, model.ClaimId, model.MemberId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.ReconcileClaim(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResult, okResult.Value);
        }
    }
}
