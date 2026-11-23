using Application.Interfaces.V1;
using Domain.Entities.Common;
using Domain.Entities.Request;
using Domain.Entities.Response;

namespace Application.Services
{
    public class PODownloadService
    {
        private readonly IAPICall _api;

        public PODownloadService(IAPICall api)
        {
            _api = api;
        }

        public async Task<(byte[] Bytes, string Filename)> TryGetPOPdfAsync(string poNumber)
        {

            var req = new SAP_API_DATA_Req { Param = poNumber, TaskName = "SAP_PO_DOWNLOAD" };
            var sapApiData = await _api.CallingAPI<ApiData, SAP_API_DATA_Req>(AllApiNames.GET_SAP_API!, req).ConfigureAwait(false);

            if (sapApiData is null)
                throw new InvalidOperationException($"SAP PO DOWNLOAD API NOT FOUND.");

            var result = await _api.CallingSAPAPI<SapPoPdfResponse, string>(string.Empty, sapApiData).ConfigureAwait(false);
            
            if (result is null || result.Status != "success")
                throw new InvalidOperationException($"SAP returned failure status for PO {poNumber}.");

            if (string.IsNullOrWhiteSpace(result.PdfBase64))
                throw new InvalidOperationException("SAP response has empty pdf_base64.");

            byte[] pdfBytes = Convert.FromBase64String(result.PdfBase64);

            return (pdfBytes, result.Filename);


        }


    }
}
