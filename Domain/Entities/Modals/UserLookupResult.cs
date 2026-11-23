using Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Modals
{
    public sealed class UserLookupResult
    {
        public UserDetails User { get; init; } = new();

        public bool Success { get; init; }

        public bool Found { get; init; }

        public string? ErrorMessage { get; init; }
    }

 
}
