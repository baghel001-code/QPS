using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Modals
{
    public class UserRoleContext
    {
        public bool IsQA { get; init; }
        public bool IsQAAdmin { get; init; }
        public bool IsQAM { get; init; }
        public bool IsVendor { get; init; }
        public bool IsQC { get; init; }
        public bool IsBuyer { get; init; }
        public bool IsCH { get; init; }
        public bool IsACH { get; init; }
        public bool IsAdmin { get; init; }
        public bool IsMerchandiser { get; init; }
        public bool IsBFT { get; init; }
        public bool IsASN { get; init; }
    }
}
