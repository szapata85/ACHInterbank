using System;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer;

[DbContext(typeof(AchDbContext))]
[Migration("20260723000000_Job41ReprocessDispatcher")]
public partial class Job41ReprocessDispatcher : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "ClaimedBy", table: "AchResponseReprocessAttempts", type: "nvarchar(150)", maxLength: 150, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "ClaimedAtUtc", table: "AchResponseReprocessAttempts", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "LeaseExpiresAtUtc", table: "AchResponseReprocessAttempts", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "StartedAtUtc", table: "AchResponseReprocessAttempts", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "LastHeartbeatAtUtc", table: "AchResponseReprocessAttempts", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ResultCode", table: "AchResponseReprocessAttempts", type: "nvarchar(50)", maxLength: 50, nullable: true);
        migrationBuilder.AddColumn<string>(name: "ErrorType", table: "AchResponseReprocessAttempts", type: "nvarchar(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>(name: "ErrorDetailSanitized", table: "AchResponseReprocessAttempts", type: "nvarchar(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "Version", table: "AchResponseReprocessAttempts", type: "uniqueidentifier", nullable: false, defaultValue: Guid.Empty);
        migrationBuilder.Sql("UPDATE AchResponseReprocessAttempts SET Version = NEWID();");
        migrationBuilder.CreateIndex(name: "IX_AchResponseReprocessAttempts_Status_LeaseExpiresAtUtc_RequestedAtUtc_Id", table: "AchResponseReprocessAttempts", columns: new[] { "Status", "LeaseExpiresAtUtc", "RequestedAtUtc", "Id" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_AchResponseReprocessAttempts_Status_LeaseExpiresAtUtc_RequestedAtUtc_Id", table: "AchResponseReprocessAttempts");
        migrationBuilder.DropColumn(name: "ClaimedBy", table: "AchResponseReprocessAttempts");
        migrationBuilder.DropColumn(name: "ClaimedAtUtc", table: "AchResponseReprocessAttempts");
        migrationBuilder.DropColumn(name: "LeaseExpiresAtUtc", table: "AchResponseReprocessAttempts");
        migrationBuilder.DropColumn(name: "StartedAtUtc", table: "AchResponseReprocessAttempts");
        migrationBuilder.DropColumn(name: "LastHeartbeatAtUtc", table: "AchResponseReprocessAttempts");
        migrationBuilder.DropColumn(name: "ResultCode", table: "AchResponseReprocessAttempts");
        migrationBuilder.DropColumn(name: "ErrorType", table: "AchResponseReprocessAttempts");
        migrationBuilder.DropColumn(name: "ErrorDetailSanitized", table: "AchResponseReprocessAttempts");
        migrationBuilder.DropColumn(name: "Version", table: "AchResponseReprocessAttempts");
    }
}
