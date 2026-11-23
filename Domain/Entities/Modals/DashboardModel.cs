using Domain.Entities.Base;
using Domain.Entities.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Modals
{
    public class DashboardModel
    {
        public int POID { get; set; }
        public int INSP_ID { get; set; }
        public DateTime DOI { get; set; }
        public bool IS_DATE_ASSIGNED_BY_QA { get; set; }
        public bool IS_INSPECTOR_ASSIGNED { get; set; }
        public int REGION_ID { get; set; }

    }
    public class QAFREEZEModel
    {

        public DateTime INSPECTION_DATE { get; set; }
        public string EMP_NAME { get; set; }
        public string EMP_CODE { get; set; }
        public DateTime CREATED_DATE { get; set; }

    }
    public class DashboardModelResponse : BaseResponse
    {
        public List<DashboardModel> ListofInspections { get; set; } = new();
        public List<QAFREEZEModel> ListofFreezeQA { get; set; } = new();
    }

    public class DashboardDateSummary
    {
        public DateTime DOI { get; set; }
        public int TotalRequests { get; set; }
        public int QAMDOIPending { get; set; }
        public int QAMDOIAssigned { get; set; }
        public int Pending { get; set; }
        public int Assigned { get; set; }
        public List<int> INSP_IDS { get; set; } = new();
        public List<int> View_Pendiing_INSP_IDS { get; set; } = new();
        public List<int> View_Assigned_INSP_IDS { get; set; } = new();
        public List<int> View_Pending_DOI_INSP_IDS { get; set; } = new();
        public List<int> View_Assigned_DOI_INSP_IDS { get; set; } = new();
        public List<int> REGION_IDS { get; set; } = new();

        public bool IsFreeze { get; set; }
        public DateTime FreezeOn { get; set; }
        public string FinalizeBy { get; set; }
    }

    //Used in Inspector Assignment
    public class InspectionAssignRowDto
    {
        public int INSP_ID { get; set; }
        public int POID { get; set; }
        public int INSPECTOR_EMP_CODE { get; set; }
        public string? INSPECTION_NO { get; set; }
        public string PO_NUMBER { get; set; }
        public string VENDOR_NAME { get; set; }
        public string VENDOR_CODE { get; set; }
        public string CATEGORY { get; set; }
        public string BUYER_NAME { get; set; }
        public string BUYER_ID { get; set; }
        public string ADDRESS_LINE1 { get; set; }
        public string CITY { get; set; }
        public string PINCODE { get; set; }
        public string EMAIL_IDS { get; set; }
        public string MOBILE_NUMBERS { get; set; }
        public string CONTACT_PERSON { get; set; }
        public int REGION_ID { get; set; }
        public decimal OFFERED_QTY { get; set; }
        public int STATUS_CODE { get; set; }
        public bool IS_CHANGEABLE_BY_ADMIN { get; set; }
        public string RequestNo { get; set; }


        public DateTime? SelectedDate;
        public TimeSpan? SelectedTime;

        private DateTime? _qaAssignedDoi;
        public DateTime? QA_ASSIGNED_DOI
        {
            get => SelectedDate.HasValue ? SelectedDate.Value.Date : _qaAssignedDoi;

            set
            {
                _qaAssignedDoi = value;

                if (value.HasValue)
                {
                    SelectedDate = value.Value.Date;

                }
                else
                {
                    SelectedDate = null;

                }
            }
        }

        public DateTime DATE_OF_INSPECTION { get; set; }
       
        public List<InspectorDto> InspectorsList { get; set; }

        public bool ISQADOICHANGED { get; set; }
        public bool ISQACHANGED { get; set; }
        public bool IS_QC_AT_DC { get; set; }
        public string AuditLog { get; set; }
        public InspectionAssignRowDto Clone()
        {
            return new InspectionAssignRowDto
            {
                INSP_ID = INSP_ID,
                POID = POID,
                INSPECTOR_EMP_CODE = INSPECTOR_EMP_CODE,
                INSPECTION_NO = INSPECTION_NO,
                PO_NUMBER = PO_NUMBER,
                VENDOR_NAME = VENDOR_NAME,
                VENDOR_CODE = VENDOR_CODE,
                CATEGORY = CATEGORY,
                BUYER_NAME = BUYER_NAME,
                BUYER_ID = BUYER_ID,
                ADDRESS_LINE1 = ADDRESS_LINE1,
                CITY = CITY,
                PINCODE = PINCODE,
                EMAIL_IDS = EMAIL_IDS,
                MOBILE_NUMBERS = MOBILE_NUMBERS,
                CONTACT_PERSON = CONTACT_PERSON,
                REGION_ID = REGION_ID,
                OFFERED_QTY = OFFERED_QTY,
                STATUS_CODE = STATUS_CODE,
                IS_CHANGEABLE_BY_ADMIN = IS_CHANGEABLE_BY_ADMIN,
                RequestNo = RequestNo,
                SelectedDate = SelectedDate,
                SelectedTime = SelectedTime,
                _qaAssignedDoi = _qaAssignedDoi,          // copy backing field directly, bypassing the setter's side effect
                DATE_OF_INSPECTION = DATE_OF_INSPECTION,
                ISQADOICHANGED = ISQADOICHANGED,
                ISQACHANGED = ISQACHANGED,
                AuditLog = AuditLog
            };
        }
    }

    public class InspectorDto
    {
        public int INSPECTOR_CODE { get; set; }
        public string InspectorName { get; set; }
        public List<string> RegionIds { get; set; } = new List<string>();
    }

    public class AssignedInspectorRequest
    {
        public DateTime DOI { get; set; }
        public string IDs { get; set; }
        public List<InspectionAssignRowDto> Assignments { get; set; }
    }

    public class INSPECTIONLISTMODEL
    {

        public DateTime DATE_OF_INSPECTION { get; set; }
        public string INSPECTION_TYPE { get; set; }
        public string? INSPECTION_NO { get; set; }
        public int SAMPLE_SIZE { get; set; }
        public string? INSPECTOR_EMP_CODE { get; set; }
        public string? EMP_NAME { get; set; }
        public string? ARTICLE { get; set; }
        public string PO_NUMBER { get; set; }
        public string? ARTICLE_DESC { get; set; }
        public string VENDOR_CODE { get; set; }
        public string VENDOR_NAME { get; set; }
        public decimal TOTAL_QTY { get; set; }
        public decimal OFFERED_QTY { get; set; }
        public string APPARELS { get; set; }
        public string CATEGORY { get; set; }
        public string REGION { get; set; }
        public string ADDRESS_LINE1 { get; set; }
        public string CONTACT_PERSON { get; set; }
        public string MOBILE_NUMBERS { get; set; }
        public string CITY { get; set; }
        public string PINCODE { get; set; }
        public string EMAIL_IDS { get; set; }
        public string BUYER_NAME { get; set; }
        public string BUYER_ID { get; set; }

        public int INSP_ID { get; set; }
        public string LINK_PO_NUMBER { get; set; }
        public int POID { get; set; }
        public DateTime QA_ASSIGNED_DOI { get; set; }
        public bool IS_INSPECTOR_ASSIGNED { get; set; }
        public bool IS_DATE_ASSIGNED_BY_QA { get; set; }
        public int REGION_ID { get; set; }
        public string MC { get; set; }
        public string MC_TEXT { get; set; }
        public decimal MRP { get; set; }
        public string BRAND_ID { get; set; }
        public string BRAND_ID2 { get; set; }
        public string FIT { get; set; }
        public int STATUS_CODE { get; set; }
        public bool IS_CHANGEABLE_BY_ADMIN { get; set; }

        public bool IS_QC_AT_DC { get; set; }

    }
    public class AssignPendingInspectionListResponse : BaseResponse
    {
        public List<INSPECTIONLISTMODEL> ListofInspections { get; set; } = new();
        public List<QAMADIN_BUYER_QA_EMAILS_MODEL> EmailIds { get; set; } = new();
    }

    public class QAMADIN_BUYER_QA_EMAILS_MODEL
    {
        public string EMP_MAIL_ID { get; set; }
        public string EMP_CODE { get; set; }
        public int ROLE_ID { get; set; }

    }
}
