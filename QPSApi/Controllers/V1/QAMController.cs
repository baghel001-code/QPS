using Application.Interfaces.V1;
using Domain.Entities.Modals;
using Domain.Entities.Request;
using Domain.Entities.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace QPSApi.Controllers.V1
{
    [Route("api/v1/[Action]")]
    [ApiController]
    public class QAMController : ControllerBase
    {
        private readonly IVendor _vendor;

        public QAMController(IVendor vendor)
        {
            _vendor = vendor;
        }

        [HttpPost]
        public async Task<IActionResult> QAMDashboard(SelectListReq obj)
        {
            var res = await _vendor.GetDashboardData(obj);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> GetInspectionsListByIds(SelectListReq Ids)
        {
            var res = await _vendor.GetInspectionsListByIds(Ids.StrField!);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> GetQALISTTOMAIL(SelectListReq Ids)
        {
            var res = await _vendor.GetQALISTTOMAIL(Ids);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> Freeze_QAAllocation(AssignInspectorFreezeRequest obj)
        {
            var res = await _vendor.FreezQA(obj);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLinkPO(UpdateLinkPO_Details Obj)
        {
            var res = await _vendor.UpdateLinkPO(Obj);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        //[HttpGet]
        //public async Task<IActionResult> ExportInspectionExcel(SelectListReq Obj)
        //{
        //    var fileBytes = await _vendor.ExportToExcel(Obj);
        //    return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Inspection.xlsx");
        //}

        [HttpPost]
        public async Task<IActionResult> SyncPOdetails(POHeader req)
        {
            var res = await _vendor.SyncPOdetailsService(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpGet]
        public async Task<IActionResult> InspectionsExportToExcel(SelectListReq Obj)
        {
            var fileBytes = await _vendor.InspectionExportToExcel(Obj);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{Obj.ResponseType}_Inspection.xlsx");
        }


        [HttpPost]
        public async Task<IActionResult> GetAllHierarchyList(SelectListReq Obj)
        {
            var res = await _vendor.GetHierarchyList(Obj);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> AddUpdateHierarchy(HierarchyDto Obj)
        {
            var res = await _vendor.AddUpdateHierarchy(Obj);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }
        [HttpPost]
        public async Task<IActionResult> GetAllEmailMgtList(SelectListReq Obj)
        {
            var res = await _vendor.GetEmailMgtList(Obj);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }
        [HttpPost]
        public async Task<IActionResult> AddUpdateEmailMgt(EmailManagementDto Obj)
        {
            var res = await _vendor.AddUpdateEmailMgt(Obj);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> GetAllColorCodeList(SelectListReq Obj)
        {
            var res = await _vendor.GetAllColorCodeList(Obj);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }
        [HttpPost]
        public async Task<IActionResult> AddUpdateColorCode(MasterColorCodeDto Obj)
        {
            var res = await _vendor.AddUpdateColorCode(Obj);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

    }
}
