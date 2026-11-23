using Application.Interfaces.V1;
using Domain.Entities.Common;
using Domain.Entities.Modals;
using Domain.Entities.Request;
using Domain.Entities.Response;
using Domain.Enums;
using Domain.Records;
using MiniMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class SapPoFetcher : ISapPoFetcher
    {
        private readonly IAPICall _api;
        public SapPoFetcher(IAPICall api) => _api = api;

        public async Task<SapPoFetchResult> FetchAsync(string poNumber, IProgress<int>? progress = null)
        {
            progress?.Report(10);

            var req = new SAP_API_DATA_Req { Param = poNumber, TaskName = "SAP_PO_DETAILS" };
            var sapApiData = await _api.CallingAPI<ApiData, SAP_API_DATA_Req>(AllApiNames.GET_SAP_API!, req)
                .ConfigureAwait(false);

            progress?.Report(30);

            // CallingAPI never returns null on failure - it returns an object with empty fields
            if (sapApiData is null || string.IsNullOrWhiteSpace(sapApiData.Api_Url))
                return new SapPoFetchResult(false, null,
                    "SAP PO API (SAP_PO_DETAILS) is not configured or not reachable. Existing PO details are retained.",
                    PoSyncIssue.SapUnavailable);

           

            var poDetails = await _api.CallingSAPAPI<POSAPResponse, string>(string.Empty, sapApiData)
                .ConfigureAwait(false);

            progress?.Report(60);

            if (poDetails is null)
                return new SapPoFetchResult(false, null,
                    "SAP did not return a response. Existing PO details are retained.", PoSyncIssue.SapUnavailable);

            // CallingSAPAPI converts every exception into responseCode 1001-1004 (N3)
            if (poDetails.responseCode != 0)
                return new SapPoFetchResult(false, null,
                    $"SAP is not reachable ({poDetails.responseCode}: {poDetails.responseMessage}). Existing PO details are retained.",
                    PoSyncIssue.SapUnavailable);

            if (poDetails.APPS_FOOTWEAR_DETAILS is null || !poDetails.APPS_FOOTWEAR_DETAILS.Any())
                return new SapPoFetchResult(false, null,
                    $"PO {poNumber} was not found in SAP. It may have been cancelled or closed.", PoSyncIssue.NotFoundInSap);

            POHeader po;
            try
            {
                po = new POHeader { PoNumber = poNumber }.MapToHeader(poDetails);
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException || ex is InvalidCastException)
            {
                return new SapPoFetchResult(false, null,
                    $"SAP returned invalid data for PO {poNumber} ({ex.Message}). Existing PO details are retained.",
                    PoSyncIssue.InvalidSapData);
            }
            progress?.Report(70);

            return new SapPoFetchResult(true, po, null);
        }
    }
    public class LocalPoSource : IPoSource
    {
        private readonly IAPICall _api;
        private readonly IMapper _mapper;

        public LocalPoSource(IAPICall api, IMapper mapper)
        {
            _api = api;
            _mapper = mapper;
        }

        public async Task<PoLookupResult> TryGetAsync(string poNumber)
        {
            var selectRequest = new SelectListReq
            {
                Id = 0,
                Cmd = CmdNames.Get_PO_Details,
                StrField = poNumber
            };

            var existingPO = await _api.CallingAPI<POResponse, SelectListReq>(AllApiNames.GET_PO_DETAILS, selectRequest);

            if (existingPO.responseCode != 0 ||
                !string.Equals(existingPO.responseMessage, "SUCCESS", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrEmpty(existingPO.POHeader.PO_NUMBER))
            {
                return new PoLookupResult(false, null, "PO not found.", DataOrigin.NotFound);
            }

            var po = _mapper.Map<POHeader>(existingPO.POHeader);
            po.Details.Clear();
            foreach (var podetail in existingPO.PODetails)
                po.Details.Add(_mapper.Map<PODetails>(podetail));

            return new PoLookupResult(true, po, null, DataOrigin.Local);
        }


    }
    public class SapPoSource : IPoSource
    {
        private readonly ISapPoFetcher _fetcher;
        public SapPoSource(ISapPoFetcher fetcher) => _fetcher = fetcher;

        public async Task<PoLookupResult> TryGetAsync(string poNumber)
        {
            var result = await _fetcher.FetchAsync(poNumber); 

            if (!result.Success)
                return new PoLookupResult(false, null, result.ErrorMessage, DataOrigin.NotFound);

            result.PO!.Insp_Req_Status = "Draft";
            return new PoLookupResult(true, result.PO, null, DataOrigin.Sap);
        }
    }
    public class PoSyncService : IPoSyncService
    {
        private const string AlreadySynced = PoSyncMessages.AlreadySynced;

        private readonly ISapPoFetcher _fetcher;
        private readonly IAPICall _api;

        public PoSyncService(ISapPoFetcher fetcher, IAPICall api)
        {
            _fetcher = fetcher;
            _api = api;
        }

        public async Task<PoSyncResult> SyncAsync(POHeader currentPO, int userId, IProgress<int>? progress = null, bool forceSync = false)
        {
            if (string.IsNullOrWhiteSpace(currentPO.PoNumber))
                return new PoSyncResult(false, "PO number is required.", PoSyncIssue.NotSavedInQps);

            if (currentPO.Id <= 0)
                return new PoSyncResult(false,
                    $"PO {currentPO.PoNumber} is not yet saved in QPS, so it cannot be synchronized.", PoSyncIssue.NotSavedInQps);

            var fetchResult = await _fetcher.FetchAsync(currentPO.PoNumber, progress);
            if (!fetchResult.Success)
                return new PoSyncResult(false, fetchResult.ErrorMessage ?? "PO not found in SAP.", fetchResult.Issue);

            var fetchedPO = fetchResult.PO!;

            // N2 - never overwrite with zero quantity
            if (fetchedPO.POTotalQty <= 0)
                return new PoSyncResult(false,
                    $"SAP returned zero quantity for PO {currentPO.PoNumber}. Sync stopped and the existing details are retained.",
                    PoSyncIssue.ZeroQuantity);

            // N8 - vendor reassigned in SAP: do not update silently
            if (!string.IsNullOrWhiteSpace(currentPO.VendorCode) &&
                !string.Equals(currentPO.VendorCode.Trim(), (fetchedPO.VendorCode ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
                return new PoSyncResult(false,
                    $"The vendor of PO {currentPO.PoNumber} has changed in SAP ({currentPO.VendorCode} to {fetchedPO.VendorCode}). " +
                    "Sync stopped. Please contact the QA Admin.",
                    PoSyncIssue.VendorChanged);

            fetchedPO.Insp_Req_Status = "Update";
            fetchedPO.CreatedBy = userId;
            fetchedPO.Id = currentPO.Id;

            var changedFields = currentPO.GetChangedFields(fetchedPO);

            if (!forceSync && changedFields.Count == 0)
            {
                progress?.Report(100);
                return new PoSyncResult(true, AlreadySynced);
            }

            var res = await _api.CallingAPI<GenRes, POHeader>(AllApiNames.SYNC_PO_DETAILS!, fetchedPO);
            progress?.Report(90);

            if (res is null || res.responseCode != 0)
                return new PoSyncResult(false, $"PO has not been synced. {res?.responseMessage}".Trim(), PoSyncIssue.SaveFailed);

            currentPO.UpdateNewValues(fetchedPO);
            progress?.Report(100);

            var message = forceSync || changedFields.Count == 0
                ? "PO details synchronized successfully."
                : $"PO details synchronized successfully. Changed: {string.Join(", ", changedFields)}.";

            bool hasQtyWarning = !string.IsNullOrWhiteSpace(res.responseMessage) &&
                                 res.responseMessage.StartsWith("WARNING:", StringComparison.OrdinalIgnoreCase);

            if (hasQtyWarning)
                return new PoSyncResult(true, $"{message}{Environment.NewLine}{res.responseMessage}", PoSyncIssue.QtyWarning);

            return new PoSyncResult(true, message);
        }
    }

    // Public static helper used by the UI to recognise the "nothing changed" result
    public static class PoSyncMessages
    {
        public const string AlreadySynced = "PO details are already synchronized.";
    }


}
