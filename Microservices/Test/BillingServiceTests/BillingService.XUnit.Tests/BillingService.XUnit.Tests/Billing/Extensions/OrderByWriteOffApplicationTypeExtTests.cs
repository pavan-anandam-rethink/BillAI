using BillingService.Domain.Extensions;
using BillingService.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Extensions
{
    /// <summary>
    /// Unit tests for OrderByWriteOffApplicationTypeExt extension class
    /// Tests the OrderByWriteOffApplicationType extension method with various sorting criteria
    /// </summary>
    public class OrderByWriteOffApplicationTypeExtTests
    {
        [Fact]
        public void OrderByWriteOffApplicationType_WithType1_ShouldSortByDOSDescending()
        {
            // Arrange
            var chargeEntry1 = new BillingClaimDetailsModel
            {
                Id = 1,
                DOS = new DateTime(2024, 1, 1),
                BalanceAmount = 100m,
                BillingCode = "99213",
                Units = 1
            };

            var chargeEntry2 = new BillingClaimDetailsModel
            {
                Id = 2,
                DOS = new DateTime(2024, 1, 3),
                BalanceAmount = 200m,
                BillingCode = "99214",
                Units = 1
            };

            var chargeEntry3 = new BillingClaimDetailsModel
            {
                Id = 3,
                DOS = new DateTime(2024, 1, 2),
                BalanceAmount = 150m,
                BillingCode = "99215",
                Units = 1
            };

            var chargeEntries = new List<BillingClaimDetailsModel> { chargeEntry1, chargeEntry2, chargeEntry3 }.AsQueryable();

            // Act
            var result = chargeEntries.OrderByWriteOffApplicationType(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            // Expected order by DOS descending: 2024-01-03, 2024-01-02, 2024-01-01
            Assert.Equal(2, result[0].Id);
            Assert.Equal(3, result[1].Id);
            Assert.Equal(1, result[2].Id);
        }

        [Fact]
        public void OrderByWriteOffApplicationType_WithType2_ShouldSortByDOSAscending()
        {
            // Arrange
            var chargeEntry1 = new BillingClaimDetailsModel
            {
                Id = 1,
                DOS = new DateTime(2024, 1, 3),
                BalanceAmount = 100m,
                BillingCode = "99213",
                Units = 1
            };

            var chargeEntry2 = new BillingClaimDetailsModel
            {
                Id = 2,
                DOS = new DateTime(2024, 1, 1),
                BalanceAmount = 200m,
                BillingCode = "99214",
                Units = 1
            };

            var chargeEntry3 = new BillingClaimDetailsModel
            {
                Id = 3,
                DOS = new DateTime(2024, 1, 2),
                BalanceAmount = 150m,
                BillingCode = "99215",
                Units = 1
            };

            var chargeEntries = new List<BillingClaimDetailsModel> { chargeEntry1, chargeEntry2, chargeEntry3 }.AsQueryable();

            // Act
            var result = chargeEntries.OrderByWriteOffApplicationType(2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            // Expected order by DOS ascending: 2024-01-01, 2024-01-02, 2024-01-03
            Assert.Equal(2, result[0].Id);
            Assert.Equal(3, result[1].Id);
            Assert.Equal(1, result[2].Id);
        }

        [Fact]
        public void OrderByWriteOffApplicationType_WithType3_ShouldSortByBalanceAmountDescending()
        {
            // Arrange
            var chargeEntry1 = new BillingClaimDetailsModel
            {
                Id = 1,
                DOS = new DateTime(2024, 1, 1),
                BalanceAmount = 100m,
                BillingCode = "99213",
                Units = 1
            };

            var chargeEntry2 = new BillingClaimDetailsModel
            {
                Id = 2,
                DOS = new DateTime(2024, 1, 2),
                BalanceAmount = 300m,
                BillingCode = "99214",
                Units = 1
            };

            var chargeEntry3 = new BillingClaimDetailsModel
            {
                Id = 3,
                DOS = new DateTime(2024, 1, 3),
                BalanceAmount = 200m,
                BillingCode = "99215",
                Units = 1
            };

            var chargeEntries = new List<BillingClaimDetailsModel> { chargeEntry1, chargeEntry2, chargeEntry3 }.AsQueryable();

            // Act
            var result = chargeEntries.OrderByWriteOffApplicationType(3);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            // Expected order by balance descending: 300, 200, 100
            Assert.Equal(2, result[0].Id);
            Assert.Equal(3, result[1].Id);
            Assert.Equal(1, result[2].Id);
        }

        [Fact]
        public void OrderByWriteOffApplicationType_WithType4_ShouldSortByBalanceAmountAscending()
        {
            // Arrange
            var chargeEntry1 = new BillingClaimDetailsModel
            {
                Id = 1,
                DOS = new DateTime(2024, 1, 1),
                BalanceAmount = 300m,
                BillingCode = "99213",
                Units = 1
            };

            var chargeEntry2 = new BillingClaimDetailsModel
            {
                Id = 2,
                DOS = new DateTime(2024, 1, 2),
                BalanceAmount = 100m,
                BillingCode = "99214",
                Units = 1
            };

            var chargeEntry3 = new BillingClaimDetailsModel
            {
                Id = 3,
                DOS = new DateTime(2024, 1, 3),
                BalanceAmount = 200m,
                BillingCode = "99215",
                Units = 1
            };

            var chargeEntries = new List<BillingClaimDetailsModel> { chargeEntry1, chargeEntry2, chargeEntry3 }.AsQueryable();

            // Act
            var result = chargeEntries.OrderByWriteOffApplicationType(4);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            // Expected order by balance ascending: 100, 200, 300
            Assert.Equal(2, result[0].Id);
            Assert.Equal(3, result[1].Id);
            Assert.Equal(1, result[2].Id);
        }

        [Fact]
        public void OrderByWriteOffApplicationType_WithNullType_ShouldReturnUnorderedListWithPositiveBalance()
        {
            // Arrange
            var chargeEntry1 = new BillingClaimDetailsModel
            {
                Id = 1,
                DOS = new DateTime(2024, 1, 3),
                BalanceAmount = 100m,
                BillingCode = "99213",
                Units = 1
            };

            var chargeEntry2 = new BillingClaimDetailsModel
            {
                Id = 2,
                DOS = new DateTime(2024, 1, 1),
                BalanceAmount = 200m,
                BillingCode = "99214",
                Units = 1
            };

            var chargeEntries = new List<BillingClaimDetailsModel> { chargeEntry1, chargeEntry2 }.AsQueryable();

            // Act
            var result = chargeEntries.OrderByWriteOffApplicationType(null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.True(item.BalanceAmount > 0));
        }

        [Fact]
        public void OrderByWriteOffApplicationType_WithInvalidType_ShouldReturnUnorderedListWithPositiveBalance()
        {
            // Arrange
            var chargeEntry1 = new BillingClaimDetailsModel
            {
                Id = 1,
                DOS = new DateTime(2024, 1, 1),
                BalanceAmount = 150m,
                BillingCode = "99213",
                Units = 1
            };

            var chargeEntry2 = new BillingClaimDetailsModel
            {
                Id = 2,
                DOS = new DateTime(2024, 1, 2),
                BalanceAmount = 250m,
                BillingCode = "99214",
                Units = 1
            };

            var chargeEntries = new List<BillingClaimDetailsModel> { chargeEntry1, chargeEntry2 }.AsQueryable();

            // Act
            var result = chargeEntries.OrderByWriteOffApplicationType(99);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.True(item.BalanceAmount > 0));
        }

        [Fact]
        public void OrderByWriteOffApplicationType_ShouldFilterOutZeroBalance()
        {
            // Arrange
            var chargeEntry1 = new BillingClaimDetailsModel
            {
                Id = 1,
                DOS = new DateTime(2024, 1, 1),
                BalanceAmount = 100m,
                BillingCode = "99213",
                Units = 1
            };

            var chargeEntry2 = new BillingClaimDetailsModel
            {
                Id = 2,
                DOS = new DateTime(2024, 1, 2),
                BalanceAmount = 0m,
                BillingCode = "99214",
                Units = 1
            };

            var chargeEntry3 = new BillingClaimDetailsModel
            {
                Id = 3,
                DOS = new DateTime(2024, 1, 3),
                BalanceAmount = 50m,
                BillingCode = "99215",
                Units = 1
            };

            var chargeEntries = new List<BillingClaimDetailsModel> { chargeEntry1, chargeEntry2, chargeEntry3 }.AsQueryable();

            // Act
            var result = chargeEntries.OrderByWriteOffApplicationType(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.DoesNotContain(result, item => item.BalanceAmount <= 0);
        }

        [Fact]
        public void OrderByWriteOffApplicationType_ShouldFilterOutNegativeBalance()
        {
            // Arrange
            var chargeEntry1 = new BillingClaimDetailsModel
            {
                Id = 1,
                DOS = new DateTime(2024, 1, 1),
                BalanceAmount = 100m,
                BillingCode = "99213",
                Units = 1
            };

            var chargeEntry2 = new BillingClaimDetailsModel
            {
                Id = 2,
                DOS = new DateTime(2024, 1, 2),
                BalanceAmount = -50m,
                BillingCode = "99214",
                Units = 1
            };

            var chargeEntry3 = new BillingClaimDetailsModel
            {
                Id = 3,
                DOS = new DateTime(2024, 1, 3),
                BalanceAmount = 75m,
                BillingCode = "99215",
                Units = 1
            };

            var chargeEntries = new List<BillingClaimDetailsModel> { chargeEntry1, chargeEntry2, chargeEntry3 }.AsQueryable();

            // Act
            var result = chargeEntries.OrderByWriteOffApplicationType(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.True(item.BalanceAmount > 0));
        }

        [Fact]
        public void OrderByWriteOffApplicationType_WithEmptyCollection_ShouldReturnEmptyList()
        {
            // Arrange
            var chargeEntries = new List<BillingClaimDetailsModel>().AsQueryable();

            // Act
            var result = chargeEntries.OrderByWriteOffApplicationType(1);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void OrderByWriteOffApplicationType_WithAllZeroBalance_ShouldReturnEmptyList()
        {
            // Arrange
            var chargeEntry1 = new BillingClaimDetailsModel
            {
                Id = 1,
                DOS = new DateTime(2024, 1, 1),
                BalanceAmount = 0m,
                BillingCode = "99213",
                Units = 1
            };

            var chargeEntry2 = new BillingClaimDetailsModel
            {
                Id = 2,
                DOS = new DateTime(2024, 1, 2),
                BalanceAmount = 0m,
                BillingCode = "99214",
                Units = 1
            };

            var chargeEntries = new List<BillingClaimDetailsModel> { chargeEntry1, chargeEntry2 }.AsQueryable();

            // Act
            var result = chargeEntries.OrderByWriteOffApplicationType(1);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void OrderByWriteOffApplicationType_WithSingleItem_ShouldReturnSingleItem()
        {
            // Arrange
            var chargeEntry = new BillingClaimDetailsModel
            {
                Id = 1,
                DOS = new DateTime(2024, 1, 1),
                BalanceAmount = 100m,
                BillingCode = "99213",
                Units = 1
            };

            var chargeEntries = new List<BillingClaimDetailsModel> { chargeEntry }.AsQueryable();

            // Act
            var result = chargeEntries.OrderByWriteOffApplicationType(1);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public void OrderByWriteOffApplicationType_Type1_PreservesOrderForSameDOS()
        {
            // Arrange
            var sameDate = new DateTime(2024, 1, 15);
            var chargeEntry1 = new BillingClaimDetailsModel
            {
                Id = 1,
                DOS = sameDate,
                BalanceAmount = 100m,
                BillingCode = "99213",
                Units = 1
            };

            var chargeEntry2 = new BillingClaimDetailsModel
            {
                Id = 2,
                DOS = sameDate,
                BalanceAmount = 200m,
                BillingCode = "99214",
                Units = 1
            };

            var chargeEntry3 = new BillingClaimDetailsModel
            {
                Id = 3,
                DOS = sameDate,
                BalanceAmount = 150m,
                BillingCode = "99215",
                Units = 1
            };

            var chargeEntries = new List<BillingClaimDetailsModel> { chargeEntry1, chargeEntry2, chargeEntry3 }.AsQueryable();

            // Act
            var result = chargeEntries.OrderByWriteOffApplicationType(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.All(result, item => Assert.Equal(sameDate, item.DOS));
        }

        [Fact]
        public void OrderByWriteOffApplicationType_Type3_PreservesOrderForSameBalance()
        {
            // Arrange
            var chargeEntry1 = new BillingClaimDetailsModel
            {
                Id = 1,
                DOS = new DateTime(2024, 1, 1),
                BalanceAmount = 100m,
                BillingCode = "99213",
                Units = 1
            };

            var chargeEntry2 = new BillingClaimDetailsModel
            {
                Id = 2,
                DOS = new DateTime(2024, 1, 2),
                BalanceAmount = 100m,
                BillingCode = "99214",
                Units = 1
            };

            var chargeEntry3 = new BillingClaimDetailsModel
            {
                Id = 3,
                DOS = new DateTime(2024, 1, 3),
                BalanceAmount = 100m,
                BillingCode = "99215",
                Units = 1
            };

            var chargeEntries = new List<BillingClaimDetailsModel> { chargeEntry1, chargeEntry2, chargeEntry3 }.AsQueryable();

            // Act
            var result = chargeEntries.OrderByWriteOffApplicationType(3);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.All(result, item => Assert.Equal(100m, item.BalanceAmount));
        }

        [Fact]
        public void OrderByWriteOffApplicationType_Type1_WithDifferentDates_ShouldMaintainDescendingOrder()
        {
            // Arrange
            var chargeEntry1 = new BillingClaimDetailsModel
            {
                Id = 1,
                DOS = new DateTime(2023, 12, 25),
                BalanceAmount = 100m,
                BillingCode = "99213",
                Units = 1
            };

            var chargeEntry2 = new BillingClaimDetailsModel
            {
                Id = 2,
                DOS = new DateTime(2024, 3, 15),
                BalanceAmount = 200m,
                BillingCode = "99214",
                Units = 1
            };

            var chargeEntry3 = new BillingClaimDetailsModel
            {
                Id = 3,
                DOS = new DateTime(2024, 1, 10),
                BalanceAmount = 150m,
                BillingCode = "99215",
                Units = 1
            };

            var chargeEntries = new List<BillingClaimDetailsModel> { chargeEntry1, chargeEntry2, chargeEntry3 }.AsQueryable();

            // Act
            var result = chargeEntries.OrderByWriteOffApplicationType(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.True(result[0].DOS >= result[1].DOS);
            Assert.True(result[1].DOS >= result[2].DOS);
        }

        [Fact]
        public void OrderByWriteOffApplicationType_Type4_WithDifferentBalances_ShouldMaintainAscendingOrder()
        {
            // Arrange
            var chargeEntry1 = new BillingClaimDetailsModel
            {
                Id = 1,
                DOS = new DateTime(2024, 1, 1),
                BalanceAmount = 999.99m,
                BillingCode = "99213",
                Units = 1
            };

            var chargeEntry2 = new BillingClaimDetailsModel
            {
                Id = 2,
                DOS = new DateTime(2024, 1, 2),
                BalanceAmount = 0.01m,
                BillingCode = "99214",
                Units = 1
            };

            var chargeEntry3 = new BillingClaimDetailsModel
            {
                Id = 3,
                DOS = new DateTime(2024, 1, 3),
                BalanceAmount = 500m,
                BillingCode = "99215",
                Units = 1
            };

            var chargeEntries = new List<BillingClaimDetailsModel> { chargeEntry1, chargeEntry2, chargeEntry3 }.AsQueryable();

            // Act
            var result = chargeEntries.OrderByWriteOffApplicationType(4);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.True(result[0].BalanceAmount <= result[1].BalanceAmount);
            Assert.True(result[1].BalanceAmount <= result[2].BalanceAmount);
        }

        [Fact]
        public void OrderByWriteOffApplicationType_ReturnsNewList_DoesNotModifyOriginal()
        {
            // Arrange
            var chargeEntry1 = new BillingClaimDetailsModel
            {
                Id = 1,
                DOS = new DateTime(2024, 1, 2),
                BalanceAmount = 100m,
                BillingCode = "99213",
                Units = 1
            };

            var chargeEntry2 = new BillingClaimDetailsModel
            {
                Id = 2,
                DOS = new DateTime(2024, 1, 1),
                BalanceAmount = 200m,
                BillingCode = "99214",
                Units = 1
            };

            var originalChargeEntries = new List<BillingClaimDetailsModel> { chargeEntry1, chargeEntry2 };
            var chargeEntries = originalChargeEntries.AsQueryable();

            // Act
            var result = chargeEntries.OrderByWriteOffApplicationType(3);

            // Assert
            Assert.NotNull(result);
            // Original should still be in original order
            Assert.Equal(1, originalChargeEntries[0].Id);
            Assert.Equal(2, originalChargeEntries[1].Id);
            // Result should be in sorted order
            Assert.Equal(2, result[0].Id);
            Assert.Equal(1, result[1].Id);
        }

        [Fact]
        public void OrderByWriteOffApplicationType_WithLargeDataSet_ShouldSortCorrectly()
        {
            // Arrange
            var chargeEntries = new List<BillingClaimDetailsModel>();
            for (int i = 1; i <= 100; i++)
            {
                chargeEntries.Add(new BillingClaimDetailsModel
                {
                    Id = i,
                    DOS = new DateTime(2024, 1, (i % 28) + 1),
                    BalanceAmount = (101 - i) * 10m,
                    BillingCode = "99213",
                    Units = 1
                });
            }

            var chargeEntriesQueryable = chargeEntries.AsQueryable();

            // Act
            var result = chargeEntriesQueryable.OrderByWriteOffApplicationType(3);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.Count);
            // Verify descending order by balance
            for (int i = 0; i < result.Count - 1; i++)
            {
                Assert.True(result[i].BalanceAmount >= result[i + 1].BalanceAmount);
            }
        }

        [Fact]
        public void OrderByWriteOffApplicationType_WithDecimalBalances_ShouldCalculateCorrectly()
        {
            // Arrange
            var chargeEntry1 = new BillingClaimDetailsModel
            {
                Id = 1,
                DOS = new DateTime(2024, 1, 1),
                BalanceAmount = 99.99m,
                BillingCode = "99213",
                Units = 1
            };

            var chargeEntry2 = new BillingClaimDetailsModel
            {
                Id = 2,
                DOS = new DateTime(2024, 1, 2),
                BalanceAmount = 100.01m,
                BillingCode = "99214",
                Units = 1
            };

            var chargeEntry3 = new BillingClaimDetailsModel
            {
                Id = 3,
                DOS = new DateTime(2024, 1, 3),
                BalanceAmount = 100m,
                BillingCode = "99215",
                Units = 1
            };

            var chargeEntries = new List<BillingClaimDetailsModel> { chargeEntry1, chargeEntry2, chargeEntry3 }.AsQueryable();

            // Act
            var result = chargeEntries.OrderByWriteOffApplicationType(4);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            // Expected order: 99.99, 100, 100.01
            Assert.Equal(1, result[0].Id);
            Assert.Equal(3, result[1].Id);
            Assert.Equal(2, result[2].Id);
        }

        [Fact]
        public void OrderByWriteOffApplicationType_WithAllTypes_ShouldProduceValidResults()
        {
            // Arrange
            var chargeEntries = new List<BillingClaimDetailsModel>
     {
         new BillingClaimDetailsModel { Id = 1, DOS = new DateTime(2024, 1, 5), BalanceAmount = 150m, BillingCode = "99213", Units = 1 },
     new BillingClaimDetailsModel { Id = 2, DOS = new DateTime(2024, 1, 2), BalanceAmount = 300m, BillingCode = "99214", Units = 1 },
    new BillingClaimDetailsModel { Id = 3, DOS = new DateTime(2024, 1, 8), BalanceAmount = 75m, BillingCode = "99215", Units = 1 },
     new BillingClaimDetailsModel { Id = 4, DOS = new DateTime(2024, 1, 1), BalanceAmount = 200m, BillingCode = "99216", Units = 1 }
      }.AsQueryable();

            // Act
            var result1 = chargeEntries.OrderByWriteOffApplicationType(1);
            var result2 = chargeEntries.OrderByWriteOffApplicationType(2);
            var result3 = chargeEntries.OrderByWriteOffApplicationType(3);
            var result4 = chargeEntries.OrderByWriteOffApplicationType(4);

            // Assert
            Assert.Equal(4, result1.Count);
            Assert.Equal(4, result2.Count);
            Assert.Equal(4, result3.Count);
            Assert.Equal(4, result4.Count);

            // Type 1: DOS descending (newest to oldest: 2024-01-08, 2024-01-05, 2024-01-02, 2024-01-01)
            Assert.Equal(3, result1[0].Id); // 2024-01-08
            Assert.Equal(1, result1[1].Id); // 2024-01-05
            Assert.Equal(2, result1[2].Id); // 2024-01-02
            Assert.Equal(4, result1[3].Id); // 2024-01-01

            // Type 2: DOS ascending (oldest to newest: 2024-01-01, 2024-01-02, 2024-01-05, 2024-01-08)
            Assert.Equal(4, result2[0].Id); // 2024-01-01
            Assert.Equal(2, result2[1].Id); // 2024-01-02
            Assert.Equal(1, result2[2].Id); // 2024-01-05
            Assert.Equal(3, result2[3].Id); // 2024-01-08

            // Type 3: Balance descending (300, 200, 150, 75)
            Assert.Equal(2, result3[0].Id); // 300
            Assert.Equal(4, result3[1].Id); // 200
            Assert.Equal(1, result3[2].Id); // 150
            Assert.Equal(3, result3[3].Id); // 75

            // Type 4: Balance ascending (75, 150, 200, 300)
            Assert.Equal(3, result4[0].Id); // 75
            Assert.Equal(1, result4[1].Id); // 150
            Assert.Equal(4, result4[2].Id); // 200
            Assert.Equal(2, result4[3].Id); // 300
        }
    }
}
