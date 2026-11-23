using Application.Interfaces.V1.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications.Resolvers
{
    // For the daily schedule, the recipient is already on the model — no lookup needed.
    public sealed class DailyScheduleRecipientResolver : IRecipientResolver<DailyScheduleModel>
    {
        public Task<EmailRecipients> ResolveAsync(DailyScheduleModel model, CancellationToken ct = default)
        {
            return Task.FromResult( new EmailRecipients
            {
                To = model.Schedule.RECIPIENT.Split(";"),
                Cc = Array.Empty<string>(),
            });
        }
            
    }
}
