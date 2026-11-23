using Application.Interfaces.V1.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Application.Notifications.Templates
{
    public sealed class TestEmailTemplate : IEmailTemplate<TestMailModel>
    {
        public string BuildSubject(TestMailModel m) => $"QPS Portal - New login to your account";

        public string BuildBody(TestMailModel m)
        {
            var istTime = TimeZoneInfo.ConvertTime(m.LoginTimeUtc, Ist);
            string E(string s) => WebUtility.HtmlEncode(s);

            return $@" <p>Dear {E(m.UserName)},</p><p>Your QPS Portal account <b>{E(m.UserCode)}</b> was just signed in.</p>
                       <table cellpadding='4' style='border-collapse:collapse;font-family:Segoe UI,Arial;font-size:13px'>
                       <tr><td><b>Time</b></td><td>{istTime:dd-MMM-yyyy hh:mm:ss tt} IST</td></tr>
                       <tr><td><b>IP address</b></td><td>{E(m.IpAddress)}</td></tr>
                       <tr><td><b>Device / browser</b></td><td>{E(m.UserAgent)}</td></tr>
                       </table>
                       <p>If this was you, no action is needed.<br/>
                       If you did <b>not</b> sign in, please change your password immediately and contact the QPS admin team.</p>"+
                       EmailFooter.Signature;

        }

        private static readonly TimeZoneInfo Ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");


    }
}
