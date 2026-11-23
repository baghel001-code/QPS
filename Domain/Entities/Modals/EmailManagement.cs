using Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Modals
{
    public class EmailManagement
    {
        public string EMAIL_ID { get; set; } = string.Empty;
        public string NAME { get; set; } = string.Empty;
        public bool IS_ACTIVE { get; set; }
        public string CREATED_BY_EMP_CODE { get; set; } = string.Empty;
        public DateTime LAST_UPDATED_DATE { get; set; }
        
    }

    public class EmailManagementDto
    {
        public string? EmailId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string Created_By_Emp_Code { get; set; } = string.Empty;
        public DateTime Last_Updated_Date { get; set; }
        public string Action { get; set; } = string.Empty;

    }
    public class EmailManagementResponse : BaseResponse
    {
        public List<EmailManagement> EmailsList { get; set; } = new();
    }
}
