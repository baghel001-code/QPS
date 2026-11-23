using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Asn
{
    public class AsnDetailsSummaryDto
    {
        public string PoNumber { get; set; } = "";
        public List<InspectionAsnGroupDto> Inspections { get; set; } = new();
    }
}
