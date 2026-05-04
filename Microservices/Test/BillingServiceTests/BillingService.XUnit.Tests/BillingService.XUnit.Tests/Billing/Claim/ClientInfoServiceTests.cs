using BillingService.Domain.Services.PatientInvoice;
using BillingService.Domain.Templates.ViewModels;
using Microsoft.Azure.ServiceBus;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models.RethinkDataEntityClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.Domain.Tests.Services.PatientInvoice
{
    public class ClientInfoServiceTests
    {
        private readonly Mock<IRethinkMasterDataMicroServices> _masterDataMock;
        private readonly Mock<IRepository<BillingDbContext, ClaimEntity>> _claimRepoMock;
        private readonly ClientInfoService _service;

        public ClientInfoServiceTests()
        {
            _masterDataMock = new Mock<IRethinkMasterDataMicroServices>();
            _claimRepoMock = new Mock<IRepository<BillingDbContext, ClaimEntity>>();

            _service = new ClientInfoService(
                _masterDataMock.Object,
                _claimRepoMock.Object
            );
        }

        [Fact]
        public async Task GetClientInfo_ReturnsClientInfo_WhenClientExists()
        {
            var clientEntity = new Rethink.Services.Common.Models.ChildProfileEntityModel
            {
                Id = 10,
                FirstName = "John",
                MiddleName = "A",
                LastName = "Doe",
                Address = "123 Street",
                City = "City",
                Town = "Town",
                ZipCode = "12345",
                StateLU = new StateModel { name = "Texas" },
                CountryLU = new CountryModel { name = "USA" }
            };

            _masterDataMock
                .Setup(x => x.GetChildProfileReturningEntity(1, 2))
                .ReturnsAsync(clientEntity);

            var result = await _service.GetClientInfo(1, 2);

            Assert.Equal("10", result.CustomerID);
            Assert.Equal("John A Doe", result.Name);
            Assert.Equal("123 Street", result.Address);
            Assert.Equal("City", result.City);
            Assert.Equal("Town", result.Town);
            Assert.Equal("Texas", result.State);
            Assert.Equal("12345", result.ZipCode);
            Assert.Equal("USA", result.Country);
        }

        [Fact]
        public async Task GetClientInfo_ThrowsException_WhenClientNotFound()
        {
            _masterDataMock
                .Setup(x => x.GetChildProfileReturningEntity(1, 2))
                .ReturnsAsync((Rethink.Services.Common.Models.ChildProfileEntityModel)null);

            await Assert.ThrowsAsync<Exception>(() =>
                _service.GetClientInfo(1, 99));
        }

        [Fact]
        public async Task GetBillingProviderInfo_ReturnsProviderInfo_WhenProviderExists()
        {
            var claims = new List<ClaimEntity>
            {
                new ClaimEntity
                {
                    AccountInfoId = 1,
                    ChildProfileId = 2,
                    ProviderLocationId = 5,
                    DateDeleted = null,
                    DateLastModified = DateTime.UtcNow
                }
            }.AsQueryable();

            _claimRepoMock.Setup(x => x.Query()).Returns(claims);

            _masterDataMock
                .Setup(x => x.GetStateList())
                .ReturnsAsync(new List<StateModel>
                {
                    new StateModel { id = 1, abbreviation = "TX" }
                });

            _masterDataMock
                .Setup(x => x.GetProviderLocation(1, 5))
                .ReturnsAsync(new Rethink.Services.Common.Models.ProviderLocations
                {
                    agencyName = "Agency",
                    phone = "123456",
                    address = new Rethink.Services.Common.Models.ProviderLocationAddress
                    {
                        street1 = "S1",
                        street2 = null,
                        city = "LA",
                        stateId = 1,
                        zip = "90001"
                    }
                });

            var result = await _service.GetBillingProviderInfo(1, 2);

            Assert.Equal("Agency", result.Name);
            //Assert.Contains("Dallas, TX 75001", result.Address);
            Assert.Equal("123456", result.Phone);
        }

        [Fact]
        public async Task GetBillingProviderInfo_ReturnsEmptyObject_WhenNoProviderLocation()
        {
            _claimRepoMock
                .Setup(x => x.Query())
                .Returns(new List<ClaimEntity>().AsQueryable());

            var result = await _service.GetBillingProviderInfo(1, 2);

            Assert.NotNull(result);
            Assert.Null(result.Name);
            Assert.Null(result.Address);
            Assert.Null(result.Phone);
        }

        [Fact]
        public async Task GetState_UsesCachedStateList_OnSecondCall()
        {
            _masterDataMock
                .Setup(x => x.GetStateList())
                .ReturnsAsync(new List<StateModel>
                {
                    new StateModel { id = 2, abbreviation = "CA" }
                });

            var claims = new List<ClaimEntity>
            {
                new ClaimEntity
                {
                    AccountInfoId = 1,
                    ChildProfileId = 2,
                    ProviderLocationId = 10,
                    DateDeleted = null,
                    DateLastModified = DateTime.UtcNow
                }
            }.AsQueryable();

            _claimRepoMock.Setup(x => x.Query()).Returns(claims);

            // Replace the problematic anonymous type with a concrete ProviderLocations object for Moq compatibility
            _masterDataMock
                .Setup(x => x.GetProviderLocation(1, 10))
                .ReturnsAsync(new Rethink.Services.Common.Models.ProviderLocations
                {
                    agencyName = "Agency",
                    phone = null,
                    address = new Rethink.Services.Common.Models.ProviderLocationAddress
                    {
                        street1 = "S1",
                        street2 = null,
                        city = "LA",
                        stateId = 2,
                        zip = "90001"
                    }
                });

            await _service.GetBillingProviderInfo(1, 2);
            await _service.GetBillingProviderInfo(1, 2);

            _masterDataMock.Verify(x => x.GetStateList(), Times.Once);
        }
    }
}
