using Application.Interfaces.V1.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications.Templates
{
    public sealed class PasswordResetLinkTemplate : IEmailTemplate<PasswordResetLinkModel>
    {
        public string BuildSubject(PasswordResetLinkModel m) => "Reset your password - QPS Portal";

        public string BuildBody(PasswordResetLinkModel m)
        {
            var name = WebUtility.HtmlEncode(m.Vendor.UserName);
            var link = WebUtility.HtmlEncode(m.ResetLink);

            return
                $"Hello {name},<br/><br/>" +
                "We received a request to reset the password for your account.<br/><br/>" +
                $"<a href=\"{link}\" style=\"display:inline-block;padding:10px 20px;background:#1565c0;" +
                "color:#ffffff;text-decoration:none;border-radius:4px;font-weight:bold;\">Reset Password</a>" +
                "<br/><br/>" +
                "If the button doesn't work, copy and paste this link into your browser:<br/>" +
                $"<a href=\"{link}\">{link}</a><br/><br/>" +
                $"This link is valid for <b>{m.Expiryin} minutes</b> and can be used only once.<br/><br/>" +
                "If you did not request a password reset, you can ignore this email. Your password will not change.<br/><br/>" +
                EmailFooter.Signature;
        }
    }
}
