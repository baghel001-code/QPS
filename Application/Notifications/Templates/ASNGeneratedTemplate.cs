using Application.Interfaces.V1.Notifications;
using Domain.Entities.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications.Templates
{
    public sealed class ASNGeneratedTemplate : IEmailTemplate<AsnGeneratedModel>
    {
        public string BuildSubject(AsnGeneratedModel m) => $"ASN Generated – Inspection No. [{m.AsnDetails.InspectionNumber}]/ PO Number [{m.AsnDetails.PoNumber}]";

        public string BuildBody(AsnGeneratedModel m)
        {
            return
                $"Dear Team, <br/><br/><br/>  Please note that the <b> ASN has been generated </b> for the below PO.<br/><br/><br/>" +
                $"<b>PO No.: </b> [{m.AsnDetails.PoNumber}] <br/><br/><br/>" +
                $"<b>Vendor Name: </b> [{m.AsnDetails.VendorName} ({m.AsnDetails.VendorCode})] <br/><br/><br/>" +
                $"<b>Vendor Name: </b> [{m.AsnDetails.Buyer} ({m.AsnDetails.BuyerCode})] <br/><br/><br/>" +
                $"<b>ASN No.: </b> [{m.AsnDetails.AsnNumber}] <br/><br/><br/>" +
                $"<b>Quantity: </b> [{m.AsnDetails.Quantity}] <br/><br/><br/>" +
                $"<b>ASN Generation Date: </b> [{m.AsnDetails.AsnGenerationDate}] <br/><br/><br/>" +
                "Please check and download the ASN PDF from the QPS portal and proceed with the further process accordingly.<br/>" +
                EmailFooter.Footer(EmailFooter.ASNNote);

        }
    }
}
