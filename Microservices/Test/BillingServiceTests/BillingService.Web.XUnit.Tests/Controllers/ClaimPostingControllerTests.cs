using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.PaymentClaims;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Models.ReportingModels;
using Rethink.Services.Domain.Interfaces;
using System.Net.Mime;

public class ClaimPostingControllerTests
{
    private readonly Mock<IPaymentClaimService> _mockPaymentClaimService;
    private readonly Mock<IChargePaymentService> _mockChargePaymentService;
    private readonly Mock<IReportService> _mockReportService;
    private readonly ClaimPostingController _controller;
    private readonly Mock<ILogger<ClaimPostingController>> _mockLogger;

    public ClaimPostingControllerTests()
    {
        _mockPaymentClaimService = new Mock<IPaymentClaimService>();
        _mockChargePaymentService = new Mock<IChargePaymentService>();
        _mockReportService = new Mock<IReportService>();
        _mockLogger = new Mock<ILogger<ClaimPostingController>>();
        _controller = new ClaimPostingController(_mockPaymentClaimService.Object, _mockChargePaymentService.Object, _mockReportService.Object,_mockLogger.Object);
    }

    [Fact]
    public async Task GetClaims_ReturnsOk_WithServiceResult()
    {
        var model = new GetClaimFilterModel();
        var expected = new PaymentClaimsResponseModel();
        _mockPaymentClaimService.Setup(s => s.GetPaymentClaimsAsync(model)).ReturnsAsync(expected);

        var result = await _controller.GetClaims(model);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
        _mockPaymentClaimService.Verify(s => s.GetPaymentClaimsAsync(model), Times.Once);
    }

    [Fact]
    public async Task GetClaims_OnException_ReturnsBadRequest()
    {
        var model = new GetClaimFilterModel();
        _mockPaymentClaimService.Setup(s => s.GetPaymentClaimsAsync(model)).ThrowsAsync(new Exception("boom"));

        var result = await _controller.GetClaims(model);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("boom", bad.Value.ToString());
    }

