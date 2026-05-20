using AutoMapper;
using LoginService.Application.Interfaces;
using LoginService.Domain.Models;
using LoginService.Infrastructure.Entities;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RethinkCore.Common.Definitions;
using RethinkCore.Common.MongoDB;
using RethinkCore.Common.MongoDB.Models;

namespace LoginService.Infrastructure.Persistence
{
    public interface IMongoUserProfileRepository : IMongoDbRepository<UserProfileEntity>
    {
        Task DeleteByIdAsync(string id, bool softDelete = true);
    }

    public class UserProfileRepository : BaseMongoRepository<UserProfileEntity>, IUserProfileRepository, IMongoUserProfileRepository
    {
        private readonly ILogger<UserProfileRepository> _logger;
        private readonly IMapper _mapper;

        public UserProfileRepository(
            IMongoCollectionFactory collectionFactory,
            ILogger<UserProfileRepository> log,
            IMapper mapper) : base(collectionFactory, log)
        {
            _logger = log;
            _mapper = mapper;
        }

        public async Task<UserProfile?> FindByMsalObjectIdAsync(string msalObjectId)
        {
            var results = await this.FindAsync(x => x.MsalObjectId == msalObjectId);
            var entity = results.FirstOrDefault();
            return entity != null ? _mapper.Map<UserProfile>(entity) : null;
        }

        public async Task<UserProfile?> FindByIdAsync(string userProfileId)
        {
            var results = await this.FindAsync(x => x.Id == userProfileId);
            var entity = results.FirstOrDefault();
            return entity != null ? _mapper.Map<UserProfile>(entity) : null;
        }

        protected override SortDefinition<UserProfileEntity> BuildSortDefinition(IPagingFilter<UserProfileEntity> filter)
        {
            if (string.IsNullOrEmpty(filter.OrderBy))
            {
                return Builders<UserProfileEntity>.Sort.Descending(x => x.Id);
            }

            switch (filter.OrderBy.ToLower())
            {
                default:
                case "createdon":
                    return filter.SortOrder == SortOrder.Ascending ?
                        Builders<UserProfileEntity>.Sort.Ascending(x => x.Metadata.DateCreated) :
                        Builders<UserProfileEntity>.Sort.Descending(x => x.Metadata.DateCreated);
                case "name":
                    return filter.SortOrder == SortOrder.Ascending ?
                        Builders<UserProfileEntity>.Sort.Ascending(x => x.LastName).Ascending(x => x.FirstName) :
                        Builders<UserProfileEntity>.Sort.Descending(x => x.LastName).Descending(x => x.FirstName);
                case "email":
                    return filter.SortOrder == SortOrder.Ascending ?
                        Builders<UserProfileEntity>.Sort.Ascending(x => x.Email) :
                        Builders<UserProfileEntity>.Sort.Descending(x => x.Email);
            }
        }

        Task IMongoUserProfileRepository.DeleteByIdAsync(string id, bool softDelete)
        {
            throw new NotImplementedException();
        }
    }
}
