using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Auth.API.Security;

public class BootstrapAdminInitializer(
    IServiceProvider serviceProvider,
    IOptions<BootstrapAdminOptions> options,
    ILogger<BootstrapAdminInitializer> logger) : IHostedService
{
    private readonly BootstrapAdminOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        ValidateOptions();

        using var scope = serviceProvider.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AuthUser>>();
        var normalizedEmail = Normalize(_options.Email);

        var existingUser = await session.Query<AuthUser>()
            .FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
            logger.LogInformation("El administrador inicial ya existe con el correo configurado.");
            return;
        }

        var userName = _options.UserName.Trim();
        var email = _options.Email.Trim();
        var admin = new AuthUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            NormalizedUserName = Normalize(userName),
            Email = email,
            NormalizedEmail = normalizedEmail,
            Role = "Admin",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        admin.PasswordHash = passwordHasher.HashPassword(admin, _options.Password);

        session.Store(admin);
        await session.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Administrador inicial creado correctamente.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.UserName) ||
            string.IsNullOrWhiteSpace(_options.Email) ||
            string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException(
                "BootstrapAdmin requiere UserName, Email y Password cuando está habilitado.");
        }
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
