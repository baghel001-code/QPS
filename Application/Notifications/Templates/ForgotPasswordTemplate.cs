using Application.Interfaces.V1.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications.Templates
{
    public sealed class ForgotPasswordTemplate : IEmailTemplate<ForgotPasswordModel>
    {
        public string BuildSubject(ForgotPasswordModel m) => "Your New Password...reg.";

        public string BuildBody(ForgotPasswordModel m) =>
            $"Hello {m.Vendor.VendorName},<br/><br/>" +
            $"Your new password is: <b>{m.PlainPassword}</b><br/><br/>" +
             EmailFooter.Footer(EmailFooter.ForgotPwdNote);
    }
}
