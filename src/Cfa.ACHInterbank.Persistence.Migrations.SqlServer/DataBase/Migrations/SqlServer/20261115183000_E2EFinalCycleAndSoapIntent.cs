using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer;

/// <summary>AUD-E2E-03/04: retry-cycle trace and durable SOAP intent, without altering monetary source records.</summary>
[DbContext(typeof(AchDbContext))]
[Migration("20261115183000_E2EFinalCycleAndSoapIntent")]
public sealed class E2EFinalCycleAndSoapIntent : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "DispatchIntentCorrelationId", table: "ContrapartidaDispatchItems",
            type: "nvarchar(120)", maxLength: 120, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "DispatchIntentPayloadHash", table: "ContrapartidaDispatchItems",
            type: "nvarchar(64)", maxLength: 64, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "DispatchIntentMappingSnapshotHash", table: "ContrapartidaDispatchItems",
            type: "nvarchar(128)", maxLength: 128, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "DispatchIntentSoapEndpoint", table: "ContrapartidaDispatchItems",
            type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "DispatchIntentExecutionMode", table: "ContrapartidaDispatchItems",
            type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "DispatchIntentPayloadXml", table: "ContrapartidaDispatchItems",
            type: "nvarchar(max)", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<Guid>(name: "DispatchIntentMappingSetId", table: "ContrapartidaDispatchItems",
            type: "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<int>(name: "DispatchIntentMappingVersion", table: "ContrapartidaDispatchItems",
            type: "int", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "DispatchIntentAtUtc", table: "ContrapartidaDispatchItems",
            type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<string>(name: "LastReassignedFromCycleId", table: "ContrapartidaDispatchItems",
            type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<DateTime>(name: "LastReassignedAtUtc", table: "ContrapartidaDispatchItems",
            type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<string>(name: "DispatchCorrelationId", table: "ContrapartidaDispatchBatches",
            type: "nvarchar(120)", maxLength: 120, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "RequestPayloadHash", table: "ContrapartidaDispatchBatches",
            type: "nvarchar(64)", maxLength: 64, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<DateTime>(name: "DispatchIntentAtUtc", table: "ContrapartidaDispatchBatches",
            type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<string>(name: "DispatchSoapEndpoint", table: "ContrapartidaDispatchBatches",
            type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "DispatchExecutionMode", table: "ContrapartidaDispatchBatches",
            type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "DispatchIntentCorrelationId", table: "ContrapartidaDispatchItems");
        migrationBuilder.DropColumn(name: "DispatchIntentPayloadHash", table: "ContrapartidaDispatchItems");
        migrationBuilder.DropColumn(name: "DispatchIntentMappingSnapshotHash", table: "ContrapartidaDispatchItems");
        migrationBuilder.DropColumn(name: "DispatchIntentSoapEndpoint", table: "ContrapartidaDispatchItems");
        migrationBuilder.DropColumn(name: "DispatchIntentExecutionMode", table: "ContrapartidaDispatchItems");
        migrationBuilder.DropColumn(name: "DispatchIntentPayloadXml", table: "ContrapartidaDispatchItems");
        migrationBuilder.DropColumn(name: "DispatchIntentMappingSetId", table: "ContrapartidaDispatchItems");
        migrationBuilder.DropColumn(name: "DispatchIntentMappingVersion", table: "ContrapartidaDispatchItems");
        migrationBuilder.DropColumn(name: "DispatchIntentAtUtc", table: "ContrapartidaDispatchItems");
        migrationBuilder.DropColumn(name: "LastReassignedFromCycleId", table: "ContrapartidaDispatchItems");
        migrationBuilder.DropColumn(name: "LastReassignedAtUtc", table: "ContrapartidaDispatchItems");
        migrationBuilder.DropColumn(name: "DispatchCorrelationId", table: "ContrapartidaDispatchBatches");
        migrationBuilder.DropColumn(name: "RequestPayloadHash", table: "ContrapartidaDispatchBatches");
        migrationBuilder.DropColumn(name: "DispatchIntentAtUtc", table: "ContrapartidaDispatchBatches");
        migrationBuilder.DropColumn(name: "DispatchSoapEndpoint", table: "ContrapartidaDispatchBatches");
        migrationBuilder.DropColumn(name: "DispatchExecutionMode", table: "ContrapartidaDispatchBatches");
    }
}
