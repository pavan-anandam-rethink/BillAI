using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.Web.XUnit.Tests.Controllers;
public class PaymentAttachmentControllerTests
{
    private readonly Mock<IPaymentAttachmentService> _mockService;
    private readonly PaymentAttachmentController _controller;
    private readonly Mock<ILogger<PaymentAttachmentController>> _mockLogger;

    public PaymentAttachmentControllerTests()
    {
        _mockService = new Mock<IPaymentAttachmentService>();
        _mockLogger = new Mock<ILogger<PaymentAttachmentController>>();
        _controller = new PaymentAttachmentController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task UploadFile_ReturnsOkResult()
    {
        var model = new PaymentUploadModelWithUserInfo { PaymentId = 1 };
        _mockService.Setup(s => s.UploadFile(model)).ReturnsAsync(42);

        var result = await _controller.UploadFile(model);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(42, ok.Value);
    }

    [Fact]
    public async Task UploadFile_OnException_ReturnsBadRequest()
    {
        var model = new PaymentUploadModelWithUserInfo { PaymentId = 1 };
        _mockService.Setup(s => s.UploadFile(model)).ThrowsAsync(new Exception("uploaderr"));

        var result = await _controller.UploadFile(model);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("uploaderr", bad.Value);
    }

    [Fact]
    public async Task DeleteUpload_ReturnsOk()
    {
        var model = new IdWithUserInfo { Id = 5 };
        _mockService.Setup(s => s.DeleteUpload(model)).Returns(Task.CompletedTask);

        var result = await _controller.DeleteUpload(model);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task DeleteUpload_OnException_ReturnsBadRequest()
    {
        var model = new IdWithUserInfo { Id = 5 };
        _mockService.Setup(s => s.DeleteUpload(model)).ThrowsAsync(new Exception("deleteerr"));

        var result = await _controller.DeleteUpload(model);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("deleteerr", bad.Value);
    }

    [Fact]
    public async Task DeleteUploads_ReturnsOk()
    {
        var model = new DeleteAttachmentsModelWithUserInfo();
        _mockService.Setup(s => s.DeleteUploads(model)).Returns(Task.CompletedTask);

        var result = await _controller.DeleteUploads(model);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task DeleteUploads_OnException_ReturnsBadRequest()
    {
        var model = new DeleteAttachmentsModelWithUserInfo();
        _mockService.Setup(s => s.DeleteUploads(model)).ThrowsAsync(new Exception("bulkerr"));

        var result = await _controller.DeleteUploads(model);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("bulkerr", bad.Value);
    }

    [Fact]
    public async Task GetFileUpload_ReturnsFile()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var attachment = new PaymentAttachmentReturnModel
        {
            MemoryStream = stream,
            Filename = "file.txt"
        };

        _mockService.Setup(s => s.GetUpload(10)).ReturnsAsync(attachment);

        var result = await _controller.GetFileUpload(10);

        var file = Assert.IsType<FileStreamResult>(result);
        Assert.Equal(MediaTypeNames.Application.Octet, file.ContentType);
        Assert.Equal("file.txt", file.FileDownloadName);
    }

    [Fact]
    public async Task GetFileUpload_OnException_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.GetUpload(10)).ThrowsAsync(new Exception("fileerr"));

        var result = await _controller.GetFileUpload(10);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("fileerr", bad.Value);
    }

    [Fact]
    public async Task GetPaymentAttachments_ReturnsOk()
    {
        var model = new GetByIdSortFilterWithUserInfo { Id = 99 };
        var expected = new AttachmentsResponseModel();

        _mockService.Setup(s => s.GetPaymentAttachmentsAsync(model)).ReturnsAsync(expected);

        var result = await _controller.GetPaymentAttachments(model);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task GetPaymentAttachments_OnException_ReturnsBadRequest()
    {
        var model = new GetByIdSortFilterWithUserInfo { Id = 99 };
        _mockService.Setup(s => s.GetPaymentAttachmentsAsync(model))
                    .ThrowsAsync(new Exception("attacherr"));

        var result = await _controller.GetPaymentAttachments(model);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("attacherr", bad.Value);
    }

    [Fact]
    public async Task RenameAttachment_ReturnsOk()
    {
        var model = new RenameAttachmentModelWithUserInfo { AttachmentId = 3 };
        _mockService.Setup(s => s.RenameAttachmentAsync(model)).Returns(Task.CompletedTask);

        var result = await _controller.RenameAttachment(model);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task RenameAttachment_OnException_ReturnsBadRequest()
    {
        var model = new RenameAttachmentModelWithUserInfo { AttachmentId = 3 };
        _mockService.Setup(s => s.RenameAttachmentAsync(model))
                    .ThrowsAsync(new Exception("renameerr"));

        var result = await _controller.RenameAttachment(model);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("renameerr", bad.Value);
    }
}
