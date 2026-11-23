using Application.Interfaces.V1;
using Domain.Entities.Modals;
using Domain.Entities.Request;
using Domain.Entities.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace QPSApi.Controllers.V1
{
    [Route("api/v1/[Action]")]
    [ApiController]
    public class VendorAuditController : ControllerBase
    {
        private readonly IVendorAudit _vendorAudit;
        public VendorAuditController(IVendorAudit vendorAudit)
        {
            _vendorAudit = vendorAudit;
        }
        [HttpPost]
        public async Task<IActionResult> FactoryAuditRequest(VendorAuditFormModel req)
        {
            var res = await _vendorAudit.CreateUpdateVendorAuditRequest(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> GetAllFactoryAuditRequest(FactoryAuditListRequest ReqObj)
        {
            var res = await _vendorAudit.GetAllVendorAuditRequest(ReqObj);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> GetFactoryAuditRequestByCode(SelectListReq req)
        {
            var res = await _vendorAudit.GetVendorAuditRequestByID(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> GetGSTDetails(SelectListReq req)
        {
            var res = await _vendorAudit.GetGSTDetails(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);

        }

    }
}
