using Application.Interfaces.V1.Notifications;
using Application.Notifications;
using Domain.Records;

namespace Admin.Auth;

public interface IAuthEmailSender
{
    Task SendPasswordResetAsync(AuthUser user, string resetLink, TimeSpan validFor, CancellationToken ct = default);
}

/// <summary>DEVELOPMENT ONLY. Writes the reset link to the log instead of sending an e-mail.</summary>
public sealed class LoggingAuthEmailSender(ILogger<LoggingAuthEmailSender> logger, IServiceScopeFactory scopeFactory) : IAuthEmailSender
{
    public Task SendPasswordResetAsync(AuthUser user, string resetLink, TimeSpan validFor, CancellationToken ct = default)
    {
        //-------Login Alert--------------------
        var alert = new PasswordResetLinkModel( user,resetLink, validFor);

         _ = SendLoginAlertAsync(scopeFactory, logger, alert);

        return Task.CompletedTask;
    }

    private static async Task SendLoginAlertAsync(IServiceScopeFactory scopeFactory, ILogger logger, PasswordResetLinkModel alert)
    {
        try
        {
            // Own scope: the request scope is disposed as soon as the redirect is returned.
            await using var scope = scopeFactory.CreateAsyncScope();
            var notifier = scope.ServiceProvider.GetRequiredService<INotifier<PasswordResetLinkModel>>();
            var res = await notifier.NotifyAsync(alert);
            if (!res)
            {
                logger.LogError("reset e-mail failed for {UserCode}, email - {email}.", alert.Vendor.UserCode, alert.Vendor.Email);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "reset e-mail failed for {UserCode}", alert.Vendor.UserCode);
        }
    }
}
