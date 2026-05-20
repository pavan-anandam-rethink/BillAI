namespace IdentityService.Application.DTOs;

public class AccountInfoDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool OsbEnabled { get; set; }
}
