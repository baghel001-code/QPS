using Application.Interfaces.V1;
using Application.Interfaces.V1.Notifications;
using Application.Interfaces.V1.User;
using Application.Services.User;
using DocumentFormat.OpenXml.Spreadsheet;
using Domain.Entities.Modals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications.Resolvers
{
    public sealed class ASNGeneratedRecipientResolver : IRecipientResolver<AsnGeneratedModel>
    {
        private readonly IUserDirectoryService _users;
        private readonly IVendorLookupService _vendorLookup;
        public ASNGeneratedRecipientResolver(IUserDirectoryService users, IVendorLookupService vendorLookup)
        {
            _users = users;
            _vendorLookup = vendorLookup;
        }


        public async Task<EmailRecipients> ResolveAsync(AsnGeneratedModel model, CancellationToken ct = default)
        {
            var emailSet = new List<string>();

            var buyer = await _users.FindUserByEmpCodeAsync(model.AsnDetails.BuyerCode, ct);
            var vendor = await _vendorLookup.GetByCodeAsync(model.AsnDetails.VendorCode);

            if (buyer.Success && buyer.User?.Emp_Mail_Id is { } empMail)
            {
                emailSet.AddRange(SplitEmails(empMail));
            }

            if (vendor.Found && vendor.Vendor?.VendorAddresses is { } addresses)
            {
                emailSet.AddRange(addresses
                    .SelectMany(a => SplitEmails(a.Email_Ids)));
            }

            return new EmailRecipients
            {
                To = emailSet,
                Cc = Array.Empty<string>(),
            };
        }

        private static IEnumerable<string> SplitEmails(string? raw) =>
            string.IsNullOrWhiteSpace(raw)
                ? Enumerable.Empty<string>()
                : raw.Split(';', StringSplitOptions.RemoveEmptyEntries);
        

    }
}
