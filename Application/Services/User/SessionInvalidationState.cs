using Application.Interfaces.V1.User;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.User
{
    public class SessionInvalidationState : ISessionInvalidationState
    {
        public SessionInvalidReason Reason { get; set; } = SessionInvalidReason.None;
    }
}
