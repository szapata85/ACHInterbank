using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Cfa.ACHInterbank.Persistence.DataBase;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer;

[DbContext(typeof(AchDbContext))]
[Migration("20260713031001_AddProcTransaccionesFunctionalSources")]
public partial class AddProcTransaccionesFunctionalSources : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "BatchNumber",
            table: "EntryDetails",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "PaymentRelatedInformation",
            table: "AddendaRecords",
            type: "nvarchar(80)",
            maxLength: 80,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "BatchNumber", table: "EntryDetails");
        migrationBuilder.DropColumn(name: "PaymentRelatedInformation", table: "AddendaRecords");
    }
}
