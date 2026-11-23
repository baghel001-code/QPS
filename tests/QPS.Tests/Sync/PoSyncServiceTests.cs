using Application.Interfaces.V1;
using Application.Services;
using Domain.Entities.AppSettings;
using Domain.Entities.Common;
using Domain.Entities.Modals;
using Domain.Entities.Response;
using Domain.Records;
using Microsoft.Extensions.Options;

namespace QPS.Tests.Sync
{
    /// <summary>
    /// PoSyncService / SapPoFetcher / POHeader without SAP or a database (PO Sync Case Analysis: forced-sync message,
    /// N7 original PO governs, N9 line-item changes, N17 how the PO quantity is read).
    /// </summary>
    public class PoSyncServiceTests
    {
        // ---------- fakes ----------

        private sealed class FakeFetcher(POHeader? sap) : ISapPoFetcher
        {
            public List<string> Requested { get; } = [];
            public Task<SapPoFetchResult> FetchAsync(string poNumber, IProgress<int>? progress = null)
            {
                Requested.Add(poNumber);
                return Task.FromResult(sap is null
                    ? new SapPoFetchResult(false, null, "not found", PoSyncIssue.NotFoundInSap)
                    : new SapPoFetchResult(true, sap, null));
            }
        }

        /// <summary>QPSApi stand-in: GET_PO_DETAILS returns <see cref="QpsLines"/>, SYNC_PO_DETAILS records the PO it was sent.</summary>
        private sealed class FakeApi : IAPICall
        {
            public List<PO_DETAILS_MODEL> QpsLines { get; } = [];
            public List<POHeader> Synced { get; } = [];
            public int LineLookups { get; private set; }

            public Task<RepT> CallingAPI<RepT, ReqT>(string ApiName, ReqT requestBody)
            {
                object res = requestBody switch
                {
                    POHeader po => Record(po),
                    _ when typeof(RepT) == typeof(POResponse) => Lines(),
                    _ when typeof(RepT) == typeof(ApiData) => new ApiData { Api_Url = "http://sap.test/po" },
                    _ => throw new NotSupportedException(typeof(RepT).Name),
                };
                return Task.FromResult((RepT)res);
            }

            private GenRes Record(POHeader po) { Synced.Add(po); return new GenRes(); }
            private POResponse Lines() { LineLookups++; return new POResponse { PODetails = QpsLines }; }
            public POSAPResponse? SapResponse { get; set; }
            public Task<RepT> CallingSAPAPI<RepT, ReqT>(ReqT requestBody, ApiData apiData) => Task.FromResult((RepT)(object)SapResponse!);
            public Task<RepT> CallingGSTAPI<RepT, ReqT>(ReqT requestBody, ApiData apiData) => throw new NotSupportedException();
        }

        private static POHeader Po(decimal qty = 1000, string fabric = "Cotton", params PODetails[] lines) => new()
        {
            Id = 7, PoNumber = "4500000001", VendorCode = "V0001", VendorName = "Acme", POTotalQty = qty, Fabric = fabric,
            Color = "Blue", Details = [.. lines],
        };

        private static PODetails Line(string item, decimal qty, string article = "ART-1", int id = 0) =>
            new() { Id = id, LineItem = item, Article = article, ArticleDesc = "Shirt", PurchaseTotQty = qty, MRP = 799 };

        private static PO_DETAILS_MODEL QpsLine(int id, string item, decimal qty, string article = "ART-1") =>
            new() { PO_DETAILS_ID = id, LINE_ITEM = item, ARTICLE = article, ARTICLE_DESC = "Shirt", PURCHASE_TOT_QTY = qty, MRP = 799 };

        // ---------- forced sync message ----------

        [Fact]
        public async Task A_forced_sync_lists_what_changed()
        {
            var api = new FakeApi();
            var r = await new PoSyncService(new FakeFetcher(Po(1200, "Linen")), api).SyncAsync(Po(), userId: 3, forceSync: true);

            Assert.True(r.Success);
            Assert.Equal("PO details synchronized successfully. Changed: PO Qty (1000 to 1200), Fabric.", r.Message);
            Assert.Single(api.Synced);
        }

        [Fact]
        public async Task A_forced_sync_without_differences_says_so_and_still_saves()
        {
            var api = new FakeApi();
            var r = await new PoSyncService(new FakeFetcher(Po()), api).SyncAsync(Po(), 3, forceSync: true);

            Assert.Equal("PO details synchronized successfully. No differences from SAP were found.", r.Message);
            Assert.Single(api.Synced);
        }

