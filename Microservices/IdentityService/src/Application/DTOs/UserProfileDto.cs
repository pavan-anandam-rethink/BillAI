namespace IdentityService.Application.DTOs;

public class UserProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string MsalObjectId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public DateTime? LastUpdate { get; set; }
}
