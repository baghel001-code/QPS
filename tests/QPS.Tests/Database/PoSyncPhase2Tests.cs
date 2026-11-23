using System.Data;
using System.Data.SqlClient;
using Application.Interfaces.V1;
using Dapper;
using Domain.Entities.Modals;

namespace QPS.Tests.Database
{
    /// <summary>
    /// The rest of the PO Sync Case Analysis on the seeded database, through the app's services:
    /// Reset by Admin limited to QA Admin (open point 7), E12 PO values frozen at completion, N11 per-PO lock,
    /// N9 line-item sync. Seed: request 3 / inspection 2 on PO 3 (completed), request 4 on PO 4 (open, qty 300).
    /// </summary>
    [Collection(QpsDatabaseCollection.Name)]
    public class PoSyncPhase2Tests(QpsDatabaseFixture db)
    {
        private const int QaUser = 2, QamUser = 3, QaAdminUser = 6;   // USER_MASTER ids (roles 1006, 1007, 1008)

        private IVendor Vendor => db.Service<IVendor>();

        private static InspectionModel Inspection(long inspectId, long requestId, int userId, int qty, bool complete,
                                                  bool reset = false, string remark = "") => new()
        {
            InspectId = inspectId, InspectionId = requestId, InspectorCode = "E1002", UserId = userId,
            OfferedQuantity = qty, POOfferQuantity = qty.ToString(), IsComplete = complete, IsResetByAdmin = reset,
            GptNo = "GPT-T", GptStatus = 1, NoOfBagsCartonsQty = 10, NoOfBagsCartonsUnit = 1, PackingStatus = 1, InlineDone = 1, PackingList = 1,
            FiberContentOnPo = "100% Cotton", FiberContentOnTestReport = "100% Cotton", FiberContentOnWc = "100% Cotton",
            GsmOnPo = "140", GsmOnTestReport = "140", MeasurementSampleSize = 1, NoOfCheck = 20, NoOfCheckUnit = 3,
            InspectionResult = 1, NameOfFactoryRepresentative = "Rep", TestReportAttachment = "/uploads/report.pdf",
            ColorCode = 3, RemarkComment = remark, Defects = [],
        };

        private (int Status, bool Complete) Request3()
        {
            using var c = db.Open();
            return c.QuerySingle<(int, bool)>(
                "SELECT R.STATUS_CODE, I.IS_COMPLETE FROM INSPECTION_REQUESTS R JOIN INSPECTIONS I ON I.INSPECTION_ID = R.INSP_ID WHERE R.INSP_ID = 3");
        }

        // ---------- Reset by Admin: QA Admin (1008) only ----------

        [SqlServerTheory]
        [InlineData(QaUser)]
        [InlineData(QamUser)]
        public async Task Reset_is_refused_for_anyone_but_QA_Admin(int userId)
        {
            var res = await Vendor.SaveInspection(Inspection(2, 3, userId, 500, complete: false, reset: true));

            Assert.Equal(50005, res.responseCode);
            Assert.Equal("Only QA Admin can reset a completed inspection.", res.responseMessage);
            Assert.Equal((7, true), Request3());
        }

        [SqlServerFact]
        public async Task QA_Admin_can_reset_and_the_report_is_reopened()
        {
            try
            {
                var res = await Vendor.SaveInspection(Inspection(2, 3, QaAdminUser, 500, complete: false, reset: true));
                Assert.True(res.responseCode == 0, res.responseMessage);
                Assert.Equal((8, false), Request3());
            }
            finally
            {
                using var c = db.Open();   // put request 3 back as completed for the other tests
                c.Execute("UPDATE INSPECTION_REQUESTS SET STATUS_CODE = 7 WHERE INSP_ID = 3; UPDATE INSPECTIONS SET IS_COMPLETE = 1 WHERE INSPECT_ID = 2;");
            }
        }

        // ---------- E12: PO values frozen at completion ----------

        [SqlServerFact]
        public async Task Completing_a_report_freezes_the_PO_values_and_the_PDF_report_keeps_them_after_a_SAP_change()
        {
            var res = await Vendor.SaveInspection(Inspection(0, 4, QaUser, 300, complete: true));
            Assert.True(res.responseCode == 0, res.responseMessage);
            var inspectId = res.recordId;

            using var c = db.Open();
            var snap = c.QuerySingle<(decimal Qty, string Fabric, string Color, decimal Mrp)>(
                "SELECT PO_TOTAL_QTY_AT_COMPLETION, PO_FABRIC_AT_COMPLETION, PO_COLOR_AT_COMPLETION, PO_MRP_AT_COMPLETION FROM INSPECTIONS WHERE INSPECT_ID = @inspectId",
                new { inspectId });
            Assert.Equal((300m, "Twill", "Khaki", 1199m), snap);
            Assert.Equal(1, c.ExecuteScalar<int>("SELECT COUNT(*) FROM INSPECTION_AUDIT_TRAIL WHERE INSPECTION_ID = @inspectId", new { inspectId }));   // one entry, not two

            // SAP changes PO 4 afterwards
            var sync = await Vendor.SyncPOdetailsService(new POHeader { Id = 4, PoNumber = "4500000004", POTotalQty = 350, Fabric = "Stretch Twill", Color = "Olive", MRP = 1299, CreatedBy = 3 });
            Assert.Equal(0, sync.responseCode);

            var report = c.QueryFirst("PRO_INSPECTION_DETAIL", new { ID = inspectId, STRFIELD = "", CMD = "INSPECTION_REPORT_PDF" }, commandType: CommandType.StoredProcedure);
            Assert.Equal(300d, (double)report.PO_QTY);
            Assert.Equal("Khaki", (string)report.COLOR);
            Assert.Equal(1199m, (decimal)report.MRP);
        }

