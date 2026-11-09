
namespace IncludePresenca.Domain.Entities;

public class PasswordResetToken
{
    public Guid Id { get; private set; }
    public Guid MemberId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }

    private PasswordResetToken()
    {
        TokenHash = null!;
    }

    public PasswordResetToken(Guid memberId, string tokenHash, DateTime expiresAt)
    {
        if (memberId == Guid.Empty)
            throw new ArgumentException("Invalid member ID.", nameof(memberId));

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Expiration must be in the future.", nameof(expiresAt));

        Id = Guid.NewGuid();
        MemberId = memberId;
        TokenHash = tokenHash;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
    }

    public bool IsValid()
    {
        return UsedAt is null && DateTime.UtcNow < ExpiresAt;
    }

    public void Consume()
    {
        if (!IsValid())
            throw new InvalidOperationException("Password reset token is invalid or expired.");

        UsedAt = DateTime.UtcNow;
    }

    public void Invalidate()
    {
        if (UsedAt is not null)
            return;

        UsedAt = DateTime.UtcNow;
    }
}
