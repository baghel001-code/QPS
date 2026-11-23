using Domain.Entities.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Common
{
    public static class AllApiNames
    {
        public static string MasterApi { get; set; } = string.Empty;
        public static string HRP_GET_EMP_LOGIN_API { get; set; } = string.Empty;
        public static string GET_VENDOR_LOGIN_API { get; set; } = string.Empty;
        public static string HRP_GET_EMP_DETAIL_CODE { get; set; } = string.Empty;
        public static string GET_EMP_COP_LOGIN_API { get; set; } = string.Empty;
        public static string INSERT_UPDATE_ROLE { get; set; } = string.Empty;
        public static string CREATE_PPS_REQUEST { get; set; } = string.Empty;
        public static string UPDATE_STATUS_PPS_REQUEST { get; set; } = string.Empty;
        public static string SELECT_ALL_ROLES { get; set; } = string.Empty;
        public static string SELECT_ALL_PPSREQUEST { get; set; } = string.Empty;
        public static string? SELECT_ROLES_LIST { get; set; } = string.Empty;
        public static string SELECT_ROLEDETAILS_ON_ID { get; set; } = string.Empty;
        public static string GET_ALL_USERS { get; set; } = string.Empty;

        public static string FIND_USER_BY_LOGIN { get; set; } = string.Empty;
        public static string RECORD_FAILED_LOGIN { get; set; } = string.Empty;
        public static string LOCK_OUT { get; set; } = string.Empty;
        public static string CLEAR_LOCKOUT { get; set; } = string.Empty;
        public static string SET_PASSWORD_HASH { get; set; } = string.Empty;
        public static string SAVE_PWD_RESET_TKN { get; set; } = string.Empty;
        public static string FIND_USER_BY_PWD_RST_TKN { get; set; } = string.Empty;
        public static string CONSUME_PWD_RST_TKN { get; set; } = string.Empty;


        public static string GET_QAMLISTTOMAIL { get; set; } = string.Empty;
        public static string GET_ALL_ASSIGNPENDING_INSPN { get; set; } = string.Empty;
        public static string GET_ALL_VENDORS { get; set; } = string.Empty;
        public static string GET_VENDOR_EXCEL_DATA { get; set; } = string.Empty;
        public static string GET_VENDOR_WISE_POS { get; set; } = string.Empty;

        public static string GET_ALL_HIERARCHY_LIST { get; set; } = string.Empty;
        public static string GET_ALL_EMAIL_MGT_LIST { get; set; } = string.Empty;
        public static string GET_DASHBOARD_DATA { get; set; } = string.Empty;
        public static string GET_PO_DETAILS { get; set; } = string.Empty;
        public static string GET_GST_DETAILS { get; set; } = string.Empty;
        public static string GET_MASTER_DATA { get; set; } = string.Empty;
        public static string GET_VENDOR_BY_CODE { get; set; } = string.Empty;
        public static string GET_INSPECTION_REQUEST_BY_ID { get; set; } = string.Empty;
        public static string GET_VENDOR_ADDRESS { get; set; } = string.Empty;
        public static string GET_SAP_API { get; set; } = string.Empty;
        public static string GET_SAP_VENDOR_API { get; set; } = string.Empty;
        public static string? SET_USER { get; set; } = string.Empty;
        public static string? UPLOAD_VENDOR_EXCEL { get; set; } = string.Empty;
        public static string? SET_VENDOR { get; set; } = string.Empty;
        public static string? ADD_ADDRESS { get; set; } = string.Empty;
        public static string? ADDEDIT_HIERARCHY { get; set; } = string.Empty;
        public static string? ADDEDIT_EMAILMGT { get; set; } = string.Empty;
        public static string GET_ALL_COLORCODE_LIST { get; set; } = string.Empty;
        public static string ADDEDIT_COLORCODE { get; set; } = string.Empty;
        public static string ADD_EDIT_AUDIT_REQUEST { get; set; } = string.Empty;
        public static string GET_ALL_AUDIT_REQUEST { get; set; } = string.Empty;
        public static string GET_AUDIT_REQUEST_BY_ID { get; set; } = string.Empty;
        public static string GET_ALL_ELEMENTS_MASTER { get; set; } = string.Empty;
        public static string GET_ALL_DOCUMENTS_MASTER { get; set; } = string.Empty;
        public static string GET_ALL_ATTACHMENTS_MASTER { get; set; } = string.Empty;
        public static string? START_AUDIT { get; set; } = string.Empty;
        public static string? SAVE_STEP_ONE { get; set; } = string.Empty;
        public static string? SAVE_STEP_TWO { get; set; } = string.Empty;
        public static string? SAVE_STEP_THREE { get; set; } = string.Empty;
        public static string? SAVE_AUDIT_ELEMENT { get; set; } = string.Empty;
        public static string? SAVE_AUDIT_ATTACHMENTS { get; set; } = string.Empty;
        public static string? SAVE_AUDIT_COMMENTS { get; set; } = string.Empty;
        public static string? SUBMIT_AUDIT { get; set; } = string.Empty;
        public static string? GET_AUDIT_DETAIL { get; set; } = string.Empty;
        public static string? UPDATE_CM_STATUS { get; set; } = string.Empty;
        public static string? SAVE_INSPECTION { get; set; } = string.Empty;
        public static string? UPDATE_OFFEREDQTY { get; set; } = string.Empty;
        public static string? SAVE_INSPECT_DATA { get; set; } = string.Empty;
        public static string? GET_INSPECT_DATA { get; set; } = string.Empty;
        public static string? GET_INSPECTION_REPORT_VIEW { get; set; } = string.Empty;
        public static string? GET_INSPECTION_PDF { get; set; } = string.Empty;
        public static string? UPDATE_LINK_PO { get; set; } = string.Empty;

        public static string? ASN_REQUEST_CREATE { get; set; } = string.Empty;
        public static string? UPDATE_ASN_NUMBER { get; set; } = string.Empty;
        public static string? UPDATE_GRD_DETAILS { get; set; } = string.Empty;
        public static string? GET_ASN_SUMMARY { get; set; } = string.Empty;
        public static string? GET_PENDING_ASN_REQUEST { get; set; } = string.Empty;
        public static string? GET_ASN_INSPECTIONS { get; set; } = string.Empty;
        public static string? GET_ASN_BY_ID { get; set; } = string.Empty;
        public static string? SAP_PO_WISE_ASN { get; set; } = string.Empty;
        public static string SAP_PO_DOWNLOAD { get; set; } = string.Empty;
        public static string? UPDATE_DOI { get; set; } = string.Empty;
        public static string? ASSIGN_INSPECTOR { get; set; } = string.Empty;
        public static string? ASSIGN_INSPECTOR_FREEZE { get; set; } = string.Empty;
        public static string SELECT_USER_ON_EMP_CODE { get; set; } = string.Empty;
        public static string VALIDATE_USER_SESSION { get; set; } = string.Empty;
        public static string ACTIVATE_USER_SESSION { get; set; } = string.Empty;
        public static string CLEAR_USER_SESSION { get; set; } = string.Empty;
        public static string SELECT_LOGGEDIN_USER_ON_EMP_CODE { get; set; } = string.Empty;
        public static string? GET_ALL_MENUS { get; set; } = string.Empty;
        public static string? GET_MENUS_LIST { get; set; } = string.Empty;
        public static string? SET_MENU { get; set; } = string.Empty;

        public static string? SET_FLAGS { get; set; } = string.Empty;

        public static string? GET_BSR_LIST { get; set; } = string.Empty;
        public static string? BSR_LIST_REPORT_PDF { get; set; } = string.Empty;
        public static string? BSR_LIST_REPORT_PDF_WITH_IMAGE { get; set; } = string.Empty;
        public static string? BSR_LIST_REPORT_EXCEL { get; set; } = string.Empty;
        public static string? BSR_LIST_REPORT_EXCEL_WITH_IMAGE { get; set; } = string.Empty;
        public static string? BSR_GET_DISPLAY_FILTERS { get; set; } = string.Empty;
        public static string? BSR_REPORT { get; set; } = string.Empty;
        public static string? QA_REPORT { get; set; } = string.Empty;

        public static string PENDING_RECEIVED_ONGOING_INSPECTIONS { get; set; } = string.Empty;
        public static string EXPORT_EXCEL_PENDING_RECEIVED_ONGOING_INSPECTIONS { get; set; } = string.Empty;

        public static string? EXPORT_INSPECTIONS { get; set; } = string.Empty;
        public static string? SYNC_PO_DETAILS { get; set; } = string.Empty;

        public static string? CREATE_DAILY_EMAIL { get; set; } = string.Empty;
        public static string? GET_PENDING_EMAILS { get; set; } = string.Empty;
        public static string? MARK_STATUS { get; set; } = string.Empty;

        public static string? GET_LIST_BASED_ON_CMD { get; set; } = string.Empty;

        public static void Initialize(ApiNames apiNames)
        {
            MasterApi = apiNames.MasterApi!;
            HRP_GET_EMP_LOGIN_API = apiNames.HRP_GET_EMP_LOGIN_API!;
            GET_VENDOR_LOGIN_API = apiNames.GET_VENDOR_LOGIN_API!;
            HRP_GET_EMP_DETAIL_CODE = apiNames.HRP_GET_EMP_DETAIL_CODE!;
            GET_EMP_COP_LOGIN_API = apiNames.GET_EMP_COP_LOGIN_API!;
            INSERT_UPDATE_ROLE = apiNames.INSERT_UPDATE_ROLE!;
            CREATE_PPS_REQUEST = apiNames.CREATE_PPS_REQUEST!;
            UPDATE_STATUS_PPS_REQUEST = apiNames.UPDATE_STATUS_PPS_REQUEST!;
            SELECT_ALL_ROLES = apiNames.SELECT_ALL_ROLES!;
            SELECT_ROLES_LIST = apiNames.SELECT_ROLES_LIST!;
            SELECT_ALL_PPSREQUEST = apiNames.SELECT_ALL_PPSREQUEST!;
            SELECT_ROLEDETAILS_ON_ID = apiNames.SELECT_ROLEDETAILS_ON_ID!;
            GET_ALL_USERS = apiNames.GET_ALL_USERS!;

            FIND_USER_BY_LOGIN = apiNames.FIND_USER_BY_LOGIN!;
            RECORD_FAILED_LOGIN = apiNames.RECORD_FAILED_LOGIN!;
            LOCK_OUT = apiNames.LOCK_OUT!;
            CLEAR_LOCKOUT = apiNames.CLEAR_LOCKOUT!;
            SET_PASSWORD_HASH = apiNames.SET_PASSWORD_HASH!;
            SAVE_PWD_RESET_TKN = apiNames.SAVE_PWD_RESET_TKN!;
            FIND_USER_BY_PWD_RST_TKN = apiNames.FIND_USER_BY_PWD_RST_TKN!;
            CONSUME_PWD_RST_TKN = apiNames.CONSUME_PWD_RST_TKN!;



            GET_QAMLISTTOMAIL = apiNames.GET_QAMLISTTOMAIL!;
            GET_ALL_ASSIGNPENDING_INSPN = apiNames.GET_ALL_ASSIGNPENDING_INSPN!;
            GET_ALL_VENDORS = apiNames.GET_ALL_VENDORS!;
            GET_VENDOR_EXCEL_DATA = apiNames.GET_VENDOR_EXCEL_DATA!;
            GET_DASHBOARD_DATA = apiNames.GET_DASHBOARD_DATA!;
            GET_VENDOR_WISE_POS = apiNames.GET_VENDOR_WISE_POS!;
            GET_ALL_HIERARCHY_LIST = apiNames.GET_ALL_HIERARCHY_LIST!;
            GET_ALL_EMAIL_MGT_LIST = apiNames.GET_ALL_EMAIL_MGT_LIST!;
            GET_PO_DETAILS = apiNames.GET_PO_DETAILS!;
            GET_GST_DETAILS = apiNames.GET_GST_DETAILS!;
            GET_MASTER_DATA = apiNames.GET_MASTER_DATA!;
            GET_VENDOR_BY_CODE = apiNames.GET_VENDOR_BY_CODE!;
            GET_INSPECTION_REQUEST_BY_ID = apiNames.GET_INSPECTION_REQUEST_BY_ID!;
            GET_VENDOR_ADDRESS = apiNames.GET_VENDOR_ADDRESS!;
            GET_SAP_API = apiNames.GET_SAP_API!;
            GET_SAP_VENDOR_API = apiNames.GET_SAP_VENDOR_API!;
            SET_USER = apiNames.SET_USER!;
            UPLOAD_VENDOR_EXCEL = apiNames.UPLOAD_VENDOR_EXCEL!;
            SET_VENDOR = apiNames.SET_VENDOR!;
            ADD_ADDRESS = apiNames.ADD_ADDRESS!;
            ADDEDIT_HIERARCHY = apiNames.ADDEDIT_HIERARCHY!;
            ADDEDIT_EMAILMGT = apiNames.ADDEDIT_EMAILMGT!;
            GET_ALL_COLORCODE_LIST = apiNames.GET_ALL_COLORCODE_LIST!;
            ADDEDIT_COLORCODE = apiNames.ADDEDIT_COLORCODE!;
            ADD_EDIT_AUDIT_REQUEST = apiNames.ADD_EDIT_AUDIT_REQUEST!;
            GET_ALL_AUDIT_REQUEST = apiNames.GET_ALL_AUDIT_REQUEST!;
            GET_AUDIT_REQUEST_BY_ID = apiNames.GET_AUDIT_REQUEST_BY_ID!;
            GET_ALL_ELEMENTS_MASTER = apiNames.GET_ALL_ELEMENTS_MASTER!;
            GET_ALL_DOCUMENTS_MASTER = apiNames.GET_ALL_DOCUMENTS_MASTER!;
            GET_ALL_ATTACHMENTS_MASTER = apiNames.GET_ALL_ATTACHMENTS_MASTER!;
            START_AUDIT = apiNames.START_AUDIT!;
            SAVE_STEP_ONE = apiNames.SAVE_STEP_ONE!;
            SAVE_STEP_TWO = apiNames.SAVE_STEP_TWO!;
            SAVE_STEP_THREE = apiNames.SAVE_STEP_THREE!;
            SAVE_AUDIT_ELEMENT = apiNames.SAVE_AUDIT_ELEMENT!;
            SAVE_AUDIT_ATTACHMENTS = apiNames.SAVE_AUDIT_ATTACHMENTS!;
            SAVE_AUDIT_COMMENTS = apiNames.SAVE_AUDIT_COMMENTS!;
            SUBMIT_AUDIT = apiNames.SUBMIT_AUDIT!;
            GET_AUDIT_DETAIL = apiNames.GET_AUDIT_DETAIL!;
            UPDATE_CM_STATUS = apiNames.UPDATE_CM_STATUS!;
            SAVE_INSPECTION = apiNames.SAVE_INSPECTION!;
            UPDATE_OFFEREDQTY = apiNames.UPDATE_OFFEREDQTY!;
            SAVE_INSPECT_DATA = apiNames.SAVE_INSPECT_DATA!;
            GET_INSPECT_DATA = apiNames.GET_INSPECT_DATA!;
            GET_INSPECTION_REPORT_VIEW = apiNames.GET_INSPECTION_REPORT_VIEW!;
            GET_INSPECTION_PDF = apiNames.GET_INSPECTION_PDF!;
            UPDATE_DOI = apiNames.UPDATE_DOI!;
            UPDATE_LINK_PO = apiNames.UPDATE_LINK_PO!;

            ASN_REQUEST_CREATE = apiNames.ASN_REQUEST_CREATE!;
            UPDATE_ASN_NUMBER = apiNames.UPDATE_ASN_NUMBER!;
            UPDATE_GRD_DETAILS = apiNames.UPDATE_GRD_DETAILS!;
            GET_ASN_SUMMARY = apiNames.GET_ASN_SUMMARY!;
            GET_PENDING_ASN_REQUEST = apiNames.GET_PENDING_ASN_REQUEST!;
            GET_ASN_INSPECTIONS = apiNames.GET_ASN_INSPECTIONS!;
            GET_ASN_BY_ID = apiNames.GET_ASN_BY_ID!;
            SAP_PO_WISE_ASN = apiNames.SAP_PO_WISE_ASN!;
            SAP_PO_DOWNLOAD = apiNames.SAP_PO_DOWNLOAD!;

            ASSIGN_INSPECTOR = apiNames.ASSIGN_INSPECTOR!;
            ASSIGN_INSPECTOR_FREEZE = apiNames.ASSIGN_INSPECTOR_FREEZE!;
            SELECT_USER_ON_EMP_CODE = apiNames.SELECT_USER_ON_EMP_CODE!;
            SELECT_LOGGEDIN_USER_ON_EMP_CODE = apiNames.SELECT_LOGGEDIN_USER_ON_EMP_CODE!;
            VALIDATE_USER_SESSION = apiNames.VALIDATE_USER_SESSION!;
            ACTIVATE_USER_SESSION = apiNames.ACTIVATE_USER_SESSION!;
            CLEAR_USER_SESSION = apiNames.CLEAR_USER_SESSION!;
            GET_ALL_MENUS = apiNames.GET_ALL_MENUS!;
            GET_MENUS_LIST = apiNames.GET_MENUS_LIST!;
            SET_MENU = apiNames.SET_MENU!;

            SET_FLAGS = apiNames.SET_FLAGS!;

            GET_BSR_LIST = apiNames.GET_BSR_LIST!;

            BSR_LIST_REPORT_PDF = apiNames.BSR_LIST_REPORT_PDF!;
            BSR_LIST_REPORT_PDF_WITH_IMAGE = apiNames.BSR_LIST_REPORT_PDF_WITH_IMAGE!;
            BSR_LIST_REPORT_EXCEL = apiNames.BSR_LIST_REPORT_EXCEL!;
            BSR_LIST_REPORT_EXCEL_WITH_IMAGE = apiNames.BSR_LIST_REPORT_EXCEL_WITH_IMAGE!;
            BSR_GET_DISPLAY_FILTERS = apiNames.BSR_GET_DISPLAY_FILTERS!;
            BSR_REPORT = apiNames.BSR_REPORT!;
            QA_REPORT = apiNames.QA_REPORT!;
            PENDING_RECEIVED_ONGOING_INSPECTIONS = apiNames.PENDING_RECEIVED_ONGOING_INSPECTIONS!;
            EXPORT_EXCEL_PENDING_RECEIVED_ONGOING_INSPECTIONS = apiNames.EXPORT_EXCEL_PENDING_RECEIVED_ONGOING_INSPECTIONS!;
            EXPORT_INSPECTIONS = apiNames.EXPORT_INSPECTIONS!;
            SYNC_PO_DETAILS = apiNames.SYNC_PO_DETAILS!;
            CREATE_DAILY_EMAIL = apiNames.CREATE_DAILY_EMAIL!;
            GET_PENDING_EMAILS = apiNames.GET_PENDING_EMAILS!;
            MARK_STATUS = apiNames.MARK_STATUS!;
            GET_LIST_BASED_ON_CMD = apiNames.GET_LIST_BASED_ON_CMD!;
        }
    }
}
