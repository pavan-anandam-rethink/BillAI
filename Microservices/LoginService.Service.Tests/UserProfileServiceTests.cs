using AutoMapper;
using LoginService.Application.Interfaces;
using LoginService.Application.Services;
using LoginService.Domain.Models;
using LoginService.Infrastructure.Entities;
using LoginService.Infrastructure.Mapping;
using Moq;
using Rethink.Services.Common.Cache;

namespace LoginService.Service.Tests
{
    public class UserProfileServiceTests
    {
        private readonly ICacheManager _cacheManager;
        private readonly IUserProfileRepository _userRepo;

        public UserProfileServiceTests()
        {
            var mockCacheManager = new Mock<ICacheManager>();
            mockCacheManager.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<Dictionary<string, string>>>>(), It.IsAny<CachingDuration>()))
                .ReturnsAsync((string key, Func<Task<Dictionary<string, string>>> func, CachingDuration duration) =>
                {
                    return new Dictionary<string, string>();
                });

            var mockUserRepo = new Mock<IUserProfileRepository>();
            mockUserRepo.Setup(x => x.FindByMsalObjectIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string msalObjectId) =>
                {
                    return new UserProfile { Id = Guid.NewGuid().ToString(), MsalObjectId = Guid.NewGuid().ToString() };
                });
            mockUserRepo.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) =>
                {
                    return new UserProfile { Id = id };
                });

            _cacheManager = mockCacheManager.Object;
            _userRepo = mockUserRepo.Object;
        }

        [Fact]
        public async Task GetUserProfileByMsalObjectId_ById_ShouldSucceed()
        {
            var svc = new UserProfileService(_userRepo, _cacheManager);
            var result = await svc.GetUserProfileByMsalObjectId("objectid", false);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetUserProfileById_WithIdAndCache_ShouldSucceed()
        {
            var svc = new UserProfileService(_userRepo, _cacheManager);
            var result = await svc.GetUserProfileById(Guid.NewGuid().ToString(), true);
        }

        [Fact]
        public async Task GetUserProfileById_WithIdNoCache_ShouldSucceed()
        {
            var svc = new UserProfileService(_userRepo, _cacheManager);
            var result = await svc.GetUserProfileById(Guid.NewGuid().ToString(), false);
        }

        [Fact]
        public void Constructor_NullRepository_ThrowsArgumentNullException()
        {
            // Arrange
            IUserProfileRepository repository = null;
            var cacheManager = new Mock<ICacheManager>().Object;

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new UserProfileService(repository, cacheManager));

            // Assert
            Assert.Equal("repository", ex.ParamName);
        }

        [Fact]
        public void Constructor_NullCacheManager_ThrowsArgumentNullException()
        {
            // Arrange
            var repository = new Mock<IUserProfileRepository>().Object;
            ICacheManager cacheManager = null;

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new UserProfileService(repository, cacheManager));

            // Assert
            Assert.Equal("cacheManager", ex.ParamName);
        }

        [Fact]
        public async Task GetUserProfileByMsalObjectId_CacheHit_UsesUserIdPath()
        {
            // Arrange
            var msalObjectId = Guid.NewGuid().ToString();
            var userId = Guid.NewGuid().ToString();
            var encodedMsalObjectId = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(msalObjectId));

            Assert.False(string.IsNullOrWhiteSpace(msalObjectId));

            var cacheManager = new Mock<ICacheManager>();
            cacheManager.Setup(x => x.GetAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<Dictionary<string, string>>>>(),
                    It.IsAny<CachingDuration>()))
                .ReturnsAsync(new Dictionary<string, string>
                {
                    { encodedMsalObjectId, userId }
                });

            var repo = new Mock<IUserProfileRepository>();
            repo.Setup(x => x.FindByMsalObjectIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string msal) => new UserProfile
                {
                    Id = userId,
                    MsalObjectId = encodedMsalObjectId
                });
            repo.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => new UserProfile
                {
                    Id = id,
                    MsalObjectId = encodedMsalObjectId
                });

            var service = new UserProfileService(repo.Object, cacheManager.Object);

            // Act
            var result = await service.GetUserProfileByMsalObjectId(encodedMsalObjectId, true);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
        }
    }
}
