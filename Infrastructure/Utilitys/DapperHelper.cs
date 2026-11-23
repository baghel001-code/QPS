using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyNewEncDec;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using System.Text;


namespace Infrastructure.Utilitys
{
    public class DapperHelper
    {
        // Queries slower than this are logged as warnings (useful for spotting parameter sniffing / missing indexes)
        private const int SlowQueryThresholdMs = 3000;
        private const int MaxLoggedValueLength = 200;
        private static readonly string[] SensitiveParamKeywords = { "password", "pwd", "secret", "token", "otp" };

        private readonly string _connectionString;
        private readonly string _dataSource;
        private readonly string _database;
        private readonly New_Enc_Dec _Enc_Dec;
        private readonly ILogger<DapperHelper> _logger;

        // Constructor to initialize the connection string
        public DapperHelper(IConfiguration configuration, New_Enc_Dec Enc_Dec, ILogger<DapperHelper> logger)
        {
            // Logger first, so every failure below can be logged
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _Enc_Dec = Enc_Dec ?? throw new ArgumentNullException(nameof(Enc_Dec));

            if (configuration == null)
            {
                _logger.LogCritical("IConfiguration was not provided to DapperHelper.");
                throw new ArgumentNullException(nameof(configuration));
            }

            var conStr = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(conStr))
            {
                _logger.LogCritical("Connection string 'DefaultConnection' is missing or empty in configuration.");
                throw new InvalidOperationException("Connection string 'DefaultConnection' is missing or empty.");
            }

