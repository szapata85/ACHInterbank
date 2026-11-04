using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres;

/// <summary>I05-CENIT: evidencia de un único artefacto local protegido; nunca tabla ACH.</summary>
[DbContext(typeof(AchDbContext))]
[Migration("20261103190000_CenitReturnArtifact2026")]
public partial class CenitReturnArtifact2026 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "ProtectedArtifactContent", table: "CenitReturnPreparations",
            type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ArtifactSha256", table: "CenitReturnPreparations",
            type: "character varying(64)", nullable: true, maxLength: 64);
        migrationBuilder.AddColumn<string>(name: "ArtifactFileName", table: "CenitReturnPreparations",
            type: "character varying(120)", nullable: true, maxLength: 120);
        migrationBuilder.AddColumn<string>(name: "ArtifactTraceNumber", table: "CenitReturnPreparations",
            type: "character varying(15)", nullable: true, maxLength: 15);
        migrationBuilder.AddColumn<string>(name: "ArtifactProfileCode", table: "CenitReturnPreparations",
            type: "character varying(100)", nullable: true, maxLength: 100);
        migrationBuilder.AddColumn<int?>(name: "ArtifactRecordCount", table: "CenitReturnPreparations",
            type: "integer", nullable: true);
        migrationBuilder.AddColumn<DateTime?>(name: "GeneratedAtUtc", table: "CenitReturnPreparations",
            type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<string>(name: "GeneratedByUserId", table: "CenitReturnPreparations",
            type: "character varying(128)", nullable: true, maxLength: 128);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "GeneratedByUserId", table: "CenitReturnPreparations");
        migrationBuilder.DropColumn(name: "GeneratedAtUtc", table: "CenitReturnPreparations");
        migrationBuilder.DropColumn(name: "ArtifactRecordCount", table: "CenitReturnPreparations");
        migrationBuilder.DropColumn(name: "ArtifactProfileCode", table: "CenitReturnPreparations");
        migrationBuilder.DropColumn(name: "ArtifactTraceNumber", table: "CenitReturnPreparations");
        migrationBuilder.DropColumn(name: "ArtifactFileName", table: "CenitReturnPreparations");
        migrationBuilder.DropColumn(name: "ArtifactSha256", table: "CenitReturnPreparations");
        migrationBuilder.DropColumn(name: "ProtectedArtifactContent", table: "CenitReturnPreparations");
    }
}
