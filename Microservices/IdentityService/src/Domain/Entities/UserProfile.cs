using System.ComponentModel.DataAnnotations;

namespace IdentityService.Domain.Entities;

public class UserProfile : BaseEntity
{
    [Required]
    public string MsalObjectId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? LastUpdate { get; set; }

    public string FullName => string.IsNullOrEmpty(FirstName) && string.IsNullOrEmpty(LastName)
        ? Email
        : $"{FirstName} {LastName}";
}
