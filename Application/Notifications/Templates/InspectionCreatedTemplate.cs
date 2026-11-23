using Application.Interfaces.V1.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications.Templates
{
    public sealed class InspectionCreatedTemplate : IEmailTemplate<InspectionCreatedModel>
    {
        public string BuildSubject(InspectionCreatedModel m) => $"New Inspection Request Submitted – [{m.RecordId}]";

        public string BuildBody(InspectionCreatedModel m)
        {
            var po = m.PO;
            var vendor = $"{po.VendorName}  <b>({po.VendorCode})</b>";
            var product = $"<b>PO~</b>{po.PoNumber}, <b>Color~</b>{po.Color}, <b>Fabric~</b>{po.Fabric}, <b>Offered Quantity~</b>{po.OfferedQty}";

            return
                "Dear QA Team,<br/><br/>" +
                "Kindly review the inspection request and take the necessary action to schedule and confirm the final date of inspection.<br/><br/>" +
                $"<b>Vendor Name: </b>{vendor}<br/>" +
                $"<b>Product: </b>{product}<br/>" +
                $"<b>Proposed Date of Inspection: </b>{po.DateOfInspection}<br/>" +
                $"<b>Location: </b>{po.InspectionLocation}<br/>" +
                $"<b>Submitted On: </b>{po.CreatedOn}<br/><br/>" +
                EmailFooter.Footer(EmailFooter.InspectionNote);
            
        }
    }
}
