using BillingService.Domain.Interfaces.PatientInvoice;
using BillingService.Domain.Models;
using BillingService.Domain.Models.PatientInvoice;
using BillingService.Domain.Models.PaymentClaims;
using BillingService.Domain.Templates.ViewModels;
using BillingService.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Entities.Billing.PatientInvoice;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;

namespace BillingService.Web.XUnit.Tests.Controllers
{
    public class PatientInvoiceControllerTests
    {
        private readonly Mock<IPatientInvoiceService> _mockInvoiceService;
        private readonly Mock<ILogger<PatientInvoiceController>> _mockLogger;
        private readonly PatientInvoiceController _controller;
        private readonly IRepository<BillingDbContext, PatientInvoiceDetailsEntity> MockPatientInvoiceDetails;

        public PatientInvoiceControllerTests()
        {
            _mockInvoiceService = new Mock<IPatientInvoiceService>();
            _mockLogger = new Mock<ILogger<PatientInvoiceController>>();
            _controller = new PatientInvoiceController(_mockInvoiceService.Object, _mockLogger.Object);
            MockPatientInvoiceDetails = new Mock<IRepository<BillingDbContext, PatientInvoiceDetailsEntity>>().Object;
        }

        [Fact]
        public async Task GetPICreationDetails_ReturnsOk_WithData()
        {
            // Arrange
            var filter = new CreateInvoiceFilters { Skip = 0, Take = 10 };
            var data = new List<PatientInvoiceCreationModel> { new PatientInvoiceCreationModel { Id = 1 } };
            _mockInvoiceService.Setup(s => s.GetPICreationDetails(filter))
                .ReturnsAsync((data, 1));

            // Act
            var result = await _controller.GetPICreationDetails(filter);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetPICreationDetails_ReturnsBadRequest_OnException()
        {
            // Arrange
            var filter = new CreateInvoiceFilters();
            _mockInvoiceService.Setup(s => s.GetPICreationDetails(filter))
                .ThrowsAsync(new Exception("Test error"));

            // Act
            var result = await _controller.GetPICreationDetails(filter);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Test error", badRequest.Value);
        }

        [Fact]
        public async Task GetPICreationDetails_ReturnsOk_WhenNoDataFound()
        {
            // Arrange
            var filter = new CreateInvoiceFilters();
            var emptyData = new List<PatientInvoiceCreationModel>();

            _mockInvoiceService
                .Setup(s => s.GetPICreationDetails(filter))
                .ReturnsAsync((emptyData, 0));

            // Act
            var result = await _controller.GetPICreationDetails(filter);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetPICreationDetails_ReturnsBadRequest_WhenExceptionMessageIsNull()
        {
            // Arrange
            var filter = new CreateInvoiceFilters();

            _mockInvoiceService
                .Setup(s => s.GetPICreationDetails(filter))
                .ThrowsAsync(new Exception());

            // Act
            var result = await _controller.GetPICreationDetails(filter);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task GetPICreationDetails_CallsServiceOnce()
        {
            // Arrange
            var filter = new CreateInvoiceFilters();

            _mockInvoiceService
                .Setup(s => s.GetPICreationDetails(filter))
                .ReturnsAsync((new List<PatientInvoiceCreationModel>(), 0));

            // Act
            await _controller.GetPICreationDetails(filter);

            // Assert
            _mockInvoiceService.Verify(
                s => s.GetPICreationDetails(filter),
                Times.Once);
        }

        [Fact]
        public async Task GetPICreationDetails_ReturnsOk_WhenTotalCountIsGreaterThanZero()
        {
            // Arrange
            var filter = new CreateInvoiceFilters();
            var data = new List<PatientInvoiceCreationModel>
    {
        new PatientInvoiceCreationModel { Id = 99 }
    };

            _mockInvoiceService
                .Setup(s => s.GetPICreationDetails(filter))
                .ReturnsAsync((data, 1));

            // Act
            var result = await _controller.GetPICreationDetails(filter);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }


        [Fact]
        public async Task GetInvoiceDetails_ReturnsOk_WithData()
        {
            // Arrange
            var filter = new PendingCollectionFilters { Skip = 0, Take = 10 };
            var data = new List<InvoiceDetailsModel> { new InvoiceDetailsModel { Id = 1, ClientName = "Test" } };
            var userList = new List<ClaimFilterOptionModel> { new ClaimFilterOptionModel { Id = 1, Name = "User" } };
            _mockInvoiceService.Setup(s => s.GetInvoiceDetails(filter))
                .ReturnsAsync((data, userList, 1));

            // Act
            var result = await _controller.GetInvoiceDetails(filter);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetInvoiceDetails_ReturnsBadRequest_OnException()
        {
            // Arrange
            var filter = new PendingCollectionFilters();
            _mockInvoiceService.Setup(s => s.GetInvoiceDetails(filter))
                .ThrowsAsync(new Exception("Test error"));

            // Act
            var result = await _controller.GetInvoiceDetails(filter);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Test error", badRequest.Value);
        }

        [Fact]
        public async Task GetInvoiceDetails_ReturnsOk_WhenTakeIsZero()
        {
            // Arrange
            var filter = new PendingCollectionFilters { Skip = 0, Take = 0 };

            var data = new List<InvoiceDetailsModel>
    {
        new InvoiceDetailsModel { Id = 1, ClientName = "Test" }
    };

            var userList = new List<ClaimFilterOptionModel>
    {
        new ClaimFilterOptionModel { Id = 1, Name = "Test" }
    };

            _mockInvoiceService
                .Setup(s => s.GetInvoiceDetails(filter))
                .ReturnsAsync((data, userList, 1));

            // Act
            var result = await _controller.GetInvoiceDetails(filter);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetInvoiceDetails_ReturnsBadRequest_WhenExceptionMessageIsNull()
        {
            // Arrange
            var filter = new PendingCollectionFilters();

            _mockInvoiceService
                .Setup(s => s.GetInvoiceDetails(filter))
                .ThrowsAsync(new Exception());

            // Act
            var result = await _controller.GetInvoiceDetails(filter);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task GetInvoiceDetails_ReturnsOk_WhenUserListHasData()
        {
            // Arrange
            var filter = new PendingCollectionFilters { Skip = 0, Take = 10 };

            var userList = new List<ClaimFilterOptionModel>
    {
        new ClaimFilterOptionModel { Id = 10, Name = "Client A" }
    };

            _mockInvoiceService
                .Setup(s => s.GetInvoiceDetails(filter))
                .ReturnsAsync((new List<InvoiceDetailsModel>(), userList, 1));

            // Act
            var result = await _controller.GetInvoiceDetails(filter);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task PrintPreview_ReturnsOk_WithPdf()
        {
            // Arrange
            var requests = new List<InvoiceRequestModel> { new InvoiceRequestModel { AccountId = 1, ClientId = 1, Charges = new List<ChargeModel>() } };
            var pdfBytes = new byte[] { 1, 2, 3 };
            var errors = new List<string>();
            _mockInvoiceService.Setup(s => s.GeneratePDF(requests, false, false, null))
                .ReturnsAsync((pdfBytes, errors));

            // Act
            var result = await _controller.PrintPreview(requests);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task PrintPreview_ReturnsOk_WithNullPdf_AndErrors()
        {
            // Arrange
            var requests = new List<InvoiceRequestModel>();
            var errors = new List<string> { "Error1" };
            _mockInvoiceService.Setup(s => s.GeneratePDF(requests, false, false, null))
                .ReturnsAsync((null, errors));

            // Act
            var result = await _controller.PrintPreview(requests);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            //var response = Assert.IsType<PdfResponse>(okResult.Value);
            //Assert.Null(response.PdfBase64);
            //Assert.Single(response.Errors);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task PrintPreview_ReturnsBadRequest_OnException()
        {
            // Arrange
            var requests = new List<InvoiceRequestModel>();
            _mockInvoiceService.Setup(s => s.GeneratePDF(requests, false, false, null))
                .ThrowsAsync(new Exception("Test error"));

            // Act
            var result = await _controller.PrintPreview(requests);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task PrintAndSubmit_ReturnsOk_WithPdf()
        {
            // Arrange
            var request = new PrintAndSubmitRequestModel
            {
                InvoiceRequests = new List<InvoiceRequestModel> { new InvoiceRequestModel { AccountId = 1, ClientId = 1, Charges = new List<ChargeModel>() } },
                includePreviousInvoices = true
            };
            var pdfBytes = new byte[] { 1, 2, 3 };
            var errors = new List<string>();
            _mockInvoiceService.Setup(s => s.GeneratePDF(request.InvoiceRequests, true, request.includePreviousInvoices, null))
                .ReturnsAsync((pdfBytes, errors));

            // Act
            var result = await _controller.PrintAndSubmit(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            //var response = Assert.IsType<PdfResponse>(okResult.Value);
            //Assert.Equal(Convert.ToBase64String(pdfBytes), response.PdfBase64);
            //Assert.Empty(response.Errors);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task PrintAndSubmit_ReturnsBadRequest_OnException()
        {
            // Arrange
            var request = new PrintAndSubmitRequestModel { InvoiceRequests = new List<InvoiceRequestModel>(), includePreviousInvoices = false };
            _mockInvoiceService.Setup(s => s.GeneratePDF(request.InvoiceRequests, true, request.includePreviousInvoices, null))
                .ThrowsAsync(new Exception("Test error"));

            // Act
            var result = await _controller.PrintAndSubmit(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task GetInvoicePDF_ReturnsOk_WithPdf()
        {
            // Arrange
            var request = new GetInvoicePDFRequestModel { AccountId = 1, ClientId = 2, InvoiceNo = "INV123" };
            var pdfBytes = new byte[] { 1, 2, 3 };
            var errors = new List<string>();
            _mockInvoiceService.Setup(s => s.GetInvoicePDF(request.AccountId, request.ClientId, request.InvoiceNo))
                .ReturnsAsync((pdfBytes, errors));

            // Act
            var result = await _controller.GetInvoicePDF(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            //var response = Assert.IsType<PdfResponse>(okResult.Value);
            //Assert.Equal(Convert.ToBase64String(pdfBytes), response.PdfBase64);
            //Assert.Empty(response.Errors);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetInvoicePDF_ReturnsBadRequest_OnException()
        {
            // Arrange
            var request = new GetInvoicePDFRequestModel { AccountId = 1, ClientId = 2, InvoiceNo = "INV123" };
            _mockInvoiceService.Setup(s => s.GetInvoicePDF(request.AccountId, request.ClientId, request.InvoiceNo))
                .ThrowsAsync(new Exception("Test error"));

            // Act
            var result = await _controller.GetInvoicePDF(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task GetInvoicePDF_ReturnsOk_WhenPdfGenerated_WithErrors()
        {
            // Arrange
            var request = new GetInvoicePDFRequestModel
            {
                AccountId = 1,
                ClientId = 2,
                InvoiceNo = "INV123"
            };

            var pdfBytes = new byte[] { 1, 2, 3 };
            var errors = new List<string> { "Missing guarantor info" };

            _mockInvoiceService
                .Setup(s => s.GetInvoicePDF(request.AccountId, request.ClientId, request.InvoiceNo))
                .ReturnsAsync((pdfBytes, errors));

            // Act
            var result = await _controller.GetInvoicePDF(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task GetInvoicePDF_ReturnsBadRequest_WhenExceptionMessageIsNull()
        {
            // Arrange
            var request = new GetInvoicePDFRequestModel
            {
                AccountId = 1,
                ClientId = 2,
                InvoiceNo = "INV123"
            };

            _mockInvoiceService
                .Setup(s => s.GetInvoicePDF(request.AccountId, request.ClientId, request.InvoiceNo))
                .ThrowsAsync(new Exception());

            // Act
            var result = await _controller.GetInvoicePDF(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task GetInvoicePDF_ReturnsOk_WhenPdfIsEmptyByteArray()
        {
            // Arrange
            var request = new GetInvoicePDFRequestModel
            {
                AccountId = 1,
                ClientId = 2,
                InvoiceNo = "INV123"
            };

            _mockInvoiceService
                .Setup(s => s.GetInvoicePDF(request.AccountId, request.ClientId, request.InvoiceNo))
                .ReturnsAsync((Array.Empty<byte>(), new List<string>()));

            // Act
            var result = await _controller.GetInvoicePDF(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

    }
}
