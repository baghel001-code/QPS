using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Asn
{
    public class AsnDetailRowDto
    {
        public int Sno { get; set; }
        public decimal RequestedQty { get; set; }
        public string? AsnNumber { get; set; }
        public DateTime DateOfCreation { get; set; }
        public decimal BalanceQty { get; set; }
        public string Status { get; set; } = "";
        public string? Remarks { get; set; }
    }
}
