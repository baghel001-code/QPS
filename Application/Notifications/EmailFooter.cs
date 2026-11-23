using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications
{
    public static class EmailFooter
    {
        public const string LoginUrl = "https://qps.vishalmegamart.com:8024/";

        public static string LoginLine =>
            $"<br/><br/>You may log in to the Quality Process System (QPS) to review the details.<br/>" +
            $"Login URL: 🔗 <a href='{LoginUrl}'>{LoginUrl}</a><br/>";

        public static string Signature =>
            "<br/><br/><br/>Regards,<br/><b>Quality Process System (QPS)</b><br/><br/>" +
            "This is an automated notification. Please do not reply to this email.";

        private static string ContactNote(string contact) => $"<b>Note : </b> In case of any concerns or required clarifications, kindly contact the {contact}.";

        private const string PasswordChangeNote = "<b>Note : </b> For security purposes, please change your password after your first login.";

        public static string ASNNote => ContactNote("buyers and QA Team");
        public static string DailyScheduleNote => ContactNote("QAM");
        public static string DoINote => ContactNote("QA team");
        public static string PPSNote => ContactNote("QA team");
        public static string InspectionNote => "<b>Note : </b> For any clarification, please coordinate with the respective vendor.";
        public static string ForgotPwdNote => PasswordChangeNote;
        public static string VendorAccCreateNote => PasswordChangeNote;

        public static string Footer(string note) => $"{LoginLine}{note}{Signature}";
        
    }
}
