using Application.Interfaces.V1;
using Application.Interfaces.V1.User;
using DocumentFormat.OpenXml.Drawing.Charts;
using Domain.Entities.Modals;
using Domain.Entities.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace QPSApi.Controllers.V1
{
    [Route("api/v1/[Action]")]
    [ApiController]
    public class UserController(IUser _User, ISessionTokenService _SessionTokenService, IUserAuthServices _authServices) : ControllerBase
    {

        [HttpPost]
        public async Task<IActionResult> CopLogin(CopLoginReq req)
        {
            var res = await _User.CopLogin(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }
        [HttpPost]
        public async Task<IActionResult> GetAllUsers(SelectListReq req)
        {
            var res = await _User.GetAllUsers(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }
        [HttpPost]
        public async Task<IActionResult> GetUserOnEmpCode(SelectListReq req)
        {
            var res = await _User.GetUserOnEmpCode(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }
        [HttpPost]
        public async Task<IActionResult> SetUser(UserDetails req)
        {
            var res = await _User.SetUser(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }
        [HttpPost]
        public async Task<IActionResult> GetLoggedInUserOnEmpCode(SelectListReq req)
        {
            var res = await _User.GetLoggedInUserOnEmpCode(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> Set_User_Session(IssueSessionRequest req)
        {
            var res = await _SessionTokenService.IssueNewSessionAsync(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }
        [HttpPost]
        public async Task<IActionResult> Validate_User_Session(IssueSessionRequest req)
        {
            var res = await _SessionTokenService.IsSessionValidAsync(req);
            return Ok(res);
        }
        [HttpPost]
        public async Task<IActionResult> Clear_User_Session(IssueSessionRequest req)
        {
            await _SessionTokenService.ClearSessionAsync(req);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> FindUserByLogin(AuthLoginRequest authLoginRequest)
        {
            var res = await _authServices.FindByLoginAsync(authLoginRequest);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        //-------------------

        [HttpPost]
        public async Task<IActionResult> RecordFailedLogin(AuthLoginRequest authLoginRequest)
        {
            var res = await _authServices.RecordFailedLoginAsync(authLoginRequest);
            return Ok(res);
        }
        [HttpPost]
        public async Task<IActionResult> LockOut(AuthLoginRequest authLoginRequest)
        {
            await _authServices.LockOutAsync(authLoginRequest);

            return Ok(true);
        }
        [HttpPost]
        public async Task<IActionResult> ClearLockout(AuthLoginRequest authLoginRequest)
        {
            await _authServices.ClearLockoutAsync(authLoginRequest);
            return Ok(true);
        }
        [HttpPost]
        public async Task<IActionResult> SetPwdHash(AuthLoginRequest authLoginRequest)
        {
            await _authServices.SetPasswordHashAsync(authLoginRequest);
            return Ok(true);
        }
        [HttpPost]
        public async Task<IActionResult> SavePwdRstTkn(AuthLoginRequest authLoginRequest)
        {
            await _authServices.SavePasswordResetTokenAsync(authLoginRequest);
            return Ok(true);
        }
        [HttpPost]
        public async Task<IActionResult> FindUserByPwdRstTkn(AuthLoginRequest authLoginRequest)
        {
            var res = await _authServices.FindUserIdByPasswordResetTokenAsync(authLoginRequest);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }
        [HttpPost]
        public async Task<IActionResult> CosumePwdRstTkn(AuthLoginRequest authLoginRequest)
        {
            var res = await _authServices.ConsumePasswordResetTokenAsync(authLoginRequest);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }
    }
}