        [Fact]
        public async Task A_normal_sync_without_differences_saves_nothing()
        {
            var api = new FakeApi();
            var r = await new PoSyncService(new FakeFetcher(Po()), api).SyncAsync(Po(), 3);

            Assert.Equal(PoSyncMessages.AlreadySynced, r.Message);
            Assert.Empty(api.Synced);
        }

        // ---------- N7 ----------

        [Fact]
        public async Task The_original_po_is_synced_even_when_the_request_is_linked_to_another_po()
        {
            var fetcher = new FakeFetcher(Po());
            var current = Po();
            current.LinkPoNumber = "4500000099";

            await new PoSyncService(fetcher, new FakeApi()).SyncAsync(current, 3);

            Assert.Equal(["4500000001"], fetcher.Requested);
        }

        // ---------- N9 ----------

        [Fact]
        public async Task A_line_only_change_is_detected_by_loading_the_qps_lines_and_is_synced()
        {
            var api = new FakeApi();
            api.QpsLines.Add(QpsLine(11, "10", 600));
            api.QpsLines.Add(QpsLine(12, "20", 400));
            var sap = Po(1000, "Cotton", Line("10", 500), Line("20", 400), Line("30", 100, "ART-3"));   // same header total

            var current = Po();   // screens hold the header only
            var r = await new PoSyncService(new FakeFetcher(sap), api).SyncAsync(current, 3);

            Assert.True(r.Success);
            Assert.Equal(1, api.LineLookups);
            Assert.Contains("Line items (1 changed, 1 added, 0 removed)", r.Message);
            var sent = Assert.Single(api.Synced);
            Assert.Equal(3, sent.Details.Count);
            Assert.Equal([11, 12, 0], current.Details.Select(d => d.Id));   // QPS ids kept for existing line items
        }

        [Fact]
        public void Identical_lines_are_not_a_change_and_missing_lines_are_not_compared()
        {
            var a = Po(1000, "Cotton", Line("10", 600), Line("20", 400));
            var b = Po(1000, " Cotton ", Line("20", 400), Line(" 10 ", 600));
            Assert.Empty(a.GetChangedFields(b));
            Assert.Empty(Po().GetChangedFields(b));            // no QPS lines loaded
            Assert.Equal("Line items (0 changed, 0 added, 1 removed)", a.LineChanges(Po(1000, "Cotton", Line("10", 600))));
        }

        // ---------- N17 ----------

        private static POSAPResponse SapRows(params (string Item, string Qty)[] rows) => new()
        {
            APPS_FOOTWEAR_DETAILS = rows.Select(r => new SAP_PO_DetailDto
            {
                PO_NUMBER = "4500000001", PO_LINE_ITEM = r.Item, ARTICLE = "A" + r.Item, VENDOR_CODE = "V0001", LOB = "MISC",
                PURCHASE_TOT_QTY = r.Qty, MRP = "0", FABRIC = "Cotton", COLOR = "Blue",
            }).ToList(),
        };

        [Fact]
        public void By_default_the_po_quantity_is_the_first_rows_value() =>
            Assert.Equal(1000, new POHeader().MapToHeader(SapRows(("10", "1000"), ("20", "1000"))).POTotalQty);

        [Fact]
        public void With_SumOfLines_the_po_quantity_is_the_sum_of_one_value_per_line_item() =>
            Assert.Equal(900, new POHeader().MapToHeader(SapRows(("10", "600"), ("20", "300"), ("20", "300")), PoTotalQtyMode.SumOfLines).POTotalQty);

        [Theory]
        [InlineData(PoTotalQtyMode.FirstRow, 600)]
        [InlineData(PoTotalQtyMode.SumOfLines, 900)]
        public async Task The_fetcher_uses_the_Sap_PoTotalQtyMode_setting(PoTotalQtyMode mode, decimal expected)
        {
            var api = new FakeApi { SapResponse = SapRows(("10", "600"), ("20", "300")) };
            var r = await new SapPoFetcher(api, Options.Create(new SapPoOptions { PoTotalQtyMode = mode })).FetchAsync("4500000001");
            Assert.True(r.Success);
            Assert.Equal(expected, r.PO!.POTotalQty);
        }

        [Fact]
        public void The_setting_defaults_to_today_s_behaviour() =>
            Assert.Equal(PoTotalQtyMode.FirstRow, new SapPoOptions().PoTotalQtyMode);
    }
}
