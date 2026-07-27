namespace Auth.API.Security;

public record AuthToken
{
    public string AccessToken { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
}
