using AutoMapper;
using LoginService.Web.Entities;
using LoginService.Web.Mapping;
using LoginService.Web.Models;
using LoginService.Web.Repositories.NoSql;
using LoginService.Web.Services;
using MongoDB.Driver;
using Moq;
using Rethink.Services.Common.Cache;
using System.Linq.Expressions;
using System.Security.Claims;

namespace LoginService.Service.Tests
{
    public class UserProfileServiceTests
    {

        private readonly ICacheManager _cacheManager;
        private readonly IUserProfileRepository _userRepo;
        private readonly IMapper _mapper;

        public UserProfileServiceTests()
        {
            var mockCacheManager = new Mock<ICacheManager>();
            mockCacheManager.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<Dictionary<string, string>>>>(), It.IsAny<CachingDuration>()))
                .ReturnsAsync((string key, Func<Task<Dictionary<string, string>>> func, CachingDuration duration) =>
                {
                    return new Dictionary<string, string>();
                });

            var mockUserRepo = new Mock<IUserProfileRepository>();
            mockUserRepo.Setup(x => x.FindOneAsync(It.IsAny<Expression<Func<UserProfileEntity, bool>>>()))
                .ReturnsAsync((Expression<Func<UserProfileEntity, bool>> predicate) =>
                {
                    return new UserProfileEntity() { Id = Guid.NewGuid().ToString(), MsalObjectId = Guid.NewGuid().ToString() };
                });
            mockUserRepo.Setup(x => x.FindAsync(It.IsAny<Expression<Func<UserProfileEntity, bool>>>(), It.IsAny<FindOptions<UserProfileEntity, UserProfileEntity>>()))
                .ReturnsAsync((Expression<Func<UserProfileEntity, bool>> filter, FindOptions<UserProfileEntity, UserProfileEntity> findOpts) =>
                {
                    return new List<UserProfileEntity>() { new UserProfileEntity() { Id = Guid.NewGuid().ToString() } };
                });

            _mapper = new Mapper(new MapperConfiguration(cfg => cfg.AddProfile(new UserProfileMapping())));
            _cacheManager = mockCacheManager.Object;
            _userRepo = mockUserRepo.Object;
        }

        [Fact]
        public async Task GetUserProfileByMsalObjectId_ById_ShouldSucceed()
        {
            var svc = new UserProfileService(_userRepo, _mapper, _cacheManager);
            var result = await svc.GetUserProfileByMsalObjectId("objectid", false);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetUserProfileById_WithIdAndCache_ShouldSucceed()
        {
            var svc = new UserProfileService(_userRepo, _mapper, _cacheManager);
            var result = await svc.GetUserProfileById(Guid.NewGuid().ToString(), true);
        }

        [Fact]
        public async Task GetUserProfileById_WithIdNoCache_ShouldSucceed()
        {
            var svc = new UserProfileService(_userRepo, _mapper, _cacheManager);
            var result = await svc.GetUserProfileById(Guid.NewGuid().ToString(), false);
        }

        [Fact]
        public void Constructor_NullRepository_ThrowsArgumentNullException()
        {
            // Arrange
            IUserProfileRepository repository = null;
            var mapper = new Mock<IMapper>().Object;
            var cacheManager = new Mock<ICacheManager>().Object;

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new UserProfileService(repository, mapper, cacheManager));

            // Assert
            Assert.Equal("repository", ex.ParamName);
        }

        [Fact]
        public void Constructor_NullMapper_ThrowsArgumentNullException()
        {
            // Arrange
            var repository = new Mock<IUserProfileRepository>().Object;
            IMapper mapper = null;
            var cacheManager = new Mock<ICacheManager>().Object;

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new UserProfileService(repository, mapper, cacheManager));

            // Assert
            Assert.Equal("mapper", ex.ParamName);
        }

        [Fact]
        public void Constructor_NullCacheManager_ThrowsArgumentNullException()
        {
            // Arrange
            var repository = new Mock<IUserProfileRepository>().Object;
            var mapper = new Mock<IMapper>().Object;
            ICacheManager cacheManager = null;

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new UserProfileService(repository, mapper, cacheManager));

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
            repo.Setup(x => x.FindOneAsync(It.IsAny<Expression<Func<UserProfileEntity, bool>>>()))
                .ReturnsAsync(new UserProfileEntity
                {
                    Id = userId,
                    MsalObjectId = encodedMsalObjectId
                });

            var service = new UserProfileService(repo.Object, _mapper, cacheManager.Object);

            // Act
            var result = await service.GetUserProfileByMsalObjectId(encodedMsalObjectId, true);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
        }




    }
}
