using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace LoginService.Web.Models
{
    public class UserProfile : Document
    {
        // provided by Azure AD to link the user record
        [Required]
        public string MsalObjectId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public DateTime? LastUpdate { get; set; }

        public string FullName
        {
            get
            {
                return string.IsNullOrEmpty(this.FirstName) && string.IsNullOrEmpty(this.LastName) ?
                    this.Email :
                    $"{this.FirstName} {this.LastName}";
            }
        }
    }

    public abstract class Document
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public DateTime CreatedOn { get; set; }
    }
}
