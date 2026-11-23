using Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Response
{
    public class IssueSessionResponce : BaseResponse
    {
        public string SessionToken { get; set; } = "";
        public bool IsValid { get; set; }
    }

    public class SessionResponceModel
    {
        public string TOKEN { get; set; } = "";
        public bool IS_VALID { get; set; }
    }
}
