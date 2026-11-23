using Domain.Entities.Base;
using Domain.Entities.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Modals
{
    public class MASTER_HIERARCHY_MODEL
    {
        public string EMP_CODE { get; set; } = string.Empty;

        public string EMP_NAME { get; set; } = string.Empty;

        public string EMAIL_ID { get; set; } = string.Empty;

        public string MOBILE_NO { get; set; } = string.Empty;

        public int ROLE_ID { get; set; }

        public string ROLE_NAME { get; set; } = string.Empty;

        public string REPORTING_EMP_ID { get; set; } = string.Empty;

        public string REPORTING_MANAGER { get; set; } = string.Empty;

        public bool IS_ACTIVE { get; set; }

        public DateTime CREATED_ON { get; set; }
        public int HIERARCHY_LEVEL { get; set; }
    }

    public class HierarchyDto
    {
        public string EmpCode { get; set; } = string.Empty;

        public string EmpName { get; set; } = string.Empty;

        public string? EmailId { get; set; }

        public string? MobileNo { get; set; }

        public int RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public string? ReportingEmpId { get; set; }

        public string? ReportingManager { get; set; }

        public ICollection<MasterData> ReportingManagerList { get; set; } = new List<MasterData>();

        public ICollection<MasterData> RoleList { get; set; } = new List<MasterData>();

        public bool IsActive { get; set; }

        public DateTime CreatedOn { get; set; }

        public int HierarchyLevel { get; set; }
        public string Action { get; set; } = string.Empty;
    }

    public class HierarchyListResponse : BaseResponse
    {
        public List<MASTER_HIERARCHY_MODEL> HierarchyList { get; set; } = new();
    }
}
