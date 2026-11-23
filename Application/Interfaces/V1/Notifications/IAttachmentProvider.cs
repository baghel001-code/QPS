using Domain.Entities.Notifications;
using EmailService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.V1.Notifications
{
    public interface IAttachmentProvider<in TModel>
    {
        List<MailAttachmentModel>? GetAttachments(TModel model);
    }
}
