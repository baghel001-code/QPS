using Application.Interfaces.V1;
using Domain.Entities.Modals;
using Domain.Entities.Request;
using Domain.Entities.Response;
using Infrastructure.Services.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace QPSApi.Controllers.V1
{

    [Route("api/v1/[Action]")]
    [ApiController]
    public class VendorController : ControllerBase
    {
        private readonly IVendor _vendor;
        private readonly BasePathOptions _basePathOptions;


        public VendorController(IVendor vendor, IOptions<BasePathOptions> basePathOptions)
        {
            _vendor = vendor;
            _basePathOptions = basePathOptions.Value;
        }


        [HttpPost]
        public async Task<IActionResult> SetVendor(Vendor req)
        {
            var res = await _vendor.SetVendor(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> GetAllVendors()
        {
            var res = await _vendor.GetAllVendors();
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> GetAddressByVendor(SelectListReq req)
        {
            var res = await _vendor.GetAddressOnVendorCode(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> GetVendorByCode(SelectListReq req)
        {
            var res = await _vendor.GetVendorByID(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        //[HttpPost]
        //public async Task<IActionResult> GetVendorWisePOs(SelectListReq req)
        //{
        //    var res = await _vendor.GetAllVendorWisePOs(req);
        //    if (res == null)
        //    {
        //        return NotFound();
        //    }
        //    return Ok(res);
        //}
        [HttpPost]
        public async Task<IActionResult> GetPODetails(SelectListReq req)
        {
            var res = await _vendor.GetPODetails(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);

        }


        [HttpPost]
        public async Task<IActionResult> SaveInspectionRequest(POHeader req)
        {
            var res = await _vendor.SaveInspectionRequest(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> GetInspectionByID(SelectListReq req)
        {
            var res = await _vendor.GetInspectionRequestByID(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> GetMasterData(SelectListReq req)
        {
            var res = await _vendor.GetMasterData(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> AddAddress(Vendor req)
        {
            var res = await _vendor.AddAddress(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDOI(UpdateDOIRequest req)
        {
            var res = await _vendor.UpdateDOI(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }
        [HttpPost]
        public async Task<IActionResult> AssignInspector(AssignInspectorRequest req)
        {
            var res = await _vendor.AssignInspector(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> SaveInspection(InspectionModel req)
        {
            var res = await _vendor.SaveInspection(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> GetInspectDataByID(SelectListReq req)
        {
            var res = await _vendor.GetInspectDataByID(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOfferedQty(UpdateDOIRequest req)
        {
            var res = await _vendor.UpdateOfferedQty(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> UploadVendorExcel(VendorExcel req)
        {
            var res = await _vendor.UploadVendorExcel(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> GetVendorExcelData(SelectListReq req)
        {
            var res = await _vendor.GetVendorExcelData(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCMStatus(UpdateCM_Status_Details req)
        {
            var res = await _vendor.UpdateCMStatus(req);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateInspectionPdf(SelectListReq Obj)
        {
            byte[] pdfBytes = null;
            var report = await _vendor.GenerateInspectionPdfAsync(Obj);
            var pdfModel = new InspectionReportPdfModel(report);

            var generator = new InspectionReportPdfGenerator(pdfModel, _basePathOptions.RootPath, _basePathOptions.PublicBaseUrl);
            if (Obj.StrField == "PASS-FAIL")
            {
                pdfBytes = generator.Generate();
            }
            else
            {
                pdfBytes = generator.GenerateAbortivePdf();
            }


            return File(pdfBytes, "application/pdf", $"InspectionReport_{report.Detail.INSPECT_ID}.pdf");

        }

        [HttpPost]
        public async Task<IActionResult> GetPenRecOng_Inspections(InspectionSearchFilterMultiSelection Obj)
        {
            var res = await _vendor.GetAllRecPenOngInspections(Obj);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);

        }

        [HttpPost]
        public async Task<IActionResult> PenRecOng_InspectionsExportExcel(InspectionSearchFilterMultiSelection Obj)
        {
            var fileBytes = await _vendor.RecPenOngInspectionsExportToExcel(Obj);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Inspection.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> InspectionReportView(SelectListReq Obj)
        {
            var report = await _vendor.GenerateInspectionPdfAsync(Obj);
            if (report == null)
            {
                return NotFound();
            }
            return Ok(report);

        }

    }
}
