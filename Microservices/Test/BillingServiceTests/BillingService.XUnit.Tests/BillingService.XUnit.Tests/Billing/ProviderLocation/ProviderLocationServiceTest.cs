using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Services.Billing;
using Moq;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.ProviderLocation
{
    public class ProviderLocationServiceTest
    {
        private readonly Mock<IRethinkMasterDataMicroServices> _rethinkServicesMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly IProviderLocationService _service;

        public ProviderLocationServiceTest()
        {
            _rethinkServicesMock = new Mock<IRethinkMasterDataMicroServices>();
            _mapperMock = new Mock<IMapper>();

            _service = new ProviderLocationService(
                _rethinkServicesMock.Object,
                _mapperMock.Object
            );
        }

        [Fact]
        public async Task GetForAccount_ReturnsActiveLocations_WhenLocationsExist()
        {
            // Arrange
            var accountInfoId = 100;

            var providerLocations = new List<ProviderLocations>
            {
                new ProviderLocations
                {
                    id = 1,
                    accountId = accountInfoId,
                    name = "Main Location",
                    phone = "123-456-7890",
                    email = "main@example.com",
                    isMainLocation = true,
                    metaData = new MetaData { deletedOn = null }
                },
                new ProviderLocations
                {
                    id = 2,
                    accountId = accountInfoId,
                    name = "Branch Location",
                    phone = "098-765-4321",
                    email = "branch@example.com",
                    isMainLocation = false,
                    metaData = new MetaData { deletedOn = null }
                },
                new ProviderLocations
                {
                    id = 3,
                    accountId = accountInfoId,
                    name = "Deleted Location",
                    phone = "555-555-5555",
                    email = "deleted@example.com",
                    isMainLocation = false,
                    metaData = new MetaData { deletedOn = DateTime.UtcNow }
                }
            };

            var mockResponse = new ClientProviderLocationsModel
            {
                total = 3,
                data = providerLocations
            };

            _rethinkServicesMock
                .Setup(x => x.GetProviderLocationList(accountInfoId))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _service.GetForAccount(accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Only non-deleted locations
            Assert.All(result, location => Assert.Null(location.metaData.deletedOn));
            Assert.Contains(result, x => x.id == 1);
            Assert.Contains(result, x => x.id == 2);
            Assert.DoesNotContain(result, x => x.id == 3); // Deleted location should be filtered out

            _rethinkServicesMock.Verify(x => x.GetProviderLocationList(accountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetForAccount_ReturnsEmptyList_WhenNoActiveLocationsExist()
        {
            // Arrange
            var accountInfoId = 100;

            var providerLocations = new List<ProviderLocations>
            {
                new ProviderLocations
                {
                    id = 1,
                    accountId = accountInfoId,
                    name = "Deleted Location 1",
                    metaData = new MetaData { deletedOn = DateTime.UtcNow.AddDays(-1) }
                },
                new ProviderLocations
                {
                    id = 2,
                    accountId = accountInfoId,
                    name = "Deleted Location 2",
                    metaData = new MetaData { deletedOn = DateTime.UtcNow }
                }
            };

            var mockResponse = new ClientProviderLocationsModel
            {
                total = 2,
                data = providerLocations
            };

            _rethinkServicesMock
                .Setup(x => x.GetProviderLocationList(accountInfoId))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _service.GetForAccount(accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            _rethinkServicesMock.Verify(x => x.GetProviderLocationList(accountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetForAccount_ReturnsEmptyList_WhenNoLocationsExist()
        {
            // Arrange
            var accountInfoId = 100;

            var mockResponse = new ClientProviderLocationsModel
            {
                total = 0,
                data = new List<ProviderLocations>()
            };

            _rethinkServicesMock
                .Setup(x => x.GetProviderLocationList(accountInfoId))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _service.GetForAccount(accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);

            _rethinkServicesMock.Verify(x => x.GetProviderLocationList(accountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetForAccount_FiltersDeletedLocations_BasedOnDeletedOnProperty()
        {
            // Arrange
            var accountInfoId = 100;

            var providerLocations = new List<ProviderLocations>
            {
                new ProviderLocations
                {
                    id = 1,
                    name = "Active Location 1",
                    metaData = new MetaData { deletedOn = null }
                },
                new ProviderLocations
                {
                    id = 2,
                    name = "Active Location 2",
                    metaData = new MetaData { deletedOn = null }
                },
                new ProviderLocations
                {
                    id = 3,
                    name = "Deleted Yesterday",
                    metaData = new MetaData { deletedOn = DateTime.UtcNow.AddDays(-1) }
                },
                new ProviderLocations
                {
                    id = 4,
                    name = "Deleted Today",
                    metaData = new MetaData { deletedOn = DateTime.UtcNow }
                },
                new ProviderLocations
                {
                    id = 5,
                    name = "Active Location 3",
                    metaData = new MetaData { deletedOn = null }
                }
            };

            var mockResponse = new ClientProviderLocationsModel
            {
                total = 5,
                data = providerLocations
            };

            _rethinkServicesMock
                .Setup(x => x.GetProviderLocationList(accountInfoId))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _service.GetForAccount(accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.All(result, location => Assert.Null(location.metaData.deletedOn));
            Assert.Contains(result, x => x.id == 1);
            Assert.Contains(result, x => x.id == 2);
            Assert.Contains(result, x => x.id == 5);

            _rethinkServicesMock.Verify(x => x.GetProviderLocationList(accountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetForAccount_PreservesOrderOfActiveLocations()
        {
            // Arrange
            var accountInfoId = 100;

            var providerLocations = new List<ProviderLocations>
            {
                new ProviderLocations
                {
                    id = 5,
                    name = "Location 5",
                    metaData = new MetaData { deletedOn = null }
                },
                new ProviderLocations
                {
                    id = 10,
                    name = "Deleted",
                    metaData = new MetaData { deletedOn = DateTime.UtcNow }
                },
                new ProviderLocations
                {
                    id = 3,
                    name = "Location 3",
                    metaData = new MetaData { deletedOn = null }
                },
                new ProviderLocations
                {
                    id = 1,
                    name = "Location 1",
                    metaData = new MetaData { deletedOn = null }
                }
            };

            var mockResponse = new ClientProviderLocationsModel
            {
                total = 4,
                data = providerLocations
            };

            _rethinkServicesMock
                .Setup(x => x.GetProviderLocationList(accountInfoId))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _service.GetForAccount(accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);

            // Verify order is preserved (5, 3, 1 - skipping deleted id 10)
            Assert.Equal(5, result[0].id);
            Assert.Equal(3, result[1].id);
            Assert.Equal(1, result[2].id);

            _rethinkServicesMock.Verify(x => x.GetProviderLocationList(accountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetForAccount_ReturnsAllProperties_ForActiveLocations()
        {
            // Arrange
            var accountInfoId = 100;
            var expectedDate = new DateTime(2024, 1, 1);

            var providerLocations = new List<ProviderLocations>
            {
                new ProviderLocations
                {
                    id = 1,
                    accountId = accountInfoId,
                    name = "Complete Location",
                    phone = "123-456-7890",
                    email = "complete@example.com",
                    website = "https://example.com",
                    addressId = 100,
                    isMainLocation = true,
                    fax = "123-456-7891",
                    isBillingLocation = true,
                    agencyName = "Test Agency",
                    federalTaxId = "12-3456789",
                    npiNumber = "1234567890",
                    taxonomyCode = "101Y00000X",
                    effectiveDate = expectedDate,
                    providerCommercialNumber = "PC123",
                    stateLicenseNumber = "SL123",
                    locationNumber = "LOC001",
                    address = new ProviderLocationAddress { city = "Test City" },
                    metaData = new MetaData { deletedOn = null, createdOn = expectedDate }
                }
            };

            var mockResponse = new ClientProviderLocationsModel
            {
                total = 1,
                data = providerLocations
            };

            _rethinkServicesMock
                .Setup(x => x.GetProviderLocationList(accountInfoId))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _service.GetForAccount(accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            var location = result.First();
            Assert.Equal(1, location.id);
            Assert.Equal(accountInfoId, location.accountId);
            Assert.Equal("Complete Location", location.name);
            Assert.Equal("123-456-7890", location.phone);
            Assert.Equal("complete@example.com", location.email);
            Assert.Equal("https://example.com", location.website);
            Assert.Equal(100, location.addressId);
            Assert.True(location.isMainLocation);
            Assert.Equal("123-456-7891", location.fax);
            Assert.True(location.isBillingLocation);
            Assert.Equal("Test Agency", location.agencyName);
            Assert.Equal("12-3456789", location.federalTaxId);
            Assert.Equal("1234567890", location.npiNumber);
            Assert.Equal("101Y00000X", location.taxonomyCode);
            Assert.Equal(expectedDate, location.effectiveDate);
            Assert.Equal("PC123", location.providerCommercialNumber);
            Assert.Equal("SL123", location.stateLicenseNumber);
            Assert.Equal("LOC001", location.locationNumber);
            Assert.NotNull(location.address);
            Assert.Equal("Test City", location.address.city);

            _rethinkServicesMock.Verify(x => x.GetProviderLocationList(accountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetForAccount_WorksWithDifferentAccountIds()
        {
            // Arrange
            var accountInfoId1 = 100;
            var accountInfoId2 = 200;

            var locations1 = new List<ProviderLocations>
            {
                new ProviderLocations
                {
                    id = 1,
                    accountId = accountInfoId1,
                    name = "Account 100 Location",
                    metaData = new MetaData { deletedOn = null }
                }
            };

            var locations2 = new List<ProviderLocations>
            {
                new ProviderLocations
                {
                    id = 2,
                    accountId = accountInfoId2,
                    name = "Account 200 Location",
                    metaData = new MetaData { deletedOn = null }
                },
                new ProviderLocations
                {
                    id = 3,
                    accountId = accountInfoId2,
                    name = "Account 200 Location 2",
                    metaData = new MetaData { deletedOn = null }
                }
            };

            _rethinkServicesMock
                .Setup(x => x.GetProviderLocationList(accountInfoId1))
                .ReturnsAsync(new ClientProviderLocationsModel { total = 1, data = locations1 });

            _rethinkServicesMock
                .Setup(x => x.GetProviderLocationList(accountInfoId2))
                .ReturnsAsync(new ClientProviderLocationsModel { total = 2, data = locations2 });

            // Act
            var result1 = await _service.GetForAccount(accountInfoId1);
            var result2 = await _service.GetForAccount(accountInfoId2);

            // Assert
            Assert.Single(result1);
            Assert.Equal("Account 100 Location", result1.First().name);

            Assert.Equal(2, result2.Count);
            Assert.All(result2, loc => Assert.Equal(accountInfoId2, loc.accountId));

            _rethinkServicesMock.Verify(x => x.GetProviderLocationList(accountInfoId1), Times.Once);
            _rethinkServicesMock.Verify(x => x.GetProviderLocationList(accountInfoId2), Times.Once);
        }

        [Fact]
        public async Task GetForAccount_IncludesMainAndBranchLocations()
        {
            // Arrange
            var accountInfoId = 100;

            var providerLocations = new List<ProviderLocations>
            {
                new ProviderLocations
                {
                    id = 1,
                    name = "Main Location",
                    isMainLocation = true,
                    isBillingLocation = true,
                    metaData = new MetaData { deletedOn = null }
                },
                new ProviderLocations
                {
                    id = 2,
                    name = "Branch Location 1",
                    isMainLocation = false,
                    isBillingLocation = false,
                    metaData = new MetaData { deletedOn = null }
                },
                new ProviderLocations
                {
                    id = 3,
                    name = "Branch Location 2",
                    isMainLocation = false,
                    isBillingLocation = true,
                    metaData = new MetaData { deletedOn = null }
                }
            };

            var mockResponse = new ClientProviderLocationsModel
            {
                total = 3,
                data = providerLocations
            };

            _rethinkServicesMock
                .Setup(x => x.GetProviderLocationList(accountInfoId))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _service.GetForAccount(accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);

            var mainLocation = result.Single(x => x.isMainLocation);
            Assert.Equal(1, mainLocation.id);
            Assert.Equal("Main Location", mainLocation.name);

            var branchLocations = result.Where(x => !x.isMainLocation).ToList();
            Assert.Equal(2, branchLocations.Count);

            _rethinkServicesMock.Verify(x => x.GetProviderLocationList(accountInfoId), Times.Once);
        }
        [Fact]
        public async Task GetForAccount_HandlesNullableProperties()
        {
            // Arrange
            var accountInfoId = 100;

            var providerLocations = new List<ProviderLocations>
            {
                new ProviderLocations
                {
                    id = 1,
                    name = "Minimal Location",
                    phone = "123-456-7890",
                    email = "minimal@example.com",
                    website = null,
                    agencyName = null,
                    federalTaxId = null,
                    npiNumber = null,
                    taxonomyCode = null,
                    effectiveDate = null,
                    providerCommercialNumber = null,
                    stateLicenseNumber = null,
                    locationNumber = null,
                    metaData = new MetaData { deletedOn = null }
                }
            };

            var mockResponse = new ClientProviderLocationsModel
            {
                total = 1,
                data = providerLocations
            };

            _rethinkServicesMock
                .Setup(x => x.GetProviderLocationList(accountInfoId))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _service.GetForAccount(accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            var location = result.First();
            Assert.Null(location.website);
            Assert.Null(location.agencyName);
            Assert.Null(location.federalTaxId);
            Assert.Null(location.npiNumber);
            Assert.Null(location.taxonomyCode);
            Assert.Null(location.effectiveDate);
            Assert.Null(location.providerCommercialNumber);
            Assert.Null(location.stateLicenseNumber);
            Assert.Null(location.locationNumber);

            _rethinkServicesMock.Verify(x => x.GetProviderLocationList(accountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetForAccount_FiltersOutRecentlyDeletedLocations()
        {
            // Arrange
            var accountInfoId = 100;
            var now = DateTime.UtcNow;

            var providerLocations = new List<ProviderLocations>
            {
                new ProviderLocations
                {
                    id = 1,
                    name = "Active Location",
                    metaData = new MetaData { deletedOn = null }
                },
                new ProviderLocations
                {
                    id = 2,
                    name = "Just Deleted",
                    metaData = new MetaData { deletedOn = now }
                },
                new ProviderLocations
                {
                    id = 3,
                    name = "Deleted 1 Second Ago",
                    metaData = new MetaData { deletedOn = now.AddSeconds(-1) }
                },
                new ProviderLocations
                {
                    id = 4,
                    name = "Deleted 1 Month Ago",
                    metaData = new MetaData { deletedOn = now.AddMonths(-1) }
                }
            };

            var mockResponse = new ClientProviderLocationsModel
            {
                total = 4,
                data = providerLocations
            };

            _rethinkServicesMock
                .Setup(x => x.GetProviderLocationList(accountInfoId))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _service.GetForAccount(accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result.First().id);
            Assert.Equal("Active Location", result.First().name);

            _rethinkServicesMock.Verify(x => x.GetProviderLocationList(accountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetForAccount_OnlyReturnsLocationsWhereDeletedOnIsNull()
        {
            // Arrange
            var accountInfoId = 100;

            var providerLocations = new List<ProviderLocations>
            {
                new ProviderLocations
                {
                    id = 1,
                    name = "Active 1",
                    metaData = new MetaData { deletedOn = null, createdOn = DateTime.UtcNow.AddDays(-10) }
                },
                new ProviderLocations
                {
                    id = 2,
                    name = "Active 2",
                    metaData = new MetaData { deletedOn = null, createdOn = DateTime.UtcNow.AddDays(-5) }
                },
                new ProviderLocations
                {
                    id = 3,
                    name = "Deleted Far Future",
                    metaData = new MetaData { deletedOn = DateTime.UtcNow.AddYears(1) }
                },
                new ProviderLocations
                {
                    id = 4,
                    name = "Deleted Far Past",
                    metaData = new MetaData { deletedOn = DateTime.UtcNow.AddYears(-1) }
                }
            };

            var mockResponse = new ClientProviderLocationsModel
            {
                total = 4,
                data = providerLocations
            };

            _rethinkServicesMock
                .Setup(x => x.GetProviderLocationList(accountInfoId))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _service.GetForAccount(accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, location => Assert.Null(location.metaData.deletedOn));
            Assert.Contains(result, x => x.name == "Active 1");
            Assert.Contains(result, x => x.name == "Active 2");

            _rethinkServicesMock.Verify(x => x.GetProviderLocationList(accountInfoId), Times.Once);
        }
    }
}
