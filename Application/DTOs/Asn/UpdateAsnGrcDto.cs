using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Asn
{
    public class UpdateAsnGrcDto
    {
        public long AsnReqId { get; set; }
        public int? GrcQty { get; set; }
        public decimal? GrcValue { get; set; }
        public DateTime? GrcDate { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
