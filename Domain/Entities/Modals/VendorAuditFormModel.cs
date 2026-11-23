using Domain.Entities.Base;
using Domain.Entities.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Modals
{
    // ─── Checklist Item ───────────────────────────────────────────
    public class AuditChecklistItem
    {
        public int Id { get; set; }
        public int Audit_Req_Id { get; set; }
        public int CheckListMasterId { get; set; }
        public int SrNo { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Status { get; set; }
        public string _status => StatusList.Any() ? StatusList.FirstOrDefault(i => i.Master_ID == Status)?.Master_Name ?? "N/A" : "";
        public string ValidityDate { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string? Attachments { get; set; } = string.Empty;

        public List<MasterData> StatusList = new List<MasterData>();

    }

    // ─── Main Audit Form Model ────────────────────────────────────
    public class VendorAuditFormModel
    {
        public long AuditReqId { get; set; }
        public long AuditId { get; set; }
        public string TempVendorCode { get; set; } = string.Empty;

        // ── Header Info ───────────────────────────────────────────
        public int BusinessHead { get; set; }
        public DateTime? AuditDate { get; set; }

        // ── GST Info ──────────────────────────────────────────────
        public string GstNumber { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string RegisteredAddress { get; set; } = string.Empty;
        public string FactoryLocation { get; set; } = string.Empty;

        // ── Contact Info ──────────────────────────────────────────
        public string ContactName { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;

        // ── Category Info ─────────────────────────────────────────
        public string Category { get; set; } = string.Empty;
        public string RequestedBuyerName { get; set; } = string.Empty;
        public string ProductCategory { get; set; } = string.Empty;

        // ── Factory Info ──────────────────────────────────────────
        public string FactoryLicenceNumber { get; set; } = string.Empty;
        public decimal FactoryAreaSqFt { get; set; }
        public int NumberOfEmployees { get; set; }

        // ── Checklist ─────────────────────────────────────────────
        public List<AuditChecklistItem> Checklist { get; set; } = new();

        // ── File Upload ─────────────────────────────────────────────
        public string? GenAttachmentFirst { get; set; } = string.Empty;
        public ICollection<MasterData> BusinessHeadList { get; set; } = new List<MasterData>();
        public string GSTApiResponse { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
        public string ReqStatus { get; set; } = string.Empty;
        public string RejectionReason { get; set; } = string.Empty;
        public int ApprovedBy { get; set; }
        public bool PromoteToMaster { get; set; }
        public int VendorMasterId { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }

        public int UserId { get; set; }
        public string Head { get; set; } = string.Empty;
    }

    public class FactoryAuditRequestDto
    {
        public long AUDIT_REQ_ID { get; set; }
        public long AUDIT_ID { get; set; }
        public string TEMP_VENDOR_CODE { get; set; } = string.Empty;
        public int BUSINESS_HEAD { get; set; }
        public DateTime AUDIT_DATE { get; set; }
        public string GST_NUMBER { get; set; } = string.Empty;
        public string VENDOR_NAME { get; set; } = string.Empty;
        public string REGISTERED_ADDRESS { get; set; } = string.Empty;
        public string FACTORY_LOCATION { get; set; } = string.Empty;
        public string CONTACT_NAME { get; set; } = string.Empty;
        public string CONTACT_NUMBER { get; set; } = string.Empty;
        public string CATEGORY { get; set; } = string.Empty;
        public string REQUESTED_BUYER_CODE { get; set; } = string.Empty;
        public string PRODUCT_CATEGORY { get; set; } = string.Empty;
        public string FACTORY_LICENCE_NUMBER { get; set; } = string.Empty;
        public decimal FACTORY_AREA_SQFT { get; set; }
        public int NUMBER_OF_EMPLOYEES { get; set; }
        public string GEN_ATTACHMENT_FIRST { get; set; } = string.Empty;
        public string GST_API_RAW_RESPONSE { get; set; } = string.Empty;
        public int? VENDOR_MASTER_ID { get; set; }
        public bool PROMOTED_TO_MASTER { get; set; }
        public DateTime? PROMOTED_ON { get; set; }
        public int? PROMOTED_BY { get; set; }
        public string REQ_STATUS { get; set; } = string.Empty;
        public string REJECTION_REASON { get; set; } = string.Empty;
        public int? APPROVED_BY { get; set; }
        public DateTime? APPROVED_ON { get; set; }
        public bool IS_COMPLETE { get; set; }
        public bool IS_ACTIVE { get; set; }
        public int CREATED_BY { get; set; }
        public DateTime CREATED_ON { get; set; }
        public int? UPDATED_BY { get; set; }
        public DateTime? UPDATED_ON { get; set; }
        public string HEAD { get; set; } = string.Empty;
        public string EMP_NAME { get; set; } = string.Empty;
        public int TOTAL_RECORDS { get; set; }
        public int CHECKLIST_COUNT { get; set; }
        public List<AuditChecklistItem> Checklist { get; set; } = new();
    }

    public class FactoryAuditChecklistDto
    {
        public long CHECKLIST_ID { get; set; }
        public long AUDIT_REQ_ID { get; set; }
        public int CHECKLIST_MST_ID { get; set; }
        public int DESCRIPTION { get; set; }
        public int? STATUS { get; set; }
        public string VALIDITY_DATE { get; set; }
        public string REMARKS { get; set; }
        public string ATTACHMENTS { get; set; }
        public int CREATED_BY { get; set; }
        public DateTime CREATED_ON { get; set; }
        public int? UPDATED_BY { get; set; }
        public DateTime? UPDATED_ON { get; set; }
    }


    public class FactoryAuditListRequest
    {
        public int? BUSINESS_HEAD { get; set; }
        public string REQ_STATUS { get; set; } = string.Empty; //DRAFT | SUBMITTED | APPROVED | REJECTED
        public string GST_NUMBER { get; set; } = string.Empty;
        public string VENDOR_NAME { get; set; } = string.Empty;
        public DateTime? AUDIT_DATE_FROM { get; set; }
        public DateTime? AUDIT_DATE_TO { get; set; }
        public bool? IS_COMPLETE { get; set; }
        public bool? PROMOTED_TO_MASTER { get; set; }
        public string REQUESTED_BUYER_CODE { get; set; } = string.Empty;
        public bool INCLUDE_INACTIVE { get; set; } = false;
        public int PAGE_NUMBER { get; set; } = 1;
        public int PAGE_SIZE { get; set; } = 20;
    }

    public class GetFactoryAuditByIdResponse : BaseResponse
    {
        public FactoryAuditRequestDto HEADER { get; set; }
        public List<FactoryAuditChecklistDto> CHECKLIST { get; set; } = new List<FactoryAuditChecklistDto>();
    }

    public class GetFactoryAuditListResponse : BaseResponse
    {
        public List<FactoryAuditRequestDto> ITEMS { get; set; } = new List<FactoryAuditRequestDto>();
        public int TOTAL_RECORDS => ITEMS?.Count ?? 0;
        public int PAGE_NUMBER { get; set; }
        public int PAGE_SIZE { get; set; }
        public int TOTAL_PAGES => PAGE_SIZE > 0 ? (int)Math.Ceiling((double)TOTAL_RECORDS / PAGE_SIZE) : 0;
    }


}
