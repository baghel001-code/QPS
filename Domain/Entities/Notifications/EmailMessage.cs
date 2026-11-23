using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Notifications
{
    public sealed class EmailMessage
    {
        public required string From { get; init; } = "";
        public IReadOnlyList<string> To { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Cc { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Bcc { get; init; } = Array.Empty<string>();
        public string Subject { get; init; } = "";
        public string Body { get; init; } = "";
        public bool IsBodyHtml { get; init; } = true;
        public List<MailAttachmentModel>? Attachments { get; init; } = new();
    }

    public class MailAttachmentModel
    {
        public string FileName { get; set; }

        public byte[] FileBytes { get; set; }

        public string FilePath { get; set; }

        public string ContentType { get; set; }
    }

}
