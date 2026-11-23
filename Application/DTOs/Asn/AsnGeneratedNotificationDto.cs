using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Asn
{
    public class AsnGeneratedNotificationDto
    {
        public DateTime AsnGenerationDate { get; set; }
        public string AsnNumber { get; set; } = "";
        public string Buyer { get; set; } = "";
        public string BuyerCode { get; set; } = "";
        public string InspectionNumber { get; set; } = "";
        public string PoNumber { get; set; } = "";
        public decimal Quantity { get; set; }
        public string VendorCode { get; set; } = "";
        public string VendorName { get; set; } = "";
        
        

    }
}
