using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Asn
{
    public class CreateAsnRequestDto
    {
        public long InspId { get; set; }
        public long? InspectId { get; set; }
        public int ShipQty { get; set; }
        public int RequestedQty { get; set; }
        public string? Remarks { get; set; }
        public decimal? PoNetValue { get; set; }
        public int CreatedBy { get; set; }
    }
    public  class SummaryRequest
    {
        public string PO_NUMBER { get; set; } = "";
    }
}
