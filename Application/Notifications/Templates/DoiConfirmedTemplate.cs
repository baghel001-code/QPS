using Application.Interfaces.V1.Notifications;
using Domain.Entities.Modals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications.Templates
{
    public sealed class DoiConfirmedTemplate : IEmailTemplate<DoiConfirmedModel>
    {
        public string BuildSubject(DoiConfirmedModel m) =>
            $"INSPECTION REQUEST FOR – [INSPECTION_ID:{m.Row.INSP_ID}], {m.Row.VENDOR_NAME}  ({m.Row.VENDOR_CODE})";

        public string BuildBody(DoiConfirmedModel m)
        {
            var row = m.Row;
            return
                $"Dear Vendor Partner,{row.VENDOR_NAME}  <b>({row.VENDOR_CODE})</b><br/><br/>" +
                "This is to inform you that the final date of inspection for your submitted inspection request has been scheduled and confirmed by the Quality Assurance team.<br/><br/>" +
                $"<b>Final Date of Inspection: </b>{row.QA_ASSIGNED_DOI?.ToString("dd-MM-yyyy")}<br/>" +
                $"<b>Location: </b>{row.ADDRESS_LINE1}, {row.CITY},({row.PINCODE})<br/></br>" +
                "Please ensure that all required documents, materials, and arrangements are in place prior to the inspection date<br/><br/>" +
                "<b>1.</b>During Inspection 100% goods should be in packing with final Carton Marking.  <br/></br>" +
                "<b>2.</b>Vendor to provide a clean inspection table, sufficient light and space for carton opening and samples keeping. <br/></br>" +
                "<b>3.</b>If the goods are not packed, Inspection coordinator should be contacted 1-2 day before for postponement. <br/></br>" +
                "<b>4.</b>If the goods are not ready upon arrival of Inspector at the factory, the inspection will be abortive and a fresh booking will be initiated.  <br/></br>" +
                "<b>5.</b>During Inspection, Packing list, Approved PPS, Colour swatches, testing reports should be available.  <br/></br>" +
                "<b>6.</b>In inspection request form, please fill all the columns properly. <br/></br>" +
                "<b>7.</b>Once the inspection date is confirmed, inspector may come anytime between 10-00 AM to 6:00 PM, if you have any specific time please do call co-ordinator first. <br/></br>" +
                EmailFooter.Footer(EmailFooter.DoINote);

        }

    }
}
