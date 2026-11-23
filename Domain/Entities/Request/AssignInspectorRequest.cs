using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Request
{
    public class AssignInspectorRequest
    {
        public int InspId { get; set; }
        public required string  INSPECTOR_EMP_CODE { get; set; }
        public decimal OfferedQty { get; set; }
        public int CreatedBy { get; set; }
    }
    public class AssignInspectorFreezeRequest
    {
        public bool IsFreeze { get; set; }
        public DateTime? QA_ASSIGNED_DOI { get; set; }
        public int CreatedBy { get; set; }
        public string Details { get; set; }
    }

}
