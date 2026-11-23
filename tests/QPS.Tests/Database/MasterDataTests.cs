using Application.Interfaces.V1;
using Dapper;
using Domain.Entities.Request;
using Domain.Entities.Response;
using Infrastructure.Utilitys;

namespace QPS.Tests.Database
{
    /// <summary>Look-up lists (PRO_MASTER_LOOKUPS through DapperHelper) and the API master (IAPI → PRO_GET_ALL_API).</summary>
    [Collection(QpsDatabaseCollection.Name)]
    public class MasterDataTests(QpsDatabaseFixture db)
    {
        private async Task<List<MasterData>> Lookup(string cmd, string strField = "")
        {
            var p = new DynamicParameters();
            p.Add("@ID", 0);
            p.Add("@STRFIELD", strField);
            p.Add("@CMD", cmd);
            return (await db.Service<DapperHelper>().ExecuteStoredProcedureListAsync<MasterData>("PRO_MASTER_LOOKUPS", p)).ToList();
        }

        [SqlServerFact]
        public async Task Regions_list_only_active_ones() =>
            Assert.Equal(["North", "South", "East"], (await Lookup("GET_REGION")).Select(r => r.Master_Name));

        [SqlServerFact]
        public async Task Pass_fail_values_list_only_active_ones() =>
            Assert.Equal([1, 2, 3], (await Lookup("GET_PASSFAIL")).Select(r => r.Master_ID).Order());

        [SqlServerFact]
        public async Task Buyers_come_from_the_purchase_orders_once_each()
        {
            var buyers = await Lookup("Buyer_Master_List");
            var b = Assert.Single(buyers);
            Assert.Equal(("E1004", "Karan Buyer"), (b.String_ID, b.Master_Name));
        }

        [SqlServerFact]
        public async Task Hierarchy_roles_are_the_placeholder_plus_buyer_and_category_heads() =>
            Assert.Equal([0, 2, 1010, 1011], (await Lookup("Get_Hierarchy_Roles_List")).Select(r => r.Master_ID).Order());

        [SqlServerFact]
        public async Task Abortive_reasons_list_only_active_ones() =>
            Assert.Equal(2, (await Lookup("GET_ABORTIVE_REASONS")).Count);

        [SqlServerFact]
        public async Task An_unknown_command_returns_nothing() =>
            Assert.Empty(await Lookup("NO_SUCH_COMMAND"));

        [SqlServerFact]
        public async Task The_api_master_builds_each_url_from_base_url_and_module()
        {
            var res = await db.Service<IAPI>().GetAllApi();

            Assert.Equal(0, res.responseCode);
            Assert.Equal(3, res.ApiData!.Count);
            var roles = res.ApiData.Single(a => a.Task_Name == "SELECT_ALL_ROLES");
            Assert.Equal("http://localhost:5279/api/v1/SelectAllRoles", roles.Api_Url);
            Assert.Equal("POST", roles.Api_Http_Method);
            Assert.Equal(60, roles.Api_Timeout);
        }

        [SqlServerFact]
        public async Task A_single_api_row_fills_the_parameter_into_the_url()
        {
            var api = await db.Service<IAPI>().GetSpecificApiData(new SAP_API_DATA_Req { TaskName = "GET_LIST_BY_PARM", Param = "42" });
            Assert.Equal("http://localhost:5279/api/v1/GetList/42?page=1", api.Api_Url);
        }

        [SqlServerFact]
        public async Task An_unknown_api_task_returns_null() =>
            Assert.Null(await db.Service<IAPI>().GetSpecificApiData(new SAP_API_DATA_Req { TaskName = "NO_SUCH_TASK" }));
    }
}
