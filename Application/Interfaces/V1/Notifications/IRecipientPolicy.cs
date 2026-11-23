using Application.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.V1.Notifications
{
    public interface IRecipientPolicy
    {
        /// <summary>Splits ";"/"," lists, validates, de-duplicates (To > Cc > Bcc) and drops reserved domains.</summary>
        EmailRecipients Filter(EmailRecipients recipients, string notificationName);

    }
}
