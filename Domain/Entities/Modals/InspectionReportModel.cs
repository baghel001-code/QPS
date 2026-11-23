using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Modals
{
    public class InspectionReportModel
    {
        public int POID { get; set; }
        public int INSP_ID { get; set; }
        public string? INSPECTION_DATE { get; set; }
        public string? DATE_OF_COMPLETION { get; set; }
        public string? TYPE_OF_INSPECTION { get; set; }
        public string? INSPECTION_REPORT_NO { get; set; }
        public string? NAME_OF_THE_VMM_QA { get; set; }
        public string? BUYER_NAME { get; set; }
        public string? ARTICLE_NO { get; set; }
        public string? PO_NUMBER { get; set; }
        public string? LINK_PO_NUMBER { get; set; }
        public string? VENDOR_CODE { get; set; }
        public string? VENDOR_NAME { get; set; }
        public string? DESCRIPTION { get; set; }
        public string? MRP { get; set; }
        public string? BRAND_LABEL { get; set; }
        public string? COLOR { get; set; }
        public string? FIT { get; set; }
        public string? CATEGORY { get; set; }
        public string? PO_QTY { get; set; }
        public string? OFFERED_QTY { get; set; }
        public string? SHIP_QTY { get; set; }
        public string? SAMPLE_SIZE { get; set; }
        public string? APPS_NON_APPS { get; set; }
        public string? LOCATION { get; set; }
        public string? ADDRESS { get; set; }
        public string? CONTACT_NAME { get; set; }
        public string? CONTACT_NUMBER { get; set; }
        public string? INSPECTION_RESULT { get; set; }
        public string? FAIL_REASON { get; set; }
        public string? ABORTIVE_REASON { get; set; }
        public string? DEFECTS { get; set; }
        public string? MAJOR { get; set; }
        public string? MINOR { get; set; }
        public string? ALLOWED { get; set; }
        public string? STATUS { get; set; }
        public string? REMARKS_DETAILS { get; set; }
        public string? NOTE { get; set; }
        public string? FIBER_CONTENT_ON_TEST_REPORT { get; set; }
        public string? FIBER_CONTENT_ON_LABEL { get; set; }
        public string? FIBER_CONTENT_ON_PO { get; set; }
        public string? GSM_ON_PO { get; set; }
        public string? GSM_ON_TEST_REPORT { get; set; }
        public string? GSM_ON_BULK { get; set; }
        public string? NO_OF_BAGS_CARTON { get; set; }
        public string? SELECTED_BAGS_CARTON_NO { get; set; }
        public string? NO_OF_CHECK_CARTON_BAG { get; set; }
        public string? NO_OF_CHECK_PRE_PACK { get; set; }
        public string? MESUREMENT_SAMPLE_SIZE { get; set; }
        public string? MEASUREMENT_PASS_FAIL { get; set; }
        public string? PACKING_STATUS { get; set; }
        public string? PACKING_LIST { get; set; }
        public string? INLINE_DONE { get; set; }
        public string? PPS_RELEASE_DATE { get; set; }
        public string? PPS_EXPIRY_DATE { get; set; }
        public string? MFG_DATE { get; set; }
        public string? SEASON { get; set; }
        public string? COLOR_CODE { get; set; }
        public string? GPT_REPORT_NO { get; set; }
        public string? TEST_REPORT_STATUS { get; set; }
        public string? AREA_OF_FAILURE_IN_GPT_TEST { get; set; }
        public string? APPROVED_DATE { get; set; }
        public string? APPROVED_BY_IF_GPT_FAILS { get; set; }
        public string? GPT_ELASTIC_NO { get; set; }
        public string? ELASTIC_TEST_REPORT_STATUS { get; set; }
        public string? ELASTIC_APPROVED_DATE { get; set; }
        public string? APPROVED_BY_IF_ELASTIC_FAILS { get; set; }
        public string? ZIPPER_REPORT_NO { get; set; }
        public string? ZIPPER_REPORT_STATUS { get; set; }
        public string? ZIPPER_APPROVED_DATE { get; set; }
        public string? IF_FAIL_ZIPPER_APPROVED_BY { get; set; }
        public string? SIZE { get; set; }
        public string? EMBRIODERY_PRINT_SHOULD_BE_AS_PER_PPS { get; set; }
        public string? ACCESSORIES_SHOULD_BE_AS_PER_PPS { get; set; }
        public string? BARCODE_COMPLIANCES { get; set; }
        public string? NO_PLASTIC_TAG_KIMBLE_TAG { get; set; }
        public string? BOGO_DISCOUNT_TAG { get; set; }
        public string? PRE_PACK_RATIO { get; set; }
        public string? THREAD_FROM_NOMINATED_Y_N { get; set; }
        public string? BUTTON_FROM_NOMINATED_Y_N { get; set; }
        public string? ZIPPER_FROM_NOMINATED_Y_N { get; set; }
        public string? INTERLINING_FROM_NOMINATED_VENDOR_Y_N { get; set; }
        public string? ELASTIC_FROM_NOMINATED_Y_N { get; set; }
        public string? FI_COMM_APPR_DATE { get; set; }
        public string? FI_COMM_APPROVED_BY { get; set; }
        public string? COMM_APPROVAL_DOCUMENTS { get; set; }
        public string? DEBIT_AMOUNT { get; set; }
        public string? AMOUNT_OF_TRAVELLING_AND_OTHER_EXPENCES { get; set; }
        public string? ADMIN_REMARK { get; set; }
    }
}
