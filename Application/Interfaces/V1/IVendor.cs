using Domain.Entities.Modals;
using Domain.Entities.Request;
using Domain.Entities.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.V1
{
    public interface IVendor
    {
        public Task<GenRes> SetVendor(Vendor req);
        public Task<GetAllVendors> GetAllVendors();
        public Task<GetAddressByVendorCode> GetAddressOnVendorCode(SelectListReq req);
        public Task<GetVendorByCode> GetVendorByID(SelectListReq req);

        //public Task<PODashboardResponse> GetAllVendorWisePOs(SelectListReq req);
        public Task<POResponse> GetPODetails(SelectListReq req);

        public Task<GenRes> SaveInspectionRequest(POHeader req);
        public Task<InspectionDetailResponse> GetInspectionRequestByID(SelectListReq req);
        public Task<MasterDataList> GetMasterData(SelectListReq req);
        public Task<GenRes> AddAddress(Vendor req);
        public Task<GenRes> UpdateDOI(UpdateDOIRequest req);
        public Task<GenRes> AssignInspector(AssignInspectorRequest req);
        public Task<GenRes> SaveInspection(InspectionModel req);
        public Task<InspectResponse> GetInspectDataByID(SelectListReq req);
        public Task<GenRes> UpdateOfferedQty(UpdateDOIRequest req);
        public Task<DashboardModelResponse> GetDashboardData(SelectListReq obj);

        public Task<AssignPendingInspectionListResponse> GetInspectionsListByIds(string Ids);

        public Task<AssignPendingInspectionListResponse> GetQALISTTOMAIL(SelectListReq req);

        public Task<GenRes> FreezQA(AssignInspectorFreezeRequest obj);

        public Task<GenRes> UploadVendorExcel(VendorExcel req);

        public Task<GetVendorExcelData> GetVendorExcelData(SelectListReq req);

        public Task<GenRes> UpdateCMStatus(UpdateCM_Status_Details req);
        public Task<GenRes> UpdateLinkPO(UpdateLinkPO_Details req);
        //public Task<byte[]> ExportToExcel(SelectListReq Obj);

        public Task<GenRes> SyncPOdetailsService(POHeader req);

        public Task<byte[]> InspectionExportToExcel(SelectListReq Obj);

        public Task<HierarchyListResponse> GetHierarchyList(SelectListReq Ids);

        public Task<GenRes> AddUpdateHierarchy(HierarchyDto Obj);
        public Task<EmailManagementResponse> GetEmailMgtList(SelectListReq Ids);
        public Task<GenRes> AddUpdateEmailMgt(EmailManagementDto Obj);

        public Task<MasterColorCodeResponse> GetAllColorCodeList(SelectListReq Ids);
        public Task<GenRes> AddUpdateColorCode(MasterColorCodeDto Obj);

        public Task<InspectionQueryResult> GenerateInspectionPdfAsync(SelectListReq req);
        public Task<ReceivedPendingOngoingInspections> GetAllRecPenOngInspections(InspectionSearchFilterMultiSelection req);

        public Task<byte[]> RecPenOngInspectionsExportToExcel(InspectionSearchFilterMultiSelection req);
    }
}
