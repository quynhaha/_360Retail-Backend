namespace _360Retail.Services.Identity.Domain.Entities;

public class AccountActivationToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime ExpiredAt { get; set; }
    public bool IsUsed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
}
