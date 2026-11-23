using Domain.Entities.Base;
using Domain.Entities.Response;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Domain.Entities.Modals
{
    public class POHeader
    {
        public int Id { get; set; }
        public int InspId { get; set; }
        public string PoNumber { get; set; }
        public string LinkPoNumber { get; set; } = "";
        public string PONumberWithLinkPONumber { get { return LinkPoNumber != "" ? $"{PoNumber} ~ {LinkPoNumber}" : PoNumber; } }
        public string VendorCode { get; set; }
        public string VendorName { get; set; }
        public string Fabric { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal OfferedQty { get; set; }
        public decimal POTotalQty { get; set; }
        //public DateTime? DateOfInspection { get; set; }

        public DateTime? SelectedDateDOI;
        public TimeSpan? SelectedTimeDOI;

        private DateTime? _Doi;
        //public DateTime? DateOfInspection
        //{
        //    get => SelectedDateDOI.HasValue && SelectedTimeDOI.HasValue ? SelectedDateDOI.Value.Date + SelectedTimeDOI.Value : _Doi;

        //    set
        //    {
        //        _Doi = value;

        //        if (value.HasValue)
        //        {
        //            SelectedDateDOI = value.Value.Date;
        //            SelectedTimeDOI = value.Value.TimeOfDay;
        //        }
        //        else
        //        {
        //            SelectedDateDOI = null;
        //            SelectedTimeDOI = null;
        //        }
        //    }
        //}

        public DateTime? DateOfInspection
        {
            get => SelectedDateDOI.HasValue ? SelectedDateDOI.Value.Date : _Doi;

            set
            {
                _Doi = value;

                if (value.HasValue)
                {
                    SelectedDateDOI = value.Value.Date;
                }
                else
                {
                    SelectedDateDOI = null;
                }
            }
        }

        public string InspectionLocation { get; set; } = "";
        public int CreatedBy { get; set; }
        public string Created_By { get; set; } = "";
        public DateTime? CreatedOn { get; set; }
        public List<PODetails> Details { get; set; } = new();
        public int AddressId { get; set; }
        public int RegionId { get; set; }
        public int InspectorId { get; set; }
        public int InspectionTypeId { get; set; }
        public ICollection<VendorAddressDetail> AddressList { get; set; } = new List<VendorAddressDetail>();
        public ICollection<MasterData> RegionList { get; set; } = new List<MasterData>();
        public ICollection<MasterData> InspectorList { get; set; } = new List<MasterData>();
        public ICollection<MasterData> InspectionTypeList { get; set; } = new List<MasterData>();
        public bool Is_Date_Assigned_By_QA { get; set; }
        public bool Is_Inspector_Assigned { get; set; }
        public string Insp_Req_Status { get; set; } = "Draft";
        public int Status_Code { get; set; }
        //public DateTime? QA_ASSIGNED_DOI { get; set; }
        public bool Is_Inspection_Complete
        {
            get => Insp_Req_Status == "Inspection Done" ? true : false;
        }

        public string? Inspection_Result { get; set; } = "";

        public DateTime? SelectedDate;
        public TimeSpan? SelectedTime;

        private DateTime? _qaAssignedDoi;
        //public DateTime? QA_ASSIGNED_DOI
        //{
        //    get => SelectedDate.HasValue && SelectedTime.HasValue ? SelectedDate.Value.Date + SelectedTime.Value : _qaAssignedDoi;

        //    set
        //    {
        //        _qaAssignedDoi = value;

        //        if (value.HasValue)
        //        {
        //            SelectedDate = value.Value.Date;
        //            SelectedTime = value.Value.TimeOfDay;
        //        }
        //        else
        //        {
        //            SelectedDate = null;
        //            SelectedTime = null;
        //        }
        //    }
        //}

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


        public string Apparels { get; set; } = "";
        public string Category { get; set; } = "";
        public string BuyerName { get; set; } = "";
        public string BuyereId { get; set; } = "";
        public string InspectionNo { get; set; } = "";
        public string Inspector { get; set; } = "";
        public string MC { get; set; } = "";
        public string MCText { get; set; } = "";
        public decimal MRP { get; set; }
        public string Brand_Id { get; set; } = "";
        public string Brand_Id2 { get; set; } = "";
        public string FIT { get; set; } = "";

        public bool IsCancelled { get; set; } = false;
        public bool Is_Qc_At_Dc { get; set; } = false;
        public POHeader MapToHeader(POSAPResponse root)
        {
            //APP FM GM MISC NT

            var first = root.APPS_FOOTWEAR_DETAILS.First();

            //var priceApp = root.APPS_FOOTWEAR_DETAILS.Where(x => x.LOB == "APP" && x.ATTYP == "02" && Convert.ToDecimal(x.PO_LINE_ITEM) % 100 == 0).Select(x => Convert.ToDecimal(x.MRP)).FirstOrDefault();
            //var priceGMFM = root.APPS_FOOTWEAR_DETAILS.Where(x => (x.LOB == "FM" || x.LOB == "GM") && x.ATTYP == "00" && Convert.ToDecimal(x.PO_LINE_ITEM) % 100 == 0).Select(x => Convert.ToDecimal(x.MRP)).FirstOrDefault();

            var header = new POHeader
            {
                PoNumber = first.PO_NUMBER,
                VendorCode = first.VENDOR_CODE,
                VendorName = first.VENDOR_NAME,
                Apparels = first.LOB,
                Category = first.DIVISION_TEXT,
                BuyereId = first.BUYER_ID,
                BuyerName = first.BUYER_NAME,
                MC = first.MC,
                MCText = first.MC_TEXT,
                MRP = first.LOB == "APP" ? root.APPS_FOOTWEAR_DETAILS.Where(x => x.LOB == "APP" && x.ATTYP == "02" && x.MRP != "" && Convert.ToDecimal(x.PO_LINE_ITEM) % 100 == 0).Select(x => Convert.ToDecimal(x.MRP)).FirstOrDefault() :
                (first.LOB == "FM" || first.LOB == "GM") ? root.APPS_FOOTWEAR_DETAILS.Where(x => (x.LOB == "FM" || x.LOB == "GM") && x.ATTYP == "00" && x.MRP != "" && Convert.ToDecimal(x.PO_LINE_ITEM) % 100 == 0).Select(x => Convert.ToDecimal(x.MRP)).FirstOrDefault() : 0,
                Brand_Id = first.BRAND_ID,
                Brand_Id2 = first.BRAND_ID2,
                FIT = first.FIT,
                POTotalQty = Convert.ToDecimal(first.PURCHASE_TOT_QTY),
                Fabric = string.IsNullOrWhiteSpace(first.FABRIC!) ? root.APPS_FOOTWEAR_DETAILS.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.FABRIC!))?.FABRIC! ?? "NA" : first.FABRIC,
                Color = string.IsNullOrWhiteSpace(first.COLOR!) ? root.APPS_FOOTWEAR_DETAILS.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.COLOR!))?.COLOR! ?? "NA" : first.COLOR
            };

            header.Details = root.APPS_FOOTWEAR_DETAILS.Select(x => new PODetails
            {
                LineItem = x.PO_LINE_ITEM,
                Article = x.ARTICLE,
                ArticleDesc = x.ARTICLE_DESC,
                PrepackQty = Convert.ToDecimal(string.IsNullOrWhiteSpace(x.PREPACK_QTY) ? "0" : x.PREPACK_QTY),
                PurchaseTotQty = Convert.ToDecimal(x.PURCHASE_TOT_QTY),
                PrepackQtyEaches = Convert.ToDecimal(string.IsNullOrWhiteSpace(x.PREPACK_QTY_EACHES) ? "0" : x.PREPACK_QTY_EACHES),
                Eaches = Convert.ToDecimal(string.IsNullOrWhiteSpace(x.EACHES) ? "0" : x.EACHES),
                MRP = Convert.ToDecimal(string.IsNullOrWhiteSpace(x.MRP) ? "0" : x.MRP),
                AtTyp = x.ATTYP
            }).ToList();

            return header;
        }

        private static bool SameText(string? a, string? b) =>
        string.Equals((a ?? string.Empty).Trim(), (b ?? string.Empty).Trim(), StringComparison.Ordinal);

        /// <summary>Lists the fields that differ between this (QPS) PO and the PO fetched from SAP.
        /// NULL and empty strings are treated as equal (N15).</summary>
        public List<string> GetChangedFields(POHeader sap)
        {
            var changes = new List<string>();

            if (POTotalQty != sap.POTotalQty) changes.Add($"PO Qty ({POTotalQty:0.###} to {sap.POTotalQty:0.###})");
            if (!SameText(Fabric, sap.Fabric)) changes.Add("Fabric");
            if (!SameText(Color, sap.Color)) changes.Add("Color");
            if (MRP != sap.MRP) changes.Add($"MRP ({MRP:0.##} to {sap.MRP:0.##})");
            if (!SameText(MC, sap.MC)) changes.Add("MC");
            if (!SameText(MCText, sap.MCText)) changes.Add("MC Text");
            if (!SameText(Category, sap.Category)) changes.Add("Category");
            if (!SameText(Apparels, sap.Apparels)) changes.Add("LOB");
            if (!SameText(BuyereId, sap.BuyereId)) changes.Add("Buyer ID");
            if (!SameText(BuyerName, sap.BuyerName)) changes.Add("Buyer Name");
            if (!SameText(VendorName, sap.VendorName)) changes.Add("Vendor Name");
            if (!SameText(Brand_Id, sap.Brand_Id)) changes.Add("Brand");
            if (!SameText(Brand_Id2, sap.Brand_Id2)) changes.Add("Brand 2");
            if (!SameText(FIT, sap.FIT)) changes.Add("Fit");

            return changes;
        }
        public override bool Equals(object obj) => obj is POHeader other && GetChangedFields(other).Count == 0;
        
        public override int GetHashCode()
        {
            return HashCode.Combine(PoNumber, VendorCode, BuyereId, _Doi);
        }

        public void UpdateNewValues(POHeader other)
        {
            FIT = other.FIT;
            Brand_Id2 = other.Brand_Id2;
            Brand_Id = other.Brand_Id;
            MRP = other.MRP;
            MC = other.MC;
            BuyereId = other.BuyereId;
            BuyerName = other.BuyerName;       // NEW
            VendorName = other.VendorName;     // NEW
            Category = other.Category;
            Apparels = other.Apparels;
            Color = other.Color;
            Fabric = other.Fabric;
            MCText = other.MCText;
            POTotalQty = other.POTotalQty;
        }


        public bool IsLastRequestPending { get; set; } = false;

        


    }

    public class PO_HEADER_MODEL
    {
        public int POID { get; set; }
        public int INSP_ID { get; set; }
        public string PO_NUMBER { get; set; }
        public string LINK_PO_NUMBER { get; set; }
        public string VENDOR_CODE { get; set; }
        public string VENDOR_NAME { get; set; }
        public string FABRIC { get; set; }
        public string COLOR { get; set; }
        public decimal TOTAL_QTY { get; set; }
        public decimal OFFERED_QTY { get; set; }
        public int ADDRESS_ID { get; set; }
        public int REGION_ID { get; set; }
        public bool IS_DATE_ASSIGNED_BY_QA { get; set; }
        public bool IS_INSPECTOR_ASSIGNED { get; set; }
        public bool IS_QC_AT_DC { get; set; }
        public string? INSP_REQ_STATUS { get; set; }
        public int STATUS_CODE { get; set; }
        public string? INSP_RESULT { get; set; }
        public int INSPECTOR_EMP_CODE { get; set; }
        public string INSPECTOR { get; set; } = "";

        public int INSPECTION_TYPE_ID { get; set; }
        public string APPARELS { get; set; } = "";
        public string CATEGORY { get; set; } = "";
        public string BUYER_NAME { get; set; } = "";
        public string BUYER_ID { get; set; } = "";
        public string MC { get; set; } = "";
        public string MC_TEXT { get; set; } = "";
        public decimal MRP { get; set; }
        public string BRAND_ID { get; set; } = "";
        public string BRAND_ID2 { get; set; } = "";
        public string FIT { get; set; } = "";
        public string INSPECTION_NO { get; set; } = "";

        public string LOCATION { get; set; } = "";
        public DateTime DATE_OF_INSPECTION { get; set; }
        public DateTime? QA_ASSIGNED_DOI { get; set; }
        public DateTime CREATED_ON { get; set; }
        public string CREATED_BY { get; set; }
        public DateTime LAST_UPDATED_ON { get; set; }
        public string UPDATED_BY { get; set; }
        public string CREATED_BY_NAME { get; set; }
        public string CREATED_DATE { get; set; }

        public DateTime DATE_OF_COMPLETION { get; set; }

        public bool ISLASTREQUESTPENDING { get; set; } = false;
    }

    public sealed record INSPECTION_REQUEST_LIST_MODEL
    {
        public int POID { get; init; }
        public int INSP_ID { get; init; }
        public string INSPECTION_DATE { get; init; }
        public string TYPE_OF_INSPECTION { get; init; }
        public bool IS_QC_AT_DC { get; init; }
        public string INSPECTION_REPORT_NO { get; init; }
        public string INSPECTOR { get; init; }
        public string BUYER_NAME { get; init; }
        public string ARTICLE_NO { get; init; }
        public string PO_NUMBER { get; init; }
        public string LINK_PO_NUMBER { get; init; }
        public string PO_NUMBER_WITH_LINK_PO_NUMBER { get; init; }
        public string VENDOR { get; init; }
        public string DESCRIPTION { get; init; }
        public decimal MRP { get; init; }
        public string BRAND_LABEL { get; init; }
        public string COLOR { get; init; }
        public string FIT { get; init; }
        public string MC { get; init; }
        public string FABRIC { get; init; }
        public string CATEGORY { get; init; }
        public decimal PO_QTY { get; init; }
        public decimal OFFERED_QTY { get; init; }
        public string APPS_NON_APPS { get; init; }
        public string LOCATION { get; init; }
        public string ADDRESS { get; init; }
        public string CONTACT_NAME { get; init; }
        public string CONTACT_NUMBER { get; init; }
        public string DATE_OF_INSPECTION { get; init; }
        public int ADDRESS_ID { get; init; }
        public DateTime CREATED_ON { get; init; }
        public DateTime LAST_UPDATED_ON { get; init; }

        public DateTime DoI_For_QA { get; init; }
        public DateTime DoI_Vendor { get; init; }
        public int REGION_ID { get; init; }
        public string? QA_ASSIGNED_DOI { get; init; }
        public int INSPECTION_TYPE_ID { get; init; }
        public bool IS_DATE_ASSIGNED_BY_QA { get; init; }
        public bool IS_INSPECTOR_ASSIGNED { get; init; }
        public string? INSP_REQ_STATUS { get; init; }
        public int STATUS_CODE { get; init; }

        public string INSP_RESULT { get; init; }
        public string DATE_OF_COMPLETION { get; init; }
        public string INSPECTOR_EMP_CODE { get; init; }
        public int INSPECT_ID { get; init; }
        public int INSPECTION_RESULT { get; init; }

        //-----------------------------------------------
        public string? SHIP_QTY { get; init; }
        public string? SAMPLE_SIZE { get; init; }
        public string? FAIL_REASON { get; init; }
        public string? ABORTIVE_REASON { get; init; }
        public string? DEFECTS { get; init; }
        public string? MAJOR { get; init; }
        public string? MINOR { get; init; }
        public string? ALLOWED { get; init; }
        public string? STATUS { get; init; }
        public string? REMARKS_DETAILS { get; init; }
        public string? NOTE { get; init; }
        public string? FIBER_CONTENT_ON_TEST_REPORT { get; init; }
        public string? FIBER_CONTENT_ON_LABEL { get; init; }
        public string? FIBER_CONTENT_ON_PO { get; init; }
        public string? GSM_ON_PO { get; init; }
        public string? GSM_ON_TEST_REPORT { get; init; }
        public string? GSM_ON_BULK { get; init; }
        public string? NO_OF_BAGS_CARTON { get; init; }
        public string? SELECTED_BAGS_CARTON_NO { get; init; }
        public string? NO_OF_CHECK_CARTON_BAG { get; init; }
        public string? NO_OF_CHECK_PRE_PACK { get; init; }
        public string? MESUREMENT_SAMPLE_SIZE { get; init; }
        public string? MEASUREMENT_PASS_FAIL { get; init; }
        public string? PACKING_STATUS { get; init; }
        public string? PACKING_LIST { get; init; }
        public string? INLINE_DONE { get; init; }
        public string? PPS_RELEASE_DATE { get; init; }
        public string? PPS_EXPIRY_DATE { get; init; }
        public string? MFG_DATE { get; init; }
        public string? SEASON { get; init; }
        public string? COLOR_CODE { get; init; }
        public string? GPT_REPORT_NO { get; init; }
        public string? TEST_REPORT_STATUS { get; init; }
        public string? AREA_OF_FAILURE_IN_GPT_TEST { get; init; }
        public string? APPROVED_DATE { get; init; }
        public string? APPROVED_BY_IF_GPT_FAILS { get; init; }
        public string? GPT_ELASTIC_NO { get; init; }
        public string? ELASTIC_TEST_REPORT_STATUS { get; init; }
        public string? ELASTIC_APPROVED_DATE { get; init; }
        public string? APPROVED_BY_IF_ELASTIC_FAILS { get; init; }
        public string? ZIPPER_REPORT_NO { get; init; }
        public string? ZIPPER_REPORT_STATUS { get; init; }
        public string? ZIPPER_APPROVED_DATE { get; init; }
        public string? IF_FAIL_ZIPPER_APPROVED_BY { get; init; }
        public string? SIZE { get; init; }
        public string? EMBRIODERY_PRINT_SHOULD_BE_AS_PER_PPS { get; init; }
        public string? ACCESSORIES_SHOULD_BE_AS_PER_PPS { get; init; }
        public string? BARCODE_COMPLIANCES { get; init; }
        public string? NO_PLASTIC_TAG_KIMBLE_TAG { get; init; }
        public string? BOGO_DISCOUNT_TAG { get; init; }
        public string? PRE_PACK_RATIO { get; init; }
        public string? THREAD_FROM_NOMINATED_Y_N { get; init; }
        public string? BUTTON_FROM_NOMINATED_Y_N { get; init; }
        public string? ZIPPER_FROM_NOMINATED_Y_N { get; init; }
        public string? INTERLINING_FROM_NOMINATED_VENDOR_Y_N { get; init; }
        public string? ELASTIC_FROM_NOMINATED_Y_N { get; init; }
        public string? FI_COMM_APPR_DATE { get; init; }
        public string? FI_COMM_APPROVED_BY { get; init; }
        public string? COMM_APPROVAL_DOCUMENTS { get; init; }
        public string? DEBIT_AMOUNT { get; init; }
        public string? AMOUNT_OF_TRAVELLING_AND_OTHER_EXPENCES { get; init; }
        public string? ADMIN_REMARK { get; init; }

    }

    public class ReceivedPendingOngoingInspections : BaseResponse
    {
        public List<INSPECTION_REQUEST_LIST_MODEL> InspectionList { get; set; } = new();
        public int TotalRows { get; set; }
    }

}

