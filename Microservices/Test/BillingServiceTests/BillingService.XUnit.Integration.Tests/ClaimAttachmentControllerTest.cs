using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests
{
    public class ClaimAttachmentControllerTests
    {
        private readonly Mock<IClaimAttachmentService> _claimAttachmentServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<ClaimAttachmentController>> _loggerMock;
        private readonly ClaimAttachmentController _controller;

        public ClaimAttachmentControllerTests()
        {
            _claimAttachmentServiceMock = new Mock<IClaimAttachmentService>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<ClaimAttachmentController>>();

            _controller = new ClaimAttachmentController(
                _claimAttachmentServiceMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Delete_ReturnsJsonResult_WithExpectedValue()
        {
            // Arrange
            var model = new ClaimAttachmentModelWithUserInfo
            {
                AccountInfoId = 123,
                MemberId = 456,
                ClaimAttachmentModel = new ClaimAttachmentModel
                {
                    Id = 1,
                    FileName = "testfile.pdf",
                    FileSize = 1024,
                    FileMimeType = "application/pdf",
                    Notes = "Unit Test",
                    FilePath = "/files/testfile.pdf",
                    DateCreated = System.DateTime.UtcNow,
                    ClaimId = 789,
                    FileLink = "http://example.com/testfile.pdf"
                }
            };

            var mappedItem = new ClaimAttachmentItem { Id = 1, FileName = "testfile.pdf" };
            var deletedItem = new ClaimAttachmentItem { Id = 1, FileName = "testfile.pdf" };

            _mapperMock.Setup(m => m.Map<ClaimAttachmentItem>(model.ClaimAttachmentModel))
                       .Returns(mappedItem);

            _claimAttachmentServiceMock.Setup(s => s.Delete(mappedItem, model.MemberId, model.AccountInfoId))
                                       .ReturnsAsync(deletedItem);

            _mapperMock.Setup(m => m.Map<ClaimAttachmentModel>(deletedItem))
                       .Returns(model.ClaimAttachmentModel);

            // Act
            var result = await _controller.Delete(model);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var actualValue = Assert.IsType<ClaimAttachmentModel>(jsonResult.Value);

            Assert.Equal(model.ClaimAttachmentModel.Id, actualValue.Id);
            Assert.Equal("testfile.pdf", actualValue.FileName);

            _mapperMock.Verify(m => m.Map<ClaimAttachmentItem>(model.ClaimAttachmentModel), Times.Once);
            _claimAttachmentServiceMock.Verify(s => s.Delete(mappedItem, model.MemberId, model.AccountInfoId), Times.Once);
            _mapperMock.Verify(m => m.Map<ClaimAttachmentModel>(deletedItem), Times.Once);
        }

        [Fact]
        public async Task UploadFile_ReturnsOkResult_WithZero_WhenFileAlreadyExists()
        {
            // Arrange
            var uploadModel = new ClaimUploadModelWithUserInfo
            {
                AccountInfoId = 123,
                MemberId = 456,
                ClaimId = 789,
                FileName = "upload.pdf",
                Data = new byte[] { 1, 2, 3 }
            };

            _claimAttachmentServiceMock.Setup(s => s.UploadFileAsync(uploadModel))
                                       .ReturnsAsync(0);

            // Act
            var result = await _controller.UploadFile(uploadModel);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualValue = Assert.IsType<int>(okResult.Value);

            Assert.Equal(0, actualValue);
            _claimAttachmentServiceMock.Verify(s => s.UploadFileAsync(uploadModel), Times.Once);
        }

        [Fact]
        public async Task UploadFile_ReturnsBadRequest_WhenExceptionIsThrown()
        {
            // Arrange
            var uploadModel = new ClaimUploadModelWithUserInfo
            {
                AccountInfoId = 123,
                MemberId = 456,
                ClaimId = 789,
                FileName = "upload.pdf",
                FileMimeType = "application/pdf",
                Data = new byte[] { 1, 2, 3 }
            };

            var exceptionMessage = "Unexpected error during upload";

            _claimAttachmentServiceMock.Setup(s => s.UploadFileAsync(uploadModel))
                                       .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _controller.UploadFile(uploadModel);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var actualMessage = Assert.IsType<string>(badRequestResult.Value);

            Assert.Equal(exceptionMessage, actualMessage);

            _claimAttachmentServiceMock.Verify(s => s.UploadFileAsync(uploadModel), Times.Once);
        }

        [Fact]
        public async Task GetForClaim_ReturnsOkResult_WithExpectedAttachmentsResponse()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 789, // ClaimId
                AccountInfoId = 123,
                MemberId = 456
            };

            var expectedResponse = new AttachmentsResponseModel
            {
                TotalCount = 2,
                Data = new List<AttachmentViewModel>
        {
            new AttachmentViewModel
            {
                Id = 1,
                Filename = "file1.pdf",
                DateCreated = DateTime.UtcNow,
                CreatedBy = "JohnDoe"
            },
            new AttachmentViewModel
            {
                Id = 2,
                Filename = "file2.pdf",
                DateCreated = DateTime.UtcNow,
                CreatedBy = "JohnDoe"
            }
        }
            };

            _claimAttachmentServiceMock.Setup(s => s.GetForClaimAsync(model))
                                       .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetForClaim(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualValue = Assert.IsType<AttachmentsResponseModel>(okResult.Value);

            Assert.Equal(expectedResponse.TotalCount, actualValue.TotalCount);
            Assert.Equal(expectedResponse.Data.Count, actualValue.Data.Count);
            Assert.Contains(actualValue.Data, a => a.Filename == "file1.pdf");
            Assert.Contains(actualValue.Data, a => a.Filename == "file2.pdf");

            _claimAttachmentServiceMock.Verify(s => s.GetForClaimAsync(model), Times.Once);
        }

        [Fact]
        public async Task RenameAttachment_ReturnsOkResult_WhenRenameIsSuccessful()
        {
            // Arrange
            var model = new RenameAttachmentModelWithUserInfo
            {
                AccountInfoId = 123,
                MemberId = 456,
                AttachmentId = 1,
                FileName = "renamed.pdf"
            };

            _claimAttachmentServiceMock.Setup(s => s.RenameAttachmentAsync(model))
                                       .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RenameAttachment(model);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            _claimAttachmentServiceMock.Verify(s => s.RenameAttachmentAsync(model), Times.Once);
        }

        [Fact]
        public async Task RenameAttachment_ReturnsBadRequest_WhenUserDoesNotOwnAttachment()
        {
            // Arrange
            var model = new RenameAttachmentModelWithUserInfo
            {
                AccountInfoId = 123,
                MemberId = 999, // Wrong user
                AttachmentId = 1,
                FileName = "renamed.pdf"
            };

            _claimAttachmentServiceMock
                .Setup(s => s.RenameAttachmentAsync(model))
                .ThrowsAsync(new UnauthorizedAccessException("User does not own this attachment"));

            // Act
            var result = await _controller.RenameAttachment(model);

            // Assert - BadRequest returned
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("User does not own this attachment", badRequestResult.Value);

            // Assert - Service called once
            _claimAttachmentServiceMock.Verify(s => s.RenameAttachmentAsync(model), Times.Once);

            // Assert - LogError called
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString().Contains("RenameAttachment failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteUpload_ReturnsOkResult_WhenDeleteIsSuccessful()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 1,
                AccountInfoId = 123,
                MemberId = 456
            };

            _claimAttachmentServiceMock.Setup(s => s.DeleteUpload(model))
                                       .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteUpload(model);

            // Assert
            var okResult = Assert.IsType<OkResult>(result);
            _claimAttachmentServiceMock.Verify(s => s.DeleteUpload(model), Times.Once);
        }

        [Fact]
        public async Task DeleteUpload_ReturnsBadRequest_WhenUserDoesNotOwnAttachment()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 1,
                AccountInfoId = 123,
                MemberId = 999 // Wrong user
            };

            _claimAttachmentServiceMock
                .Setup(s => s.DeleteUpload(model))
                .ThrowsAsync(new UnauthorizedAccessException("User does not own this attachment"));

            // Act
            var result = await _controller.DeleteUpload(model);

            // Assert - BadRequest returned
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("User does not own this attachment", badRequestResult.Value);

            // Assert - Service called once
            _claimAttachmentServiceMock.Verify(s => s.DeleteUpload(model), Times.Once);

            // Assert - LogError called
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString().Contains("DeleteUpload failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }


        [Fact]
        public async Task GetFileUpload_ReturnsOkResult_WithDownloadUrl()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 1,
                AccountInfoId = 123,
                MemberId = 456
            };

            var expectedUrl = "http://example.com/file.pdf";

            _claimAttachmentServiceMock.Setup(s => s.GetUploadAsync(model))
                                       .ReturnsAsync(expectedUrl);

            // Act
            var result = await _controller.GetFileUpload(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            // The controller returns an anonymous object { DownloadUrl = result }
            var actualValue = Assert.IsType<Dictionary<string, string>>(
                okResult.Value.GetType()
                    .GetProperties()
                    .ToDictionary(p => p.Name, p => p.GetValue(okResult.Value)?.ToString())
            );

            Assert.True(actualValue.ContainsKey("DownloadUrl"));
            Assert.Equal(expectedUrl, actualValue["DownloadUrl"]);

            _claimAttachmentServiceMock.Verify(s => s.GetUploadAsync(model), Times.Once);
        }

        [Fact]
        public async Task GetFileUpload_ReturnsBadRequest_WhenUserDoesNotOwnAttachment()
        {
            // Arrange
            var model = new IdWithUserInfo
            {
                Id = 1,
                AccountInfoId = 123,
                MemberId = 999 // Wrong user
            };

            _claimAttachmentServiceMock
                .Setup(s => s.GetUploadAsync(model))
                .ThrowsAsync(new UnauthorizedAccessException("User does not own this attachment"));

            // Act
            var result = await _controller.GetFileUpload(model);

            // Assert - BadRequest returned
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("User does not own this attachment", badRequestResult.Value);

            // Assert - Service called once
            _claimAttachmentServiceMock.Verify(s => s.GetUploadAsync(model), Times.Once);

            // Assert - LogError called
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString().Contains("GetFileUpload failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }




    }
}