using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using BillingService.Domain.Services.Common;
using BillingService.Domain.DataObjects.CompanyAccount;
using BillingService.Domain.Interfaces.Common;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using Rethink.Services.Common.Models;
using BillingService.Domain.Models.ClientMicroServicesModels;

namespace BillingService.XUnit.Tests.Billing.Common
{
    public class CommonServiceTest
    {
        private readonly Mock<IRethinkMasterDataMicroServices> _mockRethink;
        private readonly CommonService _commonService;

        public CommonServiceTest()
        {
            _mockRethink = new Mock<IRethinkMasterDataMicroServices>();
            _commonService = new CommonService(_mockRethink.Object);
        }

        [Fact]
        public async Task GetLocationCodes_WhenPlaceOfServiceExistsAndLocationCodesExist_ReturnsLocationCodesWithMatchedData()
        {
            var accountInfoId = 1;
            var existingPlaceOfServices = new PlacesOfServiceModel
            {
                placesOfService = new PlaceOfServicesList
                {
                    total = 2,
                    data = new List<placeOfService>
                    {
                        new placeOfService { id = 1, code = "11", description = "Office", isActive = true, accountId = 1 },
                        new placeOfService { id = 2, code = "12", description = "Home", isActive = false, accountId = 1 }
                    }
                }
            };

            var locationCodes = new List<LocationCodesModel>
            {
                new LocationCodesModel { id = 1, code = "11", description = "Original Office" },
                new LocationCodesModel { id = 2, code = "12", description = "Original Home" },
                new LocationCodesModel { id = 3, code = "13", description = "Assisted Living" }
            };

            _mockRethink.Setup(x => x.GetPlaceOfService(accountInfoId))
                .ReturnsAsync(existingPlaceOfServices);

            _mockRethink.Setup(x => x.GetLocationCodes())
                .ReturnsAsync(locationCodes);

            var result = await _commonService.GetLocationCodes(accountInfoId);

            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            
            var firstLocation = result.First(x => x.Code == "11");
            Assert.Equal("Office", firstLocation.Description);
            Assert.True(firstLocation.IsActive);

            var secondLocation = result.First(x => x.Code == "12");
            Assert.Equal("Home", secondLocation.Description);
            Assert.False(secondLocation.IsActive);

            var thirdLocation = result.First(x => x.Code == "13");
            Assert.Equal("Assisted Living", thirdLocation.Description);
            Assert.True(thirdLocation.IsActive);

            _mockRethink.Verify(x => x.GetPlaceOfService(accountInfoId), Times.Once);
            _mockRethink.Verify(x => x.GetLocationCodes(), Times.Once);
        }

        [Fact]
        public async Task GetLocationCodes_WhenPlaceOfServiceIsNull_ReturnsEmptyList()
        {
            var accountInfoId = 1;

            _mockRethink.Setup(x => x.GetPlaceOfService(accountInfoId))
                .ReturnsAsync((PlacesOfServiceModel)null);

            var result = await _commonService.GetLocationCodes(accountInfoId);

            Assert.NotNull(result);
            Assert.Empty(result);
            _mockRethink.Verify(x => x.GetPlaceOfService(accountInfoId), Times.Once);
            _mockRethink.Verify(x => x.GetLocationCodes(), Times.Never);
        }

        [Fact]
        public async Task GetLocationCodes_WhenPlaceOfServiceDataIsEmpty_ReturnsEmptyList()
        {
            var accountInfoId = 1;
            var existingPlaceOfServices = new PlacesOfServiceModel
            {
                placesOfService = new PlaceOfServicesList
                {
                    total = 0,
                    data = new List<placeOfService>()
                }
            };

            _mockRethink.Setup(x => x.GetPlaceOfService(accountInfoId))
                .ReturnsAsync(existingPlaceOfServices);

            var result = await _commonService.GetLocationCodes(accountInfoId);

            Assert.NotNull(result);
            Assert.Empty(result);
            _mockRethink.Verify(x => x.GetPlaceOfService(accountInfoId), Times.Once);
            _mockRethink.Verify(x => x.GetLocationCodes(), Times.Never);
        }

