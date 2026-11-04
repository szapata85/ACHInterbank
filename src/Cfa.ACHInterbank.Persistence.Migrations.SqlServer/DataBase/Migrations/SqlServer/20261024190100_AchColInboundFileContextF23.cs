using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer;

[DbContext(typeof(AchDbContext))]
[Migration("20261024190100_AchColInboundFileContextF23")]
public partial class AchColInboundFileContextF23 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CanonicalPlainFileName", table: "IncomingNachaFileIngestions",
            type: "nvarchar(260)", maxLength: 260, nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "FileIdModifier", table: "IncomingNachaFileIngestions",
            type: "nvarchar(1)", maxLength: 1, nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "FileProtectionKind", table: "IncomingNachaFileIngestions",
            type: "nvarchar(30)", maxLength: 30, nullable: true);
        migrationBuilder.AddColumn<int>(
            name: "FileSequence", table: "IncomingNachaFileIngestions",
            type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(
            name: "ExternalCycleNumber", table: "IncomingNachaFileIngestions",
            type: "int", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CanonicalPlainFileName", table: "IncomingNachaFileIngestions");
        migrationBuilder.DropColumn(name: "FileIdModifier", table: "IncomingNachaFileIngestions");
        migrationBuilder.DropColumn(name: "FileProtectionKind", table: "IncomingNachaFileIngestions");
        migrationBuilder.DropColumn(name: "FileSequence", table: "IncomingNachaFileIngestions");
        migrationBuilder.DropColumn(name: "ExternalCycleNumber", table: "IncomingNachaFileIngestions");
    }
}
