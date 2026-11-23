using Application.Interfaces.V1.Notifications;
using Application.Interfaces.V1.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications.Resolvers
{
    public sealed class PpsBuyerRecipientResolver : IRecipientResolver<PpsStatusChangedModel>
    {
        private readonly IUserDirectoryService _users;
        public PpsBuyerRecipientResolver(IUserDirectoryService users) => _users = users;

        public async Task<EmailRecipients> ResolveAsync(PpsStatusChangedModel model, CancellationToken ct = default)
        {
            var result = await _users.GetAllUsersAsync(ct);
            if (result is null)
                return EmailRecipients.None;

            var ids = result?
                .Where(u => u.Emp_Code == model.BuyerId)
                .Select(u => u.Emp_Mail_Id)
                .ToArray()
                ?? Array.Empty<string?>();

            return EmailRecipients.ToOnly(ids);
        }
    }
}