        [Fact]
        public async Task GetLocationCodes_WhenLocationCodesIsEmpty_ReturnsEmptyList()
        {
            var accountInfoId = 1;
            var existingPlaceOfServices = new PlacesOfServiceModel
            {
                placesOfService = new PlaceOfServicesList
                {
                    total = 1,
                    data = new List<placeOfService>
                    {
                        new placeOfService { id = 1, code = "11", description = "Office", isActive = true, accountId = 1 }
                    }
                }
            };

            _mockRethink.Setup(x => x.GetPlaceOfService(accountInfoId))
                .ReturnsAsync(existingPlaceOfServices);

            _mockRethink.Setup(x => x.GetLocationCodes())
                .ReturnsAsync(new List<LocationCodesModel>());

            var result = await _commonService.GetLocationCodes(accountInfoId);

            Assert.NotNull(result);
            Assert.Empty(result);
            _mockRethink.Verify(x => x.GetPlaceOfService(accountInfoId), Times.Once);
            _mockRethink.Verify(x => x.GetLocationCodes(), Times.Once);
        }

        [Fact]
        public async Task GetLocationCodes_WhenNoMatchingCodes_ReturnsLocationCodesWithDefaultValues()
        {
            var accountInfoId = 1;
            var existingPlaceOfServices = new PlacesOfServiceModel
            {
                placesOfService = new PlaceOfServicesList
                {
                    total = 1,
                    data = new List<placeOfService>
                    {
                        new placeOfService { id = 1, code = "99", description = "Other", isActive = false, accountId = 1 }
                    }
                }
            };

            var locationCodes = new List<LocationCodesModel>
            {
                new LocationCodesModel { id = 1, code = "11", description = "Office" },
                new LocationCodesModel { id = 2, code = "12", description = "Home" }
            };

            _mockRethink.Setup(x => x.GetPlaceOfService(accountInfoId))
                .ReturnsAsync(existingPlaceOfServices);

            _mockRethink.Setup(x => x.GetLocationCodes())
                .ReturnsAsync(locationCodes);

            var result = await _commonService.GetLocationCodes(accountInfoId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, item => 
            {
                Assert.True(item.IsActive == true);
            });
            _mockRethink.Verify(x => x.GetPlaceOfService(accountInfoId), Times.Once);
            _mockRethink.Verify(x => x.GetLocationCodes(), Times.Once);
        }

        [Fact]
        public async Task GetLocationCodes_WhenExceptionOccursInGetPlaceOfService_ThrowsException()
        {
            var accountInfoId = 1;
            var exceptionMessage = "Database connection failed";

            _mockRethink.Setup(x => x.GetPlaceOfService(accountInfoId))
                .ThrowsAsync(new Exception(exceptionMessage));

            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _commonService.GetLocationCodes(accountInfoId));

            Assert.Equal(exceptionMessage, exception.Message);
            _mockRethink.Verify(x => x.GetPlaceOfService(accountInfoId), Times.Once);
            _mockRethink.Verify(x => x.GetLocationCodes(), Times.Never);
        }

        [Fact]
        public async Task GetLocationCodes_WhenExceptionOccursInGetLocationCodes_ThrowsException()
        {
            var accountInfoId = 1;
            var existingPlaceOfServices = new PlacesOfServiceModel
            {
                placesOfService = new PlaceOfServicesList
                {
                    total = 1,
                    data = new List<placeOfService>
                    {
                        new placeOfService { id = 1, code = "11", description = "Office", isActive = true, accountId = 1 }
                    }
                }
            };
            var exceptionMessage = "Failed to get location codes";

            _mockRethink.Setup(x => x.GetPlaceOfService(accountInfoId))
                .ReturnsAsync(existingPlaceOfServices);

            _mockRethink.Setup(x => x.GetLocationCodes())
                .ThrowsAsync(new Exception(exceptionMessage));

            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _commonService.GetLocationCodes(accountInfoId));

