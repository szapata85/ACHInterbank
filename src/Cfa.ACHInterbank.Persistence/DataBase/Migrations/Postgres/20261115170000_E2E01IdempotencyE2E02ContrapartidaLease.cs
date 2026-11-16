using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres;

/// <summary>E2E-01 unique external instruction; E2E-02 durable SOAP claim/recovery.</summary>
[DbContext(typeof(AchDbContext))]
[Migration("20261115170000_E2E01IdempotencyE2E02ContrapartidaLease")]
public partial class E2E01IdempotencyE2E02ContrapartidaLease : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Must fail atomically on historical duplicates. Never automatically delete or choose a winner.
        migrationBuilder.Sql("DO $$ BEGIN\n    IF EXISTS (SELECT 1 FROM \"AchTransactions\"\n        WHERE \"TransactionExternalId\" IS NOT NULL AND \"TransactionExternalId\" <> ''\n        GROUP BY \"SourceInstitutionId\", \"TransactionExternalId\" HAVING COUNT(*) > 1)\n    THEN RAISE EXCEPTION 'E2E01_HISTORICAL_DUPLICATES: concilie duplicados por entidad e ID externo antes de migrar';\n    END IF;\nEND $$;");
        migrationBuilder.AddColumn<string>(name: "RequestFingerprint", table: "AchTransactions",
            type: "character varying(64)", maxLength: 64, nullable: true);
        migrationBuilder.CreateIndex(
            name: "UX_AchTransactions_SourceInstitution_ExternalId", table: "AchTransactions",
            columns: new[] { "SourceInstitutionId", "TransactionExternalId" }, unique: true,
            filter: "\"TransactionExternalId\" IS NOT NULL AND \"TransactionExternalId\" <> ''");

        migrationBuilder.AddColumn<string>(name: "DispatchLeaseToken", table: "ContrapartidaDispatchItems",
            type: "character varying(120)", maxLength: 120, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "DispatchLeasePhase", table: "ContrapartidaDispatchItems",
            type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Idle");
        migrationBuilder.AddColumn<DateTime>(name: "DispatchLeaseExpiresAtUtc", table: "ContrapartidaDispatchItems",
            type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<bool>(name: "RequiresReconciliation", table: "ContrapartidaDispatchItems",
            type: "boolean", nullable: false, defaultValue: false);
        // Treat legacy in-progress debits as outcome-UNKNOWN, never assume they were not transmitted.
        migrationBuilder.Sql("UPDATE \"ContrapartidaDispatchItems\"\nSET \"DispatchLeasePhase\" = 'InFlight', \"DispatchLeaseExpiresAtUtc\" = (CURRENT_TIMESTAMP - INTERVAL '1 minute')\nWHERE \"State\" IN ('ReportingContrapartida', 'Retrying');");
        migrationBuilder.CreateIndex(
            name: "IX_ContrapartidaDispatchItems_State_DispatchLeaseExpiresAtUtc", table: "ContrapartidaDispatchItems",
            columns: new[] { "State", "DispatchLeaseExpiresAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Rollback must occur in a maintenance window, with affected operations reconciled.
        migrationBuilder.DropIndex(name: "IX_ContrapartidaDispatchItems_State_DispatchLeaseExpiresAtUtc", table: "ContrapartidaDispatchItems");
        migrationBuilder.DropColumn(name: "DispatchLeaseToken", table: "ContrapartidaDispatchItems");
        migrationBuilder.DropColumn(name: "DispatchLeasePhase", table: "ContrapartidaDispatchItems");
        migrationBuilder.DropColumn(name: "DispatchLeaseExpiresAtUtc", table: "ContrapartidaDispatchItems");
        migrationBuilder.DropColumn(name: "RequiresReconciliation", table: "ContrapartidaDispatchItems");
        migrationBuilder.DropIndex(name: "UX_AchTransactions_SourceInstitution_ExternalId", table: "AchTransactions");
        migrationBuilder.DropColumn(name: "RequestFingerprint", table: "AchTransactions");
    }
}
