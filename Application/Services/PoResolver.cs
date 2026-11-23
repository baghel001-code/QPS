using Application.Interfaces.V1;
using Domain.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class PoResolver
    {
        private readonly IEnumerable<IPoSource> _sources;
        public PoResolver(IEnumerable<IPoSource> sources) => _sources = sources;

        public async Task<PoLookupResult> ResolveAsync(string poNumber)
        {
            PoLookupResult? last = null;
            foreach (var source in _sources)
            {
                last = await source.TryGetAsync(poNumber);
                if (last.Found) return last;
            }
            return last ?? new PoLookupResult(false, null, "No PO source configured.");
        }
    }
}
