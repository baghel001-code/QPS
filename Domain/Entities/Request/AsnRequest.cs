using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Request
{
    public class AsnRequest
    {
        public long AsnReqId { get; set; }
        public long InspId { get; set; }
        public long? InspectId { get; set; }
        public long AsnSeqNo { get; set; }
        public int ShipQty { get; set; }
        public int RequestedQty { get; set; }
        public int BalanceQty { get; set; }
        public string? AsnNumber { get; set; }
        public string? AsnAttachment { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime ReqCreateDate { get; set; }
        public DateTime? AsnUpdateDate { get; set; }
        public decimal? PoNetValue { get; set; }
        public int? GrcQty { get; set; }
        public decimal? GrcValue { get; set; }
        public DateTime? GrcDate { get; set; }
        public string? Remarks { get; set; }
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
