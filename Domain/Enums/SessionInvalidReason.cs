using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum SessionInvalidReason
    {
        None,
        Expired,       // exp claim passed, or required claims missing
        ForcedLogout,  // resigned/deactivated, or duplicate-login token replaced
        IdleTimeout    
    }
}