        [SqlServerFact]
        public async Task Saving_a_draft_does_not_freeze_anything()
        {
            using var c = db.Open();
            c.Execute(@"INSERT INTO POHEADER (PO_NUMBER, VENDOR_CODE, TOTAL_QTY, CREATED_BY) VALUES ('4500000005', 'V0001', 100, 1);
                        INSERT INTO INSPECTION_REQUESTS (POID, OFFERED_QTY, DATE_OF_INSPECTION, ADDRESS_ID, CREATED_BY, STATUS_CODE)
                        VALUES (SCOPE_IDENTITY(), 100, '2026-11-22', 1, 2, 6);");
            var requestId = c.ExecuteScalar<int>("SELECT MAX(INSP_ID) FROM INSPECTION_REQUESTS");

            var res = await Vendor.SaveInspection(Inspection(0, requestId, QaUser, 100, complete: false));

            Assert.True(res.responseCode == 0, res.responseMessage);
            Assert.Null(c.ExecuteScalar<decimal?>("SELECT PO_TOTAL_QTY_AT_COMPLETION FROM INSPECTIONS WHERE INSPECT_ID = @id", new { id = res.recordId }));
        }

        // ---------- N11: one save per PO at a time ----------

        [SqlServerFact]
        public async Task A_save_waits_while_another_save_holds_the_PO_lock()
        {
            await using var other = new SqlConnection(db.ConnectionString);
            await other.OpenAsync();
            var tx = other.BeginTransaction();
            var got = await other.ExecuteScalarAsync<int>(
                "DECLARE @r INT; EXEC @r = sp_getapplock @Resource = N'QPS_PO_QTY_3', @LockMode = 'Exclusive', @LockOwner = 'Transaction'; SELECT @r;",
                transaction: tx);
            Assert.True(got >= 0);

            // QA Admin edits the remark of the completed report on PO 3 while the lock is held
            var save = Vendor.SaveInspection(Inspection(2, 3, QaAdminUser, 500, complete: true, remark: "checked under lock"));
            var finishedEarly = await Task.WhenAny(save, Task.Delay(1500)) == save;
            tx.Rollback();   // releases the lock
            var res = await save;

            Assert.False(finishedEarly, "the save should wait for the PO lock");
            Assert.True(res.responseCode == 0, res.responseMessage);
        }

        [SqlServerFact]
        public void Both_saving_procedures_take_the_same_PO_lock()
        {
            using var c = db.Open();
            foreach (var proc in new[] { "SP_SAVE_PO", "SP_SAVE_INSPECTION" })
                Assert.Contains("QPS_PO_QTY_", c.ExecuteScalar<string>("SELECT OBJECT_DEFINITION(OBJECT_ID(@proc))", new { proc }));
        }

        // ---------- N9: line items ----------

        private List<(int Id, string Item, string Article, decimal Qty)> Lines(int poid)
        {
            using var c = db.Open();
            return c.Query<(int, string, string, decimal)>(
                "SELECT PO_DETAILS_ID, LINE_ITEM, ARTICLE, PURCHASE_TOT_QTY FROM PODETAIL WHERE POID = @poid ORDER BY LINE_ITEM", new { poid }).ToList();
        }

        private static PODetails Line(string item, string article, decimal qty) =>
            new() { LineItem = item, Article = article, ArticleDesc = article, PurchaseTotQty = qty, Eaches = qty, MRP = 799 };

        private Task<Domain.Entities.Response.GenRes> SyncPo1Lines(params PODetails[] lines) =>
            Vendor.SyncPOdetailsService(new POHeader
            {
                Id = 1, PoNumber = "4500000001", POTotalQty = 1500, Fabric = "Cotton", Color = "Blue", CreatedBy = 3, Details = [.. lines],
            });

        [SqlServerFact]
        public async Task Line_items_are_updated_added_and_removed_keeping_existing_ids()
        {
            var before = Lines(1);
            var line10 = before.Single(l => l.Item == "10").Id;

            var res = await SyncPo1Lines(Line("10", "ART-1001-M", 700), Line("30", "ART-1001-XL", 200));   // 10 changed, 20 removed, 30 added

            Assert.True(res.responseCode == 0, res.responseMessage);
            var after = Lines(1);
            Assert.Equal(["10", "30"], after.Select(l => l.Item));
            Assert.Equal((line10, 700m), (after[0].Id, after[0].Qty));
            using var c = db.Open();
            Assert.Equal(1, c.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM SPTRACE WHERE SP_NEWVALUES LIKE '%PO_NUMBER = 4500000001%PODETAIL: 1 updated, 1 added, 1 removed%'"));
        }

        [SqlServerFact]
        public async Task Repeated_line_items_from_SAP_leave_the_lines_alone_with_a_warning()
        {
            var before = Lines(2);
            var res = await Vendor.SyncPOdetailsService(new POHeader
            {
                Id = 2, PoNumber = "4500000002", POTotalQty = 800, Fabric = "Denim", Color = "Indigo", CreatedBy = 3,
                Details = [Line("10", "ART-A", 400), Line("10", "ART-B", 400)],
            });

            Assert.True(res.responseCode == 0, res.responseMessage);
            Assert.Contains("repeated line item numbers", res.responseMessage);
            Assert.Equal(before, Lines(2));
        }

        [SqlServerFact]
        public async Task A_header_only_sync_does_not_touch_the_lines()
        {
            var before = Lines(3);
            var res = await Vendor.SyncPOdetailsService(new POHeader { Id = 3, PoNumber = "4500000003", POTotalQty = 520, Fabric = "Poplin", Color = "White", CreatedBy = 3 });
            Assert.True(res.responseCode == 0, res.responseMessage);
            Assert.Equal(before, Lines(3));
        }
    }
}
