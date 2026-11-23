using Domain.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.V1.Asn
{
    public interface IGetAsn
    {
        Task<AsnLookupResult> TryGetAsync(string poNumber);
    }
}
