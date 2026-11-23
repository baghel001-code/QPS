using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.V1.User
{
    public interface ISessionInvalidationState
    {
        SessionInvalidReason Reason { get; set; }
    }

}
