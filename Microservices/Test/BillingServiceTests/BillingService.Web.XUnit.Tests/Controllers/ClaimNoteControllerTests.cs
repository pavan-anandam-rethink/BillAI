using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Web.Controllers;
using BillingService.Web.Helpers.HttpClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data.SqlClient;
using System.Reflection;

namespace BillingService.Web.XUnit.Tests.Controllers
{
    public class ClaimNoteControllerTests
    {
        private readonly Mock<IClaimNoteService> _claimNoteServiceMock;
        private readonly ClaimNoteController _controller;
        private readonly Mock<ILogger<ClaimNoteController>> _mockLogger;
        public ClaimNoteControllerTests()
        {
            _claimNoteServiceMock = new Mock<IClaimNoteService>();
            _mockLogger = new Mock<ILogger<ClaimNoteController>>();
            _controller = new ClaimNoteController(
                _claimNoteServiceMock.Object,
                _mockLogger.Object
                );

        }

        [Fact]
        public async Task GetAll_ReturnsOk_WhenServiceSucceeds()
        {
            // Arrange
            var model = new ClaimNoteGetAllModel
            {
                AccountInfoId = 1,
                MemberId = 2,
                Id = 10
            };

            var expectedNotes = new List<ClaimNote>();
            var expectedResponse = ActionResponse.SuccessResult(expectedNotes);

            _claimNoteServiceMock
                .Setup(s => s.GetAllAsync(model))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetAll(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResponse, okResult.Value);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WhenModelIsNull()
        {
            // Arrange
            ClaimNoteGetAllModel model = null;

            var expectedResp = ActionResponse.SuccessResult(new List<ClaimNote>());

            _claimNoteServiceMock
                .Setup(s => s.GetAllAsync(null))
                .ReturnsAsync(expectedResp);

            // Act
            var result = await _controller.GetAll(model);

            // Assert - Ok result
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResp, okResult.Value);
        }



