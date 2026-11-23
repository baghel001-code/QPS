using Application.Interfaces.V1;
using Domain.Entities.Common;
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
    public class VendorLookupService : IVendorLookupService
    {
        private readonly IAPICall _api;
        private readonly IMapper _mapper;

        public VendorLookupService(IAPICall api, IMapper mapper)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
        public async Task<VendorLookupResult> GetByCodeAsync(string vendorCode)
        {
            if (string.IsNullOrWhiteSpace(vendorCode))
                return new VendorLookupResult(false, null, "Vendor code is required.", DataOrigin.NotFound);

            var vendorRes = await _api.CallingAPI<GetVendorByCode, SelectListReq>(
                AllApiNames.GET_VENDOR_BY_CODE,
                new SelectListReq { Id = 0, Cmd = CmdNames.Get_Vendor_By_Code, StrField = vendorCode });

            if (vendorRes.responseCode != 0 ||
                !string.Equals(vendorRes.responseMessage, "SUCCESS", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrEmpty(vendorRes.VendorModel.VENDOR_CODE))
            {
                return new VendorLookupResult(false, null, vendorRes.responseMessage ?? "Vendor not found.", DataOrigin.NotFound);
            }

            var vendor = _mapper.Map<Vendor>(vendorRes.VendorModel);
            vendor.VendorGUId = Guid.NewGuid();

            var addrRes = await _api.CallingAPI<GetAddressByVendorCode, SelectListReq>(
                AllApiNames.GET_VENDOR_ADDRESS,
                new SelectListReq { Id = vendorRes.VendorModel.VENDOR_ID, Cmd = CmdNames.Get_Vendor_Address });

            vendor.VendorAddresses.Clear();
            if (addrRes.responseCode == 0 && string.Equals(addrRes.responseMessage, "SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var a in addrRes.AddressList)
                {
                    var mapped = _mapper.Map<VendorAddressDetail>(a);
                    mapped.AddressGUId = Guid.NewGuid();
                    vendor.VendorAddresses.Add(mapped);
                }
            }

            return new VendorLookupResult(true, vendor, null, DataOrigin.Local); 
        }
    }
}
