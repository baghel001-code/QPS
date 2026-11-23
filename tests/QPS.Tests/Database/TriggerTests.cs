using Dapper;

namespace QPS.Tests.Database
{
    /// <summary>The audit triggers: region changes on USER_MASTER and every change to INSPECTIONS are recorded.</summary>
    [Collection(QpsDatabaseCollection.Name)]
    public class TriggerTests(QpsDatabaseFixture db)
    {
        [SqlServerFact]
        public void Changing_an_employees_regions_writes_a_history_row()
        {
            using var c = db.Open();
            c.Execute("UPDATE USER_MASTER SET REGION_IDS = '1,3', UPDATED_BY = 1 WHERE EMP_CODE = 'E1002'");

            var h = c.QuerySingle<(string OLD_REGION_IDS, string NEW_REGION_IDS, int CHANGED_BY)>(
                "SELECT OLD_REGION_IDS, NEW_REGION_IDS, CHANGED_BY FROM USER_MASTER_AUDIT_HISTORY WHERE EMP_CODE = 'E1002'");
            Assert.Equal(("1", "1,3", 1), h);
        }

        [SqlServerFact]
        public void Changing_other_employee_fields_writes_no_history()
        {
            using var c = db.Open();
            c.Execute("UPDATE USER_MASTER SET EMP_DESIG = 'Senior Buyer', UPDATED_BY = 1 WHERE EMP_CODE = 'E1004'");
            Assert.Equal(0, c.ExecuteScalar<int>("SELECT COUNT(*) FROM USER_MASTER_AUDIT_HISTORY WHERE EMP_CODE = 'E1004'"));
        }

        [SqlServerFact]
        public void Updating_an_inspection_adds_a_json_audit_entry()
        {
            using var c = db.Open();
            var before = c.ExecuteScalar<int>("SELECT COUNT(*) FROM INSPECTION_AUDIT_TRAIL WHERE INSPECTION_ID = 1");

            c.Execute("UPDATE INSPECTIONS SET REMARK_COMMENT = 'Checked again', MODIFIED_BY = 3, MODIFIED_DATE = GETDATE() WHERE INSPECT_ID = 1");

            var entries = c.Query<string>("SELECT CHANGE_HISTORY FROM INSPECTION_AUDIT_TRAIL WHERE INSPECTION_ID = 1 ORDER BY AUDIT_ID").ToList();
            Assert.Equal(before + 1, entries.Count);
            Assert.Equal(1, c.ExecuteScalar<int>("SELECT ISJSON(@j)", new { j = entries[^1] }));
            Assert.Contains("Checked again", entries[^1]);
            Assert.Equal(0, c.ExecuteScalar<int>("SELECT COUNT(*) FROM TRG_ERROR_LOG"));
        }
    }
}
