using AutoFixture;
using AutoMapper;
using BillingService.Domain.DataObjects.Billing;
using BillingService.Domain.DataObjects.CompanyAccount;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Services.Billing;
using BillingService.XUnit.Tests.Common;
using Moq;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
namespace BillingService.XUnit.Tests.Billing.Appointment
{
    public class LocationCodeServiceTest : BaseTest
    {
        private readonly Mock<ICommonService> _commonService;
        private readonly IMapper _mapper;
        private readonly ILocationCodeService _locationCodeService;
        public LocationCodeServiceTest()
        {
            _commonService = new Mock<ICommonService>();
            //SetupMapper();
            _locationCodeService = new LocationCodeService(
                _commonService.Object,
                _mapper
            );
        }
        [Fact]
        public async Task GetAll_ShouldReturnMappedLocationCodes()
        {
            // Arrange
            var accountInfoId = Fixture.Create<int>();
            var locationCodesData = Fixture.Build<LocationCodeData>()
                .With(x => x.IsActive, true)
                .CreateMany(3)
                .ToList();
            _commonService
                .Setup(x => x.GetLocationCodes(accountInfoId))
                .ReturnsAsync(locationCodesData);
            // Act
            var result = await _locationCodeService.GetAll(accountInfoId);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(locationCodesData.Count, result.Count);
            foreach (var item in result)
            {
                var source = locationCodesData.First(x => x.Id == item.Id);
                Assert.Equal(source.Code, item.Code);
                Assert.Equal(source.Description, item.Description);
            }
            _commonService.Verify(x => x.GetLocationCodes(accountInfoId), Times.Once);
        }
        [Fact]
        public async Task GetAll_ShouldReturnEmptyList_WhenNoLocationCodesExist()
        {
            // Arrange
            var accountInfoId = Fixture.Create<int>();
            _commonService
                .Setup(x => x.GetLocationCodes(accountInfoId))
                .ReturnsAsync(new List<LocationCodeData>());
            // Act
            var result = await _locationCodeService.GetAll(accountInfoId);
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _commonService.Verify(x => x.GetLocationCodes(accountInfoId), Times.Once);
        }
        [Fact]
        public async Task GetAll_ShouldThrowException_WhenLocationCodesIsNull()
        {
            // Arrange
            var accountInfoId = Fixture.Create<int>();
            _commonService
                .Setup(x => x.GetLocationCodes(accountInfoId))
                .ReturnsAsync((List<LocationCodeData>)null);

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                     _locationCodeService.GetAll(accountInfoId));
            _commonService.Verify(x => x.GetLocationCodes(accountInfoId), Times.Once);
        }
        [Fact]
        public async Task GetAll_ShouldCorrectlyMapEachProperty()
        {
            // Arrange
            var accountInfoId = Fixture.Create<int>();
            var source = new LocationCodeData
            {
                Id = 101,
                Code = "POS01",
                Description = "Clinic",
                IsActive = true
            };
            _commonService
                .Setup(x => x.GetLocationCodes(accountInfoId))
                .ReturnsAsync(new List<LocationCodeData> { source });
            // Act
            var result = await _locationCodeService.GetAll(accountInfoId);
            // Assert
            Assert.Single(result);
            var item = result.First();
            Assert.Equal(101, item.Id);
            Assert.Equal("POS01", item.Code);
            Assert.Equal("Clinic", item.Description);
        }
    }
}