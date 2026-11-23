using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Modals
{
    public class AsnDetailRow
    {
        public int Sno { get; set; }
        public decimal RequestedQty { get; set; }
        public string? AsnNumber { get; set; }          // null/empty => "Pending", show "-"
        public DateTime DateOfCreation { get; set; }
        public decimal BalanceQty { get; set; }
        public string Status { get; set; } = "";          // "Completed" | "Pending"
        public string? Remarks { get; set; }
        public string? ASN_ATTACHMENT { get; set; }
    }

    public class InspectionAsnGroup
    {
        public string InspectionNumber { get; set; } = "";
        public decimal ShipQty { get; set; }
        public decimal BalanceQty { get { return ShipQty - TotalAsnQty; } }
        public decimal TotalAsnQty { get { return Rows.Sum(a => a.RequestedQty); } }
        public int TotalAsnInCurrentInspection { get { return Rows.Count; } }
        public int CompletedAsnInCurrentInspection { get { return Rows.Where(a => a.Status == "Completed").ToList().Count; } }
        public int PendingAsnInCurrentInspection { get { return Rows.Where(a => a.Status == "Pending").ToList().Count; } }
        public List<AsnDetailRow> Rows { get; set; } = new();
    }

    public class AsnDetailsSummary
    {
        public string PoNumber { get; set; } = "";
        public int TotalAsnInCurrentPo { get { return Inspections.Sum(a => a.TotalAsnInCurrentInspection); } }
        public int PendingAsnInCurrentPo { get { return Inspections.Sum(a => a.PendingAsnInCurrentInspection); } }
        public int CompleteAsnInCurrentPo { get { return Inspections.Sum(a => a.CompletedAsnInCurrentInspection); } }
        public decimal TotalRequestedQty { get { return Inspections.Sum(a => a.TotalAsnQty); } }
        public decimal InspWiseTotalRequestedQty(string inspectionNumber) => Inspections.Where(a => a.InspectionNumber == inspectionNumber).Sum(a => a.TotalAsnQty);
        public List<InspectionAsnGroup> Inspections { get; set; } = new();
    }

    public class AsnRequestModel
    {
        public int InspId { get; set; }
        public int InspectId { get; set; }
        public string PoNumber { get; set; } = "";
        public string PoNumberwithLinkPo { get; set; } = "";
        public decimal TotalPOASNQty { get; set; }
        public decimal TotalInspAsnQty { get; set; }
        public decimal TotalOrderQty { get; set; }
        public decimal ShipQty { get; set; }
        public string InspectionNo { get; set; } = "";
        public decimal ReminingQty => TotalOrderQty - TotalPOASNQty;
        public decimal TotalBalanceQty => ShipQty - TotalInspAsnQty;
        public decimal? AsnQuantity { get; set; }
        public string? Remarks { get; set; }
        public string ASNNumber { get; set; } = "";
        public string ASNAttachmentPath { get; set; } = "";
        public long AsnId { get; set; }

        public string Buyer { get; set; } = "";
        public string BuyerCode { get; set; } = "";
        public string VendorCode { get; set; } = "";
        public string VendorName { get; set; } = "";
    }

    public class PendingAsnRowVm
    {
        public int SNo { get; set; }
        public InspectionAsnGroup Group { get; set; } = null!;
        public AsnDetailRow Row { get; set; } = null!;
    }

    public class AsnSummaryFlatRow
    {
        public int ASN_REQ_ID { get; set; }
        public int INSPECT_ID { get; set; }
        public int INSP_ID { get; set; }
        public string PoNumber { get; set; } = "";
        public string InspectionNumber { get; set; } = "";
        public decimal ShipQty { get; set; }
        public long Sno { get; set; }
        public decimal RequestedQty { get; set; }
        public string? AsnNumber { get; set; }
        public DateTime DateOfCreation { get; set; }
        public decimal BalanceQty { get; set; }
        public string Status { get; set; } = "";
        public string? Remarks { get; set; }
        public string? ASN_ATTACHMENT { get; set; }
        public string PoNumberWithLinkPo { get; set; } = "";
        public string BUYER_NAME { get; set; } = "";
        public string BUYER_ID { get; set; } = "";
        public string VENDOR_NAME { get; set; } = "";
        public string VENDOR_CODE { get; set; } = "";

        public string CATEGORY { get; set; } = "";
        public string PRODUCT { get; set; } = "";
        public string COLOR { get; set; } = "";
        public decimal MRP { get; set; }

        public DateTime ASN_UPDATE_DATE { get; set; }



    }

    public record PendingASNList
    {
        public List<AsnSummaryFlatRow> asnSummaryFlatRows { get; set; } = new List<AsnSummaryFlatRow>();
    }
    public record PoWiseAsnList(List<string>? ASN_NUMBER);
    public class ASNRequest_PendingComplete
    {
        public string CMD { get; set; } = "";
        public int UserId { get; set; }
        public string? UserType { get; set; }
        public string? UserCode { get; set; }
    }
}
