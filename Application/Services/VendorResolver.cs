using Application.Interfaces.V1;
using Domain.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class VendorResolver
    {
        private readonly IEnumerable<IVendorSource> _sources;
        public VendorResolver(IEnumerable<IVendorSource> sources) => _sources = sources;

        public async Task<VendorLookupResult> ResolveAsync(string vendorCode)
        {
            VendorLookupResult? last = null;
            foreach (var source in _sources)
            {
                last = await source.TryGetAsync(vendorCode);
                if (last.Found) return last;
            }
            return last ?? new VendorLookupResult(false, null, "No vendor source configured.");
        }
    }
}