            Assert.Equal(exceptionMessage, exception.Message);
            _mockRethink.Verify(x => x.GetPlaceOfService(accountInfoId), Times.Once);
            _mockRethink.Verify(x => x.GetLocationCodes(), Times.Once);
        }

        [Fact]
        public async Task GetLocationCodes_WithMultipleMatchingCodes_UpdatesAllMatchedItems()
        {
            var accountInfoId = 1;
            var existingPlaceOfServices = new PlacesOfServiceModel
            {
                placesOfService = new PlaceOfServicesList
                {
                    total = 3,
                    data = new List<placeOfService>
                    {
                        new placeOfService { id = 1, code = "11", description = "Updated Office", isActive = true, accountId = 1 },
                        new placeOfService { id = 2, code = "12", description = "Updated Home", isActive = false, accountId = 1 },
                        new placeOfService { id = 3, code = "21", description = "Updated Hospital", isActive = true, accountId = 1 }
                    }
                }
            };

            var locationCodes = new List<LocationCodesModel>
            {
                new LocationCodesModel { id = 1, code = "11", description = "Office" },
                new LocationCodesModel { id = 2, code = "12", description = "Home" },
                new LocationCodesModel { id = 3, code = "21", description = "Hospital" },
                new LocationCodesModel { id = 4, code = "22", description = "Outpatient" }
            };

            _mockRethink.Setup(x => x.GetPlaceOfService(accountInfoId))
                .ReturnsAsync(existingPlaceOfServices);

            _mockRethink.Setup(x => x.GetLocationCodes())
                .ReturnsAsync(locationCodes);

            var result = await _commonService.GetLocationCodes(accountInfoId);

            Assert.NotNull(result);
            Assert.Equal(4, result.Count);

            Assert.Equal("Updated Office", result.First(x => x.Code == "11").Description);
            Assert.True(result.First(x => x.Code == "11").IsActive);

            Assert.Equal("Updated Home", result.First(x => x.Code == "12").Description);
            Assert.False(result.First(x => x.Code == "12").IsActive);

            Assert.Equal("Updated Hospital", result.First(x => x.Code == "21").Description);
            Assert.True(result.First(x => x.Code == "21").IsActive);

            Assert.Equal("Outpatient", result.First(x => x.Code == "22").Description);
            Assert.True(result.First(x => x.Code == "22").IsActive);
        }

        [Fact]
        public async Task GetLocationCodes_WhenAccountInfoIdIsZero_ProcessesNormally()
        {
            var accountInfoId = 0;
            var existingPlaceOfServices = new PlacesOfServiceModel
            {
                placesOfService = new PlaceOfServicesList
                {
                    total = 1,
                    data = new List<placeOfService>
                    {
                        new placeOfService { id = 1, code = "11", description = "Office", isActive = true, accountId = 1 }
                    }
                }
            };

            var locationCodes = new List<LocationCodesModel>
            {
                new LocationCodesModel { id = 1, code = "11", description = "Original Office" }
            };

            _mockRethink.Setup(x => x.GetPlaceOfService(accountInfoId))
                .ReturnsAsync(existingPlaceOfServices);

            _mockRethink.Setup(x => x.GetLocationCodes())
                .ReturnsAsync(locationCodes);

            var result = await _commonService.GetLocationCodes(accountInfoId);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Office", result[0].Description);
            _mockRethink.Verify(x => x.GetPlaceOfService(accountInfoId), Times.Once);
            _mockRethink.Verify(x => x.GetLocationCodes(), Times.Once);
        }

        [Fact]
        public async Task GetLocationCodes_WhenAccountInfoIdIsNegative_ProcessesNormally()
        {
            var accountInfoId = -1;
            
            _mockRethink.Setup(x => x.GetPlaceOfService(accountInfoId))
                .ReturnsAsync((PlacesOfServiceModel)null);

            var result = await _commonService.GetLocationCodes(accountInfoId);

            Assert.NotNull(result);
            Assert.Empty(result);
            _mockRethink.Verify(x => x.GetPlaceOfService(accountInfoId), Times.Once);
        }
    }
}
