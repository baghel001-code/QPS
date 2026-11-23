using Domain.Entities.Request;
using Domain.Entities.Response;
using Domain.Enums;
using Domain.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.V1.User
{
    public interface IUserAuthServices
    {
        /// <summary>
        /// Case-insensitive match on user name OR e-mail, among accounts of <paramref name="type"/> only.
        /// Employees and vendors can live in different tables; look in the one for this type.
        /// </summary>
        Task<Auth_User_Response> FindByLoginAsync(AuthLoginRequest authLoginRequest, CancellationToken ct = default);

        /// <summary>Increments the failed-login counter and returns the new value.</summary>
        Task<int> RecordFailedLoginAsync(AuthLoginRequest authLoginRequest, CancellationToken ct = default);

        /// <summary>Locks the account until <paramref name="untilUtc"/> and resets the counter.</summary>
        Task LockOutAsync(AuthLoginRequest authLoginRequest, CancellationToken ct = default);

        /// <summary>Resets the counter and removes any lockout.</summary>
        Task ClearLockoutAsync(AuthLoginRequest authLoginRequest, CancellationToken ct = default);

        /// <summary>
        /// Saves a new hash. When <paramref name="rotateSecurityStamp"/> is true, also sets a new
        /// SecurityStamp, which signs the user out everywhere.
        /// </summary>
        Task SetPasswordHashAsync(AuthLoginRequest authLoginRequest,  CancellationToken ct = default);

        /// <summary>Stores the SHA-256 hash of a reset token, replacing any earlier token for the user.</summary>
        Task SavePasswordResetTokenAsync(AuthLoginRequest authLoginRequest,   CancellationToken ct = default);

        /// <summary>Returns the user id for an unexpired token without using it up.</summary>
        Task<ResetTokenOwner?> FindUserIdByPasswordResetTokenAsync(AuthLoginRequest authLoginRequest, CancellationToken ct = default);

        /// <summary>Atomically deletes an unexpired token and returns its user id (null if none).</summary>
        Task<ResetTokenOwner?> ConsumePasswordResetTokenAsync(AuthLoginRequest authLoginRequest, CancellationToken ct = default);
    }
}
