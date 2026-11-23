using Application.DTOs.Asn;
using Application.Interfaces.V1;
using Application.Interfaces.V1.Asn;
using DocumentFormat.OpenXml.Drawing;
using Domain.Entities.Modals;
using Domain.Entities.Response;
using Microsoft.AspNetCore.Mvc;
namespace QPSApi.Controllers.V1
{
    [Route("api/v1/[Action]")]
    [ApiController]
    public class AsnController : ControllerBase
    {
        private readonly IAsnRequestService _asnRequestService;

        public AsnController(IAsnRequestService asnRequestService)
        {
            _asnRequestService = asnRequestService;
        }

        [HttpPost]
        public async Task<IActionResult> create(CreateAsnRequestDto dto)
        {
            var res = await _asnRequestService.CreateAsync(dto);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> complete(CompleteAsnRequestDto dto)
        {
            var res = await _asnRequestService.CompleteAsync(dto);
            if (res == null)
            {
                return NotFound();
            }
            return Ok(res);
        }

        // PUT api/AsnRequest/grc
        // Backend service posts GRC details after ASN completion.
        [HttpPost("grc")]
        public async Task<IActionResult> UpdateGrc([FromBody] UpdateAsnGrcDto dto, CancellationToken ct)
        {
            await _asnRequestService.UpdateGrcAsync(dto, ct);
            return NoContent();
        }

        // GET api/AsnRequest/summary/{poNumber}
        // Feeds AsnDetailsSummary in the Blazor UI.
        [HttpPost]
        public async Task<ActionResult<AsnDetailsSummary>> summary(SummaryRequest summaryRequest)
        {
            var summary = await _asnRequestService.GetSummaryByPoAsync(summaryRequest.PO_NUMBER);
            return Ok(summary);
        }

        
        [HttpPost]
        public async Task<IActionResult> asninspection(SummaryRequest summaryRequest)
        {
            var rows = await _asnRequestService.GetASNListAsync(summaryRequest.PO_NUMBER);
            return Ok(rows);
        }

       
        [HttpPost]
        public async Task<IActionResult> pending(ASNRequest_PendingComplete req)
        {
            var rows = await _asnRequestService.GetPendingAsync(req);
            return Ok(rows);
        }

        // GET api/AsnRequest/{asnReqId}
        [HttpPost("asngetbyid")]
        public async Task<IActionResult> GetById([FromBody] long asnReqId, CancellationToken ct)
        {
            var row = await _asnRequestService.GetByIdAsync(asnReqId, ct);
            return row is null ? NotFound() : Ok(row);
        }
    }
}
