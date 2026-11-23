using Application.Interfaces.V1;
using Domain.Entities.Common;
using Domain.Entities.Modals;
using Domain.Entities.Request;
using Domain.Entities.Response;
using Domain.Enums;
using Domain.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class LocalVendorSource : IVendorSource
    {
        private readonly IVendorLookupService _lookup;
        public LocalVendorSource(IVendorLookupService lookup) => _lookup = lookup;
        public Task<VendorLookupResult> TryGetAsync(string vendorCode) => _lookup.GetByCodeAsync(vendorCode);
    }
    public class SapVendorSource : IVendorSource
    {
        private readonly IAPICall _api;
        public SapVendorSource(IAPICall api) => _api = api;

        public async Task<VendorLookupResult> TryGetAsync(string vendorCode)
        {
            var sapApiData = await _api.CallingAPI<ApiData, SAP_API_DATA_Req>(
                AllApiNames.GET_SAP_API!,
                new SAP_API_DATA_Req { Param = vendorCode, TaskName = "SAP_VENDOR_DETAILS" });

            if (sapApiData is null)
                return new VendorLookupResult(false, null, "SAP_VENDOR_DETAILS Api not found.", DataOrigin.NotFound);

            var vendorDetails = await _api.CallingSAPAPI<VendorResponse, string>(string.Empty, sapApiData);
            var dto = vendorDetails.VendorDetails?.FirstOrDefault();

            if (dto is null)
                return new VendorLookupResult(false, null, "Vendor not found.", DataOrigin.NotFound);

            var vendor = new Vendor
            {
                VendorCode = dto.VendorCode,
                VendorName = dto.VendorName,
                GSTNo = dto.GSTNo,
                VendorGUId = Guid.NewGuid()
            };
            vendor.VendorAddresses.Add(new VendorAddressDetail
            {
                AddressLine1 = dto.Address,
                City = dto.City,
                PinCode = dto.PostCode,
                Region = dto.Region,
                RegionDescription = dto.RegionDescription,
                Mobile_Numbers = $"{dto.TelephoneNumber}; {dto.TelephoneNumber2}; {dto.TelephoneNumber3}",
                IsDefault = true,
                AddressGUId = Guid.NewGuid()
            });

            return new VendorLookupResult(true, vendor, null, DataOrigin.Sap); 
        }
    }
}
