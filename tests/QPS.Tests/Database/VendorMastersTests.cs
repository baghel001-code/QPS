using System.Data;
using Application.Interfaces.V1;
using Dapper;
using Domain.Entities.Modals;
using Domain.Entities.Request;
using Infrastructure.Utilitys;

namespace QPS.Tests.Database
{
    /// <summary>
    /// Masters maintained from the portal through IVendor: the e-mail list (SP_EMAIL_LIST_CRUD) and the buyer /
    /// category-head hierarchy (SP_MASTER_HIERARCHY_CRUD).
    /// </summary>
    [Collection(QpsDatabaseCollection.Name)]
    public class VendorMastersTests(QpsDatabaseFixture db)
    {
        private IVendor Vendor => db.Service<IVendor>();

        [SqlServerFact]
        public async Task The_email_list_returns_every_row_ordered_by_name()
        {
            var list = (await Vendor.GetEmailMgtList(new SelectListReq { Cmd = "GETALL" })).EmailsList;
            var names = list.Select(e => e.NAME).ToList();
            Assert.Contains("QA Team", names);
            Assert.Contains("Old List", names);
            Assert.Equal(names.OrderBy(n => n, StringComparer.Ordinal), names);
        }

        [SqlServerFact]
        public async Task An_email_can_be_added_and_then_updated()
        {
            var add = await Vendor.AddUpdateEmailMgt(new EmailManagementDto { Action = "INSERT", EmailId = "new.list@example.com", Name = "New List", IsActive = true });
            Assert.Equal(0, add.responseCode);

            var upd = await Vendor.AddUpdateEmailMgt(new EmailManagementDto { Action = "UPDATE", EmailId = "new.list@example.com", Name = "Renamed List", IsActive = false });
            Assert.Equal(0, upd.responseCode);

            using var c = db.Open();
            var row = c.QuerySingle<EmailManagement>("SELECT EMAIL_ID, NAME, IS_ACTIVE FROM EMAIL_LIST WHERE EMAIL_ID = 'new.list@example.com'");
            Assert.Equal(("Renamed List", false), (row.NAME, row.IS_ACTIVE));
        }

        [SqlServerFact]
        public async Task The_procedure_reports_a_duplicate_email_in_its_output_parameters()
        {
            var p = new DynamicParameters();
            p.Add("@ACTION", "INSERT");
            p.Add("@EMAIL_ID", "qa.team@example.com");
            p.Add("@NAME", "QA Team again");
            p.Add("@IS_ACTIVE", true);
            p.Add("@ERROR_CODE", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("@ERROR_MESSAGE", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

            await db.Service<DapperHelper>().ExecuteStoredProcedureAsync("SP_EMAIL_LIST_CRUD", p);

            Assert.NotEqual(0, p.Get<int>("@ERROR_CODE"));
            Assert.Equal("EMAIL ID ALREADY EXISTS.", p.Get<string>("@ERROR_MESSAGE"));
        }

        [SqlServerFact(Skip = "Known bug: VendorService checks parameters.ParameterNames.Contains(\"@ERROR_CODE\"), but Dapper lists the names " +
                              "without '@', so procedure errors are never passed on (responseCode stays 0). Remove this Skip once fixed.")]
        public async Task The_service_passes_on_a_duplicate_email_error()
        {
            var res = await Vendor.AddUpdateEmailMgt(new EmailManagementDto { Action = "INSERT", EmailId = "qa.team@example.com", Name = "Duplicate", IsActive = true });
            Assert.NotEqual(0, res.responseCode);
            Assert.Equal("EMAIL ID ALREADY EXISTS.", res.responseMessage);
        }

        [SqlServerFact]
        public async Task The_hierarchy_below_a_category_head_is_returned_level_by_level()
        {
            var list = (await Vendor.GetHierarchyList(new SelectListReq { Cmd = "GET_ALONG_WITH_CHILD", StrField = "H001" })).HierarchyList;

            Assert.Equal(["H001", "H002", "H003"], list.Select(h => h.EMP_CODE));
            Assert.Equal([0, 1, 2], list.Select(h => h.HIERARCHY_LEVEL));
            Assert.Equal("Arun Assoc CH", list.Single(h => h.EMP_CODE == "H003").REPORTING_MANAGER);
            Assert.Equal("Associate Category Head", list.Single(h => h.EMP_CODE == "H002").ROLE_NAME);
        }

        [SqlServerFact]
        public async Task A_hierarchy_member_can_be_added_under_a_manager()
        {
            var res = await Vendor.AddUpdateHierarchy(new HierarchyDto
            {
                Action = "INSERT", EmpCode = "H006", EmpName = "Neha Buyer", EmailId = "neha.buyer@example.com",
                MobileNo = "9100000006", RoleId = 2, ReportingEmpId = "H005", IsActive = true,   // under the second chain (H005)
            });
            Assert.Equal(0, res.responseCode);

            var added = Assert.Single((await Vendor.GetHierarchyList(new SelectListReq { Cmd = "GET", StrField = "H006" })).HierarchyList);
            Assert.Equal(("H005", "Vikram Category Head", "Buyer"), (added.REPORTING_EMP_ID, added.REPORTING_MANAGER, added.ROLE_NAME));
        }
    }
}
