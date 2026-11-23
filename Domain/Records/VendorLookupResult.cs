using Domain.Entities.Response;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Records
{
    public record VendorLookupResult(bool Found, Vendor? Vendor, string? ErrorMessage, DataOrigin Origin = DataOrigin.NotFound);
}
