using Auth.API.Exceptions;
using Auth.API.Features;

namespace Auth.API.Features.GetCurrentUser;

public record GetCurrentUserQuery(string? UserId)
    : IQuery<CurrentUserResponse>;

public class GetCurrentUserQueryHandler(IDocumentSession session)
    : IQueryHandler<GetCurrentUserQuery, CurrentUserResponse>
{
    public async Task<CurrentUserResponse> Handle(
        GetCurrentUserQuery query,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(query.UserId, out var userId))
        {
            throw new InvalidAuthenticatedUserException();
        }

        var user = await session.LoadAsync<AuthUser>(userId, cancellationToken);

        if (user is null)
        {
            throw new AuthUserNotFoundException();
        }

        if (!user.IsActive)
        {
            throw new InvalidAuthenticatedUserException();
        }

        return new CurrentUserResponse(AuthContractMapper.ToDto(user));
    }
}
