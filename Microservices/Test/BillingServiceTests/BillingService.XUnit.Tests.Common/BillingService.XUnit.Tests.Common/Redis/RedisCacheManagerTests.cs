using Microsoft.Extensions.Configuration;
using Moq;
using Rethink.Services.Common.Cache;
using Rethink.Services.Common.Cache.Redis;
using StackExchange.Redis;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Common.Cache.Redis
{
    public class RedisCacheManagerTests
    {
        private readonly Mock<IConnectionMultiplexer> _mockConnection;
        private readonly Mock<StackExchange.Redis.IDatabase> _mockDatabase;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IConfigurationSection> _mockSection;

        private readonly RedisCacheManager _redisCacheManager;

        public RedisCacheManagerTests()
        {
            _mockConnection = new Mock<IConnectionMultiplexer>();
            _mockDatabase = new Mock<StackExchange.Redis.IDatabase>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockSection = new Mock<IConfigurationSection>();

            _mockSection.Setup(x => x.Value).Returns("TestScope");

            _mockConfiguration
                .Setup(x => x.GetSection("CacheSettings:CacheScope"))
                .Returns(_mockSection.Object);

            _mockConnection
                .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(_mockDatabase.Object);

            _redisCacheManager = new RedisCacheManager(
                _mockConnection.Object,
                _mockConfiguration.Object
            );
        }

        [Fact]
        public async Task SetAsync_Should_Save_Data_In_Redis()
        {
            // Arrange
            var key = "testKey";
            var data = new { Name = "Test" };

            _mockDatabase
                .Setup(x => x.StringSetAsync(
                    It.IsAny<RedisKey>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<bool>(),
                    It.IsAny<When>(),
                    It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);

            // Act
            await _redisCacheManager.SetAsync(key, data, CachingDuration.OneMinute);

            // Assert
            _mockDatabase.Verify(x => x.StringSetAsync(
                It.Is<RedisKey>(k => k.ToString().Contains("TestScope:testKey")),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAsync_Should_Return_Cached_Value_When_Key_Exists()
        {
            // Arrange
            var key = "testKey";

            _mockDatabase
                .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync("\"cached-value\"");

            // Act
            var result = await _redisCacheManager.GetAsync<string>(
                key,
                () => Task.FromResult("new-value"),
                CachingDuration.OneMinute);

            // Assert
            Assert.Equal("cached-value", result);
        }

        [Fact]
        public async Task Remove_Should_Delete_Key()
        {
            // Arrange
            var key = "testKey";

            _mockDatabase
                .Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);

            // Act
            await _redisCacheManager.Remove(key);

            // Assert
            _mockDatabase.Verify(x =>
                x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()),
                Times.Once);
        }

        [Fact]
        public async Task Clear_Should_Flush_All_Databases()
        {
            // Arrange
            var mockServer = new Mock<IServer>();
            var endpoints = new EndPoint[]
            {
                new DnsEndPoint("localhost", 6379)
            };

            _mockConnection.Setup(x => x.GetEndPoints(true)).Returns(endpoints);
            _mockConnection.Setup(x => x.GetServer(It.IsAny<EndPoint>(), null))
                           .Returns(mockServer.Object);

            // Act
            await _redisCacheManager.Clear();

            // Assert
            mockServer.Verify(x => x.FlushAllDatabasesAsync(It.IsAny<CommandFlags>()), Times.Once);
        }
    }
}
