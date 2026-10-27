using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres;

[DbContext(typeof(AchDbContext))]
[Migration("20261024190000_AchColInboundFileContextF23")]
public partial class AchColInboundFileContextF23 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CanonicalPlainFileName", table: "IncomingNachaFileIngestions",
            type: "character varying(260)", maxLength: 260, nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "FileIdModifier", table: "IncomingNachaFileIngestions",
            type: "character varying(1)", maxLength: 1, nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "FileProtectionKind", table: "IncomingNachaFileIngestions",
            type: "character varying(30)", maxLength: 30, nullable: true);
        migrationBuilder.AddColumn<int>(
            name: "FileSequence", table: "IncomingNachaFileIngestions",
            type: "integer", nullable: true);
        migrationBuilder.AddColumn<int>(
            name: "ExternalCycleNumber", table: "IncomingNachaFileIngestions",
            type: "integer", nullable: true);
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
