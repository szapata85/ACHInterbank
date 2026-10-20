using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer;

public partial class OpsGap0022BManagedMftAdministration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "ProfileName", table: "AchManagedFileTransferConfigurations", type: "nvarchar(120)", maxLength: 120, nullable: false, defaultValue: "ACH Colombia Managed MFT");
        migrationBuilder.AddColumn<string>(name: "Provider", table: "AchManagedFileTransferConfigurations", type: "nvarchar(60)", maxLength: 60, nullable: false, defaultValue: "ManagedFolder");
        migrationBuilder.AddColumn<string>(name: "Protocol", table: "AchManagedFileTransferConfigurations", type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "ManagedFile");
        migrationBuilder.AddColumn<bool>(name: "ProfileEnabled", table: "AchManagedFileTransferConfigurations", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>(name: "Endpoint", table: "AchManagedFileTransferConfigurations", type: "nvarchar(300)", maxLength: 300, nullable: true);
        migrationBuilder.AddColumn<int>(name: "Port", table: "AchManagedFileTransferConfigurations", type: "int", nullable: true);
        migrationBuilder.AddColumn<string>(name: "Principal", table: "AchManagedFileTransferConfigurations", type: "nvarchar(160)", maxLength: 160, nullable: true);
        migrationBuilder.AddColumn<int>(name: "RetryDelaySeconds", table: "AchManagedFileTransferConfigurations", type: "int", nullable: false, defaultValue: 60);
        migrationBuilder.AddColumn<string>(name: "CredentialType", table: "AchManagedFileTransferConfigurations", type: "nvarchar(40)", maxLength: 40, nullable: true);
        migrationBuilder.AddColumn<string>(name: "ProtectedCredential", table: "AchManagedFileTransferConfigurations", type: "nvarchar(max)", maxLength: 8000, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "CredentialUpdatedAtUtc", table: "AchManagedFileTransferConfigurations", type: "datetime2", nullable: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder) { foreach (var name in new[] { "ProfileName", "Provider", "Protocol", "ProfileEnabled", "Endpoint", "Port", "Principal", "RetryDelaySeconds", "CredentialType", "ProtectedCredential", "CredentialUpdatedAtUtc" }) migrationBuilder.DropColumn(name, "AchManagedFileTransferConfigurations"); }
}
