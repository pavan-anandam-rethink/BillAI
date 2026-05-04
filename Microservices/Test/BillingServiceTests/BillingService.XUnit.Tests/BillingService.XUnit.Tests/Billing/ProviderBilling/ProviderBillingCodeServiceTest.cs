using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Services.Billing;
using BillingService.XUnit.Tests.Common;
using Moq;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.ProviderBilling
{
    public class ProviderBillingCodeServiceTest : BaseTest
    {
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IRethinkMasterDataMicroServices> _rethinkServicesMock;
        private readonly IProviderBillingCodeService _service;

        public ProviderBillingCodeServiceTest()
        {
            _mapperMock = new Mock<IMapper>();
            _rethinkServicesMock = new Mock<IRethinkMasterDataMicroServices>();

            _service = new ProviderBillingCodeService(
                _mapperMock.Object,
                _rethinkServicesMock.Object
            );
        }

        [Fact]
        public async Task GetServiceRateAsync_ReturnsRate_WhenBillingCodeExists()
        {
            // Arrange
            var funderId = 1;
            var serviceCode = "90834";
            var accountInfoId = 100;
            var expectedRate = 150.00m;

            var billingCodes = new List<RethinkProviderBillingCode>
            {
                new RethinkProviderBillingCode
                {
                    id = 1,
                    funderId = funderId,
                    billingCode = serviceCode,
                    rate = expectedRate,
                    billingCodeText = "Psychotherapy 45 minutes",
                    serviceId = 10,
                    unitTypeId = 1
                },
                new RethinkProviderBillingCode
                {
                    id = 2,
                    funderId = 2,
                    billingCode = "90837",
                    rate = 200.00m,
                    billingCodeText = "Psychotherapy 60 minutes",
                    serviceId = 11,
                    unitTypeId = 1
                }
            };

            _rethinkServicesMock
                .Setup(x => x.GetProviderBillingCodeList(accountInfoId))
                .ReturnsAsync(billingCodes);

            // Act
            var result = await _service.GetServiceRateAsync(funderId, serviceCode, accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedRate, result.Value);

            _rethinkServicesMock.Verify(x => x.GetProviderBillingCodeList(accountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetServiceRateAsync_ReturnsNull_WhenBillingCodeNotFound()
        {
            // Arrange
            var funderId = 1;
            var serviceCode = "99999";
            var accountInfoId = 100;

            var billingCodes = new List<RethinkProviderBillingCode>
            {
                new RethinkProviderBillingCode
                {
                    id = 1,
                    funderId = funderId,
                    billingCode = "90834",
                    rate = 150.00m,
                    serviceId = 10,
                    unitTypeId = 1
                }
            };

            _rethinkServicesMock
                .Setup(x => x.GetProviderBillingCodeList(accountInfoId))
                .ReturnsAsync(billingCodes);

            // Act
            var result = await _service.GetServiceRateAsync(funderId, serviceCode, accountInfoId);

            // Assert
            Assert.Null(result);

            _rethinkServicesMock.Verify(x => x.GetProviderBillingCodeList(accountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetServiceRateAsync_ReturnsNull_WhenFunderIdDoesNotMatch()
        {
            // Arrange
            var funderId = 999;
            var serviceCode = "90834";
            var accountInfoId = 100;

            var billingCodes = new List<RethinkProviderBillingCode>
            {
                new RethinkProviderBillingCode
                {
                    id = 1,
                    funderId = 1,
                    billingCode = serviceCode,
                    rate = 150.00m,
                    serviceId = 10,
                    unitTypeId = 1
                }
            };

            _rethinkServicesMock
                .Setup(x => x.GetProviderBillingCodeList(accountInfoId))
                .ReturnsAsync(billingCodes);

            // Act
            var result = await _service.GetServiceRateAsync(funderId, serviceCode, accountInfoId);

            // Assert
            Assert.Null(result);

            _rethinkServicesMock.Verify(x => x.GetProviderBillingCodeList(accountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetServiceRateAsync_ReturnsNull_WhenServiceCodeDoesNotMatch()
        {
            // Arrange
            var funderId = 1;
            var serviceCode = "90837";
            var accountInfoId = 100;

            var billingCodes = new List<RethinkProviderBillingCode>
            {
                new RethinkProviderBillingCode
                {
                    id = 1,
                    funderId = funderId,
                    billingCode = "90834",
                    rate = 150.00m,
                    serviceId = 10,
                    unitTypeId = 1
                }
            };

            _rethinkServicesMock
                .Setup(x => x.GetProviderBillingCodeList(accountInfoId))
                .ReturnsAsync(billingCodes);

            // Act
            var result = await _service.GetServiceRateAsync(funderId, serviceCode, accountInfoId);

            // Assert
            Assert.Null(result);

            _rethinkServicesMock.Verify(x => x.GetProviderBillingCodeList(accountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetServiceRateAsync_ReturnsNull_WhenEmptyBillingCodeList()
        {
            // Arrange
            var funderId = 1;
            var serviceCode = "90834";
            var accountInfoId = 100;

            var billingCodes = new List<RethinkProviderBillingCode>();

            _rethinkServicesMock
                .Setup(x => x.GetProviderBillingCodeList(accountInfoId))
                .ReturnsAsync(billingCodes);

            // Act
            var result = await _service.GetServiceRateAsync(funderId, serviceCode, accountInfoId);

            // Assert
            Assert.Null(result);

            _rethinkServicesMock.Verify(x => x.GetProviderBillingCodeList(accountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetServiceRateAsync_ReturnsFirstMatch_WhenMultipleMatchesExist()
        {
            // Arrange
            var funderId = 1;
            var serviceCode = "90834";
            var accountInfoId = 100;
            var firstRate = 150.00m;

            var billingCodes = new List<RethinkProviderBillingCode>
            {
                new RethinkProviderBillingCode
                {
                    id = 1,
                    funderId = funderId,
                    billingCode = serviceCode,
                    rate = firstRate,
                    serviceId = 10,
                    unitTypeId = 1
                },
                new RethinkProviderBillingCode
                {
                    id = 2,
                    funderId = funderId,
                    billingCode = serviceCode,
                    rate = 175.00m,
                    serviceId = 11,
                    unitTypeId = 1
                }
            };

            _rethinkServicesMock
                .Setup(x => x.GetProviderBillingCodeList(accountInfoId))
                .ReturnsAsync(billingCodes);

            // Act
            var result = await _service.GetServiceRateAsync(funderId, serviceCode, accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(firstRate, result.Value);

            _rethinkServicesMock.Verify(x => x.GetProviderBillingCodeList(accountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetServiceRateAsync_HandlesNullRate()
        {
            // Arrange
            var funderId = 1;
            var serviceCode = "90834";
            var accountInfoId = 100;

            var billingCodes = new List<RethinkProviderBillingCode>
            {
                new RethinkProviderBillingCode
                {
                    id = 1,
                    funderId = funderId,
                    billingCode = serviceCode,
                    rate = null,
                    serviceId = 10,
                    unitTypeId = 1
                }
            };

            _rethinkServicesMock
                .Setup(x => x.GetProviderBillingCodeList(accountInfoId))
                .ReturnsAsync(billingCodes);

            // Act
            var result = await _service.GetServiceRateAsync(funderId, serviceCode, accountInfoId);

            // Assert
            Assert.Null(result);

            _rethinkServicesMock.Verify(x => x.GetProviderBillingCodeList(accountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetServiceRateAsync_IsCaseSensitiveForServiceCode()
        {
            // Arrange
            var funderId = 1;
            var serviceCode = "90834";
            var accountInfoId = 100;

            var billingCodes = new List<RethinkProviderBillingCode>
            {
                new RethinkProviderBillingCode
                {
                    id = 1,
                    funderId = funderId,
                    billingCode = "90834",
                    rate = 150.00m,
                    serviceId = 10,
                    unitTypeId = 1
                }
            };

            _rethinkServicesMock
                .Setup(x => x.GetProviderBillingCodeList(accountInfoId))
                .ReturnsAsync(billingCodes);

            // Act - searching with lowercase
            var result = await _service.GetServiceRateAsync(funderId, "90834", accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(150.00m, result.Value);

            _rethinkServicesMock.Verify(x => x.GetProviderBillingCodeList(accountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetServiceRateAsync_WorksWithDifferentAccountInfoIds()
        {
            // Arrange
            var funderId = 1;
            var serviceCode = "90834";
            var accountInfoId1 = 100;
            var accountInfoId2 = 200;

            var billingCodes1 = new List<RethinkProviderBillingCode>
            {
                new RethinkProviderBillingCode
                {
                    id = 1,
                    funderId = funderId,
                    billingCode = serviceCode,
                    rate = 150.00m,
                    serviceId = 10,
                    unitTypeId = 1
                }
            };

            var billingCodes2 = new List<RethinkProviderBillingCode>
            {
                new RethinkProviderBillingCode
                {
                    id = 2,
                    funderId = funderId,
                    billingCode = serviceCode,
                    rate = 175.00m,
                    serviceId = 10,
                    unitTypeId = 1
                }
            };

            _rethinkServicesMock
                .Setup(x => x.GetProviderBillingCodeList(accountInfoId1))
                .ReturnsAsync(billingCodes1);

            _rethinkServicesMock
                .Setup(x => x.GetProviderBillingCodeList(accountInfoId2))
                .ReturnsAsync(billingCodes2);

            // Act
            var result1 = await _service.GetServiceRateAsync(funderId, serviceCode, accountInfoId1);
            var result2 = await _service.GetServiceRateAsync(funderId, serviceCode, accountInfoId2);

            // Assert
            Assert.NotNull(result1);
            Assert.Equal(150.00m, result1.Value);

            Assert.NotNull(result2);
            Assert.Equal(175.00m, result2.Value);

            _rethinkServicesMock.Verify(x => x.GetProviderBillingCodeList(accountInfoId1), Times.Once);
            _rethinkServicesMock.Verify(x => x.GetProviderBillingCodeList(accountInfoId2), Times.Once);
        }

        [Fact]
        public async Task GetServiceRateAsync_HandlesZeroRate()
        {
            // Arrange
            var funderId = 1;
            var serviceCode = "90834";
            var accountInfoId = 100;

            var billingCodes = new List<RethinkProviderBillingCode>
            {
                new RethinkProviderBillingCode
                {
                    id = 1,
                    funderId = funderId,
                    billingCode = serviceCode,
                    rate = 0.00m,
                    serviceId = 10,
                    unitTypeId = 1
                }
            };

            _rethinkServicesMock
                .Setup(x => x.GetProviderBillingCodeList(accountInfoId))
                .ReturnsAsync(billingCodes);

            // Act
            var result = await _service.GetServiceRateAsync(funderId, serviceCode, accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0.00m, result.Value);

            _rethinkServicesMock.Verify(x => x.GetProviderBillingCodeList(accountInfoId), Times.Once);
        }

        [Fact]
        public async Task GetServiceRateAsync_HandlesLargeRateValues()
        {
            // Arrange
            var funderId = 1;
            var serviceCode = "90834";
            var accountInfoId = 100;
            var largeRate = 999999.99m;

            var billingCodes = new List<RethinkProviderBillingCode>
            {
                new RethinkProviderBillingCode
                {
                    id = 1,
                    funderId = funderId,
                    billingCode = serviceCode,
                    rate = largeRate,
                    serviceId = 10,
                    unitTypeId = 1
                }
            };

            _rethinkServicesMock
                .Setup(x => x.GetProviderBillingCodeList(accountInfoId))
                .ReturnsAsync(billingCodes);

            // Act
            var result = await _service.GetServiceRateAsync(funderId, serviceCode, accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(largeRate, result.Value);

            _rethinkServicesMock.Verify(x => x.GetProviderBillingCodeList(accountInfoId), Times.Once);
        }
    }
}