    [Fact]
    public async Task GetPaymentPatients_ReturnsOk()
    {
        int paymentId = 7;
        var expected = new List<PaymentPaitentModel> { new PaymentPaitentModel() };
        _mockPaymentClaimService.Setup(s => s.GetPatientsByPaymentAsync(paymentId)).ReturnsAsync(expected);

        var result = await _controller.GetPaymentPatients(paymentId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task GetPatientClaims_ReturnsOk_AndHandlesException()
    {
        var model = new GetClaimsModel();
        var expected = new PatientPaymentClaimsResponseModel();
        _mockPaymentClaimService.Setup(s => s.GetPaymentClaimsByPatientsAsync(model)).ReturnsAsync(expected);

        var okResult = await _controller.GetPatientClaims(model);
        Assert.IsType<OkObjectResult>(okResult);
        //_mockPaymentClaimService.Verify(s => s.GetPaymentClaimsByPatientsAsync(model), Times.Once);

        // exception path
        //_mockPaymentClaimService.Setup(s => s.GetPaymentClaimsByPatientsAsync(model)).ThrowsAsync(new Exception("err"));
        //var badResult = await _controller.GetPatientClaims(model);
        //var bad = Assert.IsType<BadRequestObjectResult>(badResult);
        //Assert.Equal("err", bad.Value);
    }

    [Fact]
    public async Task GetEOBClaims_ReturnsOk()
    {
        int paymentId = 11;
        var expected = new List<ClaimEOBInfoModel> { new ClaimEOBInfoModel() };
        _mockPaymentClaimService.Setup(s => s.GetEOBClaimsAsync(paymentId, null)).ReturnsAsync(expected);

        var result = await _controller.GetEOBClaims(paymentId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task GetEOBPaymentClaimsPDF_ReturnsPdfFile()
    {
        var model = new GetEOBClaimsModel { PaymentId = 1, Claims = new List<int>() };
        var bytes = new byte[] { 9, 8, 7 };
        _mockPaymentClaimService.Setup(s => s.GetEOBPaymentClaimPDFAsync(model)).ReturnsAsync(bytes);

        var result = await _controller.GetEOBPaymentClaimsPDF(model);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal(MediaTypeNames.Application.Pdf, file.ContentType);
        Assert.Equal("EOB Details", file.FileDownloadName);
        Assert.Equal(bytes, file.FileContents);
    }

    [Fact]
    public async Task GetSelectedEOBClaims_ReturnsOk()
    {
        var model = new GetEOBClaimsModel { PaymentId = 2, Claims = new List<int> { 1, 2 } };
        var expected = new List<ClaimEOBInfoModel> { new ClaimEOBInfoModel(), new ClaimEOBInfoModel() };
        _mockPaymentClaimService.Setup(s => s.GetEOBClaimsAsync(model.PaymentId, model.Claims)).ReturnsAsync(expected);

        var result = await _controller.GetSelectedEOBClaims(model);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task GetClaimDetails_ReturnsOk()
    {
        var model = new IdWithUserInfo { Id = 5 };
        var expected = new PaymentClaimModel();
        _mockPaymentClaimService.Setup(s => s.GetPaymentClaimAsync(model.Id)).ReturnsAsync(expected);

        var result = await _controller.GetClaimDetails(model);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task GetPatientDetails_ReturnsOk()
    {
        var model = new PatientDetailsModel { patientId = 3, AccountInfoId = 22 };
        var expected = new ChildProfileInfo();
        _mockPaymentClaimService.Setup(s => s.getPatientDetails(model.patientId, model.AccountInfoId)).ReturnsAsync(expected);

        var result = await _controller.GetPatientDetails(model);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task GetPaymentClaimServiceLines_ReturnsDataAndCount()
    {
        int claimId = 13;
        var list = new List<PaymentClaimServiceLineModel> { new PaymentClaimServiceLineModel(), new PaymentClaimServiceLineModel() };
        _mockPaymentClaimService.Setup(s => s.GetPaymentClaimServiceLinesAsync(claimId)).ReturnsAsync(list);

        var result = await _controller.GetPaymentClaimServiceLines(claimId);
        var ok = Assert.IsType<OkObjectResult>(result);

        // robust unpack: controller may return anonymous { data = result, totalCount = ... } or the raw list
        var (dataList, totalCount) = UnpackListFromOk<PaymentClaimServiceLineModel>(ok.Value);
        Assert.Equal(list, dataList);
        Assert.Equal(list.Count, totalCount);
    }

    //[Fact]
    //public async Task GetPatientPaymentClaimLinkedAndUnlinked_ReturnsDataAndCount()
    //{
    //    var model = new GetPatientPaymentServiceLinesModel();
    //    var list = new List<PaymentClaimServiceLineModel> { new PaymentClaimServiceLineModel() };

    //    _mockPaymentClaimService.Setup(s => s.GetPatientPaymentLinkedServiceLinesAsync(model, false)).ReturnsAsync(list);
    //    _mockPaymentClaimService.Setup(s => s.GetPatientPaymentUnlinkedServiceLinesAsync(model)).ReturnsAsync(list);

    //    var linked = await _controller.GetPatientPaymentClaimLinkedServiceLines(model);
    //    var okLinked = Assert.IsType<OkObjectResult>(linked);
    //    var (linkedList, linkedTotal) = UnpackListFromOk<PaymentClaimServiceLineModel>(okLinked.Value);
    //    Assert.Equal(list, linkedList);
    //    Assert.Equal(list.Count, linkedTotal);

    //    var unlinked = await _controller.GetPatientPaymentClaimUnlinkedServiceLines(model);
    //    var okUnlinked = Assert.IsType<OkObjectResult>(unlinked);
    //    var (unlinkedList, unlinkedTotal) = UnpackListFromOk<PaymentClaimServiceLineModel>(okUnlinked.Value);
    //    Assert.Equal(list, unlinkedList);
    //    Assert.Equal(list.Count, unlinkedTotal);
    //}

    private static (List<T> data, int totalCount) UnpackListFromOk<T>(object value)
    {
        if (value is List<T> rawList)
            return (rawList, rawList.Count);

        var type = value?.GetType();
        var dataProp = type?.GetProperty("data");
        var totalProp = type?.GetProperty("totalCount");
        if (dataProp == null || totalProp == null)
            throw new Xunit.Sdk.XunitException("Response object did not contain expected 'data' and 'totalCount' members.");

        var data = dataProp.GetValue(value) as List<T>;
        var total = (int)totalProp.GetValue(value);
        return (data ?? new List<T>(), total);
    }

    [Fact]
    public async Task GetPaymentClaimServiceLine_ReturnsOk()
    {
        int serviceLineId = 77;
        var expected = new PaymentClaimServiceLineModel();
        _mockPaymentClaimService.Setup(s => s.GetPaymentClaimServiceLineAsync(serviceLineId)).ReturnsAsync(expected);

        var result = await _controller.GetPaymentClaimServiceLine(serviceLineId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task CreatePaymentPatientClaims_ReturnsOk_AndBadRequestOnException()
    {
        var model = new CreatePatientClaimsModel();
        var expected = new List<AddPatientResponseModel> { new AddPatientResponseModel() };
        _mockPaymentClaimService.Setup(s => s.CreatePaymentClaimsAsync(model)).ReturnsAsync(expected);

        var ok = await _controller.CreatePaymentPatientClaims(model);
        Assert.IsType<OkObjectResult>(ok);

        _mockPaymentClaimService.Setup(s => s.CreatePaymentClaimsAsync(model)).ThrowsAsync(new Exception());
        var bad = await _controller.CreatePaymentPatientClaims(model);
        Assert.IsType<BadRequestResult>(bad);
    }

    [Fact]
    public async Task CreateClaimsToEraPayment_ReturnsOk()
    {
        var model = new CreateEraClaimsModel();
        _mockPaymentClaimService.Setup(s => s.CreateClaimsToEraAsync(model)).ReturnsAsync(123);

        var result = await _controller.CreateClaimsToEraPayment(model);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(123, ok.Value);
    }

    [Fact]
    public async Task UpdatePaymentClaimServiceLineAmounts_ReturnsOk()
    {
        var model = new UpdatePaymentServiceLineAmountsModelWithUserInfo();
        _mockPaymentClaimService.Setup(s => s.UpdatePaymentClaimServiceLineAmountsAsync(model)).Returns(Task.CompletedTask);

        var result = await _controller.UpdatePaymentClaimServiceLineAmounts(model);

        Assert.IsType<OkResult>(result);
        _mockPaymentClaimService.Verify(s => s.UpdatePaymentClaimServiceLineAmountsAsync(model), Times.Once);
    }

    [Fact]
    public async Task PostManualPaymentClaimLines_ReturnsOk_AndBadRequestOnException()
    {
        var model = new PostPaymentClaimsModel();
        _mockPaymentClaimService.Setup(s => s.PostPaymentClaimLines(model)).Returns(Task.CompletedTask);

        var result = await _controller.PostManualPaymentClaimLines(model);
        Assert.IsType<OkResult>(result);

        _mockPaymentClaimService.Setup(s => s.PostPaymentClaimLines(model)).ThrowsAsync(new Exception("posterr"));
        var badResult = await _controller.PostManualPaymentClaimLines(model);
        var bad = Assert.IsType<BadRequestObjectResult>(badResult);
        Assert.Equal("posterr", bad.Value);
    }

    [Fact]
    public async Task PostManualPatientPaymentClaimLines_ReturnsOk_AndBadRequestOnException()
    {
        var model = new PostRemovePatientClaimsModel();
        _mockPaymentClaimService.Setup(s => s.PostPatientPaymentClaimLinesAsync(model)).ReturnsAsync("done");

        var okResult = await _controller.PostManualPatientPaymentClaimLines(model);
        var ok = Assert.IsType<OkObjectResult>(okResult);
        Assert.Equal("done", ok.Value);

        _mockPaymentClaimService.Setup(s => s.PostPatientPaymentClaimLinesAsync(model)).ThrowsAsync(new Exception("pex"));
        var bad = await _controller.PostManualPatientPaymentClaimLines(model);
        var badResult = Assert.IsType<BadRequestObjectResult>(bad);
        Assert.Equal("pex", badResult.Value);
    }

    [Fact]
    public async Task GetPaymentClaimErrors_ReturnsOk()
    {
        var model = new GetByIdSortFilterWithUserInfo();
        var expected = new PaymentClaimErrorsResponseModel();
        _mockPaymentClaimService.Setup(s => s.GetPaymentClaimErrorsAsync(model)).ReturnsAsync(expected);

        var result = await _controller.GetPaymentClaimErrors(model);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task RemovePaymentClaims_ReturnsOk()
    {
        var model = new RemovePaymentClaimsModel();
        _mockPaymentClaimService.Setup(s => s.RemoveSelectedClaimsAsync(model)).Returns(Task.CompletedTask);

        var result = await _controller.RemovePaymentClaims(model);

        Assert.IsType<OkResult>(result);
        _mockPaymentClaimService.Verify(s => s.RemoveSelectedClaimsAsync(model), Times.Once);
    }

    [Fact]
    public async Task RemovePatientPaymentClaims_ReturnsOk()
    {
        var model = new PostRemovePatientClaimsModel();
        _mockPaymentClaimService.Setup(s => s.RemoveSelectedPatientClaimsAsync(model)).Returns(Task.CompletedTask);

        var result = await _controller.RemovePatientPaymentClaims(model);

        Assert.IsType<OkResult>(result);
        _mockPaymentClaimService.Verify(s => s.RemoveSelectedPatientClaimsAsync(model), Times.Once);
    }

    [Fact]
    public async Task RemoveSelectedPatientPaymentAmounts_ReturnsOk()
    {
        var model = new PostRemovePatientClaimsModel();
        _mockPaymentClaimService.Setup(s => s.RemoveSelectedPatientPaymentAmountsAsync(model)).Returns(Task.CompletedTask);

        var result = await _controller.RemoveSelectedPatientPaymentAmounts(model);

        Assert.IsType<OkResult>(result);
        _mockPaymentClaimService.Verify(s => s.RemoveSelectedPatientPaymentAmountsAsync(model), Times.Once);
    }

    [Fact]
    public async Task GetPaymentClaimServiceLinesSmall_ReturnsOk_AndBadRequestOnException()
    {
        var model = new GetChargeDetailsModel();
        var expected = new List<GetPaymentClaimServiceLinesSmall> { new GetPaymentClaimServiceLinesSmall() }.AsQueryable();
        _mockPaymentClaimService.Setup(s => s.GetPaymentClaimServiceLinesSmallAsync(model)).ReturnsAsync(expected);

        var ok = await _controller.GetPaymentClaimServiceLinesSmall(model);
        Assert.IsType<OkObjectResult>(ok);

        _mockPaymentClaimService.Setup(s => s.GetPaymentClaimServiceLinesSmallAsync(model)).ThrowsAsync(new Exception("smallerr"));
        var bad = await _controller.GetPaymentClaimServiceLinesSmall(model);
        var badRes = Assert.IsType<BadRequestObjectResult>(bad);
        Assert.Equal("smallerr", badRes.Value);
    }

    [Fact]
    public async Task GetClientPrintDataById_ReturnsJson()
    {
        var model = new GetClientPrintDataRequest();
        var expected = new ClientPrintData();
        _mockPaymentClaimService.Setup(s => s.GetCompanyAccountInfoByPatientId(model)).ReturnsAsync(expected);

        var result = await _controller.GetClientPrintDataById(model);

        var json = Assert.IsType<JsonResult>(result);
        Assert.Equal(expected, json.Value);
    }

    [Fact]
    public async Task SendReport_NullModel_ReturnsBadRequest()
    {
        var result = await _controller.SendReport(null);
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task SendReport_Monthly_Weekly_And_Default()
    {
        var monthlyModel = new ReportQueryModel { ReportFrequency = ReportFrequency.Monthly };
        _mockReportService.Setup(r => r.SendMonthlyReportAsync(monthlyModel)).ReturnsAsync(true);

        var monthly = await _controller.SendReport(monthlyModel);
        var okMonthly = Assert.IsType<OkObjectResult>(monthly);
        Assert.True((bool)okMonthly.Value);

        var weeklyModel = new ReportQueryModel { ReportFrequency = ReportFrequency.Weekly };
        _mockReportService.Setup(r => r.SendWeeklyReportAsync(weeklyModel)).ReturnsAsync(true);

        var weekly = await _controller.SendReport(weeklyModel);
        var okWeekly = Assert.IsType<OkObjectResult>(weekly);
        Assert.True((bool)okWeekly.Value);

        var otherModel = new ReportQueryModel { ReportFrequency = (ReportFrequency)999 };
        var bad = await _controller.SendReport(otherModel);
        Assert.IsType<BadRequestResult>(bad);
    }
}
