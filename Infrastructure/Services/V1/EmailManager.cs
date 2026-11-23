using Application.Interfaces.V1;
using Dapper;
using Domain.Entities.Notifications;
using Domain.Entities.Request;
using Domain.Entities.Response;
using Infrastructure.Utilitys;



namespace Infrastructure.Services.V1
{
    public class EmailManager : IEmailManager
    {
        private readonly DapperHelper _dapperHelper;

        public EmailManager(DapperHelper dapperHelper)
        {
            _dapperHelper = dapperHelper;

        }

        public async Task<GenRes> CreateDailyEmail()// db work
        {
            var Res = new GenRes();
            await _dapperHelper.ExecuteStoredProcedureAsync("CREATEDAILYEMAIL");
            return Res;
        }
        public async Task<GetAllRecipients> GetPendingEmails()
        {
            var Res = new GetAllRecipients();
            try
            {
                var output = await _dapperHelper.ExecuteStoredProcedureListAsync<EmailRecipient>("GETPENDINGEMAILS");
                Res.RecipientList = (List<EmailRecipient>)output;
            }
            catch
            {
                return Res;
            }
            return Res;
        }

        public async Task<GenRes> UpDateStatus(SelectListReq obj)
        {
            var Res = new GenRes();
            var headerParams = new DynamicParameters();
            headerParams.Add("@Id", obj.Id);
            headerParams.Add("@Error", obj.StrField);
            headerParams.Add("@CMD", obj.Cmd);

            var ResSet = await _dapperHelper.ExecuteStoredProcedureAsync("MARK_STATUS", headerParams);

            if (headerParams.ParameterNames.Contains("ERROR_CODE"))
            {
                int errorCode = headerParams.Get<int?>("@ERROR_CODE") ?? 0;
                if (errorCode != 0)
                {
                    Res.responseCode = errorCode;
                    Res.responseMessage = headerParams.Get<string>("@ERROR_MESSAGE");
                }
            }

            return Res;
        }
    }
}
