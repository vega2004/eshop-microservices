using Auth.API.Exceptions;
using Auth.API.Features;
using Auth.API.Security;
using Microsoft.AspNetCore.Identity;

namespace Auth.API.Features.Register;

public record RegisterCommand(string UserName, string Email, string Password)
    : ICommand<AuthResponse>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.UserName)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("El nombre de usuario es requerido")
            .MinimumLength(3)
            .MaximumLength(50);

        RuleFor(x => x.Email)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("El correo electrónico es requerido")
            .EmailAddress()
            .MaximumLength(200);

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("La contraseña es requerida")
            .MinimumLength(8)
            .Matches("[A-Z]")
            .WithMessage("La contraseña debe contener al menos una letra mayúscula")
            .Matches("[a-z]")
            .WithMessage("La contraseña debe contener al menos una letra minúscula")
            .Matches("[0-9]")
            .WithMessage("La contraseña debe contener al menos un número");
    }
}

public class RegisterCommandHandler(
    IDocumentSession session,
    IPasswordHasher<AuthUser> passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator)
    : ICommandHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(
        RegisterCommand command,
        CancellationToken cancellationToken)
    {
        var userName = command.UserName.Trim();
        var email = command.Email.Trim();
        var normalizedUserName = Normalize(userName);
        var normalizedEmail = Normalize(email);

        var userNameExists = await session.Query<AuthUser>()
            .AnyAsync(x => x.NormalizedUserName == normalizedUserName, cancellationToken);

        var emailExists = await session.Query<AuthUser>()
            .AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

        if (userNameExists || emailExists)
        {
            throw new UserAlreadyExistsException();
        }

        var user = new AuthUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            NormalizedUserName = normalizedUserName,
            Email = email,
            NormalizedEmail = normalizedEmail,
            Role = "Customer",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, command.Password);

        session.Store(user);
        await session.SaveChangesAsync(cancellationToken);

        var token = jwtTokenGenerator.Generate(user);

        return new AuthResponse(
            AuthContractMapper.ToDto(user),
            token.AccessToken,
            token.ExpiresAtUtc);
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
