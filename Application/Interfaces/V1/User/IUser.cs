using Domain.Entities.Base;
using Domain.Entities.Modals;
using Domain.Entities.Request;
using Domain.Entities.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.V1.User
{
    public interface IUser
    {
        public Task<CopLoginRes> CopLogin(CopLoginReq req);
        public Task<GenRes> SetUser(UserDetails req);
        public Task<GetAllUsersRes> GetAllUsers();
        public Task<GetAllUsersRes> GetAllUsers(SelectListReq req);
        public Task<GetUserOnEmpCodeRes> GetUserOnEmpCode(SelectListReq req);
        public Task<CopLoginRes> GetLoggedInUserOnEmpCode(SelectListReq req);
       
    }
}
