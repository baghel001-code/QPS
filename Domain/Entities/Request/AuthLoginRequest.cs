using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Request
{
    public class AuthLoginRequest 
    {
        public string UserCode { get; set; } = "";
        public AccountType AccountType { get; set; }
        public string tokenHash { get; set; } = "";
        public string passwordHash { get; set; } = "";
        public bool rotateSecurityStamp { get; set; }
        public DateTimeOffset untilUtc { get; set; }
        public DateTimeOffset expiresUtc { get; set; }
    }

   
}
