namespace Auth.API.Features;

public record AuthUserDto(
    Guid Id,
    string UserName,
    string Email,
    string Role);

public record AuthResponse(
    AuthUserDto User,
    string AccessToken,
    DateTime ExpiresAtUtc);

public record CurrentUserResponse(AuthUserDto User);

public static class AuthContractMapper
{
    public static AuthUserDto ToDto(AuthUser user) => new(
        user.Id,
        user.UserName,
        user.Email,
        user.Role);
}
