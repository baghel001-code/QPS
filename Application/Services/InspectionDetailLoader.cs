using Application.Interfaces.V1;
using Domain.Entities.Common;
using Domain.Entities.Modals;
using Domain.Entities.Request;
using Domain.Entities.Response;
using Domain.Records;
using MiniMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class InspectionDetailLoader : IInspectionDetailLoader
    {
        private readonly IAPICall _api;
        private readonly IMapper _mapper;
        private readonly IFetchMasterList _fetchMasterList;

        public InspectionDetailLoader(IAPICall api, IMapper mapper, IFetchMasterList fetchMasterList)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _fetchMasterList = fetchMasterList ?? throw new ArgumentNullException(nameof(fetchMasterList));
        }

        public async Task<POHeader?> LoadAsync(InspectionDetailRequest request)
        {
            var res = await _api.CallingAPI<InspectionDetailResponse, SelectListReq>(
                AllApiNames.GET_INSPECTION_REQUEST_BY_ID,
                new SelectListReq { Id = request.InspId, Cmd = CmdNames.Get_InspRequest_By_Id });

            if (res.responseCode != 0 ||
                !string.Equals(res.responseMessage, "SUCCESS", StringComparison.OrdinalIgnoreCase))
                return null;

            var po = _mapper.Map<POHeader>(res.POHeader);
            po.Details = res.PODetails.Select(_mapper.Map<PODetails>).ToList();
            po.AddressList = res.AddressList.Select(_mapper.Map<VendorAddressDetail>).ToList();

            po.RegionList = (await _fetchMasterList.FetchMasterList(
                    request.UseQamRegionList ? CmdNames.Get_Region_With_QAM : CmdNames.Get_Region))
                .masterDataList;

            po.InspectionTypeList = (await _fetchMasterList.FetchMasterList(
                    CmdNames.Get_Inspection_Type,
                    id: request.FilterInspectionTypeByPoNumber ? po.InspId : 0,
                    strField: request.FilterInspectionTypeByPoNumber ? po.PoNumber : ""))
                .masterDataList;

            return po;
        }


    }
}
