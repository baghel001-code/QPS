using Domain.Enums;
using Domain.Records;

namespace Admin.Auth
{
    public enum CredentialResult { Valid, Invalid, Unavailable, MaxAttemp }

    /// <summary>
    /// Checks a password for one account type. <see cref="AccountService"/> picks the verifier by
    /// the tab the user signed in on; everything else (CAPTCHA, lockout, disabled check, ticket)
    /// is the same for both types.
    /// </summary>

    public interface ICredentialVerifier
    {
        AccountType AccountType { get; }

        /// <summary>
        /// Valid or Invalid for a definite answer. Unavailable when the answer couldn't be obtained
        /// (API down, timeout); that is not counted as a failed attempt.
        /// </summary>
        Task<CredentialResult> VerifyAsync(AuthUser user, string password, CancellationToken ct);
    }
}
