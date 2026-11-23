using Application.Interfaces.V1.User;
using Dapper;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using Domain.Entities.Modals;
using Domain.Entities.Request;
using Domain.Entities.Response;
using Domain.Enums;
using Domain.Records;
using Infrastructure.Utilitys;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.V1.User
{
    internal class UserAuthServices(DapperHelper _dapperHelper, TimeProvider _clock) : IUserAuthServices
    {

        public async Task ClearLockoutAsync(AuthLoginRequest authLoginRequest, CancellationToken ct = default)
        {
            var parameters = new DynamicParameters();
            AddUserId(parameters, authLoginRequest, _clock.GetUtcNow());
            await _dapperHelper.ExecuteStoredProcedureAsync("[auth].[PRO_LOGIN_CLEAR_LOCKOUT]", parameters);
        }

        public async Task<ResetTokenOwner?> ConsumePasswordResetTokenAsync(AuthLoginRequest authLoginRequest, CancellationToken ct = default)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@TOKEN_HASH", authLoginRequest.tokenHash, DbType.String, size: 128);
            parameters.Add("@NOW_UTC", _clock.GetUtcNow(), DbType.DateTimeOffset);
            return await _dapperHelper.StoredProcedureSingleAsync<ResetTokenOwner>("[auth].[PRO_PASSWORD_RESET_TOKEN_REDEEM]", parameters);
        }

        public async Task<Auth_User_Response> FindByLoginAsync(AuthLoginRequest authLoginRequest, CancellationToken ct = default)
        {
            var parameters = new DynamicParameters();
            AddUserId(parameters, authLoginRequest);
            var (users, modules, menus) = await _dapperHelper
                .ExecuteStoredProcedureMultiple3ListAsync<Auth_User_Row, Modules, Menu>("[auth].[PRO_LOGIN_LOOKUP]", parameters);

            var res = new Auth_User_Response
            {
                USER = users.FirstOrDefault(),                // null = not found (don't create an empty row)
                Modules = modules.FirstOrDefault() ?? new(),
                MenuList = [.. menus]                         // collection expression (C# 12) = menus.ToList()
            };

            return res;
        }
        public async Task<ResetTokenOwner?> FindUserIdByPasswordResetTokenAsync(AuthLoginRequest authLoginRequest, CancellationToken ct = default)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@TOKEN_HASH", authLoginRequest.tokenHash, DbType.String, size: 128);
            parameters.Add("@NOW_UTC", _clock.GetUtcNow(), DbType.DateTimeOffset);
            return await _dapperHelper.StoredProcedureSingleAsync<ResetTokenOwner>("[auth].[PRO_PASSWORD_RESET_TOKEN_FIND]", parameters);
        }

        public async Task LockOutAsync(AuthLoginRequest authLoginRequest, CancellationToken ct = default)
        {
            var parameters = new DynamicParameters();
            AddUserId(parameters, authLoginRequest, _clock.GetUtcNow());
            parameters.Add("@LOCKOUT_END_UTC", authLoginRequest.untilUtc, DbType.DateTimeOffset);
            await _dapperHelper.ExecuteStoredProcedureAsync("[auth].[PRO_LOGIN_LOCKOUT]", parameters);

        }

        public async Task<int> RecordFailedLoginAsync(AuthLoginRequest authLoginRequest, CancellationToken ct = default)
        {
            var parameters = new DynamicParameters();
            AddUserId(parameters, authLoginRequest, _clock.GetUtcNow());
            var count = await _dapperHelper.ExecuteStoredProcedureSingleAsync<int?>("[auth].[PRO_LOGIN_RECORD_FAILED_COUNT]", parameters);
            return count ?? 0;
        }

        public async Task SavePasswordResetTokenAsync(AuthLoginRequest authLoginRequest, CancellationToken ct = default)
        {
            try
            {
                var parameters = new DynamicParameters();
                AddUserId(parameters, authLoginRequest, _clock.GetUtcNow());
                parameters.Add("@TOKEN_HASH", authLoginRequest.tokenHash, DbType.String, size: 128);
                parameters.Add("@EXPIRES_UTC", authLoginRequest.expiresUtc, DbType.DateTimeOffset);
                await _dapperHelper.ExecuteStoredProcedureAsync("[auth].[PRO_PASSWORD_RESET_TOKEN_ISSUE]", parameters);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task SetPasswordHashAsync(AuthLoginRequest authLoginRequest, CancellationToken ct = default)
        {

            var parameters = new DynamicParameters();
            AddUserId(parameters, authLoginRequest, _clock.GetUtcNow());
            parameters.Add("@PASSWORD_HASH", authLoginRequest.passwordHash, DbType.String, size: 500);
            parameters.Add("@ROTATE_STAMP", authLoginRequest.rotateSecurityStamp, DbType.Boolean);
            parameters.Add("@NEW_SECURITY_STAMP", NewSecurityStamp(), DbType.String, size: 100);
            await _dapperHelper.ExecuteStoredProcedureAsync("[auth].[PRO_LOGIN_SET_PASSWORD]", parameters);
        }
        private static string NewSecurityStamp() => Guid.NewGuid().ToString("N").ToUpperInvariant();
        private static void AddUserId(DynamicParameters Parameters, AuthLoginRequest authLoginRequest)
        {
            Parameters.Add("@ACCOUNT_TYPE", authLoginRequest.AccountType.ToString(), DbType.String);
            Parameters.Add("@USER_CODE", authLoginRequest.UserCode?.Trim(), DbType.String);
        }
        private static void AddUserId(DynamicParameters parameters, AuthLoginRequest authLoginRequest, DateTimeOffset nowUtc)
        {
            AddUserId(parameters, authLoginRequest);                         // reuse the existing one
            parameters.Add("@NOW_UTC", nowUtc, DbType.DateTimeOffset);
        }


    }
}
