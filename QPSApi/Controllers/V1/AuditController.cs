using Application.DTOs;
using Application.Interfaces.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace QPSApi.Controllers.V1
{

    [ApiController]
    [Route("api/v1/[action]")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditWizardService _svc;

        public AuditController(IAuditWizardService svc) => _svc = svc;

        [HttpPost]
        public async Task<IActionResult> Start([FromBody] StartAuditRequest dto)
        {
            var result = await _svc.StartAuditAsync(dto);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> SaveStep1([FromBody] StartAuditRequest dto)
        {
            var result = await _svc.SaveStep1Async(dto);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> SaveStep2([FromBody] VendorInfoRequest dto)
        {
            var result = await _svc.SaveStep2Async(dto);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> SaveStep3([FromBody] ListOfDocumentResponseRequest docs)
        {
            var result = await _svc.SaveStep3Async(docs);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> SaveElement([FromBody] ElementScoresRequest dto)
        {
            var result = await _svc.SaveElementStepAsync(dto);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> SaveAttachments([FromBody] ListOfAttachmentRequest dto)
        {
            var result = await _svc.SaveAttachmentsAsync(dto);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> SaveComments([FromBody] OverallCommentsRequest dto)
        {
            var result = await _svc.SaveCommentsAsync(dto);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] AuditBaseReq obj)
        {
            var result = await _svc.SubmitAuditAsync(obj);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> GetDetail([FromBody] AuditBaseReq obj)
        {
            var result = await _svc.GetDetailAsync(obj.AuditId);
            return Ok(result);
            //return result.Success ? Ok(result) : NotFound(result);
        }
    }
}

