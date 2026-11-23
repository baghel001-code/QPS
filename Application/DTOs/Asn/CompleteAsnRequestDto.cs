using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Asn
{
    public class CompleteAsnRequestDto
    {
        public long AsnReqId { get; set; }
        public string AsnNumber { get; set; } = "";
        public string? AsnAttachment { get; set; }
        public int UpdatedBy { get; set; }
    }
}
