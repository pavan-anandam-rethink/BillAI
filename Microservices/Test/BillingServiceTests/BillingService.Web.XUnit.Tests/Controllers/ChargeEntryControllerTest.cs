using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.Web.XUnit.Tests.Controllers
{
    public class ChargeEntryControllerTest
    {
        private readonly Mock<IChargeEntryService> _chargeEntryServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly ChargeEntryController _controller;
        private readonly Mock<ILogger<ChargeEntryController>> _mockLogger;

        public ChargeEntryControllerTest()
        {
            _chargeEntryServiceMock = new Mock<IChargeEntryService>();
            _mapperMock = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<ChargeEntryController>>();
            _controller = new ChargeEntryController(_mapperMock.Object, _chargeEntryServiceMock.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task AddNote_ReturnsOkResult_WhenServiceSucceeds()
        {
            // Arrange
            var model = new AddNoteModel { ChargeId = 1, NoteText = "Test", NoteCreatedBy = 2 };
            var expectedNote = new ChargeNoteModel { NoteText = "Test", NoteCreatorName = "User", NoteCreatedDate = DateTime.UtcNow };
            _chargeEntryServiceMock.Setup(s => s.AddChargeNoteAsync(model)).ReturnsAsync(expectedNote);

            // Act
            var result = await _controller.AddNote(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedNote, okResult.Value);
        }

        [Fact]
        public async Task AddNote_ReturnsBadRequest_WhenServiceThrowsException()
        {
            // Arrange
            var model = new AddNoteModel { ChargeId = 1, NoteText = "Test", NoteCreatedBy = 2 };
            _chargeEntryServiceMock.Setup(s => s.AddChargeNoteAsync(model)).ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.AddNote(model);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Service error", badRequest.Value);
        }

        [Fact]
        public async Task DeleteNote_ReturnsOkResult_WhenServiceSucceeds()
        {
            // Arrange
            var chargeId = 1;
            _chargeEntryServiceMock.Setup(s => s.DeleteChargeNoteAsync(chargeId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteNote(chargeId);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task DeleteNote_ReturnsBadRequest_WhenServiceThrowsException()
        {
            // Arrange
            var chargeId = 1;
            _chargeEntryServiceMock.Setup(s => s.DeleteChargeNoteAsync(chargeId)).ThrowsAsync(new Exception("Delete error"));

            // Act
            var result = await _controller.DeleteNote(chargeId);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Delete error", badRequest.Value);
        }
    }
}
