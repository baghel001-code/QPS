using Application.Interfaces.V1.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications.Templates
{
    public sealed class VendorAccountTemplate : IEmailTemplate<VendorAccountModel>
    {
        public string BuildSubject(VendorAccountModel m) => "Vendor account creation...reg.";

        public string BuildBody(VendorAccountModel m)
        {
            var verb = m.IsUpdate ? "updated" : "created";
            return
                $"Hi {m.Vendor.VendorName},<br/><br/>" +
                $"Your account has been successfully <b>{verb}</b> for inspection activities.<br/><br/>" +
                "Please use the following credentials to log in:<br/>" +
                $"Login ID: <b>{m.Vendor.VendorCode}</b><br/>" +
                $"Password: <b>{m.PlainPassword}</b><br/>" +
                EmailFooter.Footer(EmailFooter.VendorAccCreateNote);

        }
    }
}
