using Application.Interfaces.V1;
using Dapper;
using Domain.Entities.Modals;

namespace QPS.Tests.Database
{
    /// <summary>
    /// PO sync from SAP into QPS (IVendor.SyncPOdetailsService → SP_UPDATE_POHEADER), per the cases in
    /// "QPS PO Sync Case Analysis" (E2, E3, E13, N2, N5, N6; test scenarios T10 and T13).
    /// Seed: PO 1 = 4500000001, qty 1200, request 1 completed PASS with 1200 shipped. PO 2 = 4500000002, qty 800.
    /// </summary>
    [Collection(QpsDatabaseCollection.Name)]
    public class PoSyncTests(QpsDatabaseFixture db)
    {
        private IVendor Vendor => db.Service<IVendor>();

        private static POHeader Sap(int poid, string poNumber, decimal qty, string fabric = "Cotton", string vendorName = "", string buyerName = "") => new()
        {
            Id = poid, PoNumber = poNumber, POTotalQty = qty, Fabric = fabric, Color = "Blue", CreatedBy = 3,
            VendorName = vendorName, BuyerName = buyerName,
        };

        private (decimal Qty, string Fabric, string VendorName, int Trace) Read(int poid)
        {
            using var c = db.Open();
            var po = c.QuerySingle<(decimal, string, string)>("SELECT TOTAL_QTY, FABRIC, VENDOR_NAME FROM POHEADER WHERE POID = @poid", new { poid });
            return (po.Item1, po.Item2, po.Item3, c.ExecuteScalar<int>("SELECT COUNT(*) FROM SPTRACE"));
        }

        [SqlServerFact]
        public async Task A_decrease_below_the_passed_quantity_is_applied_with_a_shortfall_warning()   // E2, E3
        {
            var res = await Vendor.SyncPOdetailsService(Sap(1, "4500000001", 1000));

            Assert.Equal(0, res.responseCode);
            Assert.StartsWith("WARNING:", res.responseMessage);
            Assert.Contains("Allowed quantity (110%) is 1100 but 1200 is already offered or passed", res.responseMessage);
            Assert.Contains("reduced by 100", res.responseMessage);
            Assert.Equal(1000, Read(1).Qty);   // SAP is the source of truth: the change is applied
        }

        [SqlServerFact]
        public async Task An_increase_is_applied_without_a_warning()
        {
            var res = await Vendor.SyncPOdetailsService(Sap(1, "4500000001", 1500));
            Assert.Equal(0, res.responseCode);
            Assert.False(res.responseMessage?.StartsWith("WARNING:") ?? false);
            Assert.Equal(1500, Read(1).Qty);
        }

        [SqlServerFact]
        public async Task Vendor_name_and_fabric_changes_are_synced()   // E9, E13
        {
            var res = await Vendor.SyncPOdetailsService(Sap(2, "4500000002", 800, fabric: "Stretch Denim", vendorName: "Blue Loom Pvt Ltd"));
            Assert.Equal(0, res.responseCode);
            var po = Read(2);
            Assert.Equal(("Stretch Denim", "Blue Loom Pvt Ltd"), (po.Fabric, po.VendorName));
        }

        [SqlServerFact]
        public async Task A_po_number_with_leading_zeros_or_spaces_still_matches()   // N5
        {
            var res = await Vendor.SyncPOdetailsService(Sap(2, " 004500000002 ", 850));
            Assert.Equal(0, res.responseCode);
            Assert.Equal(850, Read(2).Qty);
        }

        [SqlServerTheory]   // T10, T13: rejected, nothing updated, no SPTRACE row
        [InlineData(1, "4500000999", 50, "does not match QPS PO")]        // N5 - SAP returned another PO
        [InlineData(0, "4500000001", 50, "not saved in QPS")]             // N6 - POID missing
        [InlineData(1, "4500000001", 0, "zero quantity")]                 // N2 - SAP quantity 0
        public async Task Invalid_syncs_are_rejected_and_change_nothing(int poid, string poNumber, decimal qty, string message)
        {
            var before = Read(1);

            var res = await Vendor.SyncPOdetailsService(Sap(poid, poNumber, qty, fabric: "Should not be saved"));

            Assert.NotEqual(0, res.responseCode);
            Assert.Contains(message, res.responseMessage);
            Assert.Equal(before, Read(1));
        }
    }
}
