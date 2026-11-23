using Application.Interfaces.V1;
using Domain.Enums;
using Domain.Records;

namespace Admin.Auth
{
    /// <summary>Vendors: the password hash is stored in the QPS database.</summary>
    public sealed class VendorPasswordVerifier(IPasswordHasher hasher, ILogger<VendorPasswordVerifier> logger) : ICredentialVerifier
    {
        public AccountType AccountType => AccountType.Vendor;

        public Task<CredentialResult> VerifyAsync(AuthUser user, string password, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(user.PasswordHash))
                return Task.FromResult(CredentialResult.Invalid);   // no password set yet

            try
            {
                return Task.FromResult(hasher.VerifyPassword(password, user.PasswordHash)
                    ? CredentialResult.Valid
                    : CredentialResult.Invalid);
            }
            catch (Exception ex)
            {
                // A hash the hasher can't parse (corrupt row, old format) counts as a wrong password.
                logger.LogWarning(ex, "Stored password hash for {UserId} could not be verified", user.Id);
                return Task.FromResult(CredentialResult.Invalid);
            }
        }
    }
}
