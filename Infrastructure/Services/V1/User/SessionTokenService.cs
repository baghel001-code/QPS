using Application.Interfaces.V1.User;
using Dapper;
using DocumentFormat.OpenXml.Spreadsheet;
using Domain.Entities.Request;
using Domain.Entities.Response;
using Infrastructure.Utilitys;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.V1.User
{
    public class SessionTokenService : ISessionTokenService
    {
        private readonly DapperHelper _dapperHelper;

        public SessionTokenService(DapperHelper dapperHelper)
        {
            _dapperHelper = dapperHelper;
        }

        public async Task<IssueSessionResponce> IssueNewSessionAsync(IssueSessionRequest issueSessionRequest, CancellationToken ct = default)
        {
            IssueSessionResponce issueSessionResponce = new IssueSessionResponce();
            var parameters = new DynamicParameters();
            parameters.Add("@UserType", issueSessionRequest.UserType, DbType.String);
            parameters.Add("@UserCode", issueSessionRequest.UserCode, DbType.String);
            parameters.Add("@UserId", issueSessionRequest.UserId, DbType.Int32);
            parameters.Add("@SessionToken", issueSessionRequest.SessionToken, DbType.String);
            parameters.Add("@DeviceInfo", issueSessionRequest.DeviceInfo, DbType.String);
            await _dapperHelper.ExecuteStoredProcedureAsync("PRO_USER_SESSIONS", parameters);
            issueSessionResponce.SessionToken = issueSessionRequest.SessionToken;
            return issueSessionResponce;
        }

        public async Task<IssueSessionResponce> IsSessionValidAsync(IssueSessionRequest issueSessionRequest, CancellationToken ct = default)
        {
            IssueSessionResponce issueSessionResponce = new IssueSessionResponce();
            var parameters = new DynamicParameters();
            parameters.Add("@UserType", issueSessionRequest.UserType, DbType.String);
            parameters.Add("@UserCode", issueSessionRequest.UserCode, DbType.String);//@SessionToken
            parameters.Add("@UserId", issueSessionRequest.UserId, DbType.Int32);
            parameters.Add("@SessionToken", issueSessionRequest.SessionToken, DbType.String);

            var res = await _dapperHelper.ExecuteStoredProcedureSingleAsync<SessionResponceModel>("PRO_USER_SESSION_VALIDATE", parameters);
            if (res is null)
            {
                // No row at all (no session ever issued) -> invalid.
                issueSessionResponce.IsValid = false;
                issueSessionResponce.SessionToken = "";
                return issueSessionResponce;
            }

            issueSessionResponce.IsValid = res.IS_VALID;
            issueSessionResponce.SessionToken = res.TOKEN;
            return issueSessionResponce;
        }

        public async Task ClearSessionAsync(IssueSessionRequest issueSessionRequest, CancellationToken ct = default)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@UserType", issueSessionRequest.UserType, DbType.String);
            parameters.Add("@UserCode", issueSessionRequest.UserCode, DbType.String);
            parameters.Add("@UserId", issueSessionRequest.UserId, DbType.Int32);
            await _dapperHelper.ExecuteStoredProcedureAsync("PRO_USER_SESSION_CLEAR", parameters);
        }
    }
}
