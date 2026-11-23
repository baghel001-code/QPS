using Application.Interfaces.V1.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications.Templates
{
    public sealed class DailyScheduleTemplate : IEmailTemplate<DailyScheduleModel>
    {
        public string BuildSubject(DailyScheduleModel m) => $"All India Inspection Schedule for {DateTime.Now:dd-MMM-yyyy}";

        public string BuildBody(DailyScheduleModel m) =>
            "Dear team,<br/><br/>" +
            $"Please find attached the All India Inspection Schedule for {DateTime.Now:dd-MMM-yyyy} for your reference.<br/></br>" +
            EmailFooter.Footer(EmailFooter.DailyScheduleNote);
    }
}
