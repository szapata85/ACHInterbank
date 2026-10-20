using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
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
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSchedulerSynchronizationError",
                table: "TaskDefinition",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchedulerSynchronizationStatus",
                table: "TaskDefinition",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Synchronized");

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "AuditLog",
                type: "character varying(128)",
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
