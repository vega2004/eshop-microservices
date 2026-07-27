namespace Auth.API.Security;

public interface IJwtTokenGenerator
{
    AuthToken Generate(AuthUser user);
}
