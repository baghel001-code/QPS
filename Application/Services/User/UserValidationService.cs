using Application.Interfaces.V1;
using Application.Interfaces.V1.User;
using DocumentFormat.OpenXml.Spreadsheet;
using Domain.Entities.Common;
using Domain.Entities.Modals;
using Domain.Entities.Request;
using Domain.Entities.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.User
{
    public class UserValidationService : IUserValidationService
    {
        private readonly IAPICall _api;

        public UserValidationService(IAPICall api)
        {
            _api = api;
        }

        public async Task ClearSessionAsync(IssueSessionRequest issueSessionRequest, CancellationToken ct = default)
        {
            await _api.CallingAPI<bool, IssueSessionRequest>(AllApiNames.CLEAR_USER_SESSION, issueSessionRequest);
        }

        public async Task<bool> IsSessionValidAsync(IssueSessionRequest issueSessionRequest, CancellationToken ct = default)
        {
            var res = await _api.CallingAPI<IssueSessionResponce, IssueSessionRequest>(AllApiNames.VALIDATE_USER_SESSION, issueSessionRequest);
            return res.IsValid; 
        }

        public async Task<string> IssueNewSessionAsync(IssueSessionRequest issueSessionRequest, CancellationToken ct = default)
        {
            var token = await _api.CallingAPI<IssueSessionResponce, IssueSessionRequest>(AllApiNames.ACTIVATE_USER_SESSION, issueSessionRequest);
            return token.SessionToken;
        }
        public async Task<bool> HasActiveSessionAsync(IssueSessionRequest issueSessionRequest, CancellationToken ct = default)
        {
            var res = await _api.CallingAPI<IssueSessionResponce, IssueSessionRequest>(AllApiNames.VALIDATE_USER_SESSION, issueSessionRequest);
            return !string.IsNullOrEmpty(res.SessionToken) && !res.IsValid;
            
        }

        public async Task<bool> IsUserActiveAsync(string empCode, CancellationToken cancellationToken)
        {
            SelectListReq selectListReq = new SelectListReq
            {
                Id = 0,
                StrField = empCode,
                Cmd = CmdNames.Get_User_Details_On_User_Code
            };
            var res = await _api.CallingAPI<GetUserOnEmpCodeRes, SelectListReq>(AllApiNames.SELECT_USER_ON_EMP_CODE, selectListReq);

            if (res.responseCode == 0)
            {
                return res.userDetails != null ? res.userDetails.Id == selectListReq.Id && res.userDetails.Emp_Status == true : false;
            }
            return false;

        }
    }
}
