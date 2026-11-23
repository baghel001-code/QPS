using Domain.Entities.Notifications;
using Domain.Entities.Request;
using Domain.Entities.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.V1
{
    public interface IEmailManager
    {

        Task<GenRes> CreateDailyEmail();
        Task<GetAllRecipients> GetPendingEmails();
        Task<GenRes> UpDateStatus(SelectListReq obj);
       
    }
}
