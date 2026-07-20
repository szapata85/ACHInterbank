using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class SchedulerQuartzProduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ActualFireTimeUtc",
                table: "TaskExecutionLog",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "TaskExecutionLog",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                table: "TaskExecutionLog",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<long>(
                name: "DurationMilliseconds",
                table: "TaskExecutionLog",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorCode",
                table: "TaskExecutionLog",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExecutionId",
                table: "TaskExecutionLog",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FireInstanceId",
                table: "TaskExecutionLog",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "TaskExecutionLog",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsRecovery",
                table: "TaskExecutionLog",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "JobGroup",
                table: "TaskExecutionLog",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "JobName",
                table: "TaskExecutionLog",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ManualConcurrencyKey",
                table: "TaskExecutionLog",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MisfireDetected",
                table: "TaskExecutionLog",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFireInstanceId",
                table: "TaskExecutionLog",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecoveredByInstanceId",
                table: "TaskExecutionLog",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecoveryResult",
                table: "TaskExecutionLog",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RecoveryStartedAtUtc",
                table: "TaskExecutionLog",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RefireCount",
                table: "TaskExecutionLog",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RequestId",
                table: "TaskExecutionLog",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestReason",
                table: "TaskExecutionLog",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedByUserId",
                table: "TaskExecutionLog",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedByUserName",
                table: "TaskExecutionLog",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchedulerInstanceId",
                table: "TaskExecutionLog",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SchedulerInstanceName",
                table: "TaskExecutionLog",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "TaskExecutionLog",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TaskCode",
                table: "TaskExecutionLog",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TriggerName",
                table: "TaskExecutionLog",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TriggerType",
                table: "TaskExecutionLog",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "TaskDefinition",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ManualExecutionEnabled",
                table: "TaskDefinition",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MisfirePolicy",
                table: "TaskDefinition",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Paused",
                table: "TaskDefinition",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequestsRecovery",
                table: "TaskDefinition",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SchedulerInstanceStates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SchedulerName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    InstanceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InstanceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HostName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastHeartbeatUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StoppedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CurrentlyExecutingJobs = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedulerInstanceStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchedulerProbeExecutions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProbeKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchedulerInstanceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EffectAppliedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedulerProbeExecutions", x => x.Id);
                });

            migrationBuilder.Sql("""
                UPDATE "TaskExecutionLog" AS log
                SET "ExecutionId" = CASE
                        WHEN log."ExecutionKey" ~ '^[0-9a-fA-F]{32}$'
                        THEN (substring(log."ExecutionKey", 1, 8) || '-' ||
                              substring(log."ExecutionKey", 9, 4) || '-' ||
                              substring(log."ExecutionKey", 13, 4) || '-' ||
                              substring(log."ExecutionKey", 17, 4) || '-' ||
                              substring(log."ExecutionKey", 21, 12))::uuid
                        ELSE NULL
                    END,
                    "TaskCode" = COALESCE(
                        CASE task."Code"
                            WHEN 'AchCycleSeeder' THEN 'ACH_CYCLE_SEED'
                            WHEN 'AchCycleScheduler' THEN 'ACH_CYCLE_SCHEDULER'
                            WHEN 'SeedBankHolidays' THEN 'BANK_HOLIDAY_SEED'
                            WHEN 'AchTacitAcceptanceJob' THEN 'TACIT_ACCEPTANCE'
                            WHEN 'AchContrapartidasByCycle' THEN 'CONTRAPARTIDA_DISPATCH'
                            WHEN 'IncomingNachaPostProcessing' THEN 'INCOMING_NACHA_PROCESSING'
                            ELSE task."Code"
                        END, ''),
                    "JobName" = 'job:' || log."TaskDefinitionId",
                    "JobGroup" = 'db-tasks',
                    "TriggerName" = 'legacy:' || log."ExecutionKey",
                    "TriggerType" = 'Programada',
                    "IdempotencyKey" = log."ExecutionKey",
                    "CorrelationId" = 'legacy:' || log."ExecutionKey",
                    "CreatedAtUtc" = log."StartedAt",
                    "Status" = CASE WHEN log."FinishedAt" IS NULL THEN 1 WHEN log."Success" THEN 2 ELSE 3 END
                FROM "TaskDefinition" AS task
                WHERE task."Id" = log."TaskDefinitionId";

                UPDATE "TaskDefinition"
                SET "ManualExecutionEnabled" = true
                WHERE "Code" IN ('AchCycleScheduler', 'SeedBankHolidays');
                """);

            migrationBuilder.DeleteData(
                table: "MenuItemPermissions",
                keyColumns: new[] { "MenuItemId", "PermissionId" },
                keyValues: new object[] { 18, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") });

            migrationBuilder.DeleteData(
                table: "MenuItemPermissions",
                keyColumns: new[] { "MenuItemId", "PermissionId" },
                keyValues: new object[] { 19, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("d1445236-b093-4d6f-8b09-821599d4dd01"), "Consultar tareas programadas", "Scheduler.View" },
                    { new Guid("d1445236-b093-4d6f-8b09-821599d4dd02"), "Consultar historial del programador", "Scheduler.History.View" },
                    { new Guid("d1445236-b093-4d6f-8b09-821599d4dd03"), "Ejecutar manualmente tareas autorizadas", "Scheduler.Execute" },
                    { new Guid("d1445236-b093-4d6f-8b09-821599d4dd04"), "Editar programaciones", "Scheduler.ManageSchedule" },
                    { new Guid("d1445236-b093-4d6f-8b09-821599d4dd05"), "Pausar y reanudar tareas", "Scheduler.PauseResume" },
                    { new Guid("d1445236-b093-4d6f-8b09-821599d4dd06"), "Consultar instancias del clúster", "Scheduler.ViewInstances" }
                });

            migrationBuilder.InsertData(
                table: "MenuItemPermissions",
                columns: new[] { "MenuItemId", "PermissionId" },
                values: new object[,]
                {
                    { 18, new Guid("d1445236-b093-4d6f-8b09-821599d4dd01") },
                    { 19, new Guid("d1445236-b093-4d6f-8b09-821599d4dd01") }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("d1445236-b093-4d6f-8b09-821599d4dd01"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { new Guid("d1445236-b093-4d6f-8b09-821599d4dd02"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { new Guid("d1445236-b093-4d6f-8b09-821599d4dd03"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { new Guid("d1445236-b093-4d6f-8b09-821599d4dd04"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { new Guid("d1445236-b093-4d6f-8b09-821599d4dd05"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { new Guid("d1445236-b093-4d6f-8b09-821599d4dd06"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { new Guid("d1445236-b093-4d6f-8b09-821599d4dd01"), new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") },
                    { new Guid("d1445236-b093-4d6f-8b09-821599d4dd02"), new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") },
                    { new Guid("d1445236-b093-4d6f-8b09-821599d4dd03"), new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") },
                    { new Guid("d1445236-b093-4d6f-8b09-821599d4dd06"), new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutionLog_ExecutionId",
                table: "TaskExecutionLog",
                column: "ExecutionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutionLog_ManualConcurrencyKey",
                table: "TaskExecutionLog",
                column: "ManualConcurrencyKey",
                unique: true,
                filter: "\"ManualConcurrencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutionLog_RequestId",
                table: "TaskExecutionLog",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutionLog_Status_StartedAt",
                table: "TaskExecutionLog",
                columns: new[] { "Status", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutionLog_TaskCode_StartedAt",
                table: "TaskExecutionLog",
                columns: new[] { "TaskCode", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SchedulerInstanceStates_LastHeartbeatUtc",
                table: "SchedulerInstanceStates",
                column: "LastHeartbeatUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SchedulerInstanceStates_SchedulerName_InstanceId",
                table: "SchedulerInstanceStates",
                columns: new[] { "SchedulerName", "InstanceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchedulerProbeExecutions_ExecutionId",
                table: "SchedulerProbeExecutions",
                column: "ExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_SchedulerProbeExecutions_ProbeKey",
                table: "SchedulerProbeExecutions",
                column: "ProbeKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MenuItemPermissions",
                keyColumns: new[] { "MenuItemId", "PermissionId" },
                keyValues: new object[] { 18, new Guid("d1445236-b093-4d6f-8b09-821599d4dd01") });

            migrationBuilder.DeleteData(
                table: "MenuItemPermissions",
                keyColumns: new[] { "MenuItemId", "PermissionId" },
                keyValues: new object[] { 19, new Guid("d1445236-b093-4d6f-8b09-821599d4dd01") });

            migrationBuilder.InsertData(
                table: "MenuItemPermissions",
                columns: new[] { "MenuItemId", "PermissionId" },
                values: new object[,]
                {
                    { 18, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") },
                    { 19, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") }
                });

            migrationBuilder.DropTable(
                name: "SchedulerInstanceStates");

            migrationBuilder.DropTable(
                name: "SchedulerProbeExecutions");

            migrationBuilder.DropIndex(
                name: "IX_TaskExecutionLog_ExecutionId",
                table: "TaskExecutionLog");

            migrationBuilder.DropIndex(
                name: "IX_TaskExecutionLog_ManualConcurrencyKey",
                table: "TaskExecutionLog");

            migrationBuilder.DropIndex(
                name: "IX_TaskExecutionLog_RequestId",
                table: "TaskExecutionLog");

            migrationBuilder.DropIndex(
                name: "IX_TaskExecutionLog_Status_StartedAt",
                table: "TaskExecutionLog");

            migrationBuilder.DropIndex(
                name: "IX_TaskExecutionLog_TaskCode_StartedAt",
                table: "TaskExecutionLog");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("d1445236-b093-4d6f-8b09-821599d4dd01"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("d1445236-b093-4d6f-8b09-821599d4dd02"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("d1445236-b093-4d6f-8b09-821599d4dd03"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("d1445236-b093-4d6f-8b09-821599d4dd04"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("d1445236-b093-4d6f-8b09-821599d4dd05"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("d1445236-b093-4d6f-8b09-821599d4dd06"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("d1445236-b093-4d6f-8b09-821599d4dd01"), new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("d1445236-b093-4d6f-8b09-821599d4dd02"), new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("d1445236-b093-4d6f-8b09-821599d4dd03"), new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("d1445236-b093-4d6f-8b09-821599d4dd06"), new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("d1445236-b093-4d6f-8b09-821599d4dd01"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("d1445236-b093-4d6f-8b09-821599d4dd02"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("d1445236-b093-4d6f-8b09-821599d4dd03"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("d1445236-b093-4d6f-8b09-821599d4dd04"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("d1445236-b093-4d6f-8b09-821599d4dd05"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("d1445236-b093-4d6f-8b09-821599d4dd06"));

            migrationBuilder.DropColumn(
                name: "ActualFireTimeUtc",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "DurationMilliseconds",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "ErrorCode",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "ExecutionId",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "FireInstanceId",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "IsRecovery",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "JobGroup",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "JobName",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "ManualConcurrencyKey",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "MisfireDetected",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "OriginalFireInstanceId",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "RecoveredByInstanceId",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "RecoveryResult",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "RecoveryStartedAtUtc",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "RefireCount",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "RequestReason",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "RequestedByUserId",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "RequestedByUserName",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "SchedulerInstanceId",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "SchedulerInstanceName",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "TaskCode",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "TriggerName",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "TriggerType",
                table: "TaskExecutionLog");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "TaskDefinition");

            migrationBuilder.DropColumn(
                name: "ManualExecutionEnabled",
                table: "TaskDefinition");

            migrationBuilder.DropColumn(
                name: "MisfirePolicy",
                table: "TaskDefinition");

            migrationBuilder.DropColumn(
                name: "Paused",
                table: "TaskDefinition");

            migrationBuilder.DropColumn(
                name: "RequestsRecovery",
                table: "TaskDefinition");
        }
    }
}
