using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.AppSettings
{
    public class EmailServerSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public string SmtpUser { get; set; } = string.Empty;
        public string SmtpPwd { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public bool SmtpDefaultCredential { get; set; }
        public bool EnableSsl { get; set; } = false;
        public string From { get; set; } = string.Empty;
        public string DefaultTestToMailId { get; set; } = string.Empty;
    }
}
