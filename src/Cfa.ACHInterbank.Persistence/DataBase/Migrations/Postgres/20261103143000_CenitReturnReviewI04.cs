using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres;

/// <summary>I04-CENIT: campos de decisión humana, reversibles, sin tabla de ACH Colombia.</summary>
[DbContext(typeof(AchDbContext))]
[Migration("20261103143000_CenitReturnReviewI04")]
public partial class CenitReturnReviewI04 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "ReviewedByUserId", table: "CenitReturnPreparations",
            type: "character varying(128)", maxLength: 128, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "ReviewedAtUtc", table: "CenitReturnPreparations",
            type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<string>(name: "DecisionJustification", table: "CenitReturnPreparations",
            type: "character varying(500)", maxLength: 500, nullable: true);
        migrationBuilder.AddColumn<string>(name: "DecisionEvidenceReference", table: "CenitReturnPreparations",
            type: "character varying(120)", maxLength: 120, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ReviewedByUserId", table: "CenitReturnPreparations");
        migrationBuilder.DropColumn(name: "ReviewedAtUtc", table: "CenitReturnPreparations");
        migrationBuilder.DropColumn(name: "DecisionJustification", table: "CenitReturnPreparations");
        migrationBuilder.DropColumn(name: "DecisionEvidenceReference", table: "CenitReturnPreparations");
    }
}
