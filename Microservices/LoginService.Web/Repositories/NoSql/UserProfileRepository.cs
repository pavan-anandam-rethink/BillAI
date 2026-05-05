using LoginService.Web.Entities;
using MongoDB.Driver;
using RethinkCore.Common.MongoDB;
using RethinkCore.Common.MongoDB.Models;
using System.Linq.Expressions;
using RethinkCore.Common.Definitions;

namespace LoginService.Web.Repositories.NoSql
{
    public interface IUserProfileRepository : IMongoDbRepository<UserProfileEntity>
    {
        Task DeleteByIdAsync(string id, bool softDelete = true);
        Task<UserProfileEntity> FindOneAsync(Expression<Func<UserProfileEntity, bool>> predicate);
    }

    public class UserProfileRepository : BaseMongoRepository<UserProfileEntity>, IUserProfileRepository
    {
        private readonly ILogger<UserProfileRepository> _logger;

        public UserProfileRepository(IMongoCollectionFactory collectionFactory,
            ILogger<UserProfileRepository> log) : base(collectionFactory, log)
        {
            _logger = log;
        }

        public async Task<UserProfileEntity> FindOneAsync(Expression<Func<UserProfileEntity, bool>> predicate)
        {
            var results = await this.FindAsync(predicate);
            return results.FirstOrDefault();
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

        Task IUserProfileRepository.DeleteByIdAsync(string id, bool softDelete)
        {
            throw new NotImplementedException();
        }
    }
}
