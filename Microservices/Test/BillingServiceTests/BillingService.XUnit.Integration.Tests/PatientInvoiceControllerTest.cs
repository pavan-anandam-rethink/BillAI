using BillingService.Domain.Interfaces.PatientInvoice;
using BillingService.Domain.Models;
using BillingService.Domain.Models.PatientInvoice;
using BillingService.Domain.Models.PaymentClaims;
using BillingService.Domain.Templates.ViewModels;
using BillingService.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using Xunit;


namespace BillingService.XUnit.Integration.Tests
{
    public class PatientInvoiceControllerTest
    {
        private readonly Mock<IPatientInvoiceService> _serviceMock;
        private readonly Mock<ILogger<PatientInvoiceController>> _loggerMock;
        private readonly PatientInvoiceController _controller;

        public PatientInvoiceControllerTest()
        {
            _serviceMock = new Mock<IPatientInvoiceService>();
            _loggerMock = new Mock<ILogger<PatientInvoiceController>>();
            _controller = new PatientInvoiceController(_serviceMock.Object, _loggerMock.Object);
        }

       

        [Fact]
        public async Task GetPICreationDetails_ReturnsOkResult()
        {
            // Arrange
            var filter = new CreateInvoiceFilters { Filters = new CreateInvoice() };
            var expectedData = new List<PatientInvoiceCreationModel> { new PatientInvoiceCreationModel { Id = 1 } };

            // Mock the service to return data and total count
            _serviceMock.Setup(s => s.GetPICreationDetails(filter))
                .ReturnsAsync((expectedData, expectedData.Count));

            // Act
            var result = await _controller.GetPICreationDetails(filter);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;

            var totalCountProperty = value.GetType().GetProperty("TotalCount");
            var dataProperty = value.GetType().GetProperty("Data");

            Assert.NotNull(totalCountProperty);
            Assert.NotNull(dataProperty);

            var totalCount = (int)totalCountProperty.GetValue(value);
            var data = (IEnumerable<PatientInvoiceCreationModel>)dataProperty.GetValue(value);

            Assert.Equal(expectedData.Count, totalCount);
            Assert.Equal(expectedData, data);
        }


        [Fact]
        public async Task GetPICreationDetails_ReturnsBadRequest_OnException()
        {
            // Arrange
            var filter = new CreateInvoiceFilters { Filters = new CreateInvoice() };
            _serviceMock.Setup(s => s.GetPICreationDetails(filter))
                .ThrowsAsync(new System.Exception("Test exception"));

            // Act
            var result = await _controller.GetPICreationDetails(filter);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Test exception", badRequest.Value);
        }
                

        [Fact]
        public async Task GetInvoiceDetails_ReturnsOkResult()
        {
            // Arrange
            var filter = new PendingCollectionFilters { Filters = new PendingCollection() };
            var expectedData = new List<InvoiceDetailsModel> { new InvoiceDetailsModel { Id = 1 } };
            var expectedUserList = new List<ClaimFilterOptionModel> { new ClaimFilterOptionModel { Id = 1, Name = "Test" } };
            int totalCount = 1;
            _serviceMock.Setup(s => s.GetInvoiceDetails(filter))
                .ReturnsAsync((expectedData, expectedUserList, totalCount));

            // Act
            var result = await _controller.GetInvoiceDetails(filter);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value;

            var totalCountProperty = value.GetType().GetProperty("TotalCount");
            var dataProperty = value.GetType().GetProperty("Data");
            var userListProperty = value.GetType().GetProperty("UserList");

            Assert.NotNull(totalCountProperty);
            Assert.NotNull(dataProperty);
            Assert.NotNull(userListProperty);

            var actualTotalCount = (int)totalCountProperty.GetValue(value);
            var actualData = (IEnumerable<InvoiceDetailsModel>)dataProperty.GetValue(value);
            var actualUserList = (IEnumerable<ClaimFilterOptionModel>)userListProperty.GetValue(value);

            Assert.Equal(totalCount, actualTotalCount);
            Assert.Equal(expectedData, actualData);
            Assert.Equal(expectedUserList, actualUserList);
        }

        [Fact]
        public async Task GetInvoiceDetails_ReturnsBadRequest_OnException()
        {
            // Arrange
            var filter = new PendingCollectionFilters { Filters = new PendingCollection() };
            _serviceMock.Setup(s => s.GetInvoiceDetails(filter))
                .ThrowsAsync(new System.Exception("Test exception"));

            // Act
            var result = await _controller.GetInvoiceDetails(filter);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Test exception", badRequest.Value);
        }

        [Fact]
        public async Task PrintPreview_ReturnsOkResult()
        {
            // Arrange
            var invoiceRequests = new List<InvoiceRequestModel> { new InvoiceRequestModel { AccountId = 1, ClientId = 2, Charges = new List<ChargeModel>() } };
            var pdfData = new byte[] { 1, 2, 3 };
            var errors = new List<string>();
            _serviceMock.Setup(s => s.GeneratePDF(invoiceRequests, false, false, null))
                .ReturnsAsync((pdfData, errors));

            // Act
            var result = await _controller.PrintPreview(invoiceRequests);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<PdfResponse>(okResult.Value);
            Assert.Equal(Convert.ToBase64String(pdfData), response.PdfBase64);
            Assert.Equal(errors, response.Errors);
        }

