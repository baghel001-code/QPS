using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Domain.Entities.Common;
using Domain.Entities.Modals;
using Domain.Entities.Response;
using Domain.Enums;
using Domain.Records;
using System.Globalization;
using System.Security.Claims;

namespace Admin.Auth;

public static class AuthClaims
{
    public static ClaimsPrincipal CreatePrincipal(AuthUser user, DateTimeOffset signedInAt, string? sessionId = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserCode),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.MobilePhone, user.MobileNumber?? string.Empty),
            new(ClaimTypes.Email, user.Email?? string.Empty),
            new(AuthClaimTypes.User_Id, user.Id.ToString()),
            new(AuthClaimTypes.User_Management, user.UserModules.User_Management.ToString()),
            new(AuthClaimTypes.Media, user.UserModules.Media.ToString()),
            new(AuthClaimTypes.Menu, user.UserModules.Menu.ToString()),
            new(AuthClaimTypes.CMS, user.UserModules.CMS.ToString()),
            new(AuthClaimTypes.RGNS, user.RegionIds?? string.Empty),
            new(AuthClaimTypes.SessionToken, user.SessionToken),
            new(AuthClaimTypes.GST, user.GSTNumber?? string.Empty),
            new(AuthClaimTypes.UserType, user.UserType),
            new(AuthClaimTypes.DisplayName, user.DisplayName),
            new(AuthClaimTypes.SecurityStamp, user.SecurityStamp),
            new(AuthClaimTypes.AccountType, user.AccountType.ToString()),
            new(AuthClaimTypes.AuthTime, signedInAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
            new(AuthClaimTypes.SessionId, string.IsNullOrEmpty(sessionId) ? Guid.NewGuid().ToString("N") : sessionId),
        };
        if (user.AccountType == AccountType.Vendor)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Vendor"));
        }
        else
        {
            claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
        }
        claims.AddRange(user.Modules.Select(m => new Claim(AuthClaimTypes.Module, m)));
        if (user.MustChangePassword)
            claims.Add(new Claim(AuthClaimTypes.MustChangePassword, "true"));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, AuthConstants.Scheme, ClaimTypes.Name, ClaimTypes.Role));
    }

    public static bool IsWithinAbsoluteLifetime(ClaimsPrincipal principal, AuthSettings settings, DateTimeOffset now) =>
        long.TryParse(principal.FindFirstValue(AuthClaimTypes.AuthTime), NumberStyles.None, CultureInfo.InvariantCulture, out var unix) &&
        now - DateTimeOffset.FromUnixTimeSeconds(unix) < TimeSpan.FromHours(settings.AbsoluteLifetimeHours);

}

public static class ClaimsPrincipalExtensions
{
    public static AccountType GetAccountType(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(AuthClaimTypes.AccountType);
        return Enum.TryParse<AccountType>(value, ignoreCase: true, out var type) ? type : AccountType.None;
    }

    public static bool IsVendor(this ClaimsPrincipal principal)
        => principal.GetAccountType() == AccountType.Vendor;
    public static bool IsEmployee(this ClaimsPrincipal principal)
       => principal.GetAccountType() == AccountType.Employee;
}

/// <summary>Shared by the cookie handler (HTTP requests) and the circuit revalidator (open Blazor pages).</summary>
public static class SessionValidation
{
    /// <param name="guard">The single-session check, or null when that feature is off.</param>
    public static async Task<SessionEndReason> CheckAsync(
        ClaimsPrincipal principal, IAuthUserStore store, ISingleSessionGuard? guard,
        AuthSettings settings, DateTimeOffset now, CancellationToken ct)
    {
        var userCode = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var userType = principal.GetAccountType();
        var stamp = principal.FindFirstValue(AuthClaimTypes.SecurityStamp);
        if (userCode is null || stamp is null || !AuthClaims.IsWithinAbsoluteLifetime(principal, settings, now))
            return SessionEndReason.Expired;

        // Lockout is deliberately not checked: someone guessing a password must not be able
        // to kick the real user out of an existing session.
        var user = await store.FindByLoginAsync(userCode, userType, ct);
        if (user is not { IsActive: true } || user.SecurityStamp != stamp)
            return SessionEndReason.Expired;

        // Signed in on another device since: this session is no longer the active one.
        if (guard is not null && (SessionOwner.From(principal) is not { } session || !await guard.IsCurrentAsync(session, ct)))
            return SessionEndReason.Replaced;

        return SessionEndReason.None;
    }
}