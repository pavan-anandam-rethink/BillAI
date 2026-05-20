namespace LoginService.Domain.Models
{
    public class UserProfile
    {
        public string Id { get; set; }

        public DateTime CreatedOn { get; set; }

        // provided by Azure AD to link the user record
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
}
