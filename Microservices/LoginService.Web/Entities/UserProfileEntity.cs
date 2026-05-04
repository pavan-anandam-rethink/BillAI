using MongoDB.Bson.Serialization.Attributes;
using RethinkCore.Common.MongoDB.Attributes;
using RethinkCore.Common.MongoDB.Models;
using System.ComponentModel.DataAnnotations;

namespace LoginService.Web.Entities
{
    [MongoCollection("userProfile")]
    [BsonIgnoreExtraElements]
    public class UserProfileEntity : BaseMongoEntity
    {
        [Required]
        public string MsalObjectId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? LastUpdate { get; set; }
    }
}
