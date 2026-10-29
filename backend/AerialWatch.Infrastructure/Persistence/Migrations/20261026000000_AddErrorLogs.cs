using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AerialWatch.Infrastructure.Persistence.Migrations;

/// <summary>
/// core.ErrorLogs (D-036). Written only by the Serilog SQL Server sink (AerialWatch.Api/Logging/SerilogSetup.cs),
/// so it is not an EF entity and the model snapshot does not change. Columns must match SerilogSetup.Columns().
/// </summary>
[DbContext(typeof(AerialWatchDbContext))]
[Migration("20261026000000_AddErrorLogs")]
public partial class AddErrorLogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(Schemas.Core);
        migrationBuilder.Sql("""
            CREATE TABLE [core].[ErrorLogs] (
                [Id]              bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ErrorLogs] PRIMARY KEY,
                [TimestampUtc]    datetimeoffset(7) NOT NULL,
                [Level]           nvarchar(16)   NOT NULL,
                [Message]         nvarchar(max)  NULL,
                [MessageTemplate] nvarchar(max)  NULL,
                [Exception]       nvarchar(max)  NULL,
                [LogEvent]        nvarchar(max)  NULL,
                [Origin]          nvarchar(20)   NULL,
                [Module]          nvarchar(20)   NULL,
                [Feature]         nvarchar(60)   NULL,
                [Action]          nvarchar(60)   NULL,
                [SourceContext]   nvarchar(200)  NULL,
                [ExceptionType]   nvarchar(200)  NULL,
                [HttpMethod]      nvarchar(10)   NULL,
                [RequestPath]     nvarchar(400)  NULL,
                [QueryString]     nvarchar(1000) NULL,
                [StatusCode]      int            NULL,
                [TraceId]         nvarchar(64)   NULL,
                [UserName]        nvarchar(100)  NULL,
                [ClientIp]        nvarchar(45)   NULL,
                [UserAgent]       nvarchar(400)  NULL,
                [MachineName]     nvarchar(60)   NULL,
                [EnvironmentName] nvarchar(30)   NULL,
                [AppVersion]      nvarchar(40)   NULL
            );
            CREATE INDEX [IX_ErrorLogs_TimestampUtc] ON [core].[ErrorLogs] ([TimestampUtc] DESC);
            CREATE INDEX [IX_ErrorLogs_TraceId] ON [core].[ErrorLogs] ([TraceId]);
            CREATE INDEX [IX_ErrorLogs_Module_Level] ON [core].[ErrorLogs] ([Module], [Level]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE [core].[ErrorLogs];");
        migrationBuilder.Sql("DROP SCHEMA [core];");
    }
}
