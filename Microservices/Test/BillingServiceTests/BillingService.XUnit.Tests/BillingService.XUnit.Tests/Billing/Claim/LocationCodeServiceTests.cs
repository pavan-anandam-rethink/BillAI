using AutoMapper;
using BillingService.Domain.DataObjects.Billing;
using BillingService.Domain.DataObjects.CompanyAccount;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Services.Billing;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Claim
{
    public class LocationCodeServiceTests
    {
        [Fact]
        public async Task GetAll_ReturnsMappedItems_WhenLocationCodesExist()
        {
            // Arrange
            var accountInfoId = 42;
            var commonServiceMock = new Mock<ICommonService>();
            var mapperMock = new Mock<IMapper>();

            commonServiceMock
                .Setup(s => s.GetLocationCodes(accountInfoId))
                .ReturnsAsync(new List<LocationCodeData>
                {
                    new LocationCodeData { Id = 1, Code = "LOC1", Description = "Location 1" },
                    new LocationCodeData { Id = 2, Code = "LOC2", Description = "Location 2" }
                });

            var service = new LocationCodeService(commonServiceMock.Object, mapperMock.Object);

            // Act
            var result = await service.GetAll(accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Collection(result,
                item =>
                {
                    Assert.Equal(1, item.Id);
                    Assert.Equal("LOC1", item.Code);
                    Assert.Equal("Location 1", item.Description);
                },
                item =>
                {
                    Assert.Equal(2, item.Id);
                    Assert.Equal("LOC2", item.Code);
                    Assert.Equal("Location 2", item.Description);
                });

            commonServiceMock.Verify(s => s.GetLocationCodes(accountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetAll_ReturnsEmptyList_WhenNoLocationCodesExist()
        {
            // Arrange
            var accountInfoId = 7;
            var commonServiceMock = new Mock<ICommonService>();
            var mapperMock = new Mock<IMapper>();

            commonServiceMock
                .Setup(s => s.GetLocationCodes(accountInfoId))
                .ReturnsAsync(new List<LocationCodeData>());

            var service = new LocationCodeService(commonServiceMock.Object, mapperMock.Object);

            // Act
            var result = await service.GetAll(accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            commonServiceMock.Verify(s => s.GetLocationCodes(accountInfoId), Times.Once);
        }
    }
}
