using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class SchedulerAdministrativeAuditAndReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSchedulerSynchronizationAttemptUtc",
                table: "TaskDefinition",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSchedulerSynchronizationError",
                table: "TaskDefinition",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchedulerSynchronizationStatus",
                table: "TaskDefinition",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Synchronized");

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "AuditLog",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSchedulerSynchronizationAttemptUtc",
                table: "TaskDefinition");

            migrationBuilder.DropColumn(
                name: "LastSchedulerSynchronizationError",
                table: "TaskDefinition");

            migrationBuilder.DropColumn(
                name: "SchedulerSynchronizationStatus",
                table: "TaskDefinition");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "AuditLog");
        }
    }
}
