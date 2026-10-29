using System.Collections.ObjectModel;
using System.Data;
using System.Reflection;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

namespace AerialWatch.Api.Logging;

/// <summary>
/// Serilog setup (D-036). Warnings and errors go to SQL table core.ErrorLogs (created by migration AddErrorLogs,
/// never by the app), plus a daily file under Storage:RootPath\logs as a fallback when SQL is down, plus the console.
/// </summary>
public static class SerilogSetup
{
    private static readonly string AppVersion =
        typeof(SerilogSetup).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev";

    public static void Configure(LoggerConfiguration logger, IConfiguration config, IHostEnvironment env)
    {
        var minimum = config.GetValue("ErrorLog:MinimumLevel", LogEventLevel.Warning);

        logger
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            // ApplicationExceptionHandler logs every exception itself; stop the framework logging each one a second time.
            .MinimumLevel.Override("Microsoft.AspNetCore.Diagnostics", LogEventLevel.Fatal)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Origin", "Api")
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .Enrich.WithProperty("EnvironmentName", env.EnvironmentName)
            .Enrich.WithProperty("AppVersion", AppVersion)
            .WriteTo.Console();

        var root = config["Storage:RootPath"];
        if (!string.IsNullOrWhiteSpace(root))
            logger.WriteTo.File(Path.Combine(root, "logs", "api-.log"), restrictedToMinimumLevel: minimum,
                rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30);

        var connectionString = config.GetConnectionString("AerialWatch");
        if (config.GetValue("ErrorLog:Enabled", true) && !string.IsNullOrWhiteSpace(connectionString))
            logger.WriteTo.MSSqlServer(
                connectionString: connectionString,
                sinkOptions: new MSSqlServerSinkOptions
                {
                    SchemaName = "core",
                    TableName = "ErrorLogs",
                    AutoCreateSqlTable = false,          // D-023: the migration creates the table
                    BatchPostingLimit = 50,
                    BatchPeriod = TimeSpan.FromSeconds(2) // written in the background, never slows a request
                },
                restrictedToMinimumLevel: minimum,
                columnOptions: Columns());
    }

    /// <summary>Must match the table in migration 20261026000000_AddErrorLogs.</summary>
    private static ColumnOptions Columns()
    {
        var columns = new ColumnOptions();
        columns.Store.Remove(StandardColumn.Properties);   // XML; replaced by the JSON LogEvent column
        columns.Store.Add(StandardColumn.LogEvent);
        columns.LogEvent.ExcludeAdditionalProperties = true;
        columns.LogEvent.ExcludeStandardColumns = true;
        columns.Id.DataType = SqlDbType.BigInt;
        columns.TimeStamp.ColumnName = "TimestampUtc";
        columns.TimeStamp.DataType = SqlDbType.DateTimeOffset;
        columns.TimeStamp.ConvertToUtc = true;
        columns.Level.DataLength = 16;
        columns.AdditionalColumns = new Collection<SqlColumn>
        {
            new("Origin", SqlDbType.NVarChar, dataLength: 20),
            new("Module", SqlDbType.NVarChar, dataLength: 20),
            new("Feature", SqlDbType.NVarChar, dataLength: 60),
            new("Action", SqlDbType.NVarChar, dataLength: 60),
            new("SourceContext", SqlDbType.NVarChar, dataLength: 200),
            new("ExceptionType", SqlDbType.NVarChar, dataLength: 200),
            new("HttpMethod", SqlDbType.NVarChar, dataLength: 10),
            new("RequestPath", SqlDbType.NVarChar, dataLength: 400),
            new("QueryString", SqlDbType.NVarChar, dataLength: 1000),
            new("StatusCode", SqlDbType.Int),
            new("TraceId", SqlDbType.NVarChar, dataLength: 64),
            new("UserName", SqlDbType.NVarChar, dataLength: 100),
            new("ClientIp", SqlDbType.NVarChar, dataLength: 45),
            new("UserAgent", SqlDbType.NVarChar, dataLength: 400),
            new("MachineName", SqlDbType.NVarChar, dataLength: 60),
            new("EnvironmentName", SqlDbType.NVarChar, dataLength: 30),
            new("AppVersion", SqlDbType.NVarChar, dataLength: 40),
        };
        return columns;
    }
}
