# Test database

Scripts to build a QPS database with test data, for the SQL Server tests and for local development.
**Never run them on the production server.**

| File | What it does |
|---|---|
| `00-test-prerequisites.sql` | Stand-ins for the two things `qps.sql` needs from outside the QPS database: an empty `APPS_BSR` table, and the linked server `[192.168.0.43]` with an `OMS.dbo.TBL_ARTICLE_MST` table (pointing back at the same server). Needs sysadmin. |
| `../qps.sql` | The schema (tables, procedures, views, triggers). |
| `01-seed-test-data.sql` | Invented test data: lookup masters, 14 roles, menus, 5 employees, 3 vendors with addresses, a PO hierarchy, 2 POs, 2 inspection requests, 1 completed inspection with defects, e-mail list, API master rows. The IDs are listed at the top of the file. Seeded vendors sign in with the password `Qps@Test2026`. |

## Build a local database by hand

```bash
sqlcmd -S localhost -U sa -Q "CREATE DATABASE VMM_QPS"
sqlcmd -S localhost -U sa -d VMM_QPS -b -i Database/test/00-test-prerequisites.sql
sqlcmd -S localhost -U sa -d VMM_QPS -b -i Database/qps.sql
sqlcmd -S localhost -U sa -d VMM_QPS -b -i Database/test/01-seed-test-data.sql
```

A throw-away SQL Server in Docker:

```bash
docker run -d --name qps-sql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='<a strong password>' -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
```

## Run the database tests

```bash
QPS_SQLSERVER_TEST="Server=localhost,1433;User Id=sa;Password=<password>" dotnet test
```

Each run creates its own database `QpsTest_<random>`, loads the three scripts, runs the tests in
`tests/QPS.Tests/Database` against the app's real services (`AddInfrastructure`, `DapperHelper`), and drops the
database. Without `QPS_SQLSERVER_TEST` those tests are reported as skipped and the other tests still run.

Two tests are skipped on purpose because they show known bugs (the skip message says what to fix):

- `AuthStoreTests.The_first_failed_sign_in_of_a_user_without_a_security_row_is_counted`:
  `auth.PRO_LOGIN_RECORD_FAILED_COUNT` / `PRO_LOGIN_LOCKOUT` insert `auth.UserSecurity` without `SecurityStamp`.
- `VendorMastersTests.The_service_passes_on_a_duplicate_email_error`:
  `VendorService` never reads `@ERROR_CODE` (Dapper's `ParameterNames` has no `@`).
