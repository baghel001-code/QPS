using Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Modals
{
    public class MasterColorCode
    {
        public int COLOR_CODE_ID { get; set; }

        public string COLOR_CODE { get; set; } = string.Empty;

        public string DESCRIPTION { get; set; } = string.Empty;

        public bool ACTIVE { get; set; }

        public int ORDER_NO { get; set; }
    }
    public class MasterColorCodeDto
    {
        public int? ColorCodeId { get; set; }

        public string ColorCode { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool Active { get; set; }

        public int OrderNo { get; set; }

        public string Action { get; set; } = string.Empty;
    }
    public class MasterColorCodeResponse : BaseResponse
    {
        public List<MasterColorCode> ColorCodeList { get; set; } = new();
    }
}
