using Domain.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.V1
{
    public interface IVendorSource
    {
        Task<VendorLookupResult> TryGetAsync(string vendorCode);
    }
}
