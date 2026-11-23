using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Asn
{
    public class InspectionAsnGroupDto
    {
        public string InspectionNumber { get; set; } = "";
        public decimal ShipQty { get; set; }
        public decimal BalanceQty => ShipQty - TotalAsnQty;
        public decimal TotalAsnQty => Rows.Sum(a => a.RequestedQty);
        public int TotalAsnInCurrentInspection => Rows.Count;
        public List<AsnDetailRowDto> Rows { get; set; } = new();
    }
}
