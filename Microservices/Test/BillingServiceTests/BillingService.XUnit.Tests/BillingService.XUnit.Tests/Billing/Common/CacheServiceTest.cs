using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using BillingService.Domain.Services.Common;
using Newtonsoft.Json;

namespace BillingService.XUnit.Tests.Billing.Common
{
    public class CacheServiceTest
    {
        private readonly Mock<IDistributedCache> _mockRedisCache;
        private readonly Mock<ILogger<CacheService>> _mockLogger;
        private readonly CacheService _cacheService;

        public CacheServiceTest()
        {
            _mockRedisCache = new Mock<IDistributedCache>();
            _mockLogger = new Mock<ILogger<CacheService>>();
            _cacheService = new CacheService(_mockRedisCache.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetOrSetCacheAsync_WhenDataExistsInCache_ReturnsDataFromCache()
        {
            // Arrange
            var cacheKey = "test-key";
            var expectedData = new TestData { Id = 1, Name = "Test" };
            var cachedJson = JsonConvert.SerializeObject(expectedData);
            var cachedBytes = Encoding.UTF8.GetBytes(cachedJson);
            
            _mockRedisCache.Setup(x => x.GetAsync(cacheKey, default))
                .ReturnsAsync(cachedBytes);

            Func<Task<TestData>> fetchDataFunc = async () => await Task.FromResult(new TestData { Id = 2, Name = "Fetch" });

            // Act
            var result = await _cacheService.GetOrSetCacheAsync(cacheKey, fetchDataFunc, TimeSpan.FromMinutes(5));

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedData.Id, result.Id);
            Assert.Equal(expectedData.Name, result.Name);
            _mockRedisCache.Verify(x => x.GetAsync(cacheKey, default), Times.Once);
            _mockRedisCache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default), Times.Never);
        }

        [Fact]
        public async Task GetOrSetCacheAsync_WhenCacheIsEmpty_FetchesDataAndStoresInCache()
        {
            // Arrange
            var cacheKey = "test-key";
            var expectedData = new TestData { Id = 1, Name = "Test" };
            var expirationTime = TimeSpan.FromMinutes(10);

            _mockRedisCache.Setup(x => x.GetAsync(cacheKey, default))
                .ReturnsAsync((byte[])null);

            _mockRedisCache.Setup(x => x.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    default))
                .Returns(Task.CompletedTask);

            Func<Task<TestData>> fetchDataFunc = async () => await Task.FromResult(expectedData);

