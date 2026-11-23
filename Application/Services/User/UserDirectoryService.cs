using Application.Interfaces.V1;
using Application.Interfaces.V1.User;
using Domain.Entities.Common;
using Domain.Entities.Modals;
using Domain.Entities.Request;
using Domain.Entities.Response;
using Domain.Enums;
using Domain.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.User
{
    public sealed class UserDirectoryService(IAPICall _api) : IUserDirectoryService
    {

        public async Task<List<UserDetails>> GetAllUsersAsync(CancellationToken ct = default)
        {
            try
            {
                var req = new SelectListReq { Id = 0, Cmd = CmdNames.Get_All_Users };
                var res = await _api.CallingAPI<GetAllUsersRes, SelectListReq>(AllApiNames.GET_ALL_USERS, req);
                if (res.responseCode != 0)
                {
                    throw new InvalidOperationException(res.responseMessage ?? "Failed to load users.");
                }
                return res.UserDetailsList;
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

        public async Task<UserLookupResult> FindUserByEmpCodeAsync(string empCode, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(empCode))
            {
                return new UserLookupResult
                {
                    Success = false,
                    Found = false,
                    ErrorMessage = "Please enter a valid Emp Code."
                };
            }

            try
            {
                // 1. Try CO Portal
                var req = new SelectListReq
                {
                    Id = 0,
                    StrField = empCode,
                    Cmd = CmdNames.Get_User_Details_On_User_Code
                };
                var res = await _api.CallingAPI<GetUserOnEmpCodeRes, SelectListReq>(AllApiNames.SELECT_USER_ON_EMP_CODE, req);

                if (res.responseCode != 0)
                {
                    return new UserLookupResult
                    {
                        Success = false,
                        Found = false,
                        ErrorMessage = res.responseMessage ?? "Failed to look up user on CO Portal."
                    };
                }

                var result = res.userDetails;

                var needsHrFallback = result.Id.Equals(0) || string.IsNullOrEmpty(result.Emp_Name) || string.IsNullOrEmpty(result.Emp_Loc_Code);

                if (!needsHrFallback)
                {
                    return new UserLookupResult { Success = true, Found = true, User = result };
                }

                // 2. Not available (or incomplete) on CO Portal — fall back to HR Portal
                try
                {
                    var hrpReq = new HrpLoginByCodeReq { loginID = empCode };
                    var hrRes = await _api.CallingAPI<HrpLoginRes, HrpLoginByCodeReq>(AllApiNames.HRP_GET_EMP_DETAIL_CODE, hrpReq);

                    if (hrRes.responseCode == 0 && hrRes.responseMessage == "Success" && hrRes.employeeList?.Count > 0)
                    {
                        var emp = hrRes.employeeList[0];
                        var user = new UserDetails
                        {
                            Emp_Code = emp.empCode,
                            Emp_Name = emp.empName,
                            Emp_Location = emp.empLocation,
                            Emp_Loc_Code = emp.empLocCode,
                            Emp_Dept_Name = emp.empDeptName,
                            Emp_Desig = emp.empDesig,
                            Emp_Contact_No = emp.empContactNo,
                            Emp_Mail_Id = emp.empMailID,
                            Emp_Rpt_Mng = emp.empRptMng,
                            Is_Resign = emp.isResign,
                            Emp_Grade = emp.empGrade,
                            Emp_Grade_Desc = emp.empGradeDesc,
                            Emp_Status = emp.empStatus
                        };
                        return new UserLookupResult { Success = true, Found = true, User = user };
                    }

                    return new UserLookupResult { Success = true, Found = false, User = new UserDetails() };
                }
                catch (Exception hrEx)
                {
                    return new UserLookupResult
                    {
                        Success = false,
                        Found = false,
                        ErrorMessage = $"User not found on CO Portal, and HR Portal lookup failed: {hrEx.Message}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new UserLookupResult
                {
                    Success = false,
                    Found = false,
                    ErrorMessage = $"An unexpected error occurred while looking up Emp Code '{empCode}': {ex.Message}"
                };
            }
            finally
            {
                // Central hook for cross-cutting concerns (logging, metrics, etc.)
            }
        }
       
    }
}
