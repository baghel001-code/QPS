using Domain.Entities.Modals;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Records
{
    public enum PoSyncIssue
    {
        None = 0,
        NotFoundInSap,     // N1 - PO cancelled / closed / deleted in SAP
        SapUnavailable,    // N3 - timeout, HTTP error, auth, proxy, config
        InvalidSapData,    // N4 - non-numeric quantity / MRP etc.
        ZeroQuantity,      // N2
        VendorChanged,     // N8
        NotSavedInQps,     // N6
        SaveFailed,        // SP / API error
        QtyWarning         // Sync applied, but open requests exceed the new 110% limit
    }

    public record PoLookupResult(bool Found, POHeader? PO, string? ErrorMessage, DataOrigin Origin = DataOrigin.NotFound);
    public record PoSyncResult(bool Success, string Message, PoSyncIssue Issue = PoSyncIssue.None);
    public record SapPoFetchResult(bool Success, POHeader? PO, string? ErrorMessage, PoSyncIssue Issue = PoSyncIssue.None);

}
