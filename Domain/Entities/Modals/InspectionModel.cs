using Domain.Entities.Base;
using Domain.Entities.Response;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Modals
{
    public class InspectionModel
    {
        public long InspectId { get; set; }          // 0 = Insert, >0 = Update
        public long InspectionId { get; set; }
        public string InspectorCode { get; set; } = string.Empty;
        public string GptNo { get; set; } = string.Empty;
        public int GptStatus { get; set; }
        public string _gptStatus => PassFailGPTStatus.FirstOrDefault(i => i.Master_ID == GptStatus)?.Master_Name ?? "Pending";
        public string? GptApprovedBy { get; set; } = string.Empty;
        public DateTime? GptApprovedDate { get; set; }

        public int? NoOfBagsCartonsQty { get; set; }
        public int NoOfBagsCartonsUnit { get; set; }
        public string _noOfBagsCartonsUnit => UnitNoOfBagCarton.FirstOrDefault(i => i.Master_ID == NoOfBagsCartonsUnit)?.Master_Name ?? "NA";
        public int PackingStatus { get; set; }
        public string _packingStatus => YesNoPackingStatus.FirstOrDefault(i => i.Master_ID == PackingStatus)?.Master_Name ?? "NA";
        public int InlineDone { get; set; }
        public string _inlineDone => YesNoInlineDone.FirstOrDefault(i => i.Master_ID == InlineDone)?.Master_Name ?? "NA";
        public int PackingList { get; set; }
        public string _packingList => YesNoPackingList.FirstOrDefault(i => i.Master_ID == PackingList)?.Master_Name ?? "NA";

        public DateTime? PpsReleaseDate { get; set; }
        public DateTime? PpsExpiryDate { get { return PpsReleaseDate != null ? PpsReleaseDate.Value.AddMonths(6).AddDays(-1) : null; } set { } }

        public string FiberContentOnPo { get; set; } = string.Empty;
        public string FiberContentOnTestReport { get; set; } = string.Empty;
        public string FiberContentOnWc { get; set; } = string.Empty;

        public string GsmOnPo { get; set; } = string.Empty;
        public string GsmOnTestReport { get; set; } = string.Empty;
        public string? GsmOnBulk { get; set; } = string.Empty;

        public int MeasurementSampleSize { get; set; }
        public string _measurementSampleSize => PassFailMeasurementSampleSize.FirstOrDefault(i => i.Master_ID == MeasurementSampleSize)?.Master_Name ?? "NA";
        public string? MeasurementAttachment { get; set; } = string.Empty;

        public int? NoOfCheck { get; set; }
        public int NoOfCheckUnit { get; set; }
        public string _noOfCheckUnit => UnitNoOfCheck.FirstOrDefault(i => i.Master_ID == NoOfCheckUnit)?.Master_Name ?? "NA";
        public int? NoOfPrepackCheck { get; set; }

        public string? Workmanship { get; set; } = "( Level : I/II )";
        public string? Aql { get; set; } = "AQL: Major/Minor 2.5/2.5";
        public int AqlSampleSize { get; set; }
        public int Aql_2_5_Major { get; set; }
        public int Aql_2_5_Minor { get; set; }

        public int DefectsAllowed { get; set; }
        public int DefectsStatus { get; set; }

        public string _defectsStatus => PassFailDefectsStatus.FirstOrDefault(i => i.Master_ID == DefectsStatus)?.Master_Name ?? "NA";
        public int InspectionResult { get; set; }
        public string _inspectionResult => PassFailInspectionResult.FirstOrDefault(i => i.Master_ID == InspectionResult)?.Master_Name ?? "NA";
        public int? OfferedQuantity { get; set; } = 0;

        public string? RemarkComment { get; set; } = string.Empty;
        public string? Note { get; set; } = string.Empty;
        public string NameOfFactoryRepresentative { get; set; } = string.Empty;

        public string? TestReportAttachment { get; set; } = string.Empty;
        public string? GenAttachmentFirst { get; set; } = string.Empty;
        public string? GenAttachmentSecond { get; set; } = string.Empty;

        public bool IsComplete { get; set; }
        public DateTime? Date_of_Completion { get; set; }
        public int UserId { get; set; }

        public int _defectSttsId
        {
            get
            {
                var ttlrow = Defects.FirstOrDefault(a => a.Defects_Id == 1001);
                var alwrow = Defects.FirstOrDefault(a => a.Defects_Id == 1002);

                // If required rows are missing, treat as safe/default
                if (ttlrow == null || alwrow == null)
                    return 2;

                return (ttlrow.Major > alwrow.Major || ttlrow.Minor > alwrow.Minor)
                    ? 2
                    : 1;
            }
        }
        public string _defectStts => PassFailDefectsStatus.FirstOrDefault(i => i.Master_ID == _defectSttsId)?.Master_Name ?? "NA";

        public List<InspectionDefectDto> Defects { get; set; } = new();
        public List<AQLDataResponse> AqlData { get; set; } = new();

        public ICollection<MasterData> PassFailGPTStatus { get; set; } = new List<MasterData>();
        public ICollection<MasterData> YesNoPackingStatus { get; set; } = new List<MasterData>();
        public ICollection<MasterData> YesNoInlineDone { get; set; } = new List<MasterData>();
        public ICollection<MasterData> YesNoPackingList { get; set; } = new List<MasterData>();
        public ICollection<MasterData> PassFailMeasurementSampleSize { get; set; } = new List<MasterData>();
        public ICollection<MasterData> UnitNoOfBagCarton { get; set; } = new List<MasterData>();
        public ICollection<MasterData> UnitNoOfCheck { get; set; } = new List<MasterData>();
        public ICollection<MasterData> PassFailDefectsStatus { get; set; } = new List<MasterData>();
        public ICollection<MasterData> PassFailInspectionResult { get; set; } = new List<MasterData>();
        //-----------------------------
        public int Reason_for_fail { get; set; }
        public string _failReason => FailReasonList.FirstOrDefault(i => i.Master_ID == Reason_for_fail)?.Master_Name ?? "NA";
        public int ColorCode { get; set; }
        public string _colorCode => ColorList.FirstOrDefault(i => i.Master_ID == ColorCode)?.Master_Name ?? "NA";
        public DateTime? MFG_Date { get; set; }
        public DateTime? GPT_Comm_Approval_Date { get; set; }
        public string? Season { get; set; } = string.Empty;
        public string? Area_Of_Fail_GPTR { get; set; } = string.Empty;
        public string? GTP_Comm_Approved_By { get; set; } = "N/A";
        public bool Is_Edit_By_Admin { get; set; } = false;

        public ICollection<MasterData> FailReasonList { get; set; } = new List<MasterData>();
        public ICollection<MasterData> ColorList { get; set; } = new List<MasterData>();

        //[Required(ErrorMessage = "Elastic No. is required")]
        //[StringLength(10, MinimumLength = 5, ErrorMessage = "Elastic No. must be between 5 and 10 characters")]
        public string? gptElasticNo { get; set; }
        public int gptElasticStatus { get; set; }
        public string _gptElasticStatus => ElasticStatusList.FirstOrDefault(i => i.Master_ID == gptElasticStatus)?.Master_Name ?? "NA";
        public ICollection<MasterData> ElasticStatusList { get; set; } = new List<MasterData>();
        public string? gptElasticApprovedBy { get; set; }
        public DateTime? gptElasticApprovedDate { get; set; }

        public string? conformitySize { get; set; }
        public int conformitySizeStatus { get; set; }
        public string _conformitySizeStatus => ConformitySizeStatusList.FirstOrDefault(i => i.Master_ID == conformitySizeStatus)?.Master_Name ?? "NA";
        public ICollection<MasterData> ConformitySizeStatusList { get; set; } = new List<MasterData>();
        public string? conformityPrepack { get; set; }
        public int conformityPrepackStatus { get; set; }
        public string _conformityPrepackStatus => ConformityPrepackStatusList.FirstOrDefault(i => i.Master_ID == conformityPrepackStatus)?.Master_Name ?? "NA";
        public ICollection<MasterData> ConformityPrepackStatusList { get; set; } = new List<MasterData>();

        public int embroideryPf { get; set; }
        public string _embroideryPf => EmbroideryPFList.FirstOrDefault(i => i.Master_ID == embroideryPf)?.Master_Name ?? "NA";
        public ICollection<MasterData> EmbroideryPFList { get; set; } = new List<MasterData>();
        public int accessoriesPf { get; set; }
        public string _accessoriesPf => AccessoriesPFList.FirstOrDefault(i => i.Master_ID == accessoriesPf)?.Master_Name ?? "NA";
        public ICollection<MasterData> AccessoriesPFList { get; set; } = new List<MasterData>();
        public int barcodePf { get; set; }
        public string _barcodePf => BarcodePFList.FirstOrDefault(i => i.Master_ID == barcodePf)?.Master_Name ?? "NA";
        public ICollection<MasterData> BarcodePFList { get; set; } = new List<MasterData>();
        public int noPlasticPf { get; set; }
        public string _noPlasticPf => NoPlasticPFList.FirstOrDefault(i => i.Master_ID == noPlasticPf)?.Master_Name ?? "NA";
        public ICollection<MasterData> NoPlasticPFList { get; set; } = new List<MasterData>();
        public int bogoPf { get; set; }
        public string _bogoPf => BogoPFList.FirstOrDefault(i => i.Master_ID == bogoPf)?.Master_Name ?? "NA";
        public ICollection<MasterData> BogoPFList { get; set; } = new List<MasterData>();

        public int threadYn { get; set; }
        public string _threadYn => ThreadYNList.FirstOrDefault(i => i.Master_ID == threadYn)?.Master_Name ?? "NA";
        public ICollection<MasterData> ThreadYNList { get; set; } = new List<MasterData>();
        public int buttonYn { get; set; }
        public string _buttonYn => ButtonYNList.FirstOrDefault(i => i.Master_ID == buttonYn)?.Master_Name ?? "NA";
        public ICollection<MasterData> ButtonYNList { get; set; } = new List<MasterData>();
        public int zipperYn { get; set; }
        public string _zipperYn => ZipperYNList.FirstOrDefault(i => i.Master_ID == zipperYn)?.Master_Name ?? "NA";
        public ICollection<MasterData> ZipperYNList { get; set; } = new List<MasterData>();
        public int interliningYn { get; set; }
        public string _interliningYn => InterliningYNList.FirstOrDefault(i => i.Master_ID == interliningYn)?.Master_Name ?? "NA";
        public ICollection<MasterData> InterliningYNList { get; set; } = new List<MasterData>();
        public int elasticYn { get; set; }
        public string _elasticYn => ElasticYNList.FirstOrDefault(i => i.Master_ID == elasticYn)?.Master_Name ?? "NA";
        public ICollection<MasterData> ElasticYNList { get; set; } = new List<MasterData>();

        public int? measurementSampleQty { get; set; }

        public string ZipperRptNo { get; set; } = string.Empty;
        public int ZipperStatus { get; set; }
        public string _ZipperStatus => ZipperStatusList.FirstOrDefault(i => i.Master_ID == ZipperStatus)?.Master_Name ?? "NA";
        public ICollection<MasterData> ZipperStatusList { get; set; } = new List<MasterData>();
        public string ZipperApprovedBy { get; set; } = string.Empty;
        public DateTime? ZipperApprovedDate { get; set; }
        public string Fit { get; set; } = string.Empty;
        public string CommApprovalDoc { get; set; } = string.Empty;
        public int DebitAmount { get; set; }
        public string AdminRemark { get; set; } = string.Empty;

        public string AbortiveReasonIds { get; set; } = string.Empty; //ABORTIVE_REASON_IDS, AMT_TRAVELLING,
        public ICollection<MasterData> AbortiveReasonList { get; set; } = new List<MasterData>();

        public string _abortiveReasons
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AbortiveReasonIds))
                    return "NA";

                var ids = AbortiveReasonIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => int.TryParse(id.Trim(), out var val) ? val : (int?)null)
                    .Where(x => x.HasValue)
                    .Select(x => x.Value)
                    .ToHashSet();

                var names = AbortiveReasonList
                    .Where(i => ids.Contains(i.Master_ID))
                    .Select(i => i.Master_Name);

                return names.Any() ? string.Join(", ", names) : "NA";
            }
        }
        public List<AbortiveReasons> _listofAbortiveReasons
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AbortiveReasonIds))
                    return new List<AbortiveReasons>
            {
                new AbortiveReasons { sno = 1, AbortiveReason = "NA" }
            };

                var ids = AbortiveReasonIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => int.TryParse(id.Trim(), out var val) ? val : (int?)null)
                    .Where(x => x.HasValue)
                    .Select(x => x.Value)
                    .ToHashSet();

                var result = AbortiveReasonList
                    .Where(i => ids.Contains(i.Master_ID))
                    .Select((i, index) => new AbortiveReasons
                    {
                        sno = index + 1,
                        AbortiveId = i.Master_ID,
                        AbortiveReason = i.Master_Name + (i.Master_ID == 8 ? " : " + OtherAbortiveReason : "")
                    })
                    .ToList();

                return result.Any()
                    ? result
                    : new List<AbortiveReasons>();

            }
        }
        public int AmtTravelling { get; set; }

        public string OtherAbortiveReason { get; set; } = string.Empty;//



        public string POTotalQuantity { get; set; } = string.Empty;
        public string POOfferQuantity { get; set; } = string.Empty;

        public bool IsResetByAdmin { get; set; } = false;

        public string SelectedBagCartonsNo { get; set; } = string.Empty;//SELECTED_BAG_CARTONS_NO

        public InspectionModel CloneForAutoSave(List<int> protectedIds)
        {
            return new InspectionModel
            {
                // ── Identity ──────────────────────────────
                InspectId = InspectId,
                InspectionId = InspectionId,
                InspectorCode = InspectorCode,
                UserId = UserId,
                IsComplete = IsComplete,
                Is_Edit_By_Admin = Is_Edit_By_Admin,

                // ── GPT ───────────────────────────────────
                GptNo = GptNo,
                GptStatus = GptStatus,
                GptApprovedBy = GptApprovedBy,
                GptApprovedDate = GptApprovedDate,
                gptElasticNo = gptElasticNo,
                gptElasticStatus = gptElasticStatus,
                gptElasticApprovedBy = gptElasticApprovedBy,
                gptElasticApprovedDate = gptElasticApprovedDate,
                Area_Of_Fail_GPTR = Area_Of_Fail_GPTR,
                GTP_Comm_Approved_By = GTP_Comm_Approved_By,
                GPT_Comm_Approval_Date = GPT_Comm_Approval_Date,
                CommApprovalDoc = CommApprovalDoc,
                DebitAmount = DebitAmount,
                AdminRemark = AdminRemark,

                // ── Packing / Bags ────────────────────────
                NoOfBagsCartonsQty = NoOfBagsCartonsQty,
                NoOfBagsCartonsUnit = NoOfBagsCartonsUnit,
                PackingStatus = PackingStatus,
                PackingList = PackingList,
                InlineDone = InlineDone,
                SelectedBagCartonsNo = SelectedBagCartonsNo,
                NoOfCheck = NoOfCheck,
                NoOfCheckUnit = NoOfCheckUnit,
                NoOfPrepackCheck = NoOfPrepackCheck,

                // ── Fiber / GSM ───────────────────────────
                FiberContentOnPo = FiberContentOnPo,
                FiberContentOnTestReport = FiberContentOnTestReport,
                FiberContentOnWc = FiberContentOnWc,
                GsmOnPo = GsmOnPo,
                GsmOnTestReport = GsmOnTestReport,
                GsmOnBulk = GsmOnBulk,

                // ── Measurement ───────────────────────────
                MeasurementSampleSize = MeasurementSampleSize,
                MeasurementAttachment = MeasurementAttachment,
                measurementSampleQty = measurementSampleQty,

                // ── AQL / Inspection Result ───────────────
                AqlSampleSize = AqlSampleSize,
                Aql_2_5_Major = Aql_2_5_Major,
                Aql_2_5_Minor = Aql_2_5_Minor,
                Workmanship = Workmanship,
                Aql = Aql,
                InspectionResult = InspectionResult,
                Reason_for_fail = Reason_for_fail,
                DefectsAllowed = DefectsAllowed,
                DefectsStatus = DefectsStatus,
                OfferedQuantity = OfferedQuantity,
                POTotalQuantity = POTotalQuantity,
                POOfferQuantity = POOfferQuantity,

                // ── Conformity ────────────────────────────
                conformitySize = conformitySize,
                conformitySizeStatus = conformitySizeStatus,
                conformityPrepack = conformityPrepack,
                conformityPrepackStatus = conformityPrepackStatus,
                embroideryPf = embroideryPf,
                accessoriesPf = accessoriesPf,
                barcodePf = barcodePf,
                noPlasticPf = noPlasticPf,
                bogoPf = bogoPf,

                // ── Nominated Vendors ─────────────────────
                threadYn = threadYn,
                buttonYn = buttonYn,
                zipperYn = zipperYn,
                interliningYn = interliningYn,
                elasticYn = elasticYn,

                // ── Zipper ────────────────────────────────
                ZipperRptNo = ZipperRptNo,
                ZipperStatus = ZipperStatus,
                ZipperApprovedBy = ZipperApprovedBy,
                ZipperApprovedDate = ZipperApprovedDate,

                // ── Dates / Season / Misc ─────────────────
                PpsReleaseDate = PpsReleaseDate,
                MFG_Date = MFG_Date,
                Season = Season,
                ColorCode = ColorCode,
                Fit = Fit,
                Date_of_Completion = Date_of_Completion,

                // ── Remarks / Notes ───────────────────────
                RemarkComment = RemarkComment,
                Note = Note,
                NameOfFactoryRepresentative = NameOfFactoryRepresentative,

                // ── Attachments ───────────────────────────
                TestReportAttachment = TestReportAttachment,
                GenAttachmentFirst = GenAttachmentFirst,
                GenAttachmentSecond = GenAttachmentSecond,

                // ── Abortive ──────────────────────────────
                AbortiveReasonIds = AbortiveReasonIds,
                OtherAbortiveReason = OtherAbortiveReason,
                AmtTravelling = AmtTravelling,

                // ── Defects — safe snapshot, NO protected rows ──
                Defects = Defects
                    .Where(a => !protectedIds.Contains(a.Defects_Id))
                    .Select(d => new InspectionDefectDto
                    {
                        SNo = d.SNo,
                        Defects = d.Defects,
                        Defects_Id = d.Defects_Id,
                        Major = d.Major,
                        Minor = d.Minor,
                        Dimension = d.Dimension,
                        DefectId = d.DefectId
                    })
                    .ToList(),

                // ── Master lists — share references (read-only, safe) ──
                PassFailGPTStatus = PassFailGPTStatus,
                PassFailDefectsStatus = PassFailDefectsStatus,
                PassFailInspectionResult = PassFailInspectionResult,
                PassFailMeasurementSampleSize = PassFailMeasurementSampleSize,
                YesNoPackingStatus = YesNoPackingStatus,
                YesNoInlineDone = YesNoInlineDone,
                YesNoPackingList = YesNoPackingList,
                UnitNoOfBagCarton = UnitNoOfBagCarton,
                UnitNoOfCheck = UnitNoOfCheck,
                FailReasonList = FailReasonList,
                ColorList = ColorList,
                ElasticStatusList = ElasticStatusList,
                ConformitySizeStatusList = ConformitySizeStatusList,
                ConformityPrepackStatusList = ConformityPrepackStatusList,
                EmbroideryPFList = EmbroideryPFList,
                AccessoriesPFList = AccessoriesPFList,
                BarcodePFList = BarcodePFList,
                NoPlasticPFList = NoPlasticPFList,
                BogoPFList = BogoPFList,
                ThreadYNList = ThreadYNList,
                ButtonYNList = ButtonYNList,
                ZipperYNList = ZipperYNList,
                InterliningYNList = InterliningYNList,
                ElasticYNList = ElasticYNList,
                AbortiveReasonList = AbortiveReasonList,
                ZipperStatusList = ZipperStatusList,
            };
        }

    }

    public class InspectionDefectDto
    {
        public int Defects_Id { get; set; }
        public string Defects { get; set; } = string.Empty;
        public string? Dimension { get; set; } = string.Empty;
        public int Major { get; set; }
        public int Minor { get; set; }
        public Guid DefectId { get; set; } = new Guid();
        public int SNo { get; set; }
    }

    public class InpectionReport
    {
        public InspectionModel Model { get; set; } = new InspectionModel();
        public POHeader POHeader { get; set; } = new POHeader();
    }

    public class AbortiveReasons
    {
        public int sno { get; set; }
        public int AbortiveId { get; set; }
        public string AbortiveReason { get; set; } = string.Empty;
    }

    public sealed class InspectionReportPdfModel
    {
        // ---- Header ----
        public bool IsPass { get; }
        public string InspectionResultText { get; }
        public int InspectionTypeId { get; }
        public string InspectionTypeText { get; }

        // ---- Buyer / Report No ----
        public string BuyerName { get; }
        public string BuyerId { get; }
        public string ReportNo { get; }

        // ---- PO / FI Date / Vendor ----
        public string PONumber { get; }
        public DateTime? FIDate { get; }
        public string VendorName { get; }
        public string VendorCode { get; }
        public string VendorLocation { get; }

        // ---- Quantities / description ----
        public decimal POTotalQty { get; }
        public string MCText { get; }
        public string Fit { get; }
        public decimal MRP { get; }
        public int OfferedQuantity { get; }
        public int ShipQuantity { get; }
        public string Category { get; }
        public string BrandId { get; }
        public string Color { get; }
        public string ArticleNumber { get; }

        // ---- GPT ----
        public string GptNo { get; }
        public string GptStatus { get; }
        public string GptApprovedBy { get; }
        public string GptApprovedDate { get; }
        public string AreaOfFailGptr { get; }

        // ---- GPT Elastic ----
        public string GptElasticNo { get; }
        public string GptElasticStatus { get; }
        public string GptElasticApprovedBy { get; }
        public string GptElasticApprovedDate { get; }

        // ---- Zipper ----
        public string ZipperRptNo { get; }
        public string ZipperStatus { get; }
        public string ZipperApprovedBy { get; }
        public string ZipperApprovedDate { get; }

        // ---- Packing ----
        public int? NoOfBagsCartonsQty { get; }
        public string NoOfBagsCartonsUnit { get; }
        public string PackingStatus { get; }
        public string InlineDone { get; }
        public string PackingList { get; }
        public string? PpsReleaseDate { get; }
        public string? PpsExpiryDate { get; }

        // ---- MFG / Season / Color ----
        public string? MfgDate { get; }
        public string Season { get; }
        public string ColorCode { get; }

        // ---- Fiber / GSM ----
        public string FiberContentOnPo { get; }
        public string FiberContentOnTestReport { get; }
        public string FiberContentOnWc { get; }
        public string GsmOnPo { get; }
        public string GsmOnTestReport { get; }
        public string GsmOnBulk { get; }

        // ---- Workmanship / AQL ----
        public string SelectedBagCartonsNo { get; }
        public string InspectionFit { get; }
        public string Workmanship { get; }
        public string FailReason { get; }
        public int AqlSampleSize { get; }

        // ---- Conformity (PASS/FAIL table) ----
        public string ConformitySize { get; }
        public string ConformitySizeStatus { get; }
        public string EmbroideryPf { get; }
        public string AccessoriesPf { get; }
        public string BarcodePf { get; }
        public string NoPlasticPf { get; }
        public string BogoPf { get; }

        // ---- Conformity (YES/NO table) ----
        public string ConformityPrepack { get; }
        public string ConformityPrepackStatus { get; }
        public string ThreadYn { get; }
        public string ButtonYn { get; }
        public string ZipperYn { get; }
        public string InterliningYn { get; }
        public string ElasticYn { get; }

        // ---- Measurement / Check carton ----
        public int? MeasurementSampleQty { get; }
        public string MeasurementSampleSizeResult { get; }
        public int? NoOfCheck { get; }
        public string NoOfCheckUnit { get; }
        public int? NoOfPrepackCheck { get; }

        // ---- Defects ----
        public IReadOnlyList<DefectRowPdf> Defects { get; }

        //---- Abortive Reason ----

        public IReadOnlyList<AbortiveReasonsRowPdf> Reasons { get; }

        //---- Attachments -----
        //public IReadOnlyList<AttachmentRowPdf> QADocuments { get; }
        //public IReadOnlyList<AttachmentRowPdf> AdminDocuments { get; }

        public IReadOnlyList<AttachmentRowPdf> QADocuments { get; } = new List<AttachmentRowPdf>();
        public IReadOnlyList<AttachmentRowPdf> AdminDocuments { get; } = new List<AttachmentRowPdf>();

        // ---- Final approval / remarks ----
        public string? CommApprovalDate { get; }
        public string CommApprovedBy { get; }
        public string CommApprovalDoc { get; }
        public int DebitAmount { get; }
        public int Amt_Travelling { get; }
        public string AdminRemark { get; }
        public string RemarkComment { get; }
        public string Note { get; }
        public string InspectorName { get; }
        public int InspectorId { get; }
        public string FactoryRepresentative { get; }

        // ---- Attachments (raw comma-separated refs - resolve to local paths separately) ----
        public string? TestReportAttachmentRaw { get; }
        public string? GenAttachmentFirstRaw { get; }

        //----Stamp and Signatures
        public byte[] QASign { get; }
        public string QAStampNo { get; }
        public byte[] AdminSign { get; }
        public string AdminStampNo { get; }
        public string AdminName { get; }

        public bool Is_Edit_By_Admin { get; }
        public DateTime? Last_Amendment_Date { get; }

        public InspectionReportPdfModel(InspectionQueryResult result)
        {
            if (result is null) throw new ArgumentNullException(nameof(result));
            var d = result.Detail ?? throw new ArgumentException("Detail row was null — inspection not found.", nameof(result));

            IsPass = string.Equals(d.INSPECTION_RESULT, "PASS", StringComparison.OrdinalIgnoreCase);
            InspectionResultText = d.INSPECTION_RESULT;

            InspectionTypeId = d.INSPECTION_TYPE_ID;
            InspectionTypeText = d.TYPE_OF_INSPECTION;

            BuyerName = d.BUYER_NAME;
            BuyerId = "";

            ReportNo = d.INSPECTION_REPORT_NO;
            PONumber = d.PO_NUMBER;
            FIDate = d.DATE_OF_COMPLETION ?? DateTime.Now;
            VendorName = d.VENDOR_NAME;
            VendorCode = d.VENDOR_CODE;
            VendorLocation = d.LOCATION;

            POTotalQty = (decimal)(d.PO_QTY ?? 0);
            MCText = d.DESCRIPTION;

            Fit = d.PO_FIT; //.INSPECTION_FIT;

            MRP = d.MRP ?? 0;

            OfferedQuantity = (int)(d.OFFERED_QTY ?? 0);// d.SHIP_QTY ?? 0;

            ShipQuantity = (int)(d.SHIP_QTY ?? 0);

            Category = d.CATEGORY;

            BrandId = d.BRAND_LABEL;

            Color = d.COLOR;

            ArticleNumber = d.ARTICLES;

            GptNo = d.GPT_NO;
            GptStatus = d.GPT_STATUS;
            GptApprovedBy = d.APPROVED_BY_IF_GPT_FAILS ?? "";
            GptApprovedDate = d.APPROVED_DATE.HasValue ? d.APPROVED_DATE.Value.ToString("dd-MM-yyyy") : "";
            AreaOfFailGptr = d.AREA_OF_FAILURE_GPTR ?? "";

            GptElasticNo = d.GPT_ELASTIC_NO ?? "";
            GptElasticStatus = d.ELASTIC_STATUS;
            GptElasticApprovedBy = d.GPT_ELASTIC_APPROVED_BY ?? "";
            GptElasticApprovedDate = d.GPT_ELASTIC_APPROVED_DATE.HasValue
                ? d.GPT_ELASTIC_APPROVED_DATE.Value.ToString("dd-MM-yyyy") : "";

            ZipperRptNo = d.ZIPPER_REPORT_NO;
            ZipperStatus = d.ZIPPER_REPORT_STATUS;
            ZipperApprovedBy = d.IF_FAIL_ZIPPER_APPROVED_BY;
            ZipperApprovedDate = d.ZIPPER_APPROVED_DATE.HasValue
                ? d.ZIPPER_APPROVED_DATE.Value.ToString("dd-MM-yyyy") : "";


            NoOfBagsCartonsQty = 0;          // <-- placeholder, see TODO above
            NoOfBagsCartonsUnit = d.NO_OF_BAGS_CARTON; // holds the full "qty unit" text for now

            PackingStatus = d.PACKING_STATUS;
            InlineDone = d.INLINE_DONE;
            PackingList = d.PACKING_LIST;


            PpsReleaseDate = d.PPS_RELEASE_DATE;
            PpsExpiryDate = d.PPS_EXPIRY_DATE;
            MfgDate = d.MFG_DATE;

            Season = d.SEASON ?? "";
            ColorCode = d.COLOR_CODE;

            FiberContentOnPo = d.FIBER_CONTENT_ON_PO;
            FiberContentOnTestReport = d.FIBER_CONTENT_ON_TEST_REPORT;
            FiberContentOnWc = d.FIBER_CONTENT_ON_LABEL;
            GsmOnPo = d.GSM_ON_PO;
            GsmOnTestReport = d.GSM_ON_TEST_REPORT;
            GsmOnBulk = d.GSM_ON_BULK ?? "";

            SelectedBagCartonsNo = d.SELECTED_BAGS_CARTON_NO;
            InspectionFit = d.INSPECTION_FIT;

            Workmanship = "( Level : I/II )";

            FailReason = d.FAIL_REASON;

            AqlSampleSize = d.SAMPLE_SIZE;

            ConformitySize = d.CONFORMITY_SIZE ?? "";
            ConformitySizeStatus = d.CONFORMITY_SIZE_STATUS;
            EmbroideryPf = d.EMBRIODERY_PRINT_SHOULD_BE_AS_PER_PPS;
            AccessoriesPf = d.ACCESSORIES_SHOULD_BE_AS_PER_PPS;
            BarcodePf = d.BARCODE_COMPLIANCES;
            NoPlasticPf = d.NO_PLASTIC_TAG_KIMBLE_TAG;
            BogoPf = d.BOGO_DISCOUNT_TAG;

            ConformityPrepack = d.CONFORMITY_PREPACK ?? "";
            ConformityPrepackStatus = d.CONFORMITY_PREPACK_STATUS;
            ThreadYn = d.THREAD_FROM_NOMINATED_Y_N;
            ButtonYn = d.BUTTON_FROM_NOMINATED_Y_N;
            ZipperYn = d.ZIPPER_FROM_NOMINATED_Y_N;
            InterliningYn = d.INTERLINING_FROM_NOMINATED_VENDOR_Y_N;
            ElasticYn = d.ELASTIC_FROM_NOMINATED_Y_N;

            MeasurementSampleQty = d.MESUREMENT_SAMPLE_SIZE ?? 0;
            MeasurementSampleSizeResult = d.MEASUREMENT_PASS_FAIL;

            NoOfCheck = 0;                       // placeholder
            NoOfCheckUnit = d.NO_OF_CHECK_CARTON_BAG;  // full text for now
            NoOfPrepackCheck = d.NO_OF_CHECK_PRE_PACK ?? 0;

            const int TotalDefectsId = 1001;
            const int AllowedDefectsId = 1002;

            var realDefects = result.Defects
                .Where(x => x.DEFECTS_ID != TotalDefectsId && x.DEFECTS_ID != AllowedDefectsId)
                .Select(x => new DefectRowPdf(SNo: 0, DefectsId: x.DEFECTS_ID, Defect: x.DEFECTS ?? "", Major: x.MAJOR ?? 0, Minor: x.MINOR ?? 0, Status: ""))
                .ToList();

            var numbered = realDefects.Select((x, i) => x with { SNo = i + 1 }).ToList();

            int nextSNo = numbered.Count + 1;

            var totalRow = new DefectRowPdf(SNo: nextSNo++, DefectsId: TotalDefectsId, Defect: "Total", Major: numbered.Sum(x => x.Major), Minor: numbered.Sum(x => x.Minor), Status: "");

            var allowedRow = new DefectRowPdf(SNo: nextSNo, DefectsId: AllowedDefectsId, Defect: "Allowed", Major: d.ALLOWED_MAJOR, Minor: d.ALLOWED_MINOR,
                Status: totalRow.Major > d.ALLOWED_MAJOR || totalRow.Minor > d.ALLOWED_MINOR ? "FAIL" : "PASS");

            Defects = numbered.Append(totalRow).Append(allowedRow).ToList();


            //if (!string.IsNullOrWhiteSpace(d.ABORTIVE_REASON_IDS))
            //{
            //    var abortiveReasonIds = d.ABORTIVE_REASON_IDS.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(id => Convert.ToInt32(id.Trim())).ToHashSet();
            //    Reasons = result.AbortiveReasons.Where(x => abortiveReasonIds.Contains(x.AR_ID)).Select((x, i) => new AbortiveReasonsRowPdf(SNo: i + 1, Reason: x.REASON)).ToList();
            //}
            if (!string.IsNullOrWhiteSpace(d.ABORTIVE_REASON_IDS))
            {
                var abortiveReasonIds = d.ABORTIVE_REASON_IDS
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => Convert.ToInt32(id.Trim()))
                    .ToHashSet();

                Reasons = result.AbortiveReasons
                    .Where(x => abortiveReasonIds.Contains(x.AR_ID))
                    .Select((x, i) =>
                    {
                        var reasonText = x.AR_ID == 8 && !string.IsNullOrWhiteSpace(d.OTHER_ABORTIVE_REASON)
                            ? $"{x.REASON} : {d.OTHER_ABORTIVE_REASON.Trim()}"
                            : x.REASON;

                        return new AbortiveReasonsRowPdf(SNo: i + 1, Reason: reasonText);
                    })
                    .ToList();
            }


            CommApprovalDate = d.FI_COMM_APPR_DATE ?? "";
            CommApprovedBy = d.FI_COMM_APPROVED_BY ?? "N/A";
            CommApprovalDoc = d.COMM_APPROVAL_DOCUMENTS;
            DebitAmount = (int)(d.DEBIT_AMOUNT ?? 0);
            Amt_Travelling = (int)(d.AMT_TRAVELLING ?? 0);
            AdminRemark = d.ADMIN_REMARK;
            RemarkComment = d.REMARKS_DETAILS ?? "";
            Note = d.NOTE ?? "";

            // TODO: NAME_OF_THE_VMM_QA is pre-combined "Name [ Code ]"; InspectorId can't
            InspectorName = d.NAME_OF_THE_VMM_QA;
            InspectorId = 0;

            FactoryRepresentative = d.NAME_OF_FACTORY_REPRESENTATIVE;

            TestReportAttachmentRaw = d.TEST_REPORT_ATTACHMENT;
            GenAttachmentFirstRaw = d.GEN_ATTACHMENT_FIRST;

            if (!string.IsNullOrWhiteSpace(d.TEST_REPORT_ATTACHMENT))
            {
                var qaDocs = d.TEST_REPORT_ATTACHMENT.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                QADocuments = qaDocs.Select((x, i) => new AttachmentRowPdf(SNo: i + 1, Title: $"Attachment - {i + 1}", Url: x)).ToList();
            }
            if (!string.IsNullOrWhiteSpace(d.GEN_ATTACHMENT_FIRST))
            {
                var adminDocs = d.GEN_ATTACHMENT_FIRST.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                AdminDocuments = adminDocs.Select((x, i) => new AttachmentRowPdf(SNo: i + 1, Title: $"Attachment - {i + 1}", Url: x)).ToList();
            }

            QASign = d.SIGN;
            QAStampNo = d.STAMP_NO;
            AdminSign = d.QAADMIN_SIGN;
            AdminStampNo = d.QAADMIN_STAMP_NO;
            AdminName = d.NAME_OF_QA_ADMIN;

            Is_Edit_By_Admin = d.IS_EDIT_BY_ADMIN;

            Last_Amendment_Date = d.LAST_UPDATE_DATE_BY_ADMIN;

        }
    }

    public sealed record DefectRowPdf(int SNo, int DefectsId, string Defect, int Major, int Minor, string Status);

    public sealed record AbortiveReasonsRowPdf(int SNo, string Reason);

    public sealed record AttachmentRowPdf(int SNo, string Title, string Url);

    public sealed class AttachmentPath
    {
        public AttachmentType AttachmentType { get; set; }
        public string? AttachmentRootPath { get; set; }

    }

    public enum AttachmentType
    {
        Logo,
        QASignAndStamp,
        QPSAdminSingAndStamp
    }

    public sealed class BasePathOptions
    {
        public const string SectionName = "BasePath";

        public string RootPath { get; set; } = string.Empty;

        public string PublicBaseUrl { get; set; } = string.Empty;
    }

    public sealed record InspectionReportDetail
    {
        public DateTime? DATE_OF_COMPLETION { get; init; }
        public string INSPECTION_REPORT_NO { get; init; }
        public string TYPE_OF_INSPECTION { get; init; }
        public string INSPECTION_RESULT { get; init; }
        public string BUYER_NAME { get; init; }
        public string PO_NUMBER { get; init; }
        public string VENDOR_CODE { get; init; }
        public string VENDOR_NAME { get; init; }
        public string LOCATION { get; init; }
        public string ADDRESS { get; init; }
        public double? PO_QTY { get; init; }
        public string DESCRIPTION { get; init; }
        public string PO_FIT { get; init; }              // renamed from B.FIT — see note above
        public decimal? MRP { get; init; }
        public double? OFFERED_QTY { get; init; }
        public double? SHIP_QTY { get; init; }
        public string CATEGORY { get; init; }
        public string BRAND_LABEL { get; init; }
        public string COLOR { get; init; }
        public string ARTICLES { get; init; }
        public string GPT_NO { get; init; }
        public string GPT_STATUS { get; init; }
        public string APPROVED_BY_IF_GPT_FAILS { get; init; }
        public DateTime? APPROVED_DATE { get; init; }
        public string AREA_OF_FAILURE_GPTR { get; init; }
        public string GPT_ELASTIC_NO { get; init; }
        public string ELASTIC_STATUS { get; init; }
        public string GPT_ELASTIC_APPROVED_BY { get; init; }
        public DateTime? GPT_ELASTIC_APPROVED_DATE { get; init; }
        public string ZIPPER_REPORT_NO { get; init; }
        public string ZIPPER_REPORT_STATUS { get; init; }
        public string IF_FAIL_ZIPPER_APPROVED_BY { get; init; }
        public DateTime? ZIPPER_APPROVED_DATE { get; init; }
        public string NO_OF_BAGS_CARTON { get; init; }
        public string PACKING_STATUS { get; init; }
        public string INLINE_DONE { get; init; }
        public string PACKING_LIST { get; init; }
        public string PPS_RELEASE_DATE { get; init; }
        public string PPS_EXPIRY_DATE { get; init; }
        public string MFG_DATE { get; init; }
        public string SEASON { get; init; }
        public string COLOR_CODE { get; init; }
        public string FIBER_CONTENT_ON_PO { get; init; }
        public string FIBER_CONTENT_ON_TEST_REPORT { get; init; }
        public string FIBER_CONTENT_ON_LABEL { get; init; }
        public string GSM_ON_PO { get; init; }
        public string GSM_ON_TEST_REPORT { get; init; }
        public string GSM_ON_BULK { get; init; }
        public string INSPECTION_FIT { get; init; }      // renamed from I.FIT — see note above
        public string SELECTED_BAGS_CARTON_NO { get; init; }
        public string FAIL_REASON { get; init; }
        public int SAMPLE_SIZE { get; init; }
        public string CONFORMITY_SIZE { get; init; }
        public string CONFORMITY_SIZE_STATUS { get; init; }
        public string EMBRIODERY_PRINT_SHOULD_BE_AS_PER_PPS { get; init; }
        public string ACCESSORIES_SHOULD_BE_AS_PER_PPS { get; init; }
        public string BARCODE_COMPLIANCES { get; init; }
        public string NO_PLASTIC_TAG_KIMBLE_TAG { get; init; }
        public string BOGO_DISCOUNT_TAG { get; init; }
        public string CONFORMITY_PREPACK { get; init; }
        public string CONFORMITY_PREPACK_STATUS { get; init; }
        public string THREAD_FROM_NOMINATED_Y_N { get; init; }
        public string BUTTON_FROM_NOMINATED_Y_N { get; init; }
        public string ZIPPER_FROM_NOMINATED_Y_N { get; init; }
        public string INTERLINING_FROM_NOMINATED_VENDOR_Y_N { get; init; }
        public string ELASTIC_FROM_NOMINATED_Y_N { get; init; }
        public int? MESUREMENT_SAMPLE_SIZE { get; init; }
        public string MEASUREMENT_PASS_FAIL { get; init; }
        public int ALLOWED_MAJOR { get; init; }
        public int ALLOWED_MINOR { get; init; }
        public string NO_OF_CHECK_CARTON_BAG { get; init; }
        public int? NO_OF_CHECK_PRE_PACK { get; init; }
        public string FI_COMM_APPR_DATE { get; init; }
        public string FI_COMM_APPROVED_BY { get; init; }
        public string COMM_APPROVAL_DOCUMENTS { get; init; }
        public decimal? DEBIT_AMOUNT { get; init; }
        public decimal? AMT_TRAVELLING { get; init; }
        public string ADMIN_REMARK { get; init; }
        public string REMARKS_DETAILS { get; init; }
        public string NOTE { get; init; }
        public string NAME_OF_THE_VMM_QA { get; init; }
        public string STAMP_NO { get; init; }
        public byte[] SIGN { get; init; }
        public string NAME_OF_FACTORY_REPRESENTATIVE { get; init; }
        public string NAME_OF_QA_ADMIN { get; init; }
        public string QAADMIN_STAMP_NO { get; init; }
        public byte[] QAADMIN_SIGN { get; init; }
        public string TEST_REPORT_ATTACHMENT { get; init; }
        public string GEN_ATTACHMENT_FIRST { get; init; }
        public bool IS_COMPLETE { get; init; }
        public bool IS_EDIT_BY_ADMIN { get; init; }
        public string ABORTIVE_REASON_IDS { get; init; }
        public string OTHER_ABORTIVE_REASON { get; init; }
        public string APPS_NON_APPS { get; init; }
        public string CONTACT_NAME { get; init; }
        public string CONTACT_NUMBER { get; init; }
        public int INSPECT_ID { get; init; }
        public int INSP_ID { get; init; }
        public string POID { get; init; }
        public string INSPECTION_DATE { get; init; }
        public int INSPECTION_TYPE_ID { get; init; }
        public DateTime? LAST_UPDATE_DATE_BY_ADMIN { get; init; }

    }

    public sealed record InspectionDefectItem
    {
        public int DEFECTS_ID { get; init; }
        public string DEFECTS { get; init; }
        public int? MAJOR { get; init; }
        public int? MINOR { get; init; }
    }

    public sealed record AbortiveReasonItem
    {
        public int AR_ID { get; init; }
        public string REASON { get; init; }
    }

    public sealed record InspectionQueryResult(InspectionReportDetail Detail, IReadOnlyList<InspectionDefectItem> Defects, IReadOnlyList<AbortiveReasonItem> AbortiveReasons);


}
