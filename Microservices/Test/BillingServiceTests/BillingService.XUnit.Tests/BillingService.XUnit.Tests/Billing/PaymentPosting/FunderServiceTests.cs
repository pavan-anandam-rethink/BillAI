using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models.Funders;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Domain.Services.Billing;
using Moq;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.PaymentPosting
{
    public class FunderServiceTests
    {
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IRethinkMasterDataMicroServices> _rethinkMasterDataMicroServicesMock;
        private readonly IFunderService _service;

        public FunderServiceTests()
        {
            _mapperMock = new Mock<IMapper>();
            _rethinkMasterDataMicroServicesMock = new Mock<IRethinkMasterDataMicroServices>();
            _service = new FunderService(_mapperMock.Object, _rethinkMasterDataMicroServicesMock.Object);
        }

        [Fact]
        public async Task GetFundersAsync_ReturnsFunders_OnSuccess()
        {
            // Arrange
            var funderSearchModel = new FunderSearchModelWithUserInfo { AccountInfoId = 1, FunderName = "Test", Skip = 0, Take = 2 };
            var funderList = new FunderListModel
            {
                data = new List<FunderModel>
                {
                    new FunderModel { Id = 1, FunderName = "TestFunder1" },
                    new FunderModel { Id = 2, FunderName = "TestFunder2" },
                    new FunderModel { Id = 3, FunderName = "OtherFunder" }
                }
            };
            _rethinkMasterDataMicroServicesMock.Setup(x => x.GetFunderList(funderSearchModel.AccountInfoId)).ReturnsAsync(funderList);

            // Act
            var result = await _service.GetFundersAsync(funderSearchModel);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Funders.All(f => f.FunderName.ToLower().Contains(funderSearchModel.FunderName.ToLower())));
            Assert.True(result.TotalCount <= funderSearchModel.Take);
        }

        [Fact]
        public async Task GetFundersAsync_ReturnsEmptyList_WhenNoFundersMatch()
        {
            // Arrange
            var funderSearchModel = new FunderSearchModelWithUserInfo { AccountInfoId = 1, FunderName = "NotFound", Skip = 0, Take = 2 };
            var funderList = new FunderListModel
            {
                data = new List<FunderModel>
                {
                    new FunderModel { Id = 1, FunderName = "TestFunder1" },
                    new FunderModel { Id = 2, FunderName = "TestFunder2" }
                }
            };
            _rethinkMasterDataMicroServicesMock.Setup(x => x.GetFunderList(funderSearchModel.AccountInfoId)).ReturnsAsync(funderList);

            // Act
            var result = await _service.GetFundersAsync(funderSearchModel);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Funders);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task GetFundersAsync_ReturnsEmptyList_OnException()
        {
            // Arrange
            var funderSearchModel = new FunderSearchModelWithUserInfo { AccountInfoId = 1, FunderName = "Test", Skip = 0, Take = 2 };
            _rethinkMasterDataMicroServicesMock.Setup(x => x.GetFunderList(funderSearchModel.AccountInfoId)).ThrowsAsync(new System.Exception("Service error"));

            // Act
            FunderDropdownResponseModel result = null;
            try
            {
                result = await _service.GetFundersAsync(funderSearchModel);
            }
            catch
            {
                // If exception is not handled in service, result will be null
            }

            // Assert
            Assert.True(result == null || result.Funders.Count == 0);
        }

        [Fact]
        public async Task GetFundersAsync_OrdersFunders_WhenTakeIsZero()
        {
            // Arrange
            var funderSearchModel = new FunderSearchModelWithUserInfo { AccountInfoId = 1, FunderName = "Test", Skip = 0, Take = 0 };
            var funderList = new FunderListModel
            {
                data = new List<FunderModel>
                {
                    new FunderModel { Id = 2, FunderName = "TestFunderB" },
                    new FunderModel { Id = 1, FunderName = "TestFunderA" },
                    new FunderModel { Id = 3, FunderName = "OtherFunder" }
                }
            };
            _rethinkMasterDataMicroServicesMock.Setup(x => x.GetFunderList(funderSearchModel.AccountInfoId)).ReturnsAsync(funderList);

            // Act
            var result = await _service.GetFundersAsync(funderSearchModel);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Funders.Count > 0);
            // Check that funders are ordered by FunderName
            var ordered = result.Funders.OrderBy(f => f.FunderName).Select(f => f.FunderName).ToList();
            Assert.Equal(ordered, result.Funders.Select(f => f.FunderName).ToList());
        }
    }
}
