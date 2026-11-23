using Application.DTOs.Asn;
using Domain.Entities.Modals;
using Domain.Entities.Request;
using Domain.Entities.Response;
using Domain.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.V1.Asn
{
    public interface IAsnRequestService
    {
        Task<GenRes> CreateAsync(CreateAsnRequestDto dto, CancellationToken ct = default);
        Task<GenRes> CompleteAsync(CompleteAsnRequestDto dto, CancellationToken ct = default);
        Task UpdateGrcAsync(UpdateAsnGrcDto dto, CancellationToken ct = default);
        Task<AsnDetailsSummary> GetSummaryByPoAsync(string poNumber, CancellationToken ct = default);
        Task<IEnumerable<AsnRequest>> GetByInspectionAsync(long inspId, CancellationToken ct = default);
        Task<PendingASNList> GetPendingAsync(ASNRequest_PendingComplete req);
        Task<AsnRequest?> GetByIdAsync(long asnReqId, CancellationToken ct = default);
        Task<AsnList> GetASNListAsync(string PoNumber);
    }
}
