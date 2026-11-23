using Application.Interfaces.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace QPSApi.Controllers.V1
{
    [ApiController]
    [Route("api/v1/")]

    public class AuditMasterController : ControllerBase
    {
        private readonly IMasterDataService _svc;

        public AuditMasterController(IMasterDataService svc) => _svc = svc;

        [HttpPost("elements")]
        public async Task<IActionResult> GetElements() => Ok(await _svc.GetKeyElementsAsync());

        [HttpPost("documents")]
        public async Task<IActionResult> GetDocuments() => Ok(await _svc.GetDocumentChecklistAsync());

        [HttpPost("attachments")]
        public async Task<IActionResult> GetAttachments() => Ok(await _svc.GetAttachmentItemsAsync());
    }

}