        [Fact]
        public async Task PrintPreview_ReturnsBadRequest_OnException()
        {
            // Arrange
            var invoiceRequests = new List<InvoiceRequestModel>();
            _serviceMock.Setup(s => s.GeneratePDF(invoiceRequests, false, false, null))
                .ThrowsAsync(new Exception("PDF error"));

            // Act
            var result = await _controller.PrintPreview(invoiceRequests);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequest.Value;

            var messageProperty = value.GetType().GetProperty("message");
            var detailsProperty = value.GetType().GetProperty("details");

            Assert.NotNull(messageProperty);
            Assert.NotNull(detailsProperty);

            var message = (string)messageProperty.GetValue(value);
            var details = (string)detailsProperty.GetValue(value);

            Assert.Equal("An error occurred while processing the request.", message);
            Assert.Equal("PDF error", details);
        }

        [Fact]
        public async Task PrintAndSubmit_ReturnsOkResult()
        {
            // Arrange
            var invoiceRequest = new PrintAndSubmitRequestModel
            {
                InvoiceRequests = new List<InvoiceRequestModel> { new InvoiceRequestModel { AccountId = 1, ClientId = 2, Charges = new List<ChargeModel>() } },
                includePreviousInvoices = true
            };
            var pdfData = new byte[] { 4, 5, 6 };
            var errors = new List<string>();
            _serviceMock.Setup(s => s.GeneratePDF(invoiceRequest.InvoiceRequests, true, invoiceRequest.includePreviousInvoices, null))
                .ReturnsAsync((pdfData, errors));

            // Act
            var result = await _controller.PrintAndSubmit(invoiceRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<PdfResponse>(okResult.Value);
            Assert.Equal(Convert.ToBase64String(pdfData), response.PdfBase64);
            Assert.Equal(errors, response.Errors);
        }

        [Fact]
        public async Task PrintAndSubmit_ReturnsBadRequest_OnException()
        {
            // Arrange
            var invoiceRequest = new PrintAndSubmitRequestModel { InvoiceRequests = new List<InvoiceRequestModel>(), includePreviousInvoices = false };
            _serviceMock.Setup(s => s.GeneratePDF(invoiceRequest.InvoiceRequests, true, invoiceRequest.includePreviousInvoices, null))
                .ThrowsAsync(new Exception("Submit error"));

            // Act
            var result = await _controller.PrintAndSubmit(invoiceRequest);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequest.Value;

            var messageProperty = value.GetType().GetProperty("message");
            var detailsProperty = value.GetType().GetProperty("details");

            Assert.NotNull(messageProperty);
            Assert.NotNull(detailsProperty);

            var message = (string)messageProperty.GetValue(value);
            var details = (string)detailsProperty.GetValue(value);

            Assert.Equal("An error occurred while processing the request.", message);
            Assert.Equal("Submit error", details);
        }

        [Fact]
        public async Task GetInvoicePDF_ReturnsOkResult()
        {
            // Arrange
            var invoiceDetails = new GetInvoicePDFRequestModel { AccountId = 1, ClientId = 2, InvoiceNo = "INV123" };
            var pdfData = new byte[] { 7, 8, 9 };
            var errors = new List<string>();
            _serviceMock.Setup(s => s.GetInvoicePDF(invoiceDetails.AccountId, invoiceDetails.ClientId, invoiceDetails.InvoiceNo))
                .ReturnsAsync((pdfData, errors));

            // Act
            var result = await _controller.GetInvoicePDF(invoiceDetails);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<PdfResponse>(okResult.Value);
            Assert.Equal(Convert.ToBase64String(pdfData), response.PdfBase64);
            Assert.Equal(errors, response.Errors);
        }

        [Fact]
        public async Task GetInvoicePDF_ReturnsBadRequest_OnException()
        {
            // Arrange
            var invoiceDetails = new GetInvoicePDFRequestModel { AccountId = 1, ClientId = 2, InvoiceNo = "INV123" };
            _serviceMock.Setup(s => s.GetInvoicePDF(invoiceDetails.AccountId, invoiceDetails.ClientId, invoiceDetails.InvoiceNo))
                .ThrowsAsync(new Exception("PDF fetch error"));

            // Act
            var result = await _controller.GetInvoicePDF(invoiceDetails);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequest.Value;

            var messageProperty = value.GetType().GetProperty("message");
            var detailsProperty = value.GetType().GetProperty("details");

            Assert.NotNull(messageProperty);
            Assert.NotNull(detailsProperty);

            var message = (string)messageProperty.GetValue(value);
            var details = (string)detailsProperty.GetValue(value);

            Assert.Equal("An error occurred while processing the request.", message);
            Assert.Equal("PDF fetch error", details);
        }


    }
}
