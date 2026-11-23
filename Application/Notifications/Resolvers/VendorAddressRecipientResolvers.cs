using Application.Interfaces.V1.Notifications;
using DocumentFormat.OpenXml.Office2010.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications.Resolvers
{
    public sealed class VendorAccountRecipientResolver : IRecipientResolver<VendorAccountModel>
    {
        public Task<EmailRecipients> ResolveAsync(VendorAccountModel model, CancellationToken ct = default)
        {
            var addresses = model.Vendor.VendorAddresses.Select(a => a.Email_Ids).ToArray();
            var all = string.IsNullOrWhiteSpace(model.DefaultEmailId)
                ? addresses
                : new[] { model.DefaultEmailId }.Concat(addresses);

            if (all is null)
                return Task.FromResult(EmailRecipients.None);

            var ids = all?
                .ToArray()
                ?? Array.Empty<string?>();

            return Task.FromResult(EmailRecipients.ToOnly(ids));
        }
    }

    public sealed class ForgotPasswordRecipientResolver : IRecipientResolver<ForgotPasswordModel>
    {
        public Task<EmailRecipients> ResolveAsync(ForgotPasswordModel model, CancellationToken ct = default)
        {
            return Task.FromResult(EmailRecipients.ToOnly(model.Vendor.VendorAddresses.Select(a => a.Email_Ids).ToArray()));

        }

    }

    public sealed class PasswordResetLinkRecipientResolver : IRecipientResolver<PasswordResetLinkModel>
    {
        public Task<EmailRecipients> ResolveAsync(PasswordResetLinkModel model, CancellationToken ct = default)
        {
            return Task.FromResult(EmailRecipients.ToOnly(model.Vendor.Email));
        }

    }
}