            // Act
            var result = await _cacheService.GetOrSetCacheAsync(cacheKey, fetchDataFunc, expirationTime);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedData.Id, result.Id);
            Assert.Equal(expectedData.Name, result.Name);
            _mockRedisCache.Verify(x => x.GetAsync(cacheKey, default), Times.Once);
            _mockRedisCache.Verify(x => x.SetAsync(
                cacheKey,
                It.Is<byte[]>(bytes => Encoding.UTF8.GetString(bytes).Contains(expectedData.Name)),
                It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == expirationTime),
                default), Times.Once);
        }

        [Fact]
        public async Task GetOrSetCacheAsync_WhenCacheIsEmptyAndFetchReturnsNull_DoesNotStoreInCache()
        {
            // Arrange
            var cacheKey = "test-key";

            _mockRedisCache.Setup(x => x.GetAsync(cacheKey, default))
                .ReturnsAsync((byte[])null);

            Func<Task<TestData>> fetchDataFunc = async () => await Task.FromResult<TestData>(null);

            // Act
            var result = await _cacheService.GetOrSetCacheAsync(cacheKey, fetchDataFunc, TimeSpan.FromMinutes(5));

            // Assert
            Assert.Null(result);
            _mockRedisCache.Verify(x => x.GetAsync(cacheKey, default), Times.Once);
            _mockRedisCache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default), Times.Never);
        }

        [Fact]
        public async Task GetOrSetCacheAsync_WhenCacheIsEmptyStringAndFetchReturnsData_StoresInCache()
        {
            // Arrange
            var cacheKey = "test-key";
            var expectedData = new TestData { Id = 1, Name = "Test" };
            var emptyBytes = new byte[0];

            _mockRedisCache.Setup(x => x.GetAsync(cacheKey, default))
                .ReturnsAsync(emptyBytes);

            _mockRedisCache.Setup(x => x.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    default))
                .Returns(Task.CompletedTask);

            Func<Task<TestData>> fetchDataFunc = async () => await Task.FromResult(expectedData);

            // Act
            var result = await _cacheService.GetOrSetCacheAsync(cacheKey, fetchDataFunc, TimeSpan.FromMinutes(5));

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedData.Id, result.Id);
            _mockRedisCache.Verify(x => x.SetAsync(
                cacheKey,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default), Times.Once);
        }

        [Fact]
        public async Task GetOrSetCacheAsync_WhenExceptionOccurs_LogsErrorAndThrows()
        {
            // Arrange
            var cacheKey = "test-key";
            var exceptionMessage = "Cache connection failed";

            _mockRedisCache.Setup(x => x.GetAsync(cacheKey, default))
                .ThrowsAsync(new Exception(exceptionMessage));

            Func<Task<TestData>> fetchDataFunc = async () => await Task.FromResult(new TestData());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => 
                _cacheService.GetOrSetCacheAsync(cacheKey, fetchDataFunc, TimeSpan.FromMinutes(5)));

            Assert.Equal(exceptionMessage, exception.Message);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error in caching")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetOrSetCacheAsync_WithComplexObject_SerializesAndDeserializesCorrectly()
        {
            // Arrange
            var cacheKey = "test-key";
            var complexData = new ComplexTestData
            {
                Id = 1,
                Name = "Complex Test",
                Items = new List<string> { "Item1", "Item2" },
                CreatedDate = DateTime.Now
            };

            _mockRedisCache.Setup(x => x.GetAsync(cacheKey, default))
                .ReturnsAsync((byte[])null);

            _mockRedisCache.Setup(x => x.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    default))
                .Returns(Task.CompletedTask);

            Func<Task<ComplexTestData>> fetchDataFunc = async () => await Task.FromResult(complexData);

            // Act
            var result = await _cacheService.GetOrSetCacheAsync(cacheKey, fetchDataFunc, TimeSpan.FromMinutes(5));

            // Assert
            Assert.NotNull(result);
            Assert.Equal(complexData.Id, result.Id);
            Assert.Equal(complexData.Name, result.Name);
            Assert.Equal(complexData.Items.Count, result.Items.Count);
            _mockRedisCache.Verify(x => x.SetAsync(
                cacheKey,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default), Times.Once);
        }

        [Fact]
        public async Task RemoveAsync_CallsRedisCacheRemove_Successfully()
        {
            // Arrange
            var cacheKey = "test-key";
            _mockRedisCache.Setup(x => x.RemoveAsync(cacheKey, default))
                .Returns(Task.CompletedTask);

            // Act
            await _cacheService.RemoveAsync(cacheKey);

            // Assert
            _mockRedisCache.Verify(x => x.RemoveAsync(cacheKey, default), Times.Once);
        }

        [Fact]
        public async Task RemoveAsync_WhenExceptionOccurs_LogsError()
        {
            // Arrange
            var cacheKey = "test-key";
            var exceptionMessage = "Failed to remove key";
            var exception = new Exception(exceptionMessage);

            _mockRedisCache.Setup(x => x.RemoveAsync(cacheKey, default))
                .ThrowsAsync(exception);

            // Act
            await _cacheService.RemoveAsync(cacheKey);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error removing cache key")),
                    It.Is<Exception>(ex => ex.Message == exceptionMessage),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task RemoveAsync_WithNullOrEmptyKey_StillCallsRemove()
        {
            // Arrange
            var cacheKey = "";
            _mockRedisCache.Setup(x => x.RemoveAsync(cacheKey, default))
                .Returns(Task.CompletedTask);

            // Act
            await _cacheService.RemoveAsync(cacheKey);

            // Assert
            _mockRedisCache.Verify(x => x.RemoveAsync(cacheKey, default), Times.Once);
        }

        // Test helper classes
        private class TestData
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private class ComplexTestData
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public List<string> Items { get; set; }
            public DateTime CreatedDate { get; set; }
        }
    }
}
