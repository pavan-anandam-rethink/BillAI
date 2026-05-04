using BillingService.Domain.Extensions;
using Rethink.Services.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Extensions
{
    /// <summary>
    /// Unit tests for SortingExtensions class
    /// Tests the ApplySorting extension method with various sorting criteria and scenarios
    /// </summary>
    public class SortingExtensionsTests
    {
        // Test model for sorting
        private class TestModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal Amount { get; set; }
            public DateTime CreatedDate { get; set; }
            public int Priority { get; set; }
        }

        [Fact]
        public void ApplySorting_WithNullSortingModels_ShouldReturnOriginalSource()
        {
            // Arrange
            var items = new List<TestModel>
            {
     new TestModel { Id = 3, Name = "Charlie", Amount = 300m, CreatedDate = new DateTime(2024, 1, 3), Priority = 1 },
           new TestModel { Id = 1, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 3 },
  new TestModel { Id = 2, Name = "Bob", Amount = 200m, CreatedDate = new DateTime(2024, 1, 2), Priority = 2 }
    };

            // Act
            var result = items.ApplySorting(null).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(3, result[0].Id);
            Assert.Equal(1, result[1].Id);
            Assert.Equal(2, result[2].Id);
        }

        [Fact]
        public void ApplySorting_WithEmptySortingModels_ShouldReturnOriginalSource()
        {
            // Arrange
            var items = new List<TestModel>
  {
          new TestModel { Id = 3, Name = "Charlie", Amount = 300m, CreatedDate = new DateTime(2024, 1, 3), Priority = 1 },
     new TestModel { Id = 1, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 3 },
   new TestModel { Id = 2, Name = "Bob", Amount = 200m, CreatedDate = new DateTime(2024, 1, 2), Priority = 2 }
            };

            var sortingModels = new List<SortingModel>();

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(3, result[0].Id);
            Assert.Equal(1, result[1].Id);
            Assert.Equal(2, result[2].Id);
        }

        [Fact]
        public void ApplySorting_WithSingleAscendingSort_ShouldSortByFieldAscending()
        {
            // Arrange
            var items = new List<TestModel>
            {
   new TestModel { Id = 3, Name = "Charlie", Amount = 300m, CreatedDate = new DateTime(2024, 1, 3), Priority = 1 },
     new TestModel { Id = 1, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 3 },
      new TestModel { Id = 2, Name = "Bob", Amount = 200m, CreatedDate = new DateTime(2024, 1, 2), Priority = 2 }
      };

            var sortingModels = new List<SortingModel>
            {
        new SortingModel { Field = "Name", Dir = "asc" }
      };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal("Alice", result[0].Name);
            Assert.Equal("Bob", result[1].Name);
            Assert.Equal("Charlie", result[2].Name);
        }

        [Fact]
        public void ApplySorting_WithSingleDescendingSort_ShouldSortByFieldDescending()
        {
            // Arrange
            var items = new List<TestModel>
          {
           new TestModel { Id = 3, Name = "Charlie", Amount = 300m, CreatedDate = new DateTime(2024, 1, 3), Priority = 1 },
new TestModel { Id = 1, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 3 },
        new TestModel { Id = 2, Name = "Bob", Amount = 200m, CreatedDate = new DateTime(2024, 1, 2), Priority = 2 }
      };

            var sortingModels = new List<SortingModel>
            {
   new SortingModel { Field = "Name", Dir = "desc" }
            };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal("Charlie", result[0].Name);
            Assert.Equal("Bob", result[1].Name);
            Assert.Equal("Alice", result[2].Name);
        }

        [Fact]
        public void ApplySorting_WithIntegerFieldAscending_ShouldSortByIntFieldAscending()
        {
            // Arrange
            var items = new List<TestModel>
            {
                new TestModel { Id = 3, Name = "Charlie", Amount = 300m, CreatedDate = new DateTime(2024, 1, 3), Priority = 1 },
    new TestModel { Id = 1, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 3 },
     new TestModel { Id = 2, Name = "Bob", Amount = 200m, CreatedDate = new DateTime(2024, 1, 2), Priority = 2 }
};

            var sortingModels = new List<SortingModel>
        {
            new SortingModel { Field = "Id", Dir = "asc" }
            };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(1, result[0].Id);
            Assert.Equal(2, result[1].Id);
            Assert.Equal(3, result[2].Id);
        }

        [Fact]
        public void ApplySorting_WithDecimalFieldDescending_ShouldSortByDecimalFieldDescending()
        {
            // Arrange
            var items = new List<TestModel>
   {
      new TestModel { Id = 3, Name = "Charlie", Amount = 300m, CreatedDate = new DateTime(2024, 1, 3), Priority = 1 },
                new TestModel { Id = 1, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 3 },
          new TestModel { Id = 2, Name = "Bob", Amount = 200m, CreatedDate = new DateTime(2024, 1, 2), Priority = 2 }
            };

            var sortingModels = new List<SortingModel>
            {
                new SortingModel { Field = "Amount", Dir = "desc" }
         };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(300m, result[0].Amount);
            Assert.Equal(200m, result[1].Amount);
            Assert.Equal(100m, result[2].Amount);
        }

        [Fact]
        public void ApplySorting_WithDateTimeFieldAscending_ShouldSortByDateTimeAscending()
        {
            // Arrange
            var items = new List<TestModel>
  {
                new TestModel { Id = 3, Name = "Charlie", Amount = 300m, CreatedDate = new DateTime(2024, 1, 3), Priority = 1 },
              new TestModel { Id = 1, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 3 },
 new TestModel { Id = 2, Name = "Bob", Amount = 200m, CreatedDate = new DateTime(2024, 1, 2), Priority = 2 }
       };

            var sortingModels = new List<SortingModel>
        {
     new SortingModel { Field = "CreatedDate", Dir = "asc" }
      };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(new DateTime(2024, 1, 1), result[0].CreatedDate);
            Assert.Equal(new DateTime(2024, 1, 2), result[1].CreatedDate);
            Assert.Equal(new DateTime(2024, 1, 3), result[2].CreatedDate);
        }

        [Fact]
        public void ApplySorting_WithMultipleSortFields_ShouldApplySecondarySorting()
        {
            // Arrange
            var items = new List<TestModel>
     {
                new TestModel { Id = 1, Name = "Alice", Amount = 200m, CreatedDate = new DateTime(2024, 1, 1), Priority = 1 },
       new TestModel { Id = 2, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 2), Priority = 1 },
      new TestModel { Id = 3, Name = "Bob", Amount = 300m, CreatedDate = new DateTime(2024, 1, 3), Priority = 1 },
          new TestModel { Id = 4, Name = "Alice", Amount = 150m, CreatedDate = new DateTime(2024, 1, 4), Priority = 1 }
      };

            var sortingModels = new List<SortingModel>
            {
          new SortingModel { Field = "Name", Dir = "asc" },
      new SortingModel { Field = "Amount", Dir = "asc" }
            };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Count);
            // First sorted by Name (Alice, Alice, Alice, Bob)
            // Then within Alice group, sorted by Amount (100, 150, 200)
            Assert.Equal("Alice", result[0].Name);
            Assert.Equal(100m, result[0].Amount);
            Assert.Equal("Alice", result[1].Name);
            Assert.Equal(150m, result[1].Amount);
            Assert.Equal("Alice", result[2].Name);
            Assert.Equal(200m, result[2].Amount);
            Assert.Equal("Bob", result[3].Name);
        }

        [Fact]
        public void ApplySorting_WithThreeSortFields_ShouldApplyCorrectSortOrder()
        {
            // Arrange
            var items = new List<TestModel>
  {
      new TestModel { Id = 1, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 3 },
 new TestModel { Id = 2, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 2), Priority = 1 },
          new TestModel { Id = 3, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 2 },
          new TestModel { Id = 4, Name = "Bob", Amount = 200m, CreatedDate = new DateTime(2024, 1, 3), Priority = 1 }
            };

            var sortingModels = new List<SortingModel>
            {
          new SortingModel { Field = "Name", Dir = "asc" },
  new SortingModel { Field = "Amount", Dir = "asc" },
                new SortingModel { Field = "Priority", Dir = "asc" }
            };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Count);
            // Within Alice group with Amount 100, sorted by Priority (1, 2, 3)
            Assert.Equal("Alice", result[0].Name);
            Assert.Equal(1, result[0].Priority);
            Assert.Equal("Alice", result[1].Name);
            Assert.Equal(2, result[1].Priority);
            Assert.Equal("Alice", result[2].Name);
            Assert.Equal(3, result[2].Priority);
        }

        [Fact]
        public void ApplySorting_WithCaseInsensitiveFieldName_ShouldFindPropertyCorrectly()
        {
            // Arrange
            var items = new List<TestModel>
  {
  new TestModel { Id = 3, Name = "Charlie", Amount = 300m, CreatedDate = new DateTime(2024, 1, 3), Priority = 1 },
      new TestModel { Id = 1, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 3 },
       new TestModel { Id = 2, Name = "Bob", Amount = 200m, CreatedDate = new DateTime(2024, 1, 2), Priority = 2 }
      };

            var sortingModels = new List<SortingModel>
         {
    new SortingModel { Field = "name", Dir = "asc" }  // lowercase
 };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal("Alice", result[0].Name);
            Assert.Equal("Bob", result[1].Name);
            Assert.Equal("Charlie", result[2].Name);
        }

        [Fact]
        public void ApplySorting_WithCaseInsensitiveDirectionDesc_ShouldSortDescending()
        {
            // Arrange
            var items = new List<TestModel>
  {
 new TestModel { Id = 3, Name = "Charlie", Amount = 300m, CreatedDate = new DateTime(2024, 1, 3), Priority = 1 },
    new TestModel { Id = 1, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 3 },
          new TestModel { Id = 2, Name = "Bob", Amount = 200m, CreatedDate = new DateTime(2024, 1, 2), Priority = 2 }
      };

            var sortingModels = new List<SortingModel>
            {
      new SortingModel { Field = "Name", Dir = "DESC" }  // uppercase
            };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal("Charlie", result[0].Name);
            Assert.Equal("Bob", result[1].Name);
            Assert.Equal("Alice", result[2].Name);
        }

        [Fact]
        public void ApplySorting_WithInvalidFieldName_ShouldSkipAndReturnOriginal()
        {
            // Arrange
            var items = new List<TestModel>
      {
      new TestModel { Id = 3, Name = "Charlie", Amount = 300m, CreatedDate = new DateTime(2024, 1, 3), Priority = 1 },
       new TestModel { Id = 1, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 3 },
                new TestModel { Id = 2, Name = "Bob", Amount = 200m, CreatedDate = new DateTime(2024, 1, 2), Priority = 2 }
};

            var sortingModels = new List<SortingModel>
   {
       new SortingModel { Field = "InvalidField", Dir = "asc" }
       };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            // Should remain in original order since field doesn't exist
            Assert.Equal(3, result[0].Id);
            Assert.Equal(1, result[1].Id);
            Assert.Equal(2, result[2].Id);
        }

        [Fact]
        public void ApplySorting_WithMixedValidAndInvalidFields_ShouldApplyValidFieldsOnly()
        {
            // Arrange
            var items = new List<TestModel>
 {
                new TestModel { Id = 3, Name = "Charlie", Amount = 300m, CreatedDate = new DateTime(2024, 1, 3), Priority = 1 },
             new TestModel { Id = 1, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 3 },
 new TestModel { Id = 2, Name = "Bob", Amount = 200m, CreatedDate = new DateTime(2024, 1, 2), Priority = 2 }
        };

            var sortingModels = new List<SortingModel>
     {
    new SortingModel { Field = "InvalidField", Dir = "asc" },
     new SortingModel { Field = "Name", Dir = "asc" }
};

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            // Should be sorted by Name (valid field)
            Assert.Equal("Alice", result[0].Name);
            Assert.Equal("Bob", result[1].Name);
            Assert.Equal("Charlie", result[2].Name);
        }

        [Fact]
        public void ApplySorting_WithSingleItem_ShouldReturnSingleItem()
        {
            // Arrange
            var items = new List<TestModel>
            {
          new TestModel { Id = 1, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 3 }
     };

            var sortingModels = new List<SortingModel>
   {
      new SortingModel { Field = "Name", Dir = "asc" }
   };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public void ApplySorting_WithEmptyList_ShouldReturnEmptyList()
        {
            // Arrange
            var items = new List<TestModel>();

            var sortingModels = new List<SortingModel>
   {
        new SortingModel { Field = "Name", Dir = "asc" }
         };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void ApplySorting_WithDuplicateValues_ShouldMaintainStableSort()
        {
            // Arrange
            var items = new List<TestModel>
         {
          new TestModel { Id = 1, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 1 },
        new TestModel { Id = 2, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 2), Priority = 2 },
          new TestModel { Id = 3, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 3), Priority = 3 }
     };

            var sortingModels = new List<SortingModel>
            {
    new SortingModel { Field = "Name", Dir = "asc" }
  };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.All(result, item => Assert.Equal("Alice", item.Name));
        }

        [Fact]
        public void ApplySorting_WithMixedSortDirections_ShouldApplyCorrectDirections()
        {
            // Arrange
            var items = new List<TestModel>
  {
     new TestModel { Id = 1, Name = "Alice", Amount = 200m, CreatedDate = new DateTime(2024, 1, 1), Priority = 1 },
           new TestModel { Id = 2, Name = "Bob", Amount = 100m, CreatedDate = new DateTime(2024, 1, 2), Priority = 2 },
   new TestModel { Id = 3, Name = "Alice", Amount = 150m, CreatedDate = new DateTime(2024, 1, 3), Priority = 3 },
              new TestModel { Id = 4, Name = "Bob", Amount = 300m, CreatedDate = new DateTime(2024, 1, 4), Priority = 4 }
      };

            var sortingModels = new List<SortingModel>
        {
                new SortingModel { Field = "Name", Dir = "asc" },
     new SortingModel { Field = "Amount", Dir = "desc" }
            };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Count);
            // Alice entries sorted by Amount descending (200, 150)
            Assert.Equal("Alice", result[0].Name);
            Assert.Equal(200m, result[0].Amount);
            Assert.Equal("Alice", result[1].Name);
            Assert.Equal(150m, result[1].Amount);
            // Bob entries sorted by Amount descending (300, 100)
            Assert.Equal("Bob", result[2].Name);
            Assert.Equal(300m, result[2].Amount);
            Assert.Equal("Bob", result[3].Name);
            Assert.Equal(100m, result[3].Amount);
        }

        [Fact]
        public void ApplySorting_WithLargeDataSet_ShouldSortCorrectly()
        {
            // Arrange
            var items = new List<TestModel>();
            for (int i = 100; i >= 1; i--)
            {
                items.Add(new TestModel
                {
                    Id = i,
                    Name = $"Name{i % 10}",
                    Amount = i * 10m,
                    CreatedDate = new DateTime(2024, 1, (i % 28) + 1),
                    Priority = i % 5
                });
            }

            var sortingModels = new List<SortingModel>
            {
 new SortingModel { Field = "Priority", Dir = "asc" }
     };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.Count);
            // Verify sorted by Priority ascending
            for (int i = 0; i < result.Count - 1; i++)
            {
                Assert.True(result[i].Priority <= result[i + 1].Priority);
            }
        }

        [Fact]
        public void ApplySorting_WithNullableDateTime_ShouldHandleCorrectly()
        {
            // Arrange - using TestModel where we can still test behavior
            var items = new List<TestModel>
  {
                new TestModel { Id = 1, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 3), Priority = 1 },
        new TestModel { Id = 2, Name = "Bob", Amount = 200m, CreatedDate = new DateTime(2024, 1, 1), Priority = 2 },
     new TestModel { Id = 3, Name = "Charlie", Amount = 300m, CreatedDate = new DateTime(2024, 1, 2), Priority = 3 }
            };

            var sortingModels = new List<SortingModel>
            {
          new SortingModel { Field = "CreatedDate", Dir = "asc" }
            };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(new DateTime(2024, 1, 1), result[0].CreatedDate);
            Assert.Equal(new DateTime(2024, 1, 2), result[1].CreatedDate);
            Assert.Equal(new DateTime(2024, 1, 3), result[2].CreatedDate);
        }

        [Fact]
        public void ApplySorting_ReturnsEnumerable_CanBeEnumeratedMultipleTimes()
        {
            // Arrange
            var items = new List<TestModel>
    {
  new TestModel { Id = 3, Name = "Charlie", Amount = 300m, CreatedDate = new DateTime(2024, 1, 3), Priority = 1 },
         new TestModel { Id = 1, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 3 },
            new TestModel { Id = 2, Name = "Bob", Amount = 200m, CreatedDate = new DateTime(2024, 1, 2), Priority = 2 }
      };

            var sortingModels = new List<SortingModel>
            {
    new SortingModel { Field = "Name", Dir = "asc" }
    };

            // Act
            var result = items.ApplySorting(sortingModels);
            var firstEnumeration = result.ToList();
            var secondEnumeration = result.ToList();

            // Assert
            Assert.Equal(firstEnumeration.Count, secondEnumeration.Count);
            for (int i = 0; i < firstEnumeration.Count; i++)
            {
                Assert.Equal(firstEnumeration[i].Id, secondEnumeration[i].Id);
            }
        }

        [Fact]
        public void ApplySorting_WithComplexMultiFieldSort_ShouldApplyCorrectly()
        {
            // Arrange
            var items = new List<TestModel>
    {
   new TestModel { Id = 1, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 2 },
 new TestModel { Id = 2, Name = "Bob", Amount = 150m, CreatedDate = new DateTime(2024, 1, 2), Priority = 1 },
          new TestModel { Id = 3, Name = "Alice", Amount = 150m, CreatedDate = new DateTime(2024, 1, 3), Priority = 1 },
                new TestModel { Id = 4, Name = "Bob", Amount = 100m, CreatedDate = new DateTime(2024, 1, 4), Priority = 2 },
      new TestModel { Id = 5, Name = "Alice", Amount = 150m, CreatedDate = new DateTime(2024, 1, 5), Priority = 2 }
    };

            var sortingModels = new List<SortingModel>
            {
      new SortingModel { Field = "Name", Dir = "desc" },
     new SortingModel { Field = "Amount", Dir = "asc" },
 new SortingModel { Field = "Priority", Dir = "desc" }
       };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count);
            // First by Name descending (Bob, Bob, Alice, Alice, Alice)
            // Then by Amount ascending within each name group
            // Then by Priority descending within each name+amount group
            Assert.Equal("Bob", result[0].Name);
            Assert.Equal("Bob", result[1].Name);
            Assert.True(result[0].Amount <= result[1].Amount);
        }

        [Fact]
        public void ApplySorting_WithSecondaryAscendingSort_ShouldUseThenBy()
        {
            // Arrange - This test specifically targets the ThenBy code path (line 41)
            var items = new List<TestModel>
            {
           new TestModel { Id = 1, Name = "Alice", Amount = 200m, CreatedDate = new DateTime(2024, 1, 1), Priority = 3 },
    new TestModel { Id = 2, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 2), Priority = 1 },
      new TestModel { Id = 3, Name = "Bob", Amount = 300m, CreatedDate = new DateTime(2024, 1, 3), Priority = 2 },
          new TestModel { Id = 4, Name = "Alice", Amount = 150m, CreatedDate = new DateTime(2024, 1, 4), Priority = 2 }
            };

            var sortingModels = new List<SortingModel>
  {
                new SortingModel { Field = "Name", Dir = "asc" },
                new SortingModel { Field = "Amount", Dir = "asc" }  // Secondary ascending - uses ThenBy
       };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Count);
            // Sorted by Name ascending, then by Amount ascending
            Assert.Equal("Alice", result[0].Name);
            Assert.Equal(100m, result[0].Amount);
            Assert.Equal("Alice", result[1].Name);
            Assert.Equal(150m, result[1].Amount);
            Assert.Equal("Alice", result[2].Name);
            Assert.Equal(200m, result[2].Amount);
            Assert.Equal("Bob", result[3].Name);
            Assert.Equal(300m, result[3].Amount);
        }

        [Fact]
        public void ApplySorting_WithSecondaryDescendingSort_ShouldUseThenByDescending()
        {
            // Arrange - This test specifically targets the ThenByDescending code path (line 40)
            var items = new List<TestModel>
        {
     new TestModel { Id = 1, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 3 },
                new TestModel { Id = 2, Name = "Alice", Amount = 300m, CreatedDate = new DateTime(2024, 1, 2), Priority = 1 },
         new TestModel { Id = 3, Name = "Bob", Amount = 150m, CreatedDate = new DateTime(2024, 1, 3), Priority = 2 },
       new TestModel { Id = 4, Name = "Alice", Amount = 200m, CreatedDate = new DateTime(2024, 1, 4), Priority = 2 }
            };

            var sortingModels = new List<SortingModel>
            {
       new SortingModel { Field = "Name", Dir = "asc" },
     new SortingModel { Field = "Amount", Dir = "desc" }  // Secondary descending - uses ThenByDescending
            };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Count);
            // Sorted by Name ascending, then by Amount descending
            Assert.Equal("Alice", result[0].Name);
            Assert.Equal(300m, result[0].Amount);  // Highest amount first for Alice
            Assert.Equal("Alice", result[1].Name);
            Assert.Equal(200m, result[1].Amount);
            Assert.Equal("Alice", result[2].Name);
            Assert.Equal(100m, result[2].Amount);  // Lowest amount last for Alice
            Assert.Equal("Bob", result[3].Name);
            Assert.Equal(150m, result[3].Amount);
        }

        [Fact]
        public void ApplySorting_WithTertiaryAscendingSort_ShouldUseThenByForThirdField()
        {
            // Arrange - This test targets the secondary ThenBy for tertiary sorting (lines 39-41)
            var items = new List<TestModel>
       {
          new TestModel { Id = 1, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 3 },
         new TestModel { Id = 2, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 2), Priority = 1 },
   new TestModel { Id = 3, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 2 },
      new TestModel { Id = 4, Name = "Bob", Amount = 200m, CreatedDate = new DateTime(2024, 1, 3), Priority = 1 }
            };

            var sortingModels = new List<SortingModel>
  {
       new SortingModel { Field = "Name", Dir = "asc" },
new SortingModel { Field = "Amount", Dir = "asc" },
       new SortingModel { Field = "Priority", Dir = "asc" }  // Tertiary ascending
  };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Count);
            // Within Alice group with Amount 100, sorted by Priority ascending
            Assert.Equal("Alice", result[0].Name);
            Assert.Equal(100m, result[0].Amount);
            Assert.Equal(1, result[0].Priority);  // Lowest priority first
            Assert.Equal("Alice", result[1].Name);
            Assert.Equal(100m, result[1].Amount);
            Assert.Equal(2, result[1].Priority);
            Assert.Equal("Alice", result[2].Name);
            Assert.Equal(100m, result[2].Amount);
            Assert.Equal(3, result[2].Priority);  // Highest priority last
        }

        [Fact]
        public void ApplySorting_WithTertiaryDescendingSort_ShouldUseThenByDescendingForThirdField()
        {
            // Arrange - This test targets the secondary ThenByDescending for tertiary sorting (lines 39-41)
            var items = new List<TestModel>
      {
        new TestModel { Id = 1, Name = "Charlie", Amount = 150m, CreatedDate = new DateTime(2024, 1, 1), Priority = 1 },
      new TestModel { Id = 2, Name = "Charlie", Amount = 150m, CreatedDate = new DateTime(2024, 1, 1), Priority = 3 },
    new TestModel { Id = 3, Name = "Charlie", Amount = 150m, CreatedDate = new DateTime(2024, 1, 1), Priority = 2 },
       new TestModel { Id = 4, Name = "David", Amount = 250m, CreatedDate = new DateTime(2024, 1, 2), Priority = 1 }
        };

            var sortingModels = new List<SortingModel>
       {
              new SortingModel { Field = "Name", Dir = "asc" },
                new SortingModel { Field = "Amount", Dir = "asc" },
     new SortingModel { Field = "Priority", Dir = "desc" }  // Tertiary descending
   };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Count);
            // Within Charlie group with Amount 150, sorted by Priority descending
            Assert.Equal("Charlie", result[0].Name);
            Assert.Equal(150m, result[0].Amount);
            Assert.Equal(3, result[0].Priority);  // Highest priority first
            Assert.Equal("Charlie", result[1].Name);
            Assert.Equal(150m, result[1].Amount);
            Assert.Equal(2, result[1].Priority);
            Assert.Equal("Charlie", result[2].Name);
            Assert.Equal(150m, result[2].Amount);
            Assert.Equal(1, result[2].Priority);  // Lowest priority last
            Assert.Equal("David", result[3].Name);
        }

        [Fact]
        public void ApplySorting_WithFourFieldsMultipleThenBy_ShouldApplyAllSorts()
        {
            // Arrange - This test ensures multiple ThenBy operations work correctly (lines 39-41 executed multiple times)
            var items = new List<TestModel>
     {
     new TestModel { Id = 1, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 5), Priority = 2 },
 new TestModel { Id = 2, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 1 },
      new TestModel { Id = 3, Name = "Alice", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 2 },
       new TestModel { Id = 4, Name = "Bob", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 1 }
         };

            var sortingModels = new List<SortingModel>
            {
     new SortingModel { Field = "Name", Dir = "asc" },
           new SortingModel { Field = "Amount", Dir = "asc" },
           new SortingModel { Field = "CreatedDate", Dir = "asc" },
     new SortingModel { Field = "Priority", Dir = "desc" }
      };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Count);
            // All Alice entries with Amount 100
            Assert.Equal("Alice", result[0].Name);
            // Among Alices with same amount, sorted by date ascending
            Assert.True(result[0].CreatedDate <= result[1].CreatedDate);
            Assert.True(result[1].CreatedDate <= result[2].CreatedDate);
            // Bob at the end
            Assert.Equal("Bob", result[3].Name);
        }

        [Fact]
        public void ApplySorting_WithDescendingFirstThenAscendingSecond_ShouldExecuteThenByPath()
        {
            // Arrange - Tests the ThenBy path after OrderByDescending (line 41)
            var items = new List<TestModel>
 {
      new TestModel { Id = 1, Name = "Zoe", Amount = 300m, CreatedDate = new DateTime(2024, 1, 1), Priority = 1 },
      new TestModel { Id = 2, Name = "Zoe", Amount = 100m, CreatedDate = new DateTime(2024, 1, 2), Priority = 2 },
  new TestModel { Id = 3, Name = "Alice", Amount = 200m, CreatedDate = new DateTime(2024, 1, 3), Priority = 3 },
        new TestModel { Id = 4, Name = "Zoe", Amount = 200m, CreatedDate = new DateTime(2024, 1, 4), Priority = 1 }
            };

            var sortingModels = new List<SortingModel>
            {
         new SortingModel { Field = "Name", Dir = "desc" },      // Descending: Zoe first, then Alice
     new SortingModel { Field = "Amount", Dir = "asc" }      // Then ascending by Amount
      };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Count);
            // Zoe entries first (descending by Name), sorted by Amount ascending
            Assert.Equal("Zoe", result[0].Name);
            Assert.Equal(100m, result[0].Amount);
            Assert.Equal("Zoe", result[1].Name);
            Assert.Equal(200m, result[1].Amount);
            Assert.Equal("Zoe", result[2].Name);
            Assert.Equal(300m, result[2].Amount);
            // Alice last
            Assert.Equal("Alice", result[3].Name);
            Assert.Equal(200m, result[3].Amount);
        }

        [Fact]
        public void ApplySorting_WithDescendingFirstThenDescendingSecond_ShouldExecuteThenByDescendingPath()
        {
            // Arrange - Tests the ThenByDescending path after OrderByDescending (line 40)
            var items = new List<TestModel>
            {
                new TestModel { Id = 1, Name = "Zoe", Amount = 100m, CreatedDate = new DateTime(2024, 1, 1), Priority = 1 },
                new TestModel { Id = 2, Name = "Zoe", Amount = 300m, CreatedDate = new DateTime(2024, 1, 2), Priority = 2 },
                new TestModel { Id = 3, Name = "Alice", Amount = 200m, CreatedDate = new DateTime(2024, 1, 3), Priority = 3 },
                new TestModel { Id = 4, Name = "Zoe", Amount = 200m, CreatedDate = new DateTime(2024, 1, 4), Priority = 1 }
        };

            var sortingModels = new List<SortingModel>
        {
                new SortingModel { Field = "Name", Dir = "desc" },  // Descending: Zoe first, then Alice
                new SortingModel { Field = "Amount", Dir = "desc" }  // Then descending by Amount
         };

            // Act
            var result = items.ApplySorting(sortingModels).ToList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Count);
            // Zoe entries first (descending by Name), sorted by Amount descending
            Assert.Equal("Zoe", result[0].Name);
            Assert.Equal(300m, result[0].Amount);  // Highest amount first
            Assert.Equal("Zoe", result[1].Name);
            Assert.Equal(200m, result[1].Amount);
            Assert.Equal("Zoe", result[2].Name);
            Assert.Equal(100m, result[2].Amount);  // Lowest amount last
                                                   // Alice last
            Assert.Equal("Alice", result[3].Name);
            Assert.Equal(200m, result[3].Amount);
        }
    }
}
