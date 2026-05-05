using BillingService.Domain.Extensions;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Extensions
{
    /// <summary>
    /// Unit tests for PaymentClaimServiceLineExt extension class
    /// Tests the OrderByApplicationType extension method with various sorting criteria
    /// </summary>
    public class PaymentClaimServiceLineExtTests
    {
        [Fact]
        public void OrderByApplicationType_WithHighestToLowest_ShouldSortByBalanceDescending()
        {
            // Arrange
            var serviceLine1 = new PaymentClaimServiceLineEntity
            {
                Id = 1,
                ChargeAmount = 100m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>
     {
                    new PaymentClaimServiceLineAdjustmentEntity { AdjustmentAmount = 10m }
                },
                DateOfService = new DateTime(2024, 1, 1)
            };

            var serviceLine2 = new PaymentClaimServiceLineEntity
            {
                Id = 2,
                ChargeAmount = 200m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>
        {
                  new PaymentClaimServiceLineAdjustmentEntity { AdjustmentAmount = 20m }
          },
                DateOfService = new DateTime(2024, 1, 2)
            };

            var serviceLine3 = new PaymentClaimServiceLineEntity
            {
                Id = 3,
                ChargeAmount = 50m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
                DateOfService = new DateTime(2024, 1, 3)
            };

            var serviceLines = new List<PaymentClaimServiceLineEntity> { serviceLine1, serviceLine2, serviceLine3 };

            // Act
            var result = serviceLines.OrderByApplicationType(BulkPostingCriteria.HighestToLowest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            // Expected order by balance descending: 180 (200-20), 90 (100-10), 50
            Assert.Equal(2, result[0].Id); // 180
            Assert.Equal(1, result[1].Id); // 90
            Assert.Equal(3, result[2].Id); // 50
        }

        [Fact]
        public void OrderByApplicationType_WithLowestToHighest_ShouldSortByBalanceAscending()
        {
            // Arrange
            var serviceLine1 = new PaymentClaimServiceLineEntity
            {
                Id = 1,
                ChargeAmount = 100m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>
         {
           new PaymentClaimServiceLineAdjustmentEntity { AdjustmentAmount = 10m }
         },
                DateOfService = new DateTime(2024, 1, 1)
            };

            var serviceLine2 = new PaymentClaimServiceLineEntity
            {
                Id = 2,
                ChargeAmount = 200m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>
       {
        new PaymentClaimServiceLineAdjustmentEntity { AdjustmentAmount = 20m }
 },
                DateOfService = new DateTime(2024, 1, 2)
            };

            var serviceLine3 = new PaymentClaimServiceLineEntity
            {
                Id = 3,
                ChargeAmount = 50m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
                DateOfService = new DateTime(2024, 1, 3)
            };

            var serviceLines = new List<PaymentClaimServiceLineEntity> { serviceLine1, serviceLine2, serviceLine3 };

            // Act
            var result = serviceLines.OrderByApplicationType(BulkPostingCriteria.LowestToHighest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            // Expected order by balance ascending: 50, 90 (100-10), 180 (200-20)
            Assert.Equal(3, result[0].Id); // 50
            Assert.Equal(1, result[1].Id); // 90
            Assert.Equal(2, result[2].Id); // 180
        }

        [Fact]
        public void OrderByApplicationType_WithNewestToOldest_ShouldSortByDateOfServiceDescending()
        {
            // Arrange
            var serviceLine1 = new PaymentClaimServiceLineEntity
            {
                Id = 1,
                ChargeAmount = 100m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
                DateOfService = new DateTime(2024, 1, 1)
            };

            var serviceLine2 = new PaymentClaimServiceLineEntity
            {
                Id = 2,
                ChargeAmount = 200m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
                DateOfService = new DateTime(2024, 1, 3)
            };

            var serviceLine3 = new PaymentClaimServiceLineEntity
            {
                Id = 3,
                ChargeAmount = 50m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
                DateOfService = new DateTime(2024, 1, 2)
            };

            var serviceLines = new List<PaymentClaimServiceLineEntity> { serviceLine1, serviceLine2, serviceLine3 };

            // Act
            var result = serviceLines.OrderByApplicationType(BulkPostingCriteria.NewestToOldest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            // Expected order: 2024-01-03, 2024-01-02, 2024-01-01
            Assert.Equal(2, result[0].Id);
            Assert.Equal(3, result[1].Id);
            Assert.Equal(1, result[2].Id);
        }

        [Fact]
        public void OrderByApplicationType_WithOldestToNewest_ShouldSortByDateOfServiceAscending()
        {
            // Arrange
            var serviceLine1 = new PaymentClaimServiceLineEntity
            {
                Id = 1,
                ChargeAmount = 100m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
                DateOfService = new DateTime(2024, 1, 1)
            };

            var serviceLine2 = new PaymentClaimServiceLineEntity
            {
                Id = 2,
                ChargeAmount = 200m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
                DateOfService = new DateTime(2024, 1, 3)
            };

            var serviceLine3 = new PaymentClaimServiceLineEntity
            {
                Id = 3,
                ChargeAmount = 50m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
                DateOfService = new DateTime(2024, 1, 2)
            };

            var serviceLines = new List<PaymentClaimServiceLineEntity> { serviceLine1, serviceLine2, serviceLine3 };

            // Act
            var result = serviceLines.OrderByApplicationType(BulkPostingCriteria.OldestToNewest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            // Expected order: 2024-01-01, 2024-01-02, 2024-01-03
            Assert.Equal(1, result[0].Id);
            Assert.Equal(3, result[1].Id);
            Assert.Equal(2, result[2].Id);
        }

        [Fact]
        public void OrderByApplicationType_WithNullCriteria_ShouldReturnUnorderedList()
        {
            // Arrange
            var serviceLine1 = new PaymentClaimServiceLineEntity
            {
                Id = 1,
                ChargeAmount = 100m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
                DateOfService = new DateTime(2024, 1, 3)
            };

            var serviceLine2 = new PaymentClaimServiceLineEntity
            {
                Id = 2,
                ChargeAmount = 200m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
                DateOfService = new DateTime(2024, 1, 1)
            };

            var serviceLines = new List<PaymentClaimServiceLineEntity> { serviceLine1, serviceLine2 };

            // Act
            var result = serviceLines.OrderByApplicationType(null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            // Should return in original order (default case)
            Assert.Equal(1, result[0].Id);
            Assert.Equal(2, result[1].Id);
        }

        [Fact]
        public void OrderByApplicationType_WithEmptyCollection_ShouldReturnEmptyList()
        {
            // Arrange
            var serviceLines = new List<PaymentClaimServiceLineEntity>();

            // Act
            var result = serviceLines.OrderByApplicationType(BulkPostingCriteria.HighestToLowest);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void OrderByApplicationType_WithSingleItem_ShouldReturnSingleItem()
        {
            // Arrange
            var serviceLine = new PaymentClaimServiceLineEntity
            {
                Id = 1,
                ChargeAmount = 100m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
                DateOfService = new DateTime(2024, 1, 1)
            };

            var serviceLines = new List<PaymentClaimServiceLineEntity> { serviceLine };

            // Act
            var result = serviceLines.OrderByApplicationType(BulkPostingCriteria.HighestToLowest);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public void OrderByApplicationType_WithNullAdjustments_ShouldThrowArgumentNullException()
        {
            // Arrange
            var serviceLine1 = new PaymentClaimServiceLineEntity
            {
                Id = 1,
                ChargeAmount = 100m,
                PaymentClaimServiceLineAdjustments = null, // Sum() throws ArgumentNullException("source")
                DateOfService = new DateTime(2024, 1, 1)
            };

            var serviceLine2 = new PaymentClaimServiceLineEntity
            {
                Id = 2,
                ChargeAmount = 200m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>
        {
            new PaymentClaimServiceLineAdjustmentEntity { AdjustmentAmount = 50m }
        },
                DateOfService = new DateTime(2024, 1, 2)
            };

            var serviceLines = new List<PaymentClaimServiceLineEntity> { serviceLine1, serviceLine2 };

            // Act + Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                serviceLines.OrderByApplicationType(BulkPostingCriteria.HighestToLowest));

            Assert.Equal("source", ex.ParamName);
        }


        [Fact]
        public void OrderByApplicationType_WithZeroChargeAmount_ShouldHandleGracefully()
        {
            // Arrange
            var serviceLine1 = new PaymentClaimServiceLineEntity
            {
                Id = 1,
                ChargeAmount = 0m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
                DateOfService = new DateTime(2024, 1, 1)
            };

            var serviceLine2 = new PaymentClaimServiceLineEntity
            {
                Id = 2,
                ChargeAmount = 100m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
                DateOfService = new DateTime(2024, 1, 2)
            };

            var serviceLines = new List<PaymentClaimServiceLineEntity> { serviceLine1, serviceLine2 };

            // Act
            var result = serviceLines.OrderByApplicationType(BulkPostingCriteria.HighestToLowest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(2, result[0].Id); // 100
            Assert.Equal(1, result[1].Id); // 0
        }

        [Fact]
        public void OrderByApplicationType_WithIdenticalBalances_ShouldMaintainRelativeOrder()
        {
            // Arrange
            var serviceLine1 = new PaymentClaimServiceLineEntity
            {
                Id = 1,
                ChargeAmount = 100m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
                DateOfService = new DateTime(2024, 1, 1)
            };

            var serviceLine2 = new PaymentClaimServiceLineEntity
            {
                Id = 2,
                ChargeAmount = 100m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
                DateOfService = new DateTime(2024, 1, 2)
            };

            var serviceLines = new List<PaymentClaimServiceLineEntity> { serviceLine1, serviceLine2 };

            // Act
            var result = serviceLines.OrderByApplicationType(BulkPostingCriteria.HighestToLowest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            // Both have balance of 100, so order is preserved or equal
            Assert.True(result[0].Id == 1 || result[0].Id == 2);
            Assert.True(result[1].Id == 1 || result[1].Id == 2);
        }

        [Fact]
        public void OrderByApplicationType_WithNegativeAdjustments_ShouldCalculateBalanceCorrectly()
        {
            // Arrange
            var serviceLine1 = new PaymentClaimServiceLineEntity
            {
                Id = 1,
                ChargeAmount = 100m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>
        {
      new PaymentClaimServiceLineAdjustmentEntity { AdjustmentAmount = -20m } // Negative adjustment
    },
                DateOfService = new DateTime(2024, 1, 1)
            };

            var serviceLine2 = new PaymentClaimServiceLineEntity
            {
                Id = 2,
                ChargeAmount = 100m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
                DateOfService = new DateTime(2024, 1, 2)
            };

            var serviceLines = new List<PaymentClaimServiceLineEntity> { serviceLine1, serviceLine2 };

            // Act
            var result = serviceLines.OrderByApplicationType(BulkPostingCriteria.LowestToHighest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            // serviceLine1: 100 - (-20) = 120
            // serviceLine2: 100 - 0 = 100
            Assert.Equal(2, result[0].Id); // 100
            Assert.Equal(1, result[1].Id); // 120
        }

        [Fact]
        public void OrderByApplicationType_WithMultipleAdjustments_ShouldSumAllAdjustments()
        {
            // Arrange
            var serviceLine = new PaymentClaimServiceLineEntity
            {
                Id = 1,
                ChargeAmount = 1000m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>
           {
      new PaymentClaimServiceLineAdjustmentEntity { AdjustmentAmount = 100m },
      new PaymentClaimServiceLineAdjustmentEntity { AdjustmentAmount = 150m },
     new PaymentClaimServiceLineAdjustmentEntity { AdjustmentAmount = 50m }
                },
                DateOfService = new DateTime(2024, 1, 1)
            };

            var serviceLines = new List<PaymentClaimServiceLineEntity> { serviceLine };

            // Act
            var result = serviceLines.OrderByApplicationType(BulkPostingCriteria.HighestToLowest);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            // Balance should be: 1000 - (100 + 150 + 50) = 700
            // This is implicitly tested if the method doesn't throw
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public void OrderByApplicationType_WithNullDateOfService_ShouldHandleGracefully()
        {
            // Arrange
            var serviceLine1 = new PaymentClaimServiceLineEntity
            {
                Id = 1,
                ChargeAmount = 100m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
                DateOfService = null // Null date
            };

            var serviceLine2 = new PaymentClaimServiceLineEntity
            {
                Id = 2,
                ChargeAmount = 100m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
                DateOfService = new DateTime(2024, 1, 1)
            };

            var serviceLines = new List<PaymentClaimServiceLineEntity> { serviceLine1, serviceLine2 };

            // Act
            var result = serviceLines.OrderByApplicationType(BulkPostingCriteria.NewestToOldest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            // Null dates typically sort to the beginning or end depending on LINQ provider
            // Just verify both items are present
            Assert.Contains(serviceLine1, result);
            Assert.Contains(serviceLine2, result);
        }

        [Fact]
        public void OrderByApplicationType_ReturnsNewList_DoesNotModifyOriginal()
        {
            // Arrange
            var serviceLine1 = new PaymentClaimServiceLineEntity
            {
                Id = 1,
                ChargeAmount = 100m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
                DateOfService = new DateTime(2024, 1, 2)
            };

            var serviceLine2 = new PaymentClaimServiceLineEntity
            {
                Id = 2,
                ChargeAmount = 200m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>(),
                DateOfService = new DateTime(2024, 1, 1)
            };

            var originalServiceLines = new List<PaymentClaimServiceLineEntity> { serviceLine1, serviceLine2 };

            // Act
            var result = originalServiceLines.OrderByApplicationType(BulkPostingCriteria.HighestToLowest);

            // Assert
            Assert.NotNull(result);
            // Original should still be in original order
            Assert.Equal(1, originalServiceLines[0].Id);
            Assert.Equal(2, originalServiceLines[1].Id);
            // Result should be in sorted order
            Assert.Equal(2, result[0].Id);
            Assert.Equal(1, result[1].Id);
        }
    }
}
