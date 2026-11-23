using Admin.Auth;
using Application.Services;
using Dapper;

namespace QPS.Tests.Database
{
    /// <summary>Database/qps.sql creates every object, and the seed data is consistent with itself and with the code.</summary>
    [Collection(QpsDatabaseCollection.Name)]
    public class SchemaAndSeedTests(QpsDatabaseFixture db)
    {
        private int Count(string sql)
        {
            using var c = db.Open();
            return c.ExecuteScalar<int>(sql);
        }

        [SqlServerTheory]
        [InlineData("tables (without the APPS_BSR test stand-in)", "SELECT COUNT(*) FROM sys.tables WHERE name <> 'APPS_BSR'", 82)]
        [InlineData("stored procedures", "SELECT COUNT(*) FROM sys.procedures", 73)]
        [InlineData("views", "SELECT COUNT(*) FROM sys.views", 6)]
        [InlineData("functions", "SELECT COUNT(*) FROM sys.objects WHERE type IN ('FN', 'IF', 'TF')", 3)]
        [InlineData("table types", "SELECT COUNT(*) FROM sys.table_types", 16)]
        [InlineData("triggers", "SELECT COUNT(*) FROM sys.triggers", 3)]
        [InlineData("foreign keys", "SELECT COUNT(*) FROM sys.foreign_keys", 44)]
        public void The_schema_script_creates_every_object(string what, string sql, int expected) =>
            Assert.True(Count(sql) == expected, $"{what}: expected {expected}, found {Count(sql)}");

        [SqlServerFact]
        public void All_seeded_foreign_keys_are_trusted()
        {
            // a foreign key added WITH NOCHECK or disabled would hide bad seed rows
            Assert.Equal(0, Count("SELECT COUNT(*) FROM sys.foreign_keys WHERE is_disabled = 1 OR is_not_trusted = 1"));
        }

        [SqlServerFact]
        public void Every_role_id_the_code_knows_exists_in_ROLE_MASTER()
        {
            using var c = db.Open();
            var ids = c.Query<int>("SELECT ID FROM ROLE_MASTER").Select(i => i.ToString()).ToHashSet();
            var missing = QpsRoles.Names.Keys.Where(k => k != QpsRoles.Vendor && !ids.Contains(k)).ToList();
            Assert.Empty(missing);
        }

        [SqlServerFact]
        public void Every_employee_has_a_role_that_exists() =>
            Assert.Equal(0, Count("SELECT COUNT(*) FROM USER_MASTER u WHERE NOT EXISTS (SELECT 1 FROM ROLE_MASTER r WHERE r.ID = u.ROLE_ID)"));

        [SqlServerFact]
        public void Every_vendor_has_exactly_one_default_address() =>
            Assert.Equal(0, Count(@"SELECT COUNT(*) FROM VENDORS_MASTER v
                                    WHERE (SELECT COUNT(*) FROM VENDOR_ADDRESS a WHERE a.VENDOR_ID = v.VENDOR_ID AND a.IS_DEFAULT = 1) <> 1"));

        [SqlServerFact]
        public void Every_vendor_password_hash_matches_the_test_password()
        {
            using var c = db.Open();
            var hasher = new PasswordHasher();
            // V0003's security row is changed by AuthStoreTests, so only the rows no test touches are checked
            var hashes = c.Query<string>(@"SELECT PASSWORD FROM VENDORS_MASTER
                                           UNION ALL SELECT PasswordHash FROM auth.UserSecurity WHERE AccountType = 'Vendor' AND UserId IN ('V0001', 'V0002')").ToList();
            Assert.Equal(5, hashes.Count);
            Assert.All(hashes, h => Assert.True(hasher.VerifyPassword(QpsDatabaseFixture.VendorPassword, h)));
        }

        [SqlServerFact]
        public void The_seeded_inspection_was_recorded_by_the_audit_triggers()
        {
            Assert.Equal(1, Count("SELECT COUNT(*) FROM INSPECTION_AUDIT_TRAIL WHERE INSPECTION_ID = 1 AND CHANGE_HISTORY LIKE '%\"INSERT\"%'"));
            Assert.Equal(0, Count("SELECT COUNT(*) FROM TRG_ERROR_LOG"));
        }
    }
}
