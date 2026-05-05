using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BillingService.Web.XUnit.Tests.Controllers
{
    public class PaymentNoteControllerTest
    {
        private readonly Mock<IPaymentNoteService> _noteServiceMock;
        private readonly PaymentNoteController _controller;
        private readonly Mock<ILogger<PaymentNoteController>> _mockLogger;


        public PaymentNoteControllerTest()
        {
            _noteServiceMock = new Mock<IPaymentNoteService>();
            _mockLogger = new Mock<ILogger<PaymentNoteController>>();
            _controller = new PaymentNoteController(_noteServiceMock.Object,_mockLogger.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsOk()
        {
            int paymentId = 1;
            var expected = new List<PaymentNote>() { new PaymentNote { PaymentId = paymentId, Note = "Test" } };
            _noteServiceMock.Setup(s => s.GetAll(paymentId)).ReturnsAsync(expected);

            var result = await _controller.GetAll(paymentId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task Add_ReturnsOk()
        {
            var model = new PaymentNoteSaveModel { PaymentId = 1, Note = "Add" };
            var expected = new PaymentNoteSmall { PaymentId = 1, Note = "Add" };
            _noteServiceMock.Setup(s => s.AddNote(model)).ReturnsAsync(expected.PaymentId);

            var result = await _controller.Add(model);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected.PaymentId, okResult.Value);
        }

        [Fact]
        public async Task AddToSeveral_ReturnsOk()
        {
            var models = new[] { new PaymentNoteSaveModel { PaymentId = 1, Note = "A" }, new PaymentNoteSaveModel { PaymentId = 2, Note = "B" } };
            var expectedId = 1;
            _noteServiceMock.Setup(s => s.AddToPaymentsAsync(models)).ReturnsAsync(expectedId);

            var result = await _controller.AddToSeveral(models);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedId, okResult.Value);
        }

        [Fact]
        public async Task Delete_ReturnsOk()
        {
            var model = new PaymentNoteDeleteModel { Id = 1, DateCreated = DateTime.Now };
            var expectedId = 1;
            _noteServiceMock.Setup(s => s.DeleteNote(model)).ReturnsAsync(expectedId);

            var result = await _controller.Delete(model);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedId, okResult.Value);
        }
    }
}