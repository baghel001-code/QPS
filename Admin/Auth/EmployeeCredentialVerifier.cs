using Admin.Auth;
using Application.Interfaces.V1;
using Application.Interfaces.V1.User;
using Application.Services.User;
using Domain.Entities.Common;
using Domain.Entities.Request;
using Domain.Entities.Response;
using Domain.Enums;
using Domain.Records;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using System.Net;
using System.Threading;
using ZXing;

namespace Admin.Auth
{
    /// <summary>
    /// Employees: the password is checked by the company employee API (<see cref="IEmployeeAuthApi"/>).
    /// QPS stores no employee password; the QPS user row only supplies roles, modules and status.
    /// </summary>
    public sealed class EmployeeApiVerifier(IAPICall api,  ILogger<EmployeeApiVerifier> logger) : ICredentialVerifier
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

        public AccountType AccountType => AccountType.Employee;
        public async Task<CredentialResult> VerifyAsync(AuthUser user, string password, CancellationToken ct)
        {

            try
            {

                HrpLoginReq? Input = new() { loginID = user.UserCode, loginPassword = password, };
                var loginResponse = await api.CallingAPI<HrpLoginRes, HrpLoginReq>(AllApiNames.HRP_GET_EMP_LOGIN_API, Input!);

                if (loginResponse is null)
                    return CredentialResult.Invalid;
               
                if (loginResponse.responseCode == 0 && loginResponse!.responseMessage!.ToLower() == "success" && loginResponse.employeeList?.Count > 0)
                {
                    // Only accept the answer if it is about the account being signed in to.
                    if (!string.Equals(loginResponse.employeeList[0].empCode?.Trim(), user.UserCode, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogWarning("Employee API returned {Returned} for {UserId}; rejecting", loginResponse.employeeList[0].empCode, user.UserCode);
                        return CredentialResult.Invalid;
                    }

                    CopLoginReq copLoginReq = new CopLoginReq();
                    copLoginReq.UserDetails.Emp_Code = loginResponse.employeeList[0].empCode;
                    copLoginReq.UserDetails.Emp_Name = loginResponse.employeeList[0].empName;
                    copLoginReq.UserDetails.Emp_Location = loginResponse.employeeList[0].empLocation;
                    copLoginReq.UserDetails.Emp_Loc_Code = loginResponse.employeeList[0].empLocCode;
                    copLoginReq.UserDetails.Emp_Dept_Name = loginResponse.employeeList[0].empDeptName;
                    copLoginReq.UserDetails.Emp_Desig = loginResponse.employeeList[0].empDesig;
                    copLoginReq.UserDetails.Emp_Contact_No = loginResponse.employeeList[0].empContactNo;
                    copLoginReq.UserDetails.Emp_Mail_Id = loginResponse.employeeList[0].empMailID;
                    copLoginReq.UserDetails.Emp_Rpt_Mng = loginResponse.employeeList[0].empRptMng;
                    copLoginReq.UserDetails.Is_Resign = loginResponse.employeeList[0].isResign;
                    copLoginReq.UserDetails.Emp_Grade = loginResponse.employeeList[0].empGrade;
                    copLoginReq.UserDetails.Emp_Grade_Desc = loginResponse.employeeList[0].empGradeDesc;
                    copLoginReq.UserDetails.Emp_Status = loginResponse.employeeList[0].empStatus;

                    var copLoginRes = await api.CallingAPI<CopLoginRes, CopLoginReq>(AllApiNames.GET_EMP_COP_LOGIN_API, copLoginReq);

                    if (copLoginRes.responseCode == 0 && copLoginRes!.responseMessage!.ToLower() == "success")
                    {
                        return CredentialResult.Valid;

                    }
                    else
                    {
                        logger.LogError("Employee authentication API failed for {UserId} : {err}", user.Id, loginResponse.responseMessage);
                        return CredentialResult.Invalid;
                    }
                }
                else  //104 : Maximum login attempts limit exceeded, account is locked for 28 minutes. Please visit MyVishal HR Portal => My Page=> Unlock User to unlock yourself immediately.
                {
                    if (loginResponse.responseCode == 104)
                    {
                        logger.LogError("Employee authentication API failed for {UserId} : {err}", user.Id, loginResponse.responseMessage);
                        return CredentialResult.MaxAttemp;
                    }
                    else
                    {
                        logger.LogError("Employee authentication API failed for {UserId} : {err}", user.Id, loginResponse.responseMessage);
                        return CredentialResult.Invalid;
                    }

                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;   // the page went away; not an API problem
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Employee authentication API failed for {UserId}", user.Id);
                return CredentialResult.Unavailable;
            }
        }
    }
}