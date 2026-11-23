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
    public interface IVendorAudit
    {
        public Task<GenRes> CreateUpdateVendorAuditRequest(VendorAuditFormModel req);
        public Task<GetFactoryAuditListResponse> GetAllVendorAuditRequest(FactoryAuditListRequest ReqObj);
        public Task<GetFactoryAuditByIdResponse> GetVendorAuditRequestByID(SelectListReq req);
        public Task<GSTINLocalDBResponse> GetGSTDetails(SelectListReq req);
    }
}
