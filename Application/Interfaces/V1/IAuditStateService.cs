using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.V1
{
    // ── Wizard state service (Blazor Server scoped) ───────────────
    public interface IAuditStateService
    {
        WizardStateDto State { get; }
        List<KeyElementResponse> Elements { get; }
        List<DocumentItemResponse> DocItems { get; }
        List<AttachmentItemResponse> AttachItems { get; }

        event Action? StateChanged;

        Task InitialiseAsync(int? existingAuditId = null);
        void NavigateTo(int step);
        void GoBack();

        Task<string?> AuditStartAsync(StartAuditRequest dto);
        Task<string?> SaveStep1Async(StartAuditRequest dto);
        Task<string?> SaveStep2Async(VendorInfoRequest dto);
        Task<string?> SaveStep3Async(ListOfDocumentResponseRequest docs);
        Task<string?> SaveElementStepAsync(ElementScoresRequest dto);
        Task<string?> SaveStep14Async(ListOfAttachmentRequest attachments);
        Task<string?> SaveStep15Async(OverallCommentsRequest dto);
        Task<string?> SubmitAuditAsync(AuditBaseReq obj);
    }

}
