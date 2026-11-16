using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer;

[DbContext(typeof(AchDbContext))]
[Migration("20261115210000_Npt03Npt04NachaDispatchClaim")]
public sealed class Npt03Npt04NachaDispatchClaim : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("DispatchClaimToken", "IncomingNachaDispatchQueue", type: "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<DateTime>("DispatchLeaseExpiresAtUtc", "IncomingNachaDispatchQueue", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<DateTime>("TransportStartedAtUtc", "IncomingNachaDispatchQueue", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<bool>("RequiresReconciliation", "IncomingNachaDispatchQueue", type: "bit", nullable: false, defaultValue: false);
        // Legacy in-flight rows are permanently withheld pending reconciliation.
        migrationBuilder.Sql("UPDATE [IncomingNachaDispatchQueue] SET [QueueStatus] = 7, [RequiresReconciliation] = 1, [LastErrorCode] = N'NPT_LEGACY_OUTCOME_UNKNOWN', [NextAttemptAtUtc] = NULL WHERE [QueueStatus] = 2");
        migrationBuilder.AddColumn<string>("ClaimToken", "AchResponseNotificationAttempts", type: "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<DateTime>("LeaseExpiresAtUtc", "AchResponseNotificationAttempts", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<DateTime>("TransportStartedAtUtc", "AchResponseNotificationAttempts", type: "datetime2", nullable: true);
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
