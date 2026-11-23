using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Common
{
    public static class CmdNames
    {
        public const string Get_Breadcrumbs = "Get_Breadcrumbs"; //Get Breadcrumbs
        public const string Get_All_Roles = "Get_All_Roles"; //Select All Roles

        public const string Get_All_PPSRequest = "Get_PPS_All_Request"; //Get_PPS_All_Request
        public const string Get_PPSRequest_Details = "Get_PPS_Request_Details"; //Get_PPS_Request_Details
        public const string Get_PPS_Request_History_Timeline = "Get_PPS_Request_History_Timeline"; //Select All PPS
        public const string Get_Generic_Article_Details = "Get_Generic_Article_Details"; //Select Article

        public const string Get_Vendor_Excel_Data = "VENDOR_EXCEL_DATA";
        public const string Get_All_Vendors = "Get_All_Vendors"; //Select All Vendors
        public const string Get_Vendor_Address = "Get_Vendor_Address"; //Select All Vendors
        public const string Get_Vendor_By_Code = "Get_Vendor_By_Code";
        public const string Get_InspRequest_By_Id = "Get_InspRequest_By_Id";
        public const string Get_Inspection_Report_Pdf = "INSPECTION_REPORT_PDF";
        public const string Get_Hierarcy_Record = "Get_Hierarchy_Record";
        public const string Get_ReportingManager_Master = "Get_ReportingManager_Master";
        public const string Get_PO_Details = "Get_PO_Details";
        public const string Get_GST_Details = "Get_GST_Details";
        public const string Get_Region = "GET_REGION";
        public const string Get_Region_With_QAM = "GET_REGION_WITH_QAM";
        public const string Get_Inspection_Type = "GET_INSPECTION_TYPE";
        public const string Get_Inspect_Data = "INSPECT_DETAILS_BY_INSPECTION_ID";
        public const string Get_Assigned_QAM = "QALIST_TO_MAIL";

        public const string Get_PassFail = "GET_PASSFAIL";
        public const string Get_Unit = "GET_UNIT";
        public const string Get_YesNo = "GET_YESNO";
        public const string Get_BUSINESS_HEAD = "GET_BUSINESS_HEAD";
        public const string GET_AUDTI_CHECKLIST = "GET_AUDTI_CHECKLIST";
        public const string Get_All_Excel_Vendors = "Get_All_Excel_Vendors";
        public const string Get_Fail_Reason = "GET_FAIL_REASON";
        public const string Get_Color_Code = "GET_COLOR_CODE";
        public const string Get_Abortive_Reason = "GET_ABORTIVE_REASONS";

        public const string Get_Hierarchy_Roles_List = "Get_Hierarchy_Roles_List";
        public const string Get_Roles_List = "Get_Roles_List"; //Select Active Roles List
        public const string Get_Role_Details_On_Role_Id = "Get_Role_Details_On_Role_Id"; //Select Roles Details On Role Id
        public const string Get_Role_Details_On_User_Id = "Get_Role_Details_On_User_Id"; //Select Roles Details On User Id
        public const string Get_All_Users = "Get_All_Users"; //Select All Users
        public const string Get_Region_Wise_Inspectors = "GET_REGION_WISE_INSPECTORS"; //Select All Users
        public const string Get_CM_Status = "GET_CM_STATUS"; //Select All Users
        public const string Get_User_Details_On_User_Code = "Get_User_Details_On_User_Code"; //Select User On Emp Code

        public const string Get_All_Menu = "Get_All_Menu"; //Select All Menu
        public const string Get_Menu_List = "Get_Menu_List"; //Select All Menu
        public const string Get_Menu_List_Public = "Get_Menu_List_Public"; //Select All Menu for Public

        public const string Get_All_Content = "Get_All_Content"; //Select All Content
        public const string Get_All_ContentDetails = "Get_All_ContentDetails"; //Select All ContentDetails On Content Id
        public const string Get_Public_Content = "Get_Public_Content"; //Select All Content And ContentDetails On Page Id for public
        public const string Get_Content_Archive = "Get_Content_Archive"; //Select All Archive Content And ContentDetails On Page Id for Admin
        public const string Get_Public_Content_Archive = "Get_Public_Content_Archive"; //Select All Archive Content And ContentDetails On Page Id for public

        public const string Approve_ContentDetails = "Approve_ContentDetails"; //Approver Approve Content
        public const string Reject_ContentDetails = "Reject_ContentDetails"; //Approver Reject Content
        public const string Activate_Deactivate_ContentMaster = "Activate_Deactivate_ContentMaster"; //Activate Content
        public const string Activate_Deactivate_ContentDetails = "Activate_Deactivate_ContentDetails"; //Activate Content
        public const string Activate_Deactivate_ContentDetailsArchived = "Activate_Deactivate_ContentDetailsArchived"; //Activate Content
        public const string Activate_Deactivate_MenuMaster = "Activate_Deactivate_MenuMaster"; //Activate/Deactivate MenuMaster
        public const string Activate_Deactivate_RoleMaster = "Activate_Deactivate_RoleMaster"; //Activate/Deactivate RoleMaster

        public const string Get_BSR_Report = "Get_BSR_Report"; //Select All Menu

        public const string Mark_Attempt = "MARKATTEMPT";
        public const string Mark_Success = "MARKSUCCESS";
        public const string Mark_Failure = "MARKFAILURE";


    }
}
