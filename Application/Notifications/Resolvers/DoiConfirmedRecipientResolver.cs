using Application.Interfaces.V1;
using Application.Interfaces.V1.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications.Resolvers
{
    public sealed class DoiConfirmedRecipientResolver : IRecipientResolver<DoiConfirmedModel>
    {
        private readonly IVendorLookupService _vendorLookup; // you already have this
        public DoiConfirmedRecipientResolver(IVendorLookupService vendorLookup) => _vendorLookup = vendorLookup;

        public async Task<EmailRecipients> ResolveAsync(DoiConfirmedModel model, CancellationToken ct = default)
        {
            var result = await _vendorLookup.GetByCodeAsync(model.Row.VENDOR_CODE);
            if (!result.Found || result.Vendor is null)
                return EmailRecipients.None;

            var ids = result.Vendor.VendorAddresses?
                          .Select(a => a.Email_Ids)
                          .ToArray()
                      ?? Array.Empty<string?>();

            return EmailRecipients.ToOnly(ids);
        }
    }
}