            try
            {
                var builder = new SqlConnectionStringBuilder(conStr);
                _dataSource = builder.DataSource;
                _database = builder.InitialCatalog;

                if (!string.IsNullOrEmpty(builder.Password))
                {
                    builder.Password = _Enc_Dec.My_Decode(builder.Password);
                }

                _connectionString = builder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,
                    "Failed to parse/decrypt connection string 'DefaultConnection' (Server={Server}, Database={Database}).",
                    _dataSource ?? "?", _database ?? "?");
                throw;
            }
        }

        // Open a database connection
        private IDbConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        #region Core execution + error handling

        /// <summary>
        /// Every public method goes through here: opens the connection, runs the action,
        /// times it, and logs any exception with full context before re-throwing it
        /// (stack trace preserved via "throw;").
        /// </summary>
        private async Task<TResult> ExecuteCoreAsync<TResult>(string operation, string sql, object logParam, Func<IDbConnection, Task<TResult>> action)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                if (string.IsNullOrWhiteSpace(sql))
                {
                    throw new ArgumentException("SQL text / stored procedure name cannot be null or empty.", nameof(sql));
                }

                using (var connection = GetConnection())
                {
                    await OpenConnectionAsync(connection);
                    var result = await action(connection);

                    sw.Stop();
                    LogDuration(operation, sql, logParam, sw.ElapsedMilliseconds);
                    return result;
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                LogDbException(ex, operation, sql, logParam, sw.ElapsedMilliseconds);
                throw;
            }
        }

        private void LogDuration(string operation, string sql, object param, long elapsedMs)
        {
            if (elapsedMs >= SlowQueryThresholdMs)
            {
                _logger.LogWarning(
                    "[{Operation}] SLOW QUERY: {Sql} took {ElapsedMs} ms | Params: {Params}",
                    operation, sql, elapsedMs, DescribeParams(param));
            }
            else if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("[{Operation}] {Sql} completed in {ElapsedMs} ms", operation, sql, elapsedMs);
            }
        }

        private void LogDbException(Exception ex, string operation, string sql, object param, long elapsedMs)
        {
            var paramText = DescribeParams(param);

            if (ex is SqlException sqlEx)
            {
                LogSqlException(sqlEx, operation, sql, paramText, elapsedMs);
                return;
            }

            string category = ex switch
            {
                OperationCanceledException => "Operation was cancelled",
                TimeoutException => "Timeout",
                InvalidCastException => "Type mapping failed - column type does not match the model property type",
                DataException => "Dapper column parsing/mapping failed",
                ObjectDisposedException => "Connection or reader already disposed",
                InvalidOperationException => "Invalid operation (connection state, more than one row for QuerySingle, missing result set, or output parameter not found)",
                KeyNotFoundException => "Parameter not found (check output parameter name)",
                ArgumentException => "Invalid argument or parameter mapping",
                NullReferenceException => "Null reference",
                _ => "Unexpected error"
            };

            var level = ex is OperationCanceledException ? LogLevel.Warning : LogLevel.Error;

            _logger.Log(level, ex,
                "[{Operation}] {Category} | ExceptionType: {ExceptionType} | SQL: {Sql} | Server: {Server} | Database: {Database} | Elapsed: {ElapsedMs} ms | Params: {Params}",
                operation, category, ex.GetType().FullName, sql, _dataSource, _database, elapsedMs, paramText);
        }

        private void LogSqlException(SqlException sqlEx, string operation, string sql, string paramText, long elapsedMs)
        {
            var allErrors = new StringBuilder();
            foreach (SqlError e in sqlEx.Errors)
            {
                allErrors.AppendLine(
                    $"  Msg {e.Number}, Level {e.Class}, State {e.State}, Procedure '{e.Procedure}', Line {e.LineNumber}: {e.Message}");
            }

            _logger.LogError(sqlEx,
                "[{Operation}] SQL Error {SqlErrorNumber} ({Category}) | SQL: {Sql} | Procedure: {Procedure} | Line: {LineNumber} | " +
                "Severity: {Severity} | State: {State} | Server: {Server} | Database: {Database} | ClientConnectionId: {ClientConnectionId} | " +
                "Elapsed: {ElapsedMs} ms | Params: {Params}{NewLine}{SqlErrors}",
                operation, sqlEx.Number, ClassifySqlError(sqlEx.Number), sql, sqlEx.Procedure, sqlEx.LineNumber,
                sqlEx.Class, sqlEx.State, _dataSource, _database, sqlEx.ClientConnectionId,
                elapsedMs, paramText, Environment.NewLine, allErrors.ToString());
        }

        private static string ClassifySqlError(int number) => number switch
        {
            -2 => "Command timeout",
            -1 or 2 or 40 or 53 or 10053 or 10054 or 10060 or 11001 => "Network error / server unreachable",
            18456 => "Login failed",
            4060 => "Cannot open database",
            1205 => "Deadlock victim",
            1222 => "Lock request timeout",
            2627 or 2601 => "Duplicate key",
            547 => "Foreign key / check constraint violation",
            515 => "NULL inserted into NOT NULL column",
            8152 or 2628 => "String or binary data would be truncated",
            245 or 241 or 8114 or 8115 => "Data type conversion / arithmetic overflow",
            8134 => "Divide by zero",
            2812 => "Stored procedure not found",
            201 => "Missing required stored procedure parameter",
            8144 => "Too many arguments for stored procedure",
            207 => "Invalid column name",
            208 => "Invalid object name",
            229 or 230 => "Permission denied",
            7202 or 7303 or 7314 or 7399 => "Linked server error",
            >= 50000 => "Custom RAISERROR / THROW from stored procedure",
            _ => "SQL Server error"
        };

        private static string DescribeParams(object param)
        {
            if (param == null) return "(none)";

            try
            {
                var pairs = new List<string>();

                switch (param)
                {
                    case DynamicParameters dp:
                        foreach (var name in dp.ParameterNames)
                        {
                            object value;
                            try { value = dp.Get<object>(name); }
                            catch { value = "(unavailable)"; }
                            pairs.Add(FormatParam(name, value));
                        }
                        if (pairs.Count == 0) return "(DynamicParameters - template values not inspectable)";
                        break;

                    case IEnumerable<KeyValuePair<string, object>> dict:
                        foreach (var kv in dict) pairs.Add(FormatParam(kv.Key, kv.Value));
                        break;

                    default:
                        foreach (var prop in param.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        {
                            if (prop.GetIndexParameters().Length > 0) continue;
                            object value;
                            try { value = prop.GetValue(param); }
                            catch { value = "(unreadable)"; }
                            pairs.Add(FormatParam(prop.Name, value));
                        }
                        break;
                }

                return pairs.Count == 0 ? "(none)" : string.Join(", ", pairs);
            }
            catch (Exception ex)
            {
                return $"(could not describe parameters: {ex.Message})";
            }
        }

        private static string FormatParam(string name, object value)
        {
            if (SensitiveParamKeywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase)))
                return $"{name}=***";

            string text = value switch
            {
                null => "NULL",
                DBNull => "NULL",
                string s => $"'{Truncate(s)}'",
                DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                SqlMapper.ICustomQueryParameter => "(table-valued parameter)",
                DataTable dt => $"(DataTable, {dt.Rows.Count} rows)",
                ICollection c => $"(collection, {c.Count} items)",
                IEnumerable => "(sequence)",
                _ => Truncate(value.ToString())
            };

            return $"{name}={text}";
        }

        private static string Truncate(string s)
        {
            if (s == null) return "NULL";
            return s.Length <= MaxLoggedValueLength ? s : s.Substring(0, MaxLoggedValueLength) + "...(truncated)";
        }

        #endregion

        // Query a single entity (default: stored procedure)
        public async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, CommandType commandType = CommandType.StoredProcedure)
        {
            return await ExecuteCoreAsync<T>(nameof(QueryFirstOrDefaultAsync), sql, param,
                conn => conn.QueryFirstOrDefaultAsync<T>(sql, param, commandType: commandType));
        }

        // Query a single entity from the database without parameters
        public async Task<T> QuerySingleAsync<T>(string sql)
        {
            return await ExecuteCoreAsync<T>(nameof(QuerySingleAsync), sql, null,
                conn => conn.QuerySingleOrDefaultAsync<T>(sql));
        }

        // Query a single entity from the database
        public async Task<T> QuerySingleAsync<T>(string sql, object param = null)
        {
            return await ExecuteCoreAsync<T>(nameof(QuerySingleAsync), sql, param,
                conn => conn.QuerySingleOrDefaultAsync<T>(sql, param));
        }

        // Query a list of entities without parameters
        public async Task<IEnumerable<T>> QueryListAsync<T>(string sql)
        {
            return await ExecuteCoreAsync<IEnumerable<T>>(nameof(QueryListAsync), sql, null,
                conn => conn.QueryAsync<T>(sql));
        }

        // Query a list of entities from the database
        public async Task<IEnumerable<T>> QueryListAsync<T>(string sql, object param = null)
        {
            return await ExecuteCoreAsync<IEnumerable<T>>(nameof(QueryListAsync), sql, param,
                conn => conn.QueryAsync<T>(sql, param));
        }

        // Execute a command (e.g., Insert, Update, Delete)
        public async Task<int> ExecuteAsync(string sql, object param = null)
        {
            return await ExecuteCoreAsync<int>(nameof(ExecuteAsync), sql, param,
                conn => conn.ExecuteAsync(sql, param));
        }

        // Execute the stored procedure (non-query) and return the number of rows affected
        public async Task<int> ExecuteStoredProcedureAsync(string storedProc, object parameters = null)
        {
            return await ExecuteCoreAsync<int>(nameof(ExecuteStoredProcedureAsync), storedProc, parameters,
                conn => conn.ExecuteAsync(storedProc, parameters, commandType: CommandType.StoredProcedure));
        }

        // Execute a stored procedure with an Output Parameter
        public async Task<string> ExecuteStoredProcedureAsync(string storedProc, object inputParam, string outputParamName)
        {
            return await ExecuteCoreAsync<string>(nameof(ExecuteStoredProcedureAsync), storedProc, inputParam, async conn =>
            {
                var parameters = CreateParametersWithOutput(inputParam, outputParamName);
                await conn.ExecuteAsync(storedProc, parameters, commandType: CommandType.StoredProcedure);
                return parameters.Get<string>(outputParamName);
            });
        }

        public async Task<T> ExecuteScalerStoredProcedureAsync<T>(string storedProc, object param = null)
        {
            return await ExecuteCoreAsync<T>(nameof(ExecuteScalerStoredProcedureAsync), storedProc, param,
                conn => conn.ExecuteScalarAsync<T>(storedProc, param, commandType: CommandType.StoredProcedure));
        }

        public async Task<T> StoredProcedureSingleAsync<T>(string storedProc, object param = null)
        {
            return await ExecuteCoreAsync<T>(nameof(StoredProcedureSingleAsync), storedProc, param,
                conn => conn.QuerySingleOrDefaultAsync<T>(storedProc, param, commandType: CommandType.StoredProcedure));
        }

        // Execute a stored procedure Single
        public async Task<T> ExecuteStoredProcedureSingleAsync<T>(string storedProc, object param = null)
        {
            return await ExecuteCoreAsync<T>(nameof(ExecuteStoredProcedureSingleAsync), storedProc, param,
                conn => conn.QueryFirstOrDefaultAsync<T>(storedProc, param, commandType: CommandType.StoredProcedure));
        }

        // Execute a stored procedure Single and an output parameter
        public async Task<(T, string)> ExecuteStoredProcedureSingleAsync<T>(string storedProc, object inputParams, string outputParamName)
        {
            return await ExecuteCoreAsync<(T, string)>(nameof(ExecuteStoredProcedureSingleAsync), storedProc, inputParams, async conn =>
            {
                var parameters = CreateParametersWithOutput(inputParams, outputParamName);
                var resultSet = await conn.QueryFirstOrDefaultAsync<T>(storedProc, parameters, commandType: CommandType.StoredProcedure);
                var outputValue = parameters.Get<string>(outputParamName);
                return (resultSet!, outputValue);
            });
        }

        // Execute a stored procedure (logs and returns null on failure - original behaviour kept)
        public async Task<IEnumerable<T>> ExecuteStoredProcedureListAsync<T>(string storedProc, object param = null)
        {
            try
            {
                return await ExecuteCoreAsync<IEnumerable<T>>(nameof(ExecuteStoredProcedureListAsync), storedProc, param,
                    conn => conn.QueryAsync<T>(storedProc, param, commandType: CommandType.StoredProcedure));
            }
            catch (Exception)
            {
                // Already logged with full context in ExecuteCoreAsync
                return null;
            }
        }

        // Paged list with @TotalCount output (logs and returns (null, 0) on failure - original behaviour kept)
        public async Task<(IEnumerable<T> Data, int TotalCount)> ExecuteStoredProcedureListDPAsync<T>(string storedProc, DynamicParameters param = null)
        {
            try
            {
                return await ExecuteCoreAsync<(IEnumerable<T>, int)>(nameof(ExecuteStoredProcedureListDPAsync), storedProc, param, async conn =>
                {
                    if (param == null)
                        throw new ArgumentNullException(nameof(param), "DynamicParameters containing an '@TotalCount' output parameter is required.");

                    var res = await conn.QueryAsync<T>(storedProc, param, commandType: CommandType.StoredProcedure);

                    // int? avoids "Attempting to cast a DBNull to a non nullable type" when SP never sets @TotalCount
                    int totalCount = param.Get<int?>("@TotalCount") ?? 0;
                    return (res, totalCount);
                });
            }
            catch (Exception)
            {
                // Already logged with full context in ExecuteCoreAsync
                return (null, 0);
            }
        }

        // Execute stored procedure with a single result set and an output parameter
        public async Task<(IEnumerable<T>, string)> ExecuteStoredProcedureListAsync<T>(string storedProc, object inputParams, string outputParamName)
        {
            return await ExecuteCoreAsync<(IEnumerable<T>, string)>(nameof(ExecuteStoredProcedureListAsync), storedProc, inputParams, async conn =>
            {
                var parameters = CreateParametersWithOutput(inputParams, outputParamName);
                var resultSet = await conn.QueryAsync<T>(storedProc, parameters, commandType: CommandType.StoredProcedure);
                var outputValue = parameters.Get<string>(outputParamName);
                return (resultSet, outputValue);
            });
        }

        // Execute a stored procedure with 2 result sets
        public async Task<(IEnumerable<T1>, IEnumerable<T2>)> ExecuteStoredProcedureMultipleListAsync<T1, T2>(string storedProc, object param = null)
        {
            return await ExecuteCoreAsync<(IEnumerable<T1>, IEnumerable<T2>)>(nameof(ExecuteStoredProcedureMultipleListAsync), storedProc, param, async conn =>
            {
                using (var multi = await conn.QueryMultipleAsync(storedProc, param, commandType: CommandType.StoredProcedure))
                {
                    var result1 = await ReadRequiredAsync<T1>(multi, storedProc, 1);
                    var result2 = await ReadRequiredAsync<T2>(multi, storedProc, 2);
                    return (result1, result2);
                }
            });
        }

        // Execute a stored procedure with up to 3 result sets (missing/empty sets become empty collections)
        public async Task<(IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>)> ExecuteStoredProcedureMultiple3ListAsync<T1, T2, T3>(string storedProc, object param = null)
        {
            return await ExecuteCoreAsync<(IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>)>(nameof(ExecuteStoredProcedureMultiple3ListAsync), storedProc, param, async conn =>
            {
                using (var multi = await conn.QueryMultipleAsync(storedProc, param, commandType: CommandType.StoredProcedure))
                {
                    var result1 = await TryReadAsync<T1>(multi, storedProc, 1);
                    var result2 = await TryReadAsync<T2>(multi, storedProc, 2);
                    var result3 = await TryReadAsync<T3>(multi, storedProc, 3);
                    return (result1, result2, result3);
                }
            });
        }

        // Execute stored procedure with 2 result sets and an output parameter
        public async Task<(IEnumerable<T1>, IEnumerable<T2>, string)> ExecuteStoredProcedureMultipleListAsync<T1, T2>(string storedProc, object inputParams, string outputParamName)
        {
            return await ExecuteCoreAsync<(IEnumerable<T1>, IEnumerable<T2>, string)>(nameof(ExecuteStoredProcedureMultipleListAsync), storedProc, inputParams, async conn =>
            {
                var parameters = CreateParametersWithOutput(inputParams, outputParamName);
                IEnumerable<T1> result1;
                IEnumerable<T2> result2;

                using (var multi = await conn.QueryMultipleAsync(storedProc, parameters, commandType: CommandType.StoredProcedure))
                {
                    result1 = await ReadRequiredAsync<T1>(multi, storedProc, 1);
                    result2 = await ReadRequiredAsync<T2>(multi, storedProc, 2);
                }

                // Output params are only populated AFTER the GridReader is disposed
                var outputValue = parameters.Get<string>(outputParamName);
                return (result1, result2, outputValue);
            });
        }

        // Execute a stored procedure with N result sets
        public async Task<IEnumerable<dynamic>[]> ExecuteStoredProcedureWithNListAsync(string storedProc, object param = null)
        {
            return await ExecuteCoreAsync<IEnumerable<dynamic>[]>(nameof(ExecuteStoredProcedureWithNListAsync), storedProc, param, async conn =>
            {
                var resultSets = new List<IEnumerable<dynamic>>();
                using (var multi = await conn.QueryMultipleAsync(storedProc, param, commandType: CommandType.StoredProcedure))
                {
                    while (!multi.IsConsumed)
                    {
                        resultSets.Add(await multi.ReadAsync<dynamic>());
                    }
                }
                return resultSets.ToArray();
            });
        }

        // Execute stored procedure with N result sets and an output parameter
        public async Task<(IEnumerable<dynamic>[] resultSets, string outputValue)> ExecuteStoredProcedureWithNListAsync(string storedProc, object inputParams, string outputParamName)
        {
            return await ExecuteCoreAsync<(IEnumerable<dynamic>[], string)>(nameof(ExecuteStoredProcedureWithNListAsync), storedProc, inputParams, async conn =>
            {
                var parameters = CreateParametersWithOutput(inputParams, outputParamName);
                var resultSets = new List<IEnumerable<dynamic>>();

                using (var multi = await conn.QueryMultipleAsync(storedProc, parameters, commandType: CommandType.StoredProcedure))
                {
                    while (!multi.IsConsumed)
                    {
                        resultSets.Add(await multi.ReadAsync<dynamic>());
                    }
                }

                // Output params are only populated AFTER the GridReader is disposed
                var outputValue = parameters.Get<string>(outputParamName);
                return (resultSets.ToArray(), outputValue);
            });
        }

        #region Helpers

        private static DynamicParameters CreateParametersWithOutput(object inputParams, string outputParamName)
        {
            if (string.IsNullOrWhiteSpace(outputParamName))
                throw new ArgumentException("Output parameter name cannot be null or empty.", nameof(outputParamName));

            var parameters = new DynamicParameters(inputParams);
            parameters.Add(outputParamName, dbType: DbType.String, direction: ParameterDirection.Output, size: 100);
            return parameters;
        }

        // Helper method to open connection asynchronously
        private static async Task OpenConnectionAsync(IDbConnection connection)
        {
            if (connection is SqlConnection sqlConnection)
            {
                await sqlConnection.OpenAsync();
            }
            else
            {
                connection.Open();
            }
        }

        // Reads a result set that MUST exist; gives a clear message if the SP returned fewer sets
        private static async Task<IEnumerable<T>> ReadRequiredAsync<T>(SqlMapper.GridReader multi, string storedProc, int resultSetIndex)
        {
            if (multi.IsConsumed)
            {
                throw new InvalidOperationException(
                    $"Stored procedure '{storedProc}' returned fewer result sets than expected: result set #{resultSetIndex} ({typeof(T).Name}) is missing.");
            }
            return await multi.ReadAsync<T>();
        }

        // Reads an optional result set; returns empty on failure but LOGS why (previously swallowed silently)
        private async Task<IEnumerable<T>> TryReadAsync<T>(SqlMapper.GridReader multi, string storedProc, int resultSetIndex)
        {
            if (multi.IsConsumed)
            {
                _logger.LogWarning(
                    "SP {Sp}: result set #{Index} ({Type}) not returned - using empty collection.",
                    storedProc, resultSetIndex, typeof(T).Name);
                return Enumerable.Empty<T>();
            }

            try
            {
                var result = await multi.ReadAsync<T>();
                return result ?? Enumerable.Empty<T>();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("No columns were selected", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "SP {Sp}: result set #{Index} ({Type}) had no columns - using empty collection.",
                    storedProc, resultSetIndex, typeof(T).Name);
                return Enumerable.Empty<T>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "SP {Sp}: failed to read/map result set #{Index} into {Type} - using empty collection. ExceptionType: {ExceptionType}",
                    storedProc, resultSetIndex, typeof(T).FullName, ex.GetType().FullName);
                return Enumerable.Empty<T>();
            }
        }

        #endregion
    }
}
