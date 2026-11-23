using Application.Interfaces.V1;
using Application.Interfaces.V1.User;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Domain.Entities.Common;
using Domain.Entities.Modals;
using Domain.Entities.Request;
using Domain.Entities.Response;
using Domain.Enums;
using Domain.Records;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security;

namespace Admin.Auth;

public sealed class AuthUserStoreService(IAPICall _api) : IAuthUserStore
{

    public async Task<AuthUser?> FindByLoginAsync(string userCode, AccountType type, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userCode)) return null;
        try
        {
            var req = new AuthLoginRequest { AccountType = type, UserCode = userCode };
            var res = await _api.CallingAPI<Auth_User_Response, AuthLoginRequest>(AllApiNames.FIND_USER_BY_LOGIN, req);
            if (res.responseCode != 0)
            {
                throw new InvalidOperationException(res.responseMessage ?? "Failed to load users.");
            }
            return res.AuthUser;
        }
        catch (InvalidOperationException)
        {
            throw; // already carries a meaningful message, let it propagate as-is
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred while loading users.", ex);
        }
    }
    public async Task<int> RecordFailedLoginAsync(string userCode, AccountType type, CancellationToken ct = default)
    {
        try
        {
            var req = new AuthLoginRequest { AccountType = type, UserCode = userCode };
            return await _api.CallingAPI<int, AuthLoginRequest>(AllApiNames.RECORD_FAILED_LOGIN, req);
        }
        catch (InvalidOperationException)
        {
            throw; // already carries a meaningful message, let it propagate as-is
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred while loading users.", ex);
        }
    }
    public async Task LockOutAsync(string userCode, AccountType type, DateTimeOffset untilUtc, CancellationToken ct = default)
    {
        try
        {
            var req = new AuthLoginRequest { AccountType = type, UserCode = userCode, untilUtc = untilUtc };
            await _api.CallingAPI<bool, AuthLoginRequest>(AllApiNames.LOCK_OUT, req);
        }
        catch (InvalidOperationException)
        {
            throw; // already carries a meaningful message, let it propagate as-is
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred while lockout users.", ex);
        }
    }
    public async Task ClearLockoutAsync(string userCode, AccountType type, CancellationToken ct = default)
    {
        try
        {
            var req = new AuthLoginRequest { AccountType = type, UserCode = userCode };
            await _api.CallingAPI<bool, AuthLoginRequest>(AllApiNames.CLEAR_LOCKOUT, req);
        }
        catch (InvalidOperationException)
        {
            throw; // already carries a meaningful message, let it propagate as-is
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred while clear lockout users.", ex);
        }
    }
    public async Task SetPasswordHashAsync(string userCode, AccountType type, string passwordHash, bool rotateSecurityStamp, CancellationToken ct = default)
    {
        try
        {
            var req = new AuthLoginRequest { AccountType = type, UserCode = userCode, passwordHash = passwordHash, rotateSecurityStamp = rotateSecurityStamp };
            await _api.CallingAPI<bool, AuthLoginRequest>(AllApiNames.SET_PASSWORD_HASH, req);
        }
        catch (InvalidOperationException)
        {
            throw; // already carries a meaningful message, let it propagate as-is
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred while clear lockout users.", ex);
        }
    }
    public async Task SavePasswordResetTokenAsync(string userCode, AccountType type, string tokenHash, DateTimeOffset expiresUtc, CancellationToken ct = default)
    {
        try
        {
            var req = new AuthLoginRequest { AccountType = type, UserCode = userCode, tokenHash = tokenHash, expiresUtc = expiresUtc };
            await _api.CallingAPI<bool, AuthLoginRequest>(AllApiNames.SAVE_PWD_RESET_TKN, req);
        }
        catch (InvalidOperationException)
        {
            throw; // already carries a meaningful message, let it propagate as-is
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred while clear lockout users.", ex);
        }
    }
    public async Task<ResetTokenOwner?> FindUserIdByPasswordResetTokenAsync(string tokenHash, CancellationToken ct = default)
    {
        try
        {
            var req = new AuthLoginRequest { tokenHash = tokenHash };
            return await _api.CallingAPI<ResetTokenOwner, AuthLoginRequest>(AllApiNames.FIND_USER_BY_PWD_RST_TKN, req);
        }
        catch (InvalidOperationException)
        {
            throw; // already carries a meaningful message, let it propagate as-is
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred while clear lockout users.", ex);
        }
    }
    public async Task<ResetTokenOwner?> ConsumePasswordResetTokenAsync(string tokenHash, CancellationToken ct = default)
    {
        try
        {
            var req = new AuthLoginRequest { tokenHash = tokenHash };
            return await _api.CallingAPI<ResetTokenOwner, AuthLoginRequest>(AllApiNames.CONSUME_PWD_RST_TKN, req);
        }
        catch (InvalidOperationException)
        {
            throw; // already carries a meaningful message, let it propagate as-is
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred while clear lockout users.", ex);
        }
       
    }
}