using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ReportingService.Web.Controllers;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.ReportingModels;
using SummationService.Domain.Interfaces;

namespace ReportingService.Tests.Controllers
{
    public class AccountsReceivableControllerTest
    {
        private readonly Mock<IAccountsReceivableService> _mockService;
        private readonly Mock<ILogger<AccountsReceivableController>> _mockLogger;
        private readonly AccountsReceivableController _controller;
        private readonly Mock<IRethinkMasterDataMicroServices> _rethinkServices;

        public AccountsReceivableControllerTest()
        {
            _mockService = new Mock<IAccountsReceivableService>();
            _mockLogger = new Mock<ILogger<AccountsReceivableController>>();
            _rethinkServices = new Mock<IRethinkMasterDataMicroServices>();
            _controller = new AccountsReceivableController(_mockService.Object, _mockLogger.Object, _rethinkServices.Object);

            _rethinkServices.Setup(s => s.GetChildProfilesForAccount(It.IsAny<int>()))
                .ReturnsAsync(new List<ChildProfileEntityModel>
                {
                    new ChildProfileEntityModel { Id = 1, FirstName = "John", LastName = "Doe", DateDeleted = null },
                    new ChildProfileEntityModel { Id = 2, FirstName = "Jane", LastName = "Smith", DateDeleted = null }
                });
        }

        [Fact]
        public async Task AddOrUpdateAccountsReceivable_ReturnsOk_OnSuccess()
        {
            // Arrange
            var model = new ClaimTransactionModel { TransactionType = (int)ClaimTransactionType.patientPayment, TransactionTypeId = 1 };

            _mockService.Setup(s => s.AddOrUpdateAccountsReceivableAsync(It.IsAny<ClaimTransactionType>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            // Act
            var result = await _controller.AddOrUpdateAccountsReceivable(model, CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task AddOrUpdateAccountsReceivable_Returns500_OnException()
        {
            var model = new ClaimTransactionModel { TransactionType = (int)ClaimTransactionType.patientPayment, TransactionTypeId = 1 };

            _mockService.Setup(s => s.AddOrUpdateAccountsReceivableAsync(It.IsAny<ClaimTransactionType>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Test exception"));

            var result = await _controller.AddOrUpdateAccountsReceivable(model, CancellationToken.None);

            var objResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objResult.StatusCode);
        }

        [Fact]
        public async Task GetAccountsReceivables_ReturnsOk_WithData()
        {
            var model = new AccountsRecievablesRequestModel();
            var expected = new AccountsReceivablesResponseModel 
            { 
                AccountsReceivables =
                [
                    new AccountsReceivablesResponse { ClientId = 1, BilledAmount = 100.0m, FunderName = "Funder1", ClientFirstName = "Test1" },
                    new AccountsReceivablesResponse { ClientId = 2, BilledAmount = 200.0m, FunderName = "Funder2", ClientFirstName = "Test2" }
                ]
            };

            _mockService.Setup(s => s.GetAccountsReceivablesAsync(model, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expected);

            var result = await _controller.GetAccountsReceivables(model, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GetAccountsReceivables_Returns500_OnException()
        {
            var model = new AccountsRecievablesRequestModel();

            _mockService.Setup(s => s.GetAccountsReceivablesAsync(model, It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("error"));

            var result = await _controller.GetAccountsReceivables(model, CancellationToken.None);

            var objResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objResult.StatusCode);
        }

        [Fact]
        public async Task GetAccountsReceivablesChargeLevel_ReturnsOk_WithData()
        {
            var model = new AccountsRecievablesRequestModel();
            var expected = new AccountsReceivablesChargeLevelResponseModel
            {
                AccountsReceivables =
                [
                    new AccountsReceivablesChargeLevelResponse { ClientId = 1, BilledAmount = 150.0m },
                    new AccountsReceivablesChargeLevelResponse { ClientId = 2, BilledAmount = 250.0m }
                ]
            };

            _mockService.Setup(s => s.GetAccountsReceivablesChargeLevelAsync(model, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expected);

            var result = await _controller.GetAccountsReceivablesChargeLevel(model, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task GetFunders_ReturnsOk_WithData()
        {
            var expected = new List<FunderDetailsResponseModel>
            {
                new FunderDetailsResponseModel { FunderId = 1, FunderName = "Funder1" },
                new FunderDetailsResponseModel { FunderId = 2, FunderName = "Funder2" }
            };

            _mockService.Setup(s => s.GetFundersAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(expected);

            var result = await _controller.GetFunders(CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, okResult.Value);
        }

        [Fact]
        public async Task ExportToExcel_ReturnsOk_WithBase64()
        {
            var model = new AccountsRecievablesRequestModel();
            var fakeFile = new byte[] { 1, 2, 3 };

            _mockService.Setup(x => x.GetAccountsReceivablesAsync(model, CancellationToken.None))
                .ReturnsAsync(new AccountsReceivablesResponseModel { AccountsReceivables = new List<AccountsReceivablesResponse>()});

            _mockService.Setup(s => s.ExportToExcelAsync(model,It.IsAny<AccountsReceivablesResponseModel>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(fakeFile);

            var result = await _controller.ExportToExcel(model, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = okResult.Value.GetType().GetProperty("data")?.GetValue(okResult.Value, null)?.ToString();
            Assert.Equal(Convert.ToBase64String(fakeFile), data);
        }

        [Fact]
        public async Task ExportToExcelChargeLevel_ReturnsOk_WithBase64()
        {
            var model = new AccountsRecievablesRequestModel();
            var fakeFile = new byte[] { 4, 5, 6 };

            _mockService.Setup(x => x.GetAccountsReceivablesChargeLevelAsync(model, CancellationToken.None))
               .ReturnsAsync(new AccountsReceivablesChargeLevelResponseModel { AccountsReceivables = new List<AccountsReceivablesChargeLevelResponse>() });

            _mockService.Setup(s => s.ExportToExcelChargeLevelAsync(model, It.IsAny<AccountsReceivablesChargeLevelResponseModel>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(fakeFile);

            var result = await _controller.ExportToExcelChargeLevel(model, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = okResult.Value.GetType().GetProperty("data")?.GetValue(okResult.Value, null)?.ToString();
            Assert.Equal(Convert.ToBase64String(fakeFile), data);
        }
    }
}
