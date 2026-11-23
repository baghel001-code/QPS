using Application.Interfaces.V1.Notifications;
using DocumentFormat.OpenXml.ExtendedProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications.Resolvers
{
    public sealed class TestMailRecipientResolver : IRecipientResolver<TestMailModel>
    {
        public Task<EmailRecipients> ResolveAsync(TestMailModel m, CancellationToken ct = default)
        {
            return Task.FromResult(new EmailRecipients
            {
                To = new[] { m.Email, "dinesh.kumar2@vishalwholesale.co.in" },
                Cc = new[] { "er.dbkumar@gmail.com" },
                Bcc = new[] { "sdp.baghel001@gmail.com" }
            });
        }

    }
}
