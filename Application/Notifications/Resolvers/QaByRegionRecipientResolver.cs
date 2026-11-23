using Application.Interfaces.V1.Notifications;
using Application.Interfaces.V1.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications.Resolvers
{
    public sealed class QaByRegionRecipientResolver : IRecipientResolver<InspectionCreatedModel>
    {
        private readonly IUserDirectoryService _users;
        public QaByRegionRecipientResolver(IUserDirectoryService users) => _users = users;

        public async Task<EmailRecipients> ResolveAsync(InspectionCreatedModel model, CancellationToken ct = default)
        {
            var result = await _users.GetAllUsersAsync(ct);
            if (result is null)
                return EmailRecipients.None;

            var ids = result?
                .Where(u => u.Role_Id == 1007 && u.REGION_IDS == model.PO.RegionId.ToString())
                .Select(u => u.Emp_Mail_Id)
                .ToArray()
                ?? Array.Empty<string?>();

            return EmailRecipients.ToOnly(ids);
        }
    }
}
