using Application.Interfaces.V1;
using Domain.Entities.Request;
using Microsoft.AspNetCore.Mvc;

using Domain.Entities.Modals;
using Domain.Entities.Response;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace QPSApi.Controllers.V1
{
    [Route("api/v1/[Action]")]
    [ApiController]
    public class EmailServiceManagerController : ControllerBase
    {
        private readonly IEmailManager _emailManager;
        public EmailServiceManagerController(IEmailManager emailManager)
        {
            _emailManager = emailManager;
        }

        [HttpPost]
        public async Task<IActionResult> CreateDailyEmail()
        {
            var res = await _emailManager.CreateDailyEmail();
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> GetPendingEmails()
        {
            var res = await _emailManager.GetPendingEmails();
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public  async Task<IActionResult> UpdateStatus(SelectListReq obj)
        {
            var res = await  _emailManager.UpDateStatus(obj);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }


    }
}
