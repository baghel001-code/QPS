using Application.DTOs.Asn;
using Application.Interfaces.V1;
using Application.Interfaces.V1.Asn;
using Domain.Entities.Common;
using Domain.Entities.Request;
using Domain.Enums;
using Domain.Records;

namespace Application.Services.Asn
{
    public class SAPAsnSource : IGetAsn
    {
        private readonly IAPICall _api;
        public SAPAsnSource(IAPICall api) => _api = api;

        public async Task<AsnLookupResult> TryGetAsync(string poNumber)
        {

            var req = new SAP_API_DATA_Req { Param = poNumber, TaskName = "SAP_PO_WISE_ASN" };
            var sapApiData = await _api.CallingAPI<ApiData, SAP_API_DATA_Req>(AllApiNames.GET_SAP_API!, req)
                .ConfigureAwait(false);

            if (sapApiData is null)
                return new AsnLookupResult(false, null, "SAP_PO_WISE_ASN Api not found.");

            var aslList = await _api.CallingSAPAPI<AsnLookupResult, string>(string.Empty, sapApiData)
                .ConfigureAwait(false);

            if (aslList.ASN_DETAILS is null || !aslList.ASN_DETAILS.Any())
                return new AsnLookupResult(false, null, "ASN not found.");

            return new AsnLookupResult(true, aslList.ASN_DETAILS, null, DataOrigin.Sap);
        }
    }
    public class LocalAsnSource : IGetAsn
    {
        private readonly IAPICall _api;
        public LocalAsnSource(IAPICall api)
        {
            _api = api;
        }

        public async Task<AsnLookupResult> TryGetAsync(string poNumber)
        {
            SummaryRequest summaryRequest = new SummaryRequest() { PO_NUMBER = poNumber };
            var existingAsnList = await _api.CallingAPI<AsnList, SummaryRequest>(AllApiNames.GET_ASN_INSPECTIONS!, summaryRequest);

            if (!existingAsnList.Found || !existingAsnList.ASN_NUMBER.Any())
                return new AsnLookupResult(false, null, "ASN not found.");

            var abc = existingAsnList.ASN_NUMBER
                .Select(asn => new AsnDetail { Asn = asn })
                .ToList();

            return new AsnLookupResult(true, abc, null, DataOrigin.Local);


        }


    }
}
