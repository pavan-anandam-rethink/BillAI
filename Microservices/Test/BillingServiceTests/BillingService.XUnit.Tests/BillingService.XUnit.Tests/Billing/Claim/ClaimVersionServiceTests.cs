using BillingService.Domain.Models;
using BillingService.Domain.Services.Billing;
using BillingService.Domain.Models;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Claim
{
    public class ClaimVersionServiceTests
    {
        private readonly Mock<IRepository<BillingDbContext, ClaimVersionEntity>> _repoMock;
        private readonly ClaimVersionService _service;

        public ClaimVersionServiceTests()
        {
            _repoMock = new Mock<IRepository<BillingDbContext, ClaimVersionEntity>>();
            _service = new ClaimVersionService(_repoMock.Object);
        }

        [Fact]
        public async Task CreateAsync_Should_Create_Record_Without_Type_Errors()
        {
            // Arrange
            var claim = new ClaimDetailsModel
            {
                Id = 1,
                ClaimIdentifier = "TEST-CLAIM",
                DiagnosisCodes = new List<ClaimDiagnosisCodeModel>()
            };



            _repoMock.Setup(x => x.AddAsync(It.IsAny<ClaimVersionEntity>()))
                     .Returns(Task.CompletedTask);

            _repoMock.Setup(x => x.SaveChangesAsync())
                     .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateAsync(claim, 99, 77);

            // Assert
            Assert.True(result >= 0);

            _repoMock.Verify(x => x.AddAsync(It.IsAny<ClaimVersionEntity>()), Times.Once);
            _repoMock.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_Returns_Entity_When_Found()
        {
            // Arrange
            var entity = new ClaimVersionEntity { Id = 22 };

            _repoMock.Setup(x => x.GetByIdAsync(22))
                     .ReturnsAsync(entity);

            // Act
            var result = await _service.GetByIdAsync(22);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(22, result.Id);
        }

        [Fact]
        public async Task GetByIdAsync_Returns_New_Entity_When_Not_Found()
        {
            // Arrange
            _repoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                     .ReturnsAsync((ClaimVersionEntity)null);

            // Act
            var result = await _service.GetByIdAsync(500);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Id);
        }
    }
}