        [Fact]
        public async Task GetAll_ReturnsOk_WhenServiceReturnsEmptyList()
        {
            // Arrange
            var model = new ClaimNoteGetAllModel
            {
                AccountInfoId = 1,
                MemberId = 2,
                Id = 100
            };

            var expected = ActionResponse.SuccessResult(new List<ClaimNote>());

            _claimNoteServiceMock
                .Setup(s => s.GetAllAsync(model))
                .ReturnsAsync(expected);

            // Act
            var result = await _controller.GetAll(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task Add_ReturnsOk_WhenServiceSucceeds()
        {
            // Arrange
            var model = new ClaimNoteSaveModel
            {
                ClaimId = 10,
                MemberId = 2,
                Note = "Test note",
                RemindDate = DateTime.UtcNow
            };

            var expectedResponse = ActionResponse.SuccessResult();

            _claimNoteServiceMock
                .Setup(s => s.AddAsync(model))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Add(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResponse, okResult.Value);
        }

        [Fact]
        public async Task Add_ReturnsOk_WhenNoteIsEmpty()
        {
            // Arrange
            var model = new ClaimNoteSaveModel
            {
                ClaimId = 10,
                MemberId = 2,
                Note = "",
                RemindDate = DateTime.UtcNow
            };

            var expectedResponse = ActionResponse.SuccessResult();

            _claimNoteServiceMock
                .Setup(s => s.AddAsync(model))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Add(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResponse, okResult.Value);
        }

        [Fact]
        public async Task Add_ReturnsOk_WithSuccessActionResponse()
        {
            // Arrange
            var model = new ClaimNoteSaveModel { Note = "Hi" };

            var expectedResponse = ActionResponse.SuccessResult("done");

            _claimNoteServiceMock
                .Setup(s => s.AddAsync(model))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Add(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResponse, okResult.Value);
        }

        [Fact]
        public async Task AddToSeveral_ReturnsOk_WhenServiceSucceeds()
        {
            // Arrange
            var model = new ClaimNoteRequestModel
            {
                MemberId = 5,
                ClaimNoteModels = new[]
                {
                    new ClaimNoteSmall { ClaimId = 101, Note = "Note1", RemindDate = DateTime.UtcNow },
                    new ClaimNoteSmall { ClaimId = 102, Note = "Note2", RemindDate = DateTime.UtcNow }
                }
            };

            var expectedResponse = ActionResponse.SuccessResult();

            _claimNoteServiceMock
                .Setup(s => s.AddToClaimsAsync(model))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.AddToSeveral(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResponse, okResult.Value);
        }

        [Fact]
        public async Task AddToSeveral_WhenExceptionThrown_ReturnsBadRequest()
        {
            // Arrange
            var model = new ClaimNoteRequestModel
            {
                MemberId = 10,
                ClaimNoteModels = new[]
                {
            new ClaimNoteSmall
            {
                ClaimId = 1,
                Note = "Test",
                RemindDate = DateTime.UtcNow
            }
        }
            };

            _claimNoteServiceMock
                .Setup(x => x.AddToClaimsAsync(It.IsAny<ClaimNoteRequestModel>()))
                .ThrowsAsync(new Exception("AddToSeveral failed"));

            // Act
            var result = await _controller.AddToSeveral(model);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("AddToSeveral failed", badRequest.Value);
        }

        [Fact]
        public async Task AddToSeveral_ReturnsOk_WhenModelHasEmptyArray()
        {
            // Arrange
            var model = new ClaimNoteRequestModel
            {
                MemberId = 5,
                ClaimNoteModels = Array.Empty<ClaimNoteSmall>()
            };

            var expectedResponse = ActionResponse.SuccessResult();

            _claimNoteServiceMock
                .Setup(s => s.AddToClaimsAsync(model))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.AddToSeveral(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResponse, okResult.Value);
        }

        [Fact]
        public async Task AddToSeveral_ReturnsOk_WhenServiceReturnsMessage()
        {
            // Arrange
            var model = new ClaimNoteRequestModel
            {
                MemberId = 1,
                ClaimNoteModels = new[]
                {
            new ClaimNoteSmall { ClaimId = 200, Note = "Hello", RemindDate = DateTime.UtcNow }
        }
            };

            var expectedData = "Added";

            var expectedResponse = ActionResponse.SuccessResult(expectedData);

            _claimNoteServiceMock
                .Setup(s => s.AddToClaimsAsync(model))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.AddToSeveral(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResponse, okResult.Value);
        }

        [Fact]
        public async Task AddToSeveral_ReturnsOk_WhenModelIsNull()
        {
            // Arrange
            ClaimNoteRequestModel model = null;

            var expectedResponse = ActionResponse.SuccessResult();

            _claimNoteServiceMock
                .Setup(s => s.AddToClaimsAsync(null))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.AddToSeveral(model);

            // Assert - Ok result
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResponse, okResult.Value);

        }


        [Fact]
        public async Task AddToSeveral_ReturnsOk_WhenClaimNotesContainNullValues()
        {
            // Arrange
            var model = new ClaimNoteRequestModel
            {
                MemberId = 5,
                ClaimNoteModels = new[]
                {
            new ClaimNoteSmall { ClaimId = 101, Note = null, RemindDate = DateTime.UtcNow },
            new ClaimNoteSmall { ClaimId = 102, Note = "",   RemindDate = DateTime.UtcNow }
            }
            };

            var expectedResponse = ActionResponse.SuccessResult();

            _claimNoteServiceMock
                .Setup(s => s.AddToClaimsAsync(model))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.AddToSeveral(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResponse, okResult.Value);
        }

        [Fact]
        public async Task Delete_ReturnsOk_WhenNoteExists()
        {
            // Arrange
            var model = new ClaimNoteDeleteModel
            {
                MemberId = 1,
                Id = 101,
                DateCreated = DateTime.UtcNow
            };

            var expectedResponse = ActionResponse.SuccessResult();

            _claimNoteServiceMock
                .Setup(s => s.DeleteAsync(model))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Delete(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResponse, okResult.Value);
        }

        [Fact]
        public async Task Delete_ReturnsFailResult_WhenNoteDoesNotExist()
        {
            // Arrange
            var model = new ClaimNoteDeleteModel
            {
                MemberId = 1,
                Id = 999,
                DateCreated = DateTime.UtcNow
            };

            var expectedError = "Note not found";
            var expectedResponse = ActionResponse.FailResult(expectedError);

            _claimNoteServiceMock
                .Setup(s => s.DeleteAsync(model))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Delete(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actionResponse = Assert.IsType<ActionResponse>(okResult.Value);
            Assert.False(actionResponse.Success);
            Assert.Equal(expectedError, actionResponse.Error);
        }
        [Fact]
        public async Task Delete_WhenExceptionThrown_ReturnsBadRequest()
        {
            // Arrange
            var model = new ClaimNoteDeleteModel { MemberId = 1, Id = 101 };

            _claimNoteServiceMock
                .Setup(s => s.DeleteAsync(It.IsAny<ClaimNoteDeleteModel>()))
                .ThrowsAsync(new Exception("Delete failed"));

            // Act
            var result = await _controller.Delete(model);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Delete failed", badRequest.Value);
        }

        [Fact]
        public async Task Add_WhenExceptionThrown_ReturnsBadRequest()
        {
            // Arrange
            var model = new ClaimNoteSaveModel
            {
                ClaimId = 10,
                MemberId = 2,
                Note = "Test note"
            };

            _claimNoteServiceMock
                .Setup(s => s.AddAsync(It.IsAny<ClaimNoteSaveModel>()))
                .ThrowsAsync(new Exception("Add failed"));

            // Act
            var result = await _controller.Add(model);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Add failed", badRequest.Value);
        }











    }
}
