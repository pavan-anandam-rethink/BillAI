using LoginService.Web.Entities;
using LoginService.Web.Repositories.NoSql;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Moq.Protected;
using RethinkCore.Common.Definitions;
using RethinkCore.Common.MongoDB;
using RethinkCore.Common.MongoDB.Models;
using System.Linq.Expressions;

namespace LoginService.Web.XUnit.Tests.Repositories
{
    // Test helper that exposes the protected BuildSortDefinition for testing.
    internal class TestableUserProfileRepository : UserProfileRepository
    {
        public TestableUserProfileRepository(IMongoCollectionFactory collectionFactory, ILogger<UserProfileRepository> log)
            : base(collectionFactory, log)
        { }

        public SortDefinition<UserProfileEntity> ExposeBuildSortDefinition(IPagingFilter<UserProfileEntity> filter)
            => base.BuildSortDefinition(filter);
    }

    public class UserProfileRepositoryTests
    {
        private readonly Mock<IMongoCollectionFactory> _collectionFactoryMock = new();
        private readonly Mock<ILogger<UserProfileRepository>> _loggerMock = new();

        [Fact]
        public void BuildSortDefinition_Default_OrderBy_Null_Returns_Id_Descending()
        {
            // Arrange
            var filterMock = new Mock<IPagingFilter<UserProfileEntity>>();
            filterMock.SetupGet(f => f.OrderBy).Returns((string?)null);
            // SortOrder doesn't matter for default path but set to Ascending
            filterMock.SetupGet(f => f.SortOrder).Returns(SortOrder.Ascending);

            var repo = new TestableUserProfileRepository(_collectionFactoryMock.Object, _loggerMock.Object);

            // Act
            var sortDef = repo.ExposeBuildSortDefinition(filterMock.Object);
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<UserProfileEntity>();
            var rendered = sortDef.Render(serializer, BsonSerializer.SerializerRegistry);

            // Assert: single element and descending (-1)
            Assert.Single(rendered.Elements);
            var value = rendered.Elements.First().Value;
            Assert.Equal(-1, value.AsInt32);
        }

        [Theory]
        [InlineData("createdon", SortOrder.Ascending, 1)]
        [InlineData("createdon", SortOrder.Descending, -1)]
        [InlineData("email", SortOrder.Ascending, 1)]
        [InlineData("email", SortOrder.Descending, -1)]
        public void BuildSortDefinition_SingleField_Order_By_Works(string orderBy, SortOrder order, int expectedSign)
        {
            // Arrange
            var filterMock = new Mock<IPagingFilter<UserProfileEntity>>();
            filterMock.SetupGet(f => f.OrderBy).Returns(orderBy);
            filterMock.SetupGet(f => f.SortOrder).Returns(order);

            var repo = new TestableUserProfileRepository(_collectionFactoryMock.Object, _loggerMock.Object);

            // Act
            var sortDef = repo.ExposeBuildSortDefinition(filterMock.Object);
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<UserProfileEntity>();
            var rendered = sortDef.Render(serializer, BsonSerializer.SerializerRegistry);

            // Assert: single element and correct sign (1 or -1)
            Assert.Single(rendered.Elements);
            var value = rendered.Elements.First().Value;
            Assert.Equal(expectedSign, value.AsInt32);
        }

        [Theory]
        [InlineData("name", SortOrder.Ascending, 1, 1)]
        [InlineData("name", SortOrder.Descending, -1, -1)]
        public void BuildSortDefinition_Name_OrderBy_Sorts_LastNameThenFirstName(string orderBy, SortOrder order, int expectedFirst, int expectedSecond)
        {
            // Arrange
            var filterMock = new Mock<IPagingFilter<UserProfileEntity>>();
            filterMock.SetupGet(f => f.OrderBy).Returns(orderBy);
            filterMock.SetupGet(f => f.SortOrder).Returns(order);

            var repo = new TestableUserProfileRepository(_collectionFactoryMock.Object, _loggerMock.Object);

            // Act
            var sortDef = repo.ExposeBuildSortDefinition(filterMock.Object);
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<UserProfileEntity>();
            var rendered = sortDef.Render(serializer, BsonSerializer.SerializerRegistry);

            // Assert: two elements and they have the proper sign sequence
            Assert.Equal(2, rendered.Elements.Count());
            var signs = rendered.Elements.Select(e => e.Value.AsInt32).ToArray();
            Assert.Equal(expectedFirst, signs[0]);
            Assert.Equal(expectedSecond, signs[1]);
        }

        [Fact]
        public void DeleteByIdAsync_Throws_NotImplementedException_When_Called_Via_Interface()
        {
            // Arrange
            var repo = new TestableUserProfileRepository(_collectionFactoryMock.Object, _loggerMock.Object);
            var asInterface = (IUserProfileRepository)repo;

            // Act & Assert
            var ex = Assert.ThrowsAsync<NotImplementedException>(async () => await asInterface.DeleteByIdAsync("some-id", true));
            Assert.IsType<NotImplementedException>(ex.Result);
        }
    }
}