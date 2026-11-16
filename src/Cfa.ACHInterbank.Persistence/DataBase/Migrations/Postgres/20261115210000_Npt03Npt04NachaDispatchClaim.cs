using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres;

[DbContext(typeof(AchDbContext))]
[Migration("20261115210000_Npt03Npt04NachaDispatchClaim")]
public sealed class Npt03Npt04NachaDispatchClaim : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("DispatchClaimToken", "IncomingNachaDispatchQueue", type: "character varying(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<DateTime>("DispatchLeaseExpiresAtUtc", "IncomingNachaDispatchQueue", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<DateTime>("TransportStartedAtUtc", "IncomingNachaDispatchQueue", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<bool>("RequiresReconciliation", "IncomingNachaDispatchQueue", type: "boolean", nullable: false, defaultValue: false);
        // Legacy in-flight rows are permanently withheld pending reconciliation.
        migrationBuilder.Sql("UPDATE \"IncomingNachaDispatchQueue\" SET \"QueueStatus\" = 7, \"RequiresReconciliation\" = TRUE, \"LastErrorCode\" = 'NPT_LEGACY_OUTCOME_UNKNOWN', \"NextAttemptAtUtc\" = NULL WHERE \"QueueStatus\" = 2");
        migrationBuilder.AddColumn<string>("ClaimToken", "AchResponseNotificationAttempts", type: "character varying(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<DateTime>("LeaseExpiresAtUtc", "AchResponseNotificationAttempts", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<DateTime>("TransportStartedAtUtc", "AchResponseNotificationAttempts", type: "timestamp with time zone", nullable: true);
        migrationBuilder.CreateIndex("IX_AxonNotification_ClaimRecovery", "AchResponseNotificationAttempts", new[] { "EstadoNotificacion", "LeaseExpiresAtUtc" });
        migrationBuilder.CreateIndex("IX_IncomingNachaDispatchQueue_ClaimRecovery", "IncomingNachaDispatchQueue", new[] { "QueueStatus", "DispatchLeaseExpiresAtUtc" });
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_AxonNotification_ClaimRecovery", "AchResponseNotificationAttempts");
        migrationBuilder.DropColumn("ClaimToken", "AchResponseNotificationAttempts");
        migrationBuilder.DropColumn("LeaseExpiresAtUtc", "AchResponseNotificationAttempts");
        migrationBuilder.DropColumn("TransportStartedAtUtc", "AchResponseNotificationAttempts");
        migrationBuilder.DropIndex("IX_IncomingNachaDispatchQueue_ClaimRecovery", "IncomingNachaDispatchQueue");
        migrationBuilder.DropColumn("DispatchClaimToken", "IncomingNachaDispatchQueue");
        migrationBuilder.DropColumn("DispatchLeaseExpiresAtUtc", "IncomingNachaDispatchQueue");
        migrationBuilder.DropColumn("TransportStartedAtUtc", "IncomingNachaDispatchQueue");
        migrationBuilder.DropColumn("RequiresReconciliation", "IncomingNachaDispatchQueue");
    }
}
