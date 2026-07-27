using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Basket.API.Exceptions;

namespace Basket.API.Security;

public static class ClaimsPrincipalExtensions
{
    public static string GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidAuthenticatedUserException();
        }

        return userId.ToString("D");
    }
}
