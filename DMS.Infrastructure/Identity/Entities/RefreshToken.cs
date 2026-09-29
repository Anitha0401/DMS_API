namespace MarineDMS.Infrastructure.Identity.Entities;
public class RefreshToken
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresOn { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? RevokedOn { get; set; }
    public bool IsRevoked => RevokedOn != null;
    public Guid UserId { get; set; }
    //public ApplicationUser User { get; set; } = default!;
}