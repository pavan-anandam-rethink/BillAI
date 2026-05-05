using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Domain.Services.Billing;
using Moq;
using Rethink.Services.Common.Interfaces;
using Xunit;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using Rethink.Services.Common.Models;

namespace BillingService.XUnit.Tests.Billing.Funder
{
    public class FunderServiceTest
    {
        private static FunderListModel BuildFunderList(params (int id, string name)[] items)
        {
            var list = new FunderListModel { total = items.Length, data = new() };
            foreach (var it in items)
                list.data.Add(new FunderModel { Id = it.id, FunderName = it.name });
            return list;
        }

        [Fact]
        public async Task GetFundersAsync_FiltersAndPaginates()
        {
            var mapper = new MapperConfiguration(cfg => { }).CreateMapper();
            var rethinkMock = new Mock<IRethinkMasterDataMicroServices>();

            var funderListResponse = BuildFunderList(
                (1, "Alpha Health"),
                (2, "Beta Care"),
                (3, "Gamma Health"),
                (4, "Delta Insurance")
            );

            rethinkMock.Setup(x => x.GetFunderList(It.IsAny<int>()))
                .ReturnsAsync(funderListResponse);

            var sut = new FunderService(mapper, rethinkMock.Object);

            var search = new FunderSearchModelWithUserInfo
            {
                AccountInfoId = 123,
                FunderName = "Health",
                Skip = 0,
                Take = 2
            };

            var result = await sut.GetFundersAsync(search);

            Assert.NotNull(result);
            Assert.Equal(2, result.Funders.Count);
            Assert.All(result.Funders, f => Assert.Contains("Health", f.FunderName));
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public async Task GetFundersAsync_ReturnsAllOrdered_WhenNoPagination()
        {
            var mapper = new MapperConfiguration(cfg => { }).CreateMapper();
            var rethinkMock = new Mock<IRethinkMasterDataMicroServices>();

            var funderListResponse = BuildFunderList(
                (10, "Zeta"),
                (2, "Alpha"),
                (7, "Beta")
            );

            rethinkMock.Setup(x => x.GetFunderList(It.IsAny<int>()))
                .ReturnsAsync(funderListResponse);

            var sut = new FunderService(mapper, rethinkMock.Object);

            var search = new FunderSearchModelWithUserInfo
            {
                AccountInfoId = 123,
                FunderName = string.Empty,
                Skip = 0,
                Take = 0
            };

            var result = await sut.GetFundersAsync(search);

            Assert.Equal(3, result.Funders.Count);
            Assert.Equal(new[] { "Alpha", "Beta", "Zeta" }, result.Funders.Select(x => x.FunderName).ToArray());
            Assert.Equal(3, result.TotalCount);
        }

        [Fact]
        public async Task GetFundersAsync_CaseInsensitiveFilter()
        {
            var mapper = new MapperConfiguration(cfg => { }).CreateMapper();
            var mock = new Mock<IRethinkMasterDataMicroServices>();
            mock.Setup(x => x.GetFunderList(It.IsAny<int>()))
                .ReturnsAsync(BuildFunderList((1, "health plan"), (2, "HEALTH PLUS"), (3, "Care")));

            var sut = new FunderService(mapper, mock.Object);
            var search = new FunderSearchModelWithUserInfo { AccountInfoId = 1, FunderName = "HeAlTh", Skip = 0, Take = 0 };

            var result = await sut.GetFundersAsync(search);

            Assert.Equal(2, result.Funders.Count);
            Assert.All(result.Funders, f => Assert.Contains("health", f.FunderName.ToLower()));
        }

        [Fact]
        public async Task GetFundersAsync_SkipAndTake_PaginatesOrderedList()
        {
            var mapper = new MapperConfiguration(cfg => { }).CreateMapper();
            var mock = new Mock<IRethinkMasterDataMicroServices>();
            mock.Setup(x => x.GetFunderList(It.IsAny<int>()))
                .ReturnsAsync(BuildFunderList((1, "A"), (2, "B"), (3, "C"), (4, "D"), (5, "E")));

            var sut = new FunderService(mapper, mock.Object);
            var search = new FunderSearchModelWithUserInfo { AccountInfoId = 1, FunderName = string.Empty, Skip = 1, Take = 2 };

            var result = await sut.GetFundersAsync(search);

            // Base query is ordered by name, then skip 1 and take 2 => B, C
            Assert.Equal(new[] { "B", "C" }, result.Funders.Select(x => x.FunderName).ToArray());
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public async Task GetFundersAsync_EmptyData_ReturnsEmpty()
        {
            var mapper = new MapperConfiguration(cfg => { }).CreateMapper();
            var mock = new Mock<IRethinkMasterDataMicroServices>();
            mock.Setup(x => x.GetFunderList(It.IsAny<int>()))
                .ReturnsAsync(BuildFunderList());

            var sut = new FunderService(mapper, mock.Object);
            var search = new FunderSearchModelWithUserInfo { AccountInfoId = 1, FunderName = "any", Skip = 0, Take = 10 };

            var result = await sut.GetFundersAsync(search);

            Assert.Empty(result.Funders);
            Assert.Equal(0, result.TotalCount);
        }
    }
}
