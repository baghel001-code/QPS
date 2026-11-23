using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Common
{
    public class ApiNames
    {
        public string? MasterApi { get; set; }
        public string? HRP_GET_EMP_LOGIN_API { get; set; }
        public string? GET_VENDOR_LOGIN_API { get; set; }
        public string? HRP_GET_EMP_DETAIL_CODE { get; set; }
        public string? GET_EMP_COP_LOGIN_API { get; set; }
        public string? INSERT_UPDATE_ROLE { get; set; }
        public string? CREATE_PPS_REQUEST { get; set; }
        public string? UPDATE_STATUS_PPS_REQUEST { get; set; }
        public string? SELECT_ALL_ROLES { get; set; }
        public string? SELECT_ROLES_LIST { get; set; }
        public string? SELECT_ALL_PPSREQUEST { get; set; }
        public string? SELECT_ROLEDETAILS_ON_ID { get; set; }
        public string? SELECT_LOGGEDIN_USER_ON_EMP_CODE { get; set; }
        public string? VALIDATE_USER_SESSION { get; set; }
        public string? ACTIVATE_USER_SESSION { get; set; } 
        public string? CLEAR_USER_SESSION { get; set; }
        public string? GET_ALL_USERS { get; set; }

        public string? FIND_USER_BY_LOGIN { get; set; }
        public string? RECORD_FAILED_LOGIN { get; set; }
        public string? LOCK_OUT { get; set; }
        public string? CLEAR_LOCKOUT { get; set; }
        public string? SET_PASSWORD_HASH { get; set; }
        public string? SAVE_PWD_RESET_TKN { get; set; }
        public string? FIND_USER_BY_PWD_RST_TKN { get; set; }
        public string? CONSUME_PWD_RST_TKN { get; set; }

        public string? GET_QAMLISTTOMAIL { get; set; }
        public string? GET_ALL_ASSIGNPENDING_INSPN { get; set; }
        public string? GET_ALL_VENDORS { get; set; }
        public string? GET_VENDOR_EXCEL_DATA { get; set; }
        public string? GET_DASHBOARD_DATA { get; set; }
        public string? GET_VENDOR_WISE_POS { get; set; }
        public string? GET_ALL_HIERARCHY_LIST { get; set; }
        public string? GET_ALL_EMAIL_MGT_LIST { get; set; }
        public string? GET_PO_DETAILS { get; set; }
        public string? GET_GST_DETAILS { get; set; }
        public string? GET_MASTER_DATA { get; set; }
        public string? GET_VENDOR_BY_CODE { get; set; }
        public string? GET_INSPECTION_REQUEST_BY_ID { get; set; }
        public string? GET_VENDOR_ADDRESS { get; set; }
        public string? GET_SAP_API { get; set; }
        public string? GET_SAP_VENDOR_API { get; set; }
        public string? SET_USER { get; set; }
        public string? UPLOAD_VENDOR_EXCEL { get; set; }
        public string? SET_VENDOR { get; set; }
        public string? ADD_ADDRESS { get; set; }
        public string? ADDEDIT_HIERARCHY { get; set; }
        public string? ADDEDIT_EMAILMGT { get; set; }
        public string? GET_ALL_COLORCODE_LIST { get; set; }

        public string? ADDEDIT_COLORCODE { get; set; }
        public string? ADD_EDIT_AUDIT_REQUEST { get; set; }
        public string? GET_ALL_AUDIT_REQUEST { get; set; }
        public string? GET_AUDIT_REQUEST_BY_ID { get; set; }
        public string? GET_ALL_ELEMENTS_MASTER { get; set; }
        public string? GET_ALL_DOCUMENTS_MASTER { get; set; }
        public string? GET_ALL_ATTACHMENTS_MASTER { get; set; }

        public string? START_AUDIT { get; set; }
        public string? SAVE_STEP_ONE { get; set; }
        public string? SAVE_STEP_TWO { get; set; }
        public string? SAVE_STEP_THREE { get; set; }
        public string? SAVE_AUDIT_ELEMENT { get; set; }
        public string? SAVE_AUDIT_ATTACHMENTS { get; set; }
        public string? SAVE_AUDIT_COMMENTS { get; set; }
        public string? SUBMIT_AUDIT { get; set; }
        public string? GET_AUDIT_DETAIL { get; set; }

        public string? UPDATE_CM_STATUS { get; set; }
        public string? SAVE_INSPECTION { get; set; }
        public string? UPDATE_OFFEREDQTY { get; set; }
        public string? UPDATE_LINK_PO { get; set; }

        public string? ASN_REQUEST_CREATE { get; set; }
        public string? UPDATE_ASN_NUMBER { get; set; }
        public string? UPDATE_GRD_DETAILS { get; set; }
        public string? GET_ASN_SUMMARY { get; set; }
        public string? GET_PENDING_ASN_REQUEST { get; set; }
        public string? GET_ASN_INSPECTIONS { get; set; }
        public string? GET_ASN_BY_ID { get; set; }
        public string? SAP_PO_WISE_ASN { get; set; }
        public string? SAP_PO_DOWNLOAD { get; set; }
        public string? SAVE_INSPECT_DATA { get; set; }
        public string? GET_INSPECT_DATA { get; set; }
        public string? GET_INSPECTION_REPORT_VIEW { get; set; }
        public string? GET_INSPECTION_PDF { get; set; }
        public string? UPDATE_DOI { get; set; }
        public string? ASSIGN_INSPECTOR { get; set; }
        public string? ASSIGN_INSPECTOR_FREEZE { get; set; }
        public string? SELECT_USER_ON_EMP_CODE { get; set; }

        public string? GET_ALL_MENUS { get; set; }
        public string? GET_MENUS_LIST { get; set; }
        public string? SET_MENU { get; set; }

        public string? SET_FLAGS { get; set; }

        public string? GET_BSR_LIST { get; set; }
        public string? BSR_LIST_REPORT_PDF { get; set; }
        public string? BSR_LIST_REPORT_PDF_WITH_IMAGE { get; set; }
        public string? BSR_LIST_REPORT_EXCEL { get; set; }
        public string? BSR_LIST_REPORT_EXCEL_WITH_IMAGE { get; set; }
        public string? BSR_GET_DISPLAY_FILTERS { get; set; }

        public string? BSR_REPORT { get; set; }
        public string? QA_REPORT { get; set; }
        public string? PENDING_RECEIVED_ONGOING_INSPECTIONS { get; set; }
        public string? EXPORT_EXCEL_PENDING_RECEIVED_ONGOING_INSPECTIONS { get; set; }
        public string? EXPORT_INSPECTIONS { get; set; }
        public string? SYNC_PO_DETAILS { get; set; }
        public string? GET_LIST_BASED_ON_CMD { get; set; }

        public string? CREATE_DAILY_EMAIL { get; set; }
        public string? GET_PENDING_EMAILS { get; set; }
        public string? MARK_STATUS { get; set; }
    }
}
