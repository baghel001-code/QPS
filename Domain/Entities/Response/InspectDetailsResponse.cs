using Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Response
{
    public class InspectDetails
    {
        public long INSPECT_ID { get; set; }
        public long INSPECTION_ID { get; set; }
        public string INSPECTOR_CODE { get; set; } = string.Empty;
        public int OFFERED_QTY { get; set; }
        public string GPT_NO { get; set; } = string.Empty;
        public int GPT_STATUS { get; set; }
        public string? GPT_APPROVED_BY { get; set; } = string.Empty;
        public DateTime? GPT_APPROVED_DATE { get; set; }

        public int NO_OF_BAGS_CARTONS_QTY { get; set; }
        public int NO_OF_BAGS_CARTONS_UNIT { get; set; }
        public int PACKING_STATUS { get; set; }
        public int INLINE_DONE { get; set; }
        public int PACKING_LIST { get; set; }

        public DateTime? PPS_RELEASE_DATE { get; set; }
        public DateTime? PPS_EXPIRY_DATE { get; set; }

        public string FIBER_CONTENT_ON_PO { get; set; } = string.Empty;
        public string FIBER_CONTENT_ON_TEST_REPORT { get; set; } = string.Empty;
        public string FIBER_CONTENT_ON_WC { get; set; } = string.Empty;

        public string GSM_ON_PO { get; set; } = string.Empty;
        public string GSM_ON_TEST_REPORT { get; set; } = string.Empty;
        public string? GSM_ON_BULK { get; set; } = string.Empty;

        public int MEASUREMENT_SAMPLE_SIZE { get; set; }
        public string? MEASUREMENT_ATTACHMENT { get; set; } = string.Empty;

        public int NO_OF_CHECK { get; set; }
        public int NO_OF_CHECK_UNIT { get; set; }
        public int? NO_OF_PREPACK_CHECK { get; set; }

        public string? WORKMANSHIP { get; set; } = string.Empty;
        public string? AQL { get; set; } = string.Empty;
        public int AQL_SAMPLE_SIZE { get; set; }
        public int AQL_2_5_MAJOR { get; set; }
        public int AQL_2_5_MINOR { get; set; }


        public int DEFECTS_ALLOWED { get; set; }
        public int DEFECTS_STATUS { get; set; }
        public int INSPECTION_RESULT { get; set; }

        public string? REMARK_COMMENT { get; set; } = string.Empty;
        public string? NOTE { get; set; } = string.Empty;
        public string NAME_OF_FACTORY_REPRESENTATIVE { get; set; } = string.Empty;

        public string? TEST_REPORT_ATTACHMENT { get; set; } = string.Empty;
        public string? GEN_ATTACHMENT_FIRST { get; set; } = string.Empty;
        public string? GEN_ATTACHMENT_SECOND { get; set; } = string.Empty;

        public bool IS_COMPLETE { get; set; }
        public int CREATED_BY { get; set; }
        public DateTime? CREATED_DATE { get; set; }
        public int MODIFIED_BY { get; set; }
        public DateTime? MODIFIED_DATE { get; set; }
        public List<DefectsResponse> Defects { get; set; } = new();

        public int RESEAN_FOR_FAIL { get; set; }
        public int COLOR_CODE { get; set; }
        public DateTime? MFG_DATE { get; set; }
        public DateTime? GPT_COMM_APPROVAL_DATE { get; set; }
        public DateTime? DATE_OF_COMPLETION { get; set; }
        public string? SEASON { get; set; } = string.Empty;
        public string? AREA_OF_FAIL_GPTR { get; set; } = string.Empty;
        public string? GPT_COMM_APPROVED_BY { get; set; } = string.Empty;
        public bool IS_EDIT_BY_ADMIN { get; set; }

        public string? GPT_ELASTIC_NO { get; set; }
        public int GPT_ELASTIC_STATUS { get; set; }
        public string? GPT_ELASTIC_APPROVED_BY { get; set; }
        public DateTime? GPT_ELASTIC_APPROVED_DATE { get; set; }
        public string? CONFORMITY_SIZE { get; set; }
        public int CONFORMITY_SIZE_STATUS { get; set; }
        public string? CONFORMITY_PREPACK { get; set; }
        public int CONFORMITY_PREPACK_STATUS { get; set; }
        public int EMBROIDERY_PF { get; set; }
        public int ACCESSORIES_PF { get; set; }
        public int BARCODE_PF { get; set; }
        public int NO_PLASTIC_PF { get; set; }
        public int BOGO_PF { get; set; }
        public int THREAD_YN { get; set; }
        public int BUTTON_YN { get; set; }
        public int ZIPPER_YN { get; set; }
        public int INTERLINING_YN { get; set; }
        public int ELASTIC_YN { get; set; }
        public int MEASUREMENT_SAMPLE_QTY { get; set; }

        public string ZIPPER_RPT_NO { get; set; } = string.Empty;
        public int ZIPPER_STATUS { get; set; }
        public string ZIPPER_APPROVED_BY { get; set; } = string.Empty;
        public DateTime? ZIPPER_APPROVED_DATE { get; set; }
        public string FIT { get; set; } = string.Empty;
        public string COMM_APPROVAL_DOC { get; set; } = string.Empty;
        public decimal DEBIT_AMOUNT { get; set; }
        public string ADMIN_REMARK { get; set; } = string.Empty;

        public string ABORTIVE_REASON_IDS { get; set; } = string.Empty;
        public string OTHER_ABORTIVE_REASON { get; set; } = string.Empty;
        public int AMT_TRAVELLING { get; set; }
        public string SELECTED_BAG_CARTONS_NO { get; set; } = string.Empty;
    }

    public class DefectsResponse
    {
        public int DEFECTS_ID { get; set; }
        public int INSPECTION_ID { get; set; }
        public string DEFECTS { get; set; } = string.Empty;
        public string? DIMENSION { get; set; } = string.Empty;
        public int MAJOR { get; set; }
        public int MINOR { get; set; }
        public Guid DefectId { get; set; } = new Guid();
    }

    public class AQLDataResponse
    {
        public int BATCH_SIZE_MIN { get; set; }
        public int BATCH_SIZE_MAX { get; set; }
        public int SAMPLE_SIZE { get; set; }
        public int AQL_2_5_MAJOR { get; set; }
        public int AQL_2_5_MINOR { get; set; }
    }

    public class InspectResponse : BaseResponse
    {
        public InspectDetails InspectDetails { get; set; } = new InspectDetails();
        public List<DefectsResponse> Defects { get; set; } = new List<DefectsResponse>();
        public List<AQLDataResponse> AQLList { get; set; } = new List<AQLDataResponse>();

    }
}
