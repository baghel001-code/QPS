using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Dapper;
using Infrastructure.ServicesConfiguration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyNewEncDec;
using QPS.Tests.Platform;
using Serilog.Core;
using Serilog.Events;

namespace QPS.Tests.Database
{
    /// <summary>
    /// The SQL Server tests run only when the environment variable QPS_SQLSERVER_TEST holds a connection string to a
    /// SQL Server where the login may create databases and linked servers (sysadmin on a local / CI server), e.g.
    /// <c>Server=localhost,1433;User Id=sa;Password=…</c>. Each run creates a fresh database QpsTest_&lt;random&gt;,
    /// loads Database/test/00-test-prerequisites.sql, Database/qps.sql and Database/test/01-seed-test-data.sql,
    /// and drops the database afterwards. Without the variable the tests are reported as skipped.
    /// </summary>
    public static class SqlServerTestEnv
    {
        public const string Variable = "QPS_SQLSERVER_TEST";
        public static string? ConnectionString => Environment.GetEnvironmentVariable(Variable) is { Length: > 0 } s ? s : null;
        public static string SkipReason => $"set {Variable} to run the SQL Server tests";
    }

    /// <summary>[Fact] that is skipped when QPS_SQLSERVER_TEST is not set.</summary>
    public sealed class SqlServerFactAttribute : FactAttribute
    {
        public SqlServerFactAttribute()
        {
            if (SqlServerTestEnv.ConnectionString == null) Skip = SqlServerTestEnv.SkipReason;
        }
    }

    /// <summary>[Theory] that is skipped when QPS_SQLSERVER_TEST is not set.</summary>
    public sealed class SqlServerTheoryAttribute : TheoryAttribute
    {
        public SqlServerTheoryAttribute()
        {
            if (SqlServerTestEnv.ConnectionString == null) Skip = SqlServerTestEnv.SkipReason;
        }
    }

    [CollectionDefinition(Name)]
    public sealed class QpsDatabaseCollection : ICollectionFixture<QpsDatabaseFixture>
    {
        public const string Name = "QPS SQL Server database";
    }

    /// <summary>One seeded test database for all SQL Server tests, plus the app's real Infrastructure services wired to it.</summary>
    public sealed class QpsDatabaseFixture : IAsyncLifetime
    {
        /// <summary>The test password of every seeded vendor (Database/test/01-seed-test-data.sql).</summary>
        public const string VendorPassword = "Qps@Test2026";

        public static readonly DateTimeOffset Start = new(2026, 11, 23, 9, 0, 0, TimeSpan.Zero);

        public string ConnectionString { get; private set; } = "";
        public FakeClock Clock { get; } = new() { Now = Start };
        public ServiceProvider Services { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            if (SqlServerTestEnv.ConnectionString is not { } cs) return;

            var builder = new SqlConnectionStringBuilder(cs) { InitialCatalog = "QpsTest_" + Guid.NewGuid().ToString("N")[..10] };
            ConnectionString = builder.ConnectionString;

            await using (var master = new SqlConnection(new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = "master" }.ConnectionString))
                await master.ExecuteAsync($"CREATE DATABASE [{builder.InitialCatalog}]");

            var root = RepoRoot();
            foreach (var script in new[] { "Database/test/00-test-prerequisites.sql", "Database/qps.sql", "Database/test/01-seed-test-data.sql" })
                await RunScriptAsync(Path.Combine(root, script));

            Services = BuildServices();
        }

        public async Task DisposeAsync()
        {
            if (ConnectionString.Length == 0) return;
            await (Services?.DisposeAsync() ?? ValueTask.CompletedTask);
            SqlConnection.ClearAllPools();
            var db = new SqlConnectionStringBuilder(ConnectionString).InitialCatalog;
            await using var master = new SqlConnection(new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = "master" }.ConnectionString);
            await master.ExecuteAsync($"ALTER DATABASE [{db}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{db}];");
        }

        public SqlConnection Open()
        {
            var c = new SqlConnection(ConnectionString);
            c.Open();
            return c;
        }

        public T Service<T>() where T : notnull => Services.CreateScope().ServiceProvider.GetRequiredService<T>();

        /// <summary>Runs a sqlcmd-style script: batches separated by GO lines; stops at the first error.</summary>
        private async Task RunScriptAsync(string path)
        {
            var text = await File.ReadAllTextAsync(path);
            var batches = Regex.Split(text, @"^\s*GO\s*;?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            await using var con = Open();
            var line = 1;
            foreach (var batch in batches)
            {
                if (!string.IsNullOrWhiteSpace(batch))
                {
                    try
                    {
                        await using var cmd = new SqlCommand(batch, con) { CommandTimeout = 300 };
                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (SqlException ex)
                    {
                        throw new InvalidOperationException($"{Path.GetFileName(path)}: batch starting near line {line} failed: {ex.Message}", ex);
                    }
                }
                line += batch.Count(c => c == '\n') + 1;
            }
        }

        /// <summary>The same registration QPSApi uses (AddInfrastructure), pointed at the test database.</summary>
        private ServiceProvider BuildServices()
        {
            var codec = new New_Enc_Dec();
            var cs = new SqlConnectionStringBuilder(ConnectionString);
            if (!string.IsNullOrEmpty(cs.Password)) cs.Password = codec.My_Encode(cs.Password);   // DapperHelper decodes it

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = cs.ConnectionString })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton(codec);
            services.AddSingleton<TimeProvider>(Clock);
            var logs = Path.Combine(Path.GetTempPath(), "qps-test-logs") + Path.DirectorySeparatorChar;
            services.AddInfrastructure(0, logs, new LoggingLevelSwitch(LogEventLevel.Warning));
            return services.BuildServiceProvider();
        }

        private static string RepoRoot()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, "QPS.sln"))) return dir.FullName;
            throw new DirectoryNotFoundException("QPS.sln not found above " + AppContext.BaseDirectory);
        }
    }
}
