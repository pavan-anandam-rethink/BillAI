using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.Web.XUnit.Tests.Controllers
{
    public class PaymentAttachmentControllerTests
    {
        private readonly Mock<IPaymentAttachmentService> _mockPaymentAttachmentService = new();

        private readonly Mock<ILogger<PaymentAttachmentController>> _loggerMock = new();

        private PaymentAttachmentController CreateController()
        {
            return new PaymentAttachmentController(_mockPaymentAttachmentService.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task UploadFile_ReturnsOk_WhenCalled()
        {
            // Arrange
            var model = new PaymentUploadModelWithUserInfo
            {
                FileName = "test.pdf",
                FileMimeType = "application/pdf",
                PaymentId = 123,
                AccountInfoId = 1,
                MemberId = 2
            };

            var expectedResult = 123;

            _mockPaymentAttachmentService
                .Setup(s => s.UploadFile(model))
                .ReturnsAsync(expectedResult);

            var controller = CreateController();

            // Act
            var result = await controller.UploadFile(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResult, okResult.Value);
        }

        [Fact]
        public async Task DeleteUpload_ReturnsOk_WhenCalled()
        {
            // Arrange
            var model = new IdWithUserInfo { Id = 1 };

            _mockPaymentAttachmentService
                .Setup(s => s.DeleteUpload(model))
                .Returns(Task.CompletedTask);

            var controller = CreateController();

            // Act
            var result = await controller.DeleteUpload(model);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task DeleteUploads_ReturnsOk_WhenCalled()
        {
            // Arrange
            var model = new DeleteAttachmentsModelWithUserInfo
            {
                Ids = new List<int> { 1, 2 }
            };

            _mockPaymentAttachmentService
                .Setup(s => s.DeleteUploads(model))
                .Returns(Task.CompletedTask);

            var controller = CreateController();

            // Act
            var result = await controller.DeleteUploads(model);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task GetFileUpload_ReturnsFile_WhenCalled()
        {
            // Arrange
            int id = 10;
            var expectedFile = new PaymentAttachmentReturnModel
            {
                MemoryStream = new MemoryStream(new byte[] { 1, 2, 3 }),
                Filename = "receipt.pdf"
            };

            _mockPaymentAttachmentService
                .Setup(s => s.GetUpload(id))
                .ReturnsAsync(expectedFile);

            var controller = CreateController();

            // Act
            var result = await controller.GetFileUpload(id);

            // Assert
            var fileResult = Assert.IsType<FileStreamResult>(result);
            Assert.Equal(MediaTypeNames.Application.Octet, fileResult.ContentType);
            Assert.Equal(expectedFile.Filename, fileResult.FileDownloadName);
        }

        [Fact]
        public async Task GetPaymentAttachments_ReturnsOk_WhenCalled()
        {
            // Arrange
            var model = new GetByIdSortFilterWithUserInfo { Id = 1 };
            var expectedAttachments = new AttachmentsResponseModel
            {
                Data = new List<AttachmentViewModel>
                {
                    new AttachmentViewModel
                    {
                        Id = 1,
                        Filename = "file1.pdf",
                        DateCreated = DateTime.Now, // or any specific date
                        CreatedBy = "John Doe" // optional, since it's [NotMapped]
                    },
                    new AttachmentViewModel
                    {
                        Id = 2,
                        Filename = "file2.docx",
                        DateCreated = DateTime.Now,
                        CreatedBy = "Jane Smith"
                    }
                },
                TotalCount = 2
            };


            _mockPaymentAttachmentService
                .Setup(s => s.GetPaymentAttachmentsAsync(model))
                .ReturnsAsync(expectedAttachments);

            var controller = CreateController();

            // Act
            var result = await controller.GetPaymentAttachments(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedAttachments, okResult.Value);
        }

        [Fact]
        public async Task RenameAttachment_ReturnsOk_WhenCalled()
        {
            // Arrange
            var model = new RenameAttachmentModelWithUserInfo
            {
                AttachmentId = 1,
                FileName = "updated.pdf"
            };

            _mockPaymentAttachmentService
                .Setup(s => s.RenameAttachmentAsync(model))
                .Returns(Task.CompletedTask);

            var controller = CreateController();

            // Act
            var result = await controller.RenameAttachment(model);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        // ---------- Exception Handling Tests ----------

        [Fact]
        public async Task UploadFile_LogsErrorAndReturnsBadRequest_WhenServiceThrows()
        {
            // Arrange
            var model = new PaymentUploadModelWithUserInfo
            {
                PaymentId = 123
            };

            _mockPaymentAttachmentService
                .Setup(s => s.UploadFile(model))
                .ThrowsAsync(new Exception("Service failed"));

            var controller = CreateController();

            // Act
            var result = await controller.UploadFile(model);

            // Assert - BadRequest
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Service failed", badRequestResult.Value);

            // Assert - Service called
            _mockPaymentAttachmentService.Verify(
                s => s.UploadFile(model),
                Times.Once);

            // Assert - LogError
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString().Contains(
                            "PaymentAttachmentController.UploadFile -UploadFile failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }


        [Fact]
        public async Task GetFileUpload_LogsErrorAndReturnsBadRequest_WhenServiceThrows()
        {
            // Arrange
            int id = 1;

            _mockPaymentAttachmentService
                .Setup(s => s.GetUpload(id))
                .ThrowsAsync(new Exception("Failed to retrieve file"));

            var controller = CreateController();

            // Act
            var result = await controller.GetFileUpload(id);

            // Assert - BadRequest
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Failed to retrieve file", badRequestResult.Value);

            // Assert - LogError
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString().Contains(
                            "PaymentAttachmentController.GetFileUpload -GetFileUpload failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

    }
}
