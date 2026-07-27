using Auth.API.Exceptions;
using Auth.API.Features;
using Auth.API.Security;
using Microsoft.AspNetCore.Identity;

namespace Auth.API.Features.Login;

public record LoginCommand(string Email, string Password)
    : ICommand<AuthResponse>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("El correo electrónico es requerido")
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("La contraseña es requerida");
    }
}

public class LoginCommandHandler(
    IDocumentSession session,
    IPasswordHasher<AuthUser> passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator)
    : ICommandHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = command.Email.Trim().ToUpperInvariant();
        var user = await session.Query<AuthUser>()
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new InvalidCredentialsException();
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            command.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new InvalidCredentialsException();
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, command.Password);
            session.Store(user);
            await session.SaveChangesAsync(cancellationToken);
        }

        var token = jwtTokenGenerator.Generate(user);

        return new AuthResponse(
            AuthContractMapper.ToDto(user),
            token.AccessToken,
            token.ExpiresAtUtc);
    }
}
