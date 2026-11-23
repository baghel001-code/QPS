using Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Modals
{

    public sealed class Auth_User_Row
    {
        public int ID { get; set; }
        public string USERCODE { get; set; } = "";
        public string USERNAME { get; set; } = "";
        public string? MOBILENUMBER { get; set; }
        public string? EMAIL { get; set; }
        public string? DISPLAYNAME { get; set; }
        public string? PASSWORDHASH { get; set; }
        public string? SECURITYSTAMP { get; set; }
        public bool ISACTIVE { get; set; }
        public int? FAILEDLOGINCOUNT { get; set; }          // NULL when no UserSecurity row (LEFT JOIN)
        public DateTimeOffset? LOCKOUTENDUTC { get; set; }
        public int ROLEID { get; set; }
        public string? REGION_IDS { get; set; }
        public string? GST_NO { get; set; }
        public bool? MUSTCHANGEPASSWORD { get; set; }       // NULL when no UserSecurity row
        public string ACCOUNTTYPE { get; set; } = "";
    }

   

}
