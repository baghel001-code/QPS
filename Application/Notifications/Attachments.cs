using Application.Interfaces.V1.Notifications;
using Domain.Entities.Notifications;
using EmailService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications
{
    public sealed class DailyScheduleAttachmentProvider : IAttachmentProvider<DailyScheduleModel>
    {
        public List<MailAttachmentModel>? GetAttachments(DailyScheduleModel model)
        {
            if (model.Schedule.FileBytes == null || model.Schedule.FileBytes.Length == 0)
            {
                return null;
            }

            return new List<MailAttachmentModel>
                {
                    new MailAttachmentModel
                    {
                        FileName = $"Inspection_Schedule_{DateTime.Now:ddMMyyyy}.xlsx",
                        FileBytes = model.Schedule.FileBytes,
                        ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    }
                };
        }
    }
}
