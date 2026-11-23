using Domain.Entities.Modals;
using Domain.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.V1
{
    public interface ISapPoFetcher
    {
        Task<SapPoFetchResult> FetchAsync(string poNumber, IProgress<int>? progress = null);
    }
    public interface IPoSource
    {
        Task<PoLookupResult> TryGetAsync(string poNumber);
    }

    public interface IPoSyncService
    {
        Task<PoSyncResult> SyncAsync(POHeader currentPO, int userId, IProgress<int>? progress = null, bool forceSync = false);
    }

   
}
