using Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Notifications
{
    public class EmailRecipient
    {
        public int ID { get; set; }
        public string RECIPIENT { get; set; }
        public string SUBJECT { get; set; }
        public string BODY { get; set; }
        public byte[]? FileBytes { get; set; }
    }

    public class GetAllRecipients : BaseResponse
    {
        public List<EmailRecipient> RecipientList { get; set; } = new();
    }

  
}
