using Domain.Entities.Response;
using Domain.Enums;
using Domain.Records;

namespace Admin.Auth;

/// <summary>
/// Data access for authentication. Implement this against your database;
/// <see cref="InMemoryAuthUserStore"/> is a development stand-in.
/// </summary>
public interface IAuthUserStore
{
    /// <summary>
    /// Case-insensitive match on user name OR e-mail, among accounts of <paramref name="type"/> only.
    /// Employees and vendors can live in different tables; look in the one for this type.
    /// </summary>
    Task<AuthUser?> FindByLoginAsync(string userCode, AccountType type, CancellationToken ct = default);

    /// <summary>Increments the failed-login counter and returns the new value.</summary>
    Task<int> RecordFailedLoginAsync(string userCode, AccountType type, CancellationToken ct = default);

    /// <summary>Locks the account until <paramref name="untilUtc"/> and resets the counter.</summary>
    Task LockOutAsync(string userCode, AccountType type, DateTimeOffset untilUtc, CancellationToken ct = default);

    /// <summary>Resets the counter and removes any lockout.</summary>
    Task ClearLockoutAsync(string userCode, AccountType type, CancellationToken ct = default);

    /// <summary>
    /// Saves a new hash. When <paramref name="rotateSecurityStamp"/> is true, also sets a new
    /// SecurityStamp, which signs the user out everywhere.
    /// </summary>
    Task SetPasswordHashAsync(string userCode, AccountType type, string passwordHash, bool rotateSecurityStamp, CancellationToken ct = default);

    /// <summary>Stores the SHA-256 hash of a reset token, replacing any earlier token for the user.</summary>
    Task SavePasswordResetTokenAsync(string userCode, AccountType type, string tokenHash, DateTimeOffset expiresUtc, CancellationToken ct = default);

    /// <summary>Returns the user id for an unexpired token without using it up.</summary>
    Task<ResetTokenOwner?> FindUserIdByPasswordResetTokenAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>Atomically deletes an unexpired token and returns its user id (null if none).</summary>
    Task<ResetTokenOwner?> ConsumePasswordResetTokenAsync(string tokenHash, CancellationToken ct = default);
}
